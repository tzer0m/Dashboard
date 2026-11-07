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

    updateHostPills();

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
 * Recomputes the online/offline/pending pills on each host's info cell from the badge state of that host's service cards, hiding any pill whose count is zero.
 */
function updateHostPills() {
    document.querySelectorAll('.host-pills').forEach(pills => {
        const host = pills.closest('[data-host]').dataset.host;
        const badges = document.querySelectorAll(`[data-host="${host}"] [data-service-name] .badge`);
        const counts = { online: 0, offline: 0, pending: 0 };

        badges.forEach(badge => {
            if (badge.classList.contains('bg-success')) counts.online++;
            else if (badge.classList.contains('bg-danger')) counts.offline++;
            else counts.pending++;
        });

        pills.querySelectorAll('[data-pill]').forEach(pill => {
            const count = counts[pill.dataset.pill];
            pill.textContent = count;
            pill.style.display = count > 0 ? '' : 'none';
        });
    });
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

/**
 * Fills in every deploy status pill on the page by reading the latest GitHub Actions result from this app's own /Badge endpoint, so the pill renders without relying on shields.io. Runs on load and then once a minute.
 */
async function refreshRepoBadges() {
    const pills = document.querySelectorAll('.repo-badge[data-repo]');
    for (const pill of pills) {
        try {
            const response = await fetch(`/Badge?repo=${encodeURIComponent(pill.dataset.repo)}`);
            if (!response.ok) continue;
            const badge = await response.json();
            pill.querySelector('.repo-badge-text').textContent = badge.message;
            pill.style.backgroundColor = `#${badge.color}`;
        } catch (err) {
            console.error(err);
        }
    }
}

document.addEventListener("DOMContentLoaded", refreshRepoBadges);
setInterval(refreshRepoBadges, 60000);

// How far, in pixels, each host's outline is pulled in from the column grid lines, which is what leaves a gap between neighbouring hosts side by side.
const OutlineInsetX = 2;

// How far, in pixels, the top of each host's outline sits below the row grid line.
const OutlineInsetTop = 2;

// How far, in pixels, the bottom of each host's outline sits above the row grid line.
const OutlineInsetBottom = 2;

// Corner radius, in pixels, for both the outer and inner corners of a host outline.
const OutlineRadius = 8;

// Each host's cells in the order the server rendered them (alphabetical), captured on first layout so the grid can be reordered from a stable starting point.
let HostGroups = null;

/**
 * Reorders the host groups in the grid so that every host stays one connected shape for the current column count. A host that wraps onto a second row only connects if its cells on the second row reach under its cells on the first; otherwise the hosts are reordered, keeping as close to name order as possible.
 *
 * @param {HTMLElement} grid - The flowing host grid.
 * @param {number} columns - The number of grid columns.
 */
function arrangeHostGroups(grid, columns) {
    if (!HostGroups) {
        HostGroups = [];
        Array.from(grid.children).forEach(cell => {
            const last = HostGroups[HostGroups.length - 1];
            if (last && last.host === cell.dataset.host) last.cells.push(cell);
            else HostGroups.push({ host: cell.dataset.host, cells: [cell] });
        });
    }

    // True when a group of the given size starting at the given position would be one connected shape.
    const fits = (position, size) => {
        const column = position % columns;
        if (column + size <= columns) return true;
        if (column + size > columns * 2) return true;
        return column + size - columns > column;
    };

    // Depth-first search for the earliest order (by name) in which every host connects, allowing up to the given number of empty spacer cells where nothing else fits. There are only a handful of hosts, so this is instant.
    const search = (remaining, position, spacers) => {
        if (remaining.length === 0) return [];
        for (let index = 0; index < remaining.length; index++) {
            const group = remaining[index];
            if (!fits(position, group.cells.length)) continue;
            const rest = search(remaining.filter((_, other) => other !== index), position + group.cells.length, spacers);
            if (rest) return [group, ...rest];
        }
        if (spacers > 0) {
            const rest = search(remaining, position + 1, spacers - 1);
            if (rest) return [null, ...rest];
        }
        return null;
    };

    // Try with no spacers first, then allow more until an order works.
    let ordered = null;
    for (let spacers = 0; spacers < columns && !ordered; spacers++) ordered = search(HostGroups, 0, spacers);
    ordered = ordered ?? HostGroups;

    // Only touch the DOM if the order actually changed.
    const wanted = ordered.flatMap(group => group ? group.cells : [null]);
    const current = Array.from(grid.children);
    if (wanted.length === current.length && wanted.every((cell, index) => cell ? current[index] === cell : current[index].classList.contains('flow-spacer'))) return;
    grid.querySelectorAll('.flow-spacer').forEach(spacer => spacer.remove());
    wanted.forEach(cell => {
        if (cell) {
            grid.appendChild(cell);
        } else {
            const spacer = document.createElement('div');
            spacer.className = 'flow-spacer';
            grid.appendChild(spacer);
        }
    });
}

/**
 * Works out which sides of each cell in the flowing host grid border a different host, then draws one filled, rounded outline per host behind the cards. A host whose cards wrap across rows is outlined as one snaking shape, with proper inner corners. Runs on load and whenever the grid changes size.
 */
function outlineHostGroups() {
    const grid = document.getElementById('host-flow');
    const svg = document.getElementById('host-outlines');
    if (!grid || !svg) return;

    const columns = getComputedStyle(grid).gridTemplateColumns.split(' ').length;
    arrangeHostGroups(grid, columns);
    const cells = Array.from(grid.children);

    // True when the cell at the given index exists and belongs to the given host.
    const isMember = (host, index) => index >= 0 && index < cells.length && cells[index].dataset.host === host;

    // Which sides of each cell border a different host or the edge of the grid.
    const sides = cells.map((cell, index) => {
        const host = cell.dataset.host;
        const column = index % columns;
        return {
            top: !isMember(host, index - columns),
            bottom: !isMember(host, index + columns),
            left: column === 0 || !isMember(host, index - 1),
            right: column === columns - 1 || !isMember(host, index + 1)
        };
    });

    // Pixel positions of the grid lines, read from the laid-out tracks.
    const style = getComputedStyle(grid);
    const xs = [0];
    style.gridTemplateColumns.split(' ').forEach(width => xs.push(xs[xs.length - 1] + parseFloat(width)));
    const ys = [0];
    style.gridTemplateRows.split(' ').forEach(height => ys.push(ys[ys.length - 1] + parseFloat(height)));

    svg.setAttribute('viewBox', `0 0 ${grid.offsetWidth} ${grid.offsetHeight}`);
    svg.replaceChildren();

    new Set(cells.map(cell => cell.dataset.host).filter(host => host)).forEach(host => {
        const pathData = buildHostOutline(host, cells, columns, sides, xs, ys);
        if (!pathData) return;
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute('d', pathData);
        path.setAttribute('class', 'host-outline');
        svg.appendChild(path);
    });
}

/**
 * Builds the SVG path for one host's outline: traces the boundary of its cells around the grid lines, pulls it in by the inset, and rounds every corner (inner corners included).
 *
 * @param {string} host - The host whose cells are outlined.
 * @param {HTMLElement[]} cells - Every cell in the grid, in order.
 * @param {number} columns - The number of grid columns.
 * @param {Object[]} sides - Per cell, which sides border a different host.
 * @param {number[]} xs - Pixel positions of the column grid lines.
 * @param {number[]} ys - Pixel positions of the row grid lines.
 * @returns {string} The path data, or an empty string if the host has no cells.
 */
function buildHostOutline(host, cells, columns, sides, xs, ys) {
    // Directed boundary edges between grid line intersections, travelling clockwise so the host's cells are always on the right.
    const edges = [];
    cells.forEach((cell, index) => {
        if (cell.dataset.host !== host) return;
        const column = index % columns;
        const row = Math.floor(index / columns);
        if (sides[index].top) edges.push({ from: [column, row], to: [column + 1, row], used: false });
        if (sides[index].right) edges.push({ from: [column + 1, row], to: [column + 1, row + 1], used: false });
        if (sides[index].bottom) edges.push({ from: [column + 1, row + 1], to: [column, row + 1], used: false });
        if (sides[index].left) edges.push({ from: [column, row + 1], to: [column, row], used: false });
    });
    if (edges.length === 0) return '';

    let pathData = '';
    edges.forEach(startEdge => {
        if (startEdge.used) return;

        // Follow connected edges until the loop closes.
        const loop = [];
        let edge = startEdge;
        while (edge && !edge.used) {
            edge.used = true;
            loop.push(edge.from);
            edge = edges.find(candidate => !candidate.used && candidate.from[0] === edge.to[0] && candidate.from[1] === edge.to[1]);
        }

        // Drop points that sit in the middle of a straight run.
        const corners = loop.filter((point, index) => {
            const before = loop[(index - 1 + loop.length) % loop.length];
            const after = loop[(index + 1) % loop.length];
            return (point[0] - before[0]) * (after[1] - point[1]) - (point[1] - before[1]) * (after[0] - point[0]) !== 0;
        });
        if (corners.length < 4) return;

        // Pull each corner in along the inward normals of its two edges.
        const direction = (from, to) => { const dx = Math.sign(to[0] - from[0]); const dy = Math.sign(to[1] - from[1]); return [dx, dy]; };
        const inset = corners.map((point, index) => {
            const before = corners[(index - 1 + corners.length) % corners.length];
            const after = corners[(index + 1) % corners.length];
            const into = direction(before, point);
            const out = direction(point, after);
            return [xs[point[0]] + OutlineInsetX * (-into[1] - out[1]), ys[point[1]] + (into[0] + out[0] > 0 ? OutlineInsetTop : OutlineInsetBottom) * (into[0] + out[0])];
        });

        // Round every corner with an arc, clamping the radius so short edges still work.
        const length = (a, b) => Math.abs(b[0] - a[0]) + Math.abs(b[1] - a[1]);
        const unit = (a, b) => { const l = length(a, b); return [(b[0] - a[0]) / l, (b[1] - a[1]) / l]; };
        let first = true;
        let loopPath = '';
        inset.forEach((point, index) => {
            const before = inset[(index - 1 + inset.length) % inset.length];
            const after = inset[(index + 1) % inset.length];
            const radius = Math.min(OutlineRadius, length(before, point) / 2, length(point, after) / 2);
            const into = unit(before, point);
            const out = unit(point, after);
            const start = [point[0] - into[0] * radius, point[1] - into[1] * radius];
            const end = [point[0] + out[0] * radius, point[1] + out[1] * radius];
            const turnsRight = into[0] * out[1] - into[1] * out[0] > 0;
            loopPath += first ? `M${start[0]},${start[1]}` : `L${start[0]},${start[1]}`;
            loopPath += `A${radius},${radius} 0 0 ${turnsRight ? 1 : 0} ${end[0]},${end[1]}`;
            first = false;
        });
        pathData += `${loopPath}Z`;
    });
    return pathData;
}

document.addEventListener("DOMContentLoaded", () => {
    outlineHostGroups();
    const grid = document.getElementById('host-flow');
    if (grid) new ResizeObserver(outlineHostGroups).observe(grid);
});
window.addEventListener('load', outlineHostGroups);
