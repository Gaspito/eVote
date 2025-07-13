/*
    This script gives very simple modal functionalities to the UI.
*/

// The modal currently open
window.currentModal = null;

/**
 * Creates a new modal, with optional size values (in css notation)
 * Use the close() method of the modal to remove it from view safely.
 * Note: multiple modals cannot exist at the same time. The new one
 * will automatically close the older.
 * @param {String[]} size Optional size values of the modal
 * @returns the new modal element
 */
function createModal(size=["300px", "300px"]) {
    window.currentModal?.close();

    const container = document.createElement("div");
    container.classList.add("modal-bg");
    const modal = document.createElement("div");
    modal.id = "modal";
    modal.classList.add("modal");
    modal.style.setProperty("width", size[0]);
    modal.style.setProperty("height", size[1]);
    modal.close = function (arg=null) {
        this.parentElement.remove();
        window.currentModal = null;
    };
    container.append(modal);

    document.body.append(container);

    window.currentModal = modal;

    return modal;
}

/**
 * Creates a new modal with an error message template, and a button to close it.
 * @param {String} title Title to give the modal
 * @param {String} message Error message to show
 * @returns the new modal
 */
function createErrorModal(title="Error", message="Something went wrong") {
    return new Promise((resolve) => {
        const modal = createModal(["min(80vw, 500px)", "fit-content"]);
        const prevClose = modal.close;
        modal.close = function() {
            resolve();
            prevClose.call(this);
        };
        modal.innerHTML = `
        <h1 class="error-title">${title}</h1>
        <p>${message}</p>
        <div style="display: grid; grid-template-columns: 1fr; gap: 10px">
            <button class="btn btn-secondary" onclick="window.currentModal?.close()">Close</button>
        </div>
        `;
    });
}

/**
 * Returns an awaitable promise that resolves with True if the user clicks yes, and false
 * if the modal closes otherwise (clicking No or closing otherwise).
 * The modal is tailored for a simple confirmation dialog, with Yes and No messages.
 * @param {String} title Title to give the modal
 * @param {String} message Confirmation message to display
 * @returns the new modal
 */
function createConfirmModal(title="Confirm", message="You need to confirm something") {
    return new Promise((resolve) => {
        const modal = createModal(["300px", "fit-content"]);
        const prevClose = modal.close;
        modal.close = function(value=false) {
            resolve(value);
            prevClose.call(this);
        };
        modal.innerHTML = `
        <h1>${title}</h1>
        <p>${message}</p>
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px">
            <button class="btn btn-primary" onclick="window.currentModal?.close(true)">Yes</button>
            <button class="btn btn-danger" onclick="window.currentModal?.close(false)">No</button>
        </div>
        `;
    });
}
