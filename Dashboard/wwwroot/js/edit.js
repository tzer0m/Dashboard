// The editor form, its host list, and whether anything has changed since the page loaded.
const editForm = document.getElementById('edit-form');
const editHosts = document.getElementById('edit-hosts');
let editDirty = false;

/**
 * Returns a fresh copy of the markup inside the given template.
 *
 * @param {string} id - The template's element ID.
 * @returns {HTMLElement} The copied element.
 */
function cloneTemplate(id) {
    return document.getElementById(id).content.firstElementChild.cloneNode(true);
}

/**
 * Updates the service count shown in a host's summary line.
 *
 * @param {HTMLElement} hostEl - The host's details element.
 */
function updateServiceCount(hostEl) {
    const count = hostEl.querySelectorAll('[data-service]').length;
    hostEl.querySelector('[data-service-count]').textContent = `${count} ${count === 1 ? 'Service' : 'Services'}`;
}

// Handles every add and delete button through one listener, since rows are added after the page loads.
editForm.addEventListener('click', event => {
    const button = event.target.closest('[data-action]');
    if (!button) return;
    const hostEl = button.closest('[data-host]');

    if (button.dataset.action === 'add-host') {
        const host = cloneTemplate('host-template');
        editHosts.appendChild(host);
        host.querySelector('[data-host-field="Host"]').focus();
    } else if (button.dataset.action === 'remove-host') {
        const count = hostEl.querySelectorAll('[data-service]').length;
        if (count > 0 && !confirm(`Delete this host and its ${count} ${count === 1 ? 'service' : 'services'}?`)) return;
        hostEl.remove();
    } else if (button.dataset.action === 'add-service') {
        const row = cloneTemplate('service-template');
        hostEl.querySelector('[data-services]').appendChild(row);
        row.querySelector('[data-field="Name"]').focus();
        updateServiceCount(hostEl);
    } else if (button.dataset.action === 'remove-service') {
        button.closest('[data-service]').remove();
        updateServiceCount(hostEl);
    }
    editDirty = true;
});

// Marks the form as changed, and keeps a host's summary title in step with its Name field.
editForm.addEventListener('input', event => {
    editDirty = true;
    if (event.target.dataset.hostField === 'Host') event.target.closest('[data-host]').querySelector('.edit-host-title').textContent = event.target.value || 'New Host';
});

// Renumbers every field's name before posting, so the list has no gaps after rows were added or deleted.
editForm.addEventListener('submit', () => {
    editHosts.querySelectorAll('[data-host]').forEach((hostEl, hostIndex) => {
        hostEl.querySelectorAll('[data-host-field]').forEach(input => input.name = `Hosts[${hostIndex}].${input.dataset.hostField}`);
        hostEl.querySelectorAll('[data-service]').forEach((row, serviceIndex) => {
            row.querySelectorAll('[data-field]').forEach(input => input.name = `Hosts[${hostIndex}].Services[${serviceIndex}].${input.dataset.field}`);
        });
    });
    editDirty = false;
});

// Warns before leaving the page with unsaved changes.
window.addEventListener('beforeunload', event => {
    if (editDirty) event.preventDefault();
});
