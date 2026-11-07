// Updates the UTC and local clocks in the new-tab bar every second. Purely client-side.
function updateClocks() {
    const now = new Date();
    const utcEl = document.getElementById('utc-clock');
    const localEl = document.getElementById('local-clock');
    if (utcEl) utcEl.textContent = `${now.toISOString().substring(11, 19)} UTC`;
    if (localEl) localEl.textContent = now.toLocaleTimeString('en-GB', { hour12: false, timeZoneName: 'short' });
}

setInterval(updateClocks, 1000);
updateClocks();

/**
 * Records a status push on the matching monitor's dot, then recomputes the summary pills.
 *
 * @param {Object} status - The status payload broadcast from the server.
 * @param {string} status.name - The monitor name, used to find the matching dot.
 * @param {boolean} status.isOnline - Whether the monitor responded successfully.
 * @param {string|null} status.error - Error message if the monitor is down.
 */
function handleStatusUpdate(status) {
    const dot = document.querySelector(`.newtab-dot[data-monitor="${status.name}"]`);
    if (dot) {
        const hasExtras = dot.parentElement.children.length > 1;
        const error = status.isOnline ? null : (status.error ?? null);
        dot.dataset.state = status.isOnline ? 'online' : 'offline';
        dot.title = hasExtras ? (error ? `${status.name}: ${error}` : status.name) : (error ?? '');
    }
    recomputeStatusSummary();
}

/**
 * Recomputes the overall status pill and the online/offline/pending counts from the state of every monitor's dot.
 */
function recomputeStatusSummary() {
    const counts = { online: 0, offline: 0, pending: 0 };
    document.querySelectorAll('.newtab-dot[data-monitor]').forEach(dot => counts[dot.dataset.state]++);

    ['online', 'offline', 'pending'].forEach(state => {
        const countEl = document.getElementById(`${state}-count`);
        const cardEl = document.getElementById(`${state}-count-card`);
        if (countEl) countEl.textContent = counts[state];
        if (cardEl) cardEl.style.display = counts[state] > 0 ? '' : 'none';
    });

    const overallCard = document.getElementById('overall-status-card');
    const overallText = document.getElementById('overall-status-text');
    if (!overallCard || !overallText) return;
    overallCard.classList.remove('bg-success', 'bg-danger');
    overallCard.classList.add(counts.offline === 0 ? 'bg-success' : 'bg-danger');
    overallText.textContent = counts.offline === 0 ? 'All Online' : `${counts.offline} Offline`;
}

// Connects to the status hub once the DOM is ready, reconnecting automatically after a dropped connection.
document.addEventListener('DOMContentLoaded', () => {
    const connection = new signalR.HubConnectionBuilder().withUrl('/serviceStatusHub').withAutomaticReconnect().build();
    connection.on('ServiceStatusUpdated', handleStatusUpdate);
    connection.start().catch(err => console.error('SignalR connection failed:', err));
});
