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
 * Records a status push on the matching service link, then recomputes the summary pills.
 *
 * @param {Object} status - The status payload broadcast from the server.
 * @param {string} status.name - The service name, used to find the matching link.
 * @param {boolean} status.isOnline - Whether the service responded successfully.
 */
function handleStatusUpdate(status) {
    const link = document.querySelector(`[data-service-name="${status.name}"]`);
    if (link) {
        link.dataset.state = status.isOnline ? 'online' : 'offline';
        link.querySelector('.newtab-dot').title = status.isOnline ? '' : (status.error ?? '');
    }
    recomputeStatusSummary();
}

/**
 * Recomputes the overall status pill and the online/offline/pending counts from the state of every service link.
 */
function recomputeStatusSummary() {
    const counts = { online: 0, offline: 0, pending: 0 };
    document.querySelectorAll('[data-service-name]').forEach(link => counts[link.dataset.state]++);

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
