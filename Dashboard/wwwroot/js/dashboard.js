// Updates the UTC clock display in the dashboard header, ticking every second. Purely client-side — no network requests involved.
function updateUtcClock() {
    const clockEl = document.getElementById('utc-clock');
    if (!clockEl) return;

    const now = new Date();
    clockEl.textContent = `${now.toISOString().substring(11, 19)} UTC`;
}

// Uses the browser's local timezone, purely client-side.
function updateLocalClock() {
    const clockEl = document.getElementById('local-clock');
    if (!clockEl) return;

    const now = new Date();
    const parts = now.toLocaleTimeString('en-GB', { hour12: false, timeZoneName: 'short' });
    clockEl.textContent = parts;
}

// Updates both clocks every second.
setInterval(() => {
    updateUtcClock();
    updateLocalClock();
}, 1000);

updateUtcClock();
updateLocalClock();

// Active SignalR connection to the ServiceStatusHub. Initialised on page load, used to receive live status pushes and to invoke manual refresh requests.
let connection = null;

// Establishes the SignalR connection to the dashboard's status hub. Automatically reconnects if the connection drops (e.g. brief network blip), and registers the handler for incoming status updates.
function initDashboardConnection() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/serviceStatusHub")
        .withAutomaticReconnect()
        .build();

    connection.on("ServiceStatusUpdated", handleStatusUpdate);
    connection.on("UptimeUpdated", handleUptimeUpdate);

    connection.start().catch(err => console.error("SignalR connection failed:", err));
}

/**
 * Updates a single service card in place when a status push arrives, either from the normal 60-second health check cycle or a manual refresh.
 * Matches the card by its data-service-name attribute, and patches every piece of status info shown on the card: badge, tooltip, response time, status code, and last-checked timestamp.
 *
 * @param {Object} status - The status payload broadcast from the server.
 * @param {string} status.name - The service name, used to find the matching card.
 * @param {boolean} status.isOnline - Whether the service responded successfully.
 * @param {number|null} status.statusCode - The HTTP status code returned, if any.
 * @param {number} status.responseTimeMs - Response time in milliseconds.
 * @param {string} status.lastChecked - ISO timestamp of the check, in UTC.
 * @param {string|null} status.error - Error message if the service was unreachable.
 */
function handleStatusUpdate(status) {
    const card = document.querySelector(`[data-service-name="${status.name}"]`);
    if (!card) return;

    const isOnline = status.isOnline;
    const isAuthRequired = isOnline && status.statusCode === 401;

    // Badge state and tooltip
    const badge = card.querySelector('.badge');
    badge.classList.remove('bg-success', 'bg-danger', 'bg-secondary');
    badge.classList.add(isOnline ? 'bg-success' : 'bg-danger');
    badge.textContent = isAuthRequired ? 'AuthRequired' : (isOnline ? 'Online' : 'Offline');
    badge.title = isOnline ? '' : (status.error ?? '');

    // Response time, colour-coded the same way the server-rendered version is
    const responseTimeEl = card.querySelector('.response-time');
    if (responseTimeEl) {
        responseTimeEl.textContent = `${status.responseTimeMs} ms`;
        responseTimeEl.classList.remove('response-time-fast', 'response-time-moderate', 'response-time-slow');
        if (status.responseTimeMs <= 200) responseTimeEl.classList.add('response-time-fast');
        else if (status.responseTimeMs <= 500) responseTimeEl.classList.add('response-time-moderate');
        else responseTimeEl.classList.add('response-time-slow');
    }

    // Status code — shows the HTTP status if present, or a placeholder otherwise
    const statusCodeEl = card.querySelector('.status-code');
    if (statusCodeEl) {
        statusCodeEl.textContent = status.statusCode != null ? `HTTP ${status.statusCode}` : '—';
    }

    // Last-checked timestamp, formatted the same way as the server-rendered HH:mm:ss
    const lastCheckedEl = card.querySelector('.last-checked');
    if (lastCheckedEl) {
        const checkedDate = new Date(status.lastChecked);
        lastCheckedEl.textContent = `${checkedDate.toISOString().substring(11, 19)} UTC`;
    }

    // Re-enable the refresh button now that a result has come back, whether it was this button's own request or the regular 60s cycle.
    const btn = card.querySelector('.refresh-btn');
    btn.classList.remove('checking');
    btn.disabled = false;
}

/**
 * Updates a single service card's uptime percentage and 30-day bar strip in place
 * when a daily uptime recompute is broadcast from the server.
 *
 * @param {Object} uptime - The uptime summary payload broadcast from the server.
 * @param {string} uptime.name - The service name, used to find the matching card.
 * @param {number|null} uptime.uptimePercent - The 30-day uptime percentage, or null if no data.
 * @param {Array<{date: string, status: string}>} uptime.days - The 30 daily bar statuses, oldest first.
 */
function handleUptimeUpdate(uptime) {
    const card = document.querySelector(`[data-service-name="${uptime.name}"]`);
    if (!card) return;

    const percentEl = card.querySelector('.uptime-percent');
    if (percentEl) {
        percentEl.classList.remove('uptime-good', 'uptime-warn', 'uptime-bad', 'text-secondary');
        if (uptime.uptimePercent == null) {
            percentEl.textContent = '—';
            percentEl.classList.add('text-secondary');
        } else {
            percentEl.textContent = `${uptime.uptimePercent.toFixed(1)}%`;
            if (uptime.uptimePercent >= 99) percentEl.classList.add('uptime-good');
            else if (uptime.uptimePercent >= 95) percentEl.classList.add('uptime-warn');
            else percentEl.classList.add('uptime-bad');
        }
    }

    const stripEl = card.querySelector('.uptime-strip');
    if (stripEl) {
        stripEl.innerHTML = '';
        uptime.days.forEach(day => {
            const bar = document.createElement('span');
            let barClass = 'uptime-bar-up';
            if (day.status === 'Down') barClass = 'uptime-bar-down';
            else if (day.status === 'Unknown') barClass = 'uptime-bar-unknown';
            bar.className = `uptime-bar ${barClass}`;
            bar.title = new Date(day.date).toLocaleDateString('en-GB', { day: '2-digit', month: 'short' });
            stripEl.appendChild(bar);
        });
    }
}

/**
 * Triggers an immediate, out-of-band health check for a single service, bypassing the normal 60-second cycle. Called from the refresh button on each service card.
 *
 * @param {string} serviceName - The name of the service to refresh.
 * @param {HTMLElement} btnEl - The button element that was clicked, used to show a loading state.
 */
function refreshService(serviceName, btnEl) {
    btnEl.classList.add('checking');
    btnEl.disabled = true;
    connection.invoke("RequestRefresh", serviceName).catch(err => {
        console.error(err);
        btnEl.classList.remove('checking');
        btnEl.disabled = false;
    });
}

// Establish the connection once the DOM is ready, so querySelector calls in handleStatusUpdate can reliably find the service cards.
document.addEventListener("DOMContentLoaded", initDashboardConnection);