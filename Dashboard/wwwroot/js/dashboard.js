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

    connection.start().catch(err => console.error("SignalR connection failed:", err));
}

/**
 * Updates a single service card in place when a status push arrives, either from the normal polling cycle or a manual refresh.
 * Matches the card by its data-service-name attribute, and patches the status info shown on the card: badge, tooltip, and response time.
 * Also bumps the page-level "last updated" timestamp, since every update now comes from a full refresh of all services together.
 *
 * @param {Object} status - The status payload broadcast from the server.
 * @param {string} status.name - The service name, used to find the matching card.
 * @param {boolean} status.isOnline - Whether the service responded successfully.
 * @param {number} status.responseTimeMs - Response time in milliseconds.
 * @param {string} status.lastChecked - ISO timestamp of the check, in UTC.
 * @param {string|null} status.error - Error message if the service was unreachable.
 */
function handleStatusUpdate(status) {
    updateLastUpdatedDisplay(status.lastChecked);

    const card = document.querySelector(`[data-service-name="${status.name}"]`);
    if (!card) return;

    const isOnline = status.isOnline;

    // Badge state and tooltip
    const badge = card.querySelector('.badge');
    badge.classList.remove('bg-success', 'bg-danger', 'bg-secondary');
    badge.classList.add(isOnline ? 'bg-success' : 'bg-danger');
    badge.textContent = isOnline ? '✓' : '✕';
    badge.title = isOnline ? '' : (status.error ?? '');

    // Response time, colour-coded the same way the server-rendered version is. Hidden when the service is offline, since a response time from a failed check isn't meaningful.
    const responseTimeEl = card.querySelector('.response-time');
    if (responseTimeEl) {
        responseTimeEl.classList.remove('response-time-fast', 'response-time-moderate', 'response-time-slow');
        if (isOnline) {
            responseTimeEl.textContent = `${status.responseTimeMs} ms`;
            if (status.responseTimeMs <= 200) responseTimeEl.classList.add('response-time-fast');
            else if (status.responseTimeMs <= 500) responseTimeEl.classList.add('response-time-moderate');
            else responseTimeEl.classList.add('response-time-slow');
        } else {
            responseTimeEl.textContent = '—';
        }
    }

    recomputeStatusSummary();
}

/**
 * Updates the page-level "last updated" timestamp shown next to the global refresh button.
 *
 * @param {string} isoTimestamp - ISO timestamp of the check that triggered this update, in UTC.
 */
function updateLastUpdatedDisplay(isoTimestamp) {
    const lastUpdatedEl = document.getElementById('last-updated');
    if (!lastUpdatedEl) return;

    const checkedDate = new Date(isoTimestamp);
    lastUpdatedEl.textContent = `${checkedDate.toISOString().substring(11, 19)} UTC`;
}

/**
 * Recomputes the top-right status summary (overall card plus online/offline/pending counts)
 * by reading the current badge state off every service card on the page. Called whenever a
 * status push arrives, so the summary stays in sync without a full page reload.
 */
function recomputeStatusSummary() {
    const badges = document.querySelectorAll('[data-service-name] .badge');
    let online = 0;
    let offline = 0;
    let pending = 0;

    badges.forEach(badge => {
        if (badge.classList.contains('bg-success')) online++;
        else if (badge.classList.contains('bg-danger')) offline++;
        else pending++;
    });

    const onlineEl = document.getElementById('online-count');
    const offlineEl = document.getElementById('offline-count');
    const pendingEl = document.getElementById('pending-count');
    if (onlineEl) onlineEl.textContent = online;
    if (offlineEl) offlineEl.textContent = offline;
    if (pendingEl) pendingEl.textContent = pending;

    const onlineCard = document.getElementById('online-count-card');
    const offlineCard = document.getElementById('offline-count-card');
    const pendingCard = document.getElementById('pending-count-card');
    if (onlineCard) onlineCard.style.display = online > 0 ? '' : 'none';
    if (offlineCard) offlineCard.style.display = offline > 0 ? '' : 'none';
    if (pendingCard) pendingCard.style.display = pending > 0 ? '' : 'none';

    const overallCard = document.getElementById('overall-status-card');
    const overallText = document.getElementById('overall-status-text');
    if (!overallCard || !overallText) return;

    overallCard.classList.remove('bg-success', 'bg-danger');
    if (offline === 0) {
        overallCard.classList.add('bg-success');
        overallText.textContent = 'All Online';
    } else {
        overallCard.classList.add('bg-danger');
        overallText.textContent = `${offline} Offline`;
    }
}

/**
 * Triggers an immediate refresh of every service's status from Kuma, bypassing the normal polling interval. Called from the single global refresh button in the page header.
 */
async function refreshAll() {
    const btn = document.getElementById('global-refresh-btn');
    if (!btn) return;

    btn.classList.add('checking');
    btn.disabled = true;
    try {
        // Resolves once the server has finished re-fetching from Kuma and broadcasting every update, so it's safe to stop spinning here rather than tracking individual card updates.
        await connection.invoke("RequestRefresh");
    } catch (err) {
        console.error(err);
    } finally {
        btn.classList.remove('checking');
        btn.disabled = false;
    }
}

// Establish the connection once the DOM is ready, so querySelector calls in handleStatusUpdate can reliably find the service cards.
document.addEventListener("DOMContentLoaded", initDashboardConnection);