// Robust delete modal handler - null-safe and focus-friendly
document.addEventListener('DOMContentLoaded', function () {
    const deleteModalEl = document.getElementById('deleteModal');
    if (!deleteModalEl) return console.warn('deleteModal element not found.');

    // Use scoped queries inside the modal to avoid nulls from global lookups
    const deleteForm = deleteModalEl.querySelector('#deleteForm') || document.getElementById('deleteForm');
    const deleteSubmitBtn = deleteModalEl.querySelector('#deleteSubmitBtn') || document.getElementById('deleteSubmitBtn');

    const recordIdInput = deleteModalEl.querySelector('#deleteRecordId');
    const recordNameEl = deleteModalEl.querySelector('#deleteRecordName');
    const itemTypeEl = deleteModalEl.querySelector('#deleteItemType');
    const buttonTextEl = deleteModalEl.querySelector('#deleteButtonText');

    // Single reusable instance
    let modalInstance = bootstrap.Modal.getOrCreateInstance(deleteModalEl);
    let lastTrigger = null;

    function clearBackdrops() {
        document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
    }

    function resetModalState() {
        if (deleteSubmitBtn) {
            deleteSubmitBtn.disabled = false;
            // safe: only set innerHTML if button exists
            const btnText = (buttonTextEl && buttonTextEl.textContent) ? buttonTextEl.textContent.trim() : 'Record';
            deleteSubmitBtn.innerHTML = `<i class="fas fa-trash me-1"></i>Delete ${btnText}`;
        }

        if (recordIdInput) recordIdInput.value = '';
        if (recordNameEl) recordNameEl.textContent = '';
        if (itemTypeEl) itemTypeEl.textContent = 'the record';
        if (buttonTextEl) buttonTextEl.textContent = 'Record';
        if (deleteForm) deleteForm.action = '';
    }

    function updateModalContent({ id, name, action, itemType, buttonText }) {
        if (recordIdInput) recordIdInput.value = id || '';
        if (recordNameEl) recordNameEl.textContent = name || '';
        if (itemTypeEl) itemTypeEl.textContent = itemType || 'the record';
        if (buttonTextEl) buttonTextEl.textContent = buttonText || 'Record';
        if (deleteForm) deleteForm.action = action || '';
    }

    function handleDeleteClick(button) {
        lastTrigger = button;
        const recordId = button.dataset.recordId;
        const recordName = button.dataset.recordName || 'this item';
        const deleteAction = button.dataset.deleteAction;
        const itemType = button.dataset.itemType || 'the record';
        const buttonText = button.dataset.buttonText || 'Record';

        if (!recordId || !deleteAction) {
            console.error('Delete button must have data-record-id and data-delete-action attributes');
            return;
        }

        updateModalContent({ id: recordId, name: recordName, action: deleteAction, itemType, buttonText });

        // If modal element already has "show" class it may be stuck — hide & re-show safely
        if (deleteModalEl.classList.contains('show')) {
            try {
                modalInstance.hide();
            } catch (err) {
                // If hide fails, dispose and recreate
                try { modalInstance.dispose(); } catch (e) { /* ignore */ }
                modalInstance = bootstrap.Modal.getOrCreateInstance(deleteModalEl);
            }
            setTimeout(() => {
                clearBackdrops();
                modalInstance.show();
            }, 50);
            return;
        }

        // Normal show
        clearBackdrops();
        modalInstance.show();
    }

    // Delegated click handling so it works after partial updates
    document.addEventListener('click', function (e) {
        const deleteButton = e.target.closest('.delete-btn');
        if (deleteButton) {
            e.preventDefault();
            handleDeleteClick(deleteButton);
        }
    });

    // Submit button loading state
    if (deleteForm && deleteSubmitBtn) {
        deleteForm.addEventListener('submit', function () {
            deleteSubmitBtn.disabled = true;
            deleteSubmitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Deleting...';
        });
    }

    // When modal fully hidden: reset state, remove stray backdrops, and restore focus
    deleteModalEl.addEventListener('hidden.bs.modal', function () {
        // Move focus back to the button that opened the modal to avoid aria-hidden focus problem
        setTimeout(() => {
            if (lastTrigger && typeof lastTrigger.focus === 'function') {
                try { lastTrigger.focus(); } catch (e) { document.body.focus(); }
            } else {
                document.body.focus();
            }
        }, 0);

        resetModalState();
        clearBackdrops();
        lastTrigger = null;
    });

    // When shown, focus a sensible element
    deleteModalEl.addEventListener('shown.bs.modal', function () {
        const cancelBtn = deleteModalEl.querySelector('.btn-cancel, .btn-close');
        (cancelBtn || deleteSubmitBtn)?.focus();
    });
});











//// wwwroot/js/delete-modal.js
//// Generic Delete Modal Handler
//document.addEventListener('DOMContentLoaded', function () {
//    const deleteModal = document.getElementById('deleteModal');
//    const deleteForm = document.getElementById('deleteForm');
//    const deleteSubmitBtn = document.getElementById('deleteSubmitBtn');

//    // Function to handle delete button click
//    function handleDeleteClick(button) {
//        const recordId = button.dataset.recordId;
//        const recordName = button.dataset.recordName || 'this item';
//        const deleteAction = button.dataset.deleteAction;
//        const itemType = button.dataset.itemType || 'the record';
//        const buttonText = button.dataset.buttonText || 'Record';

//        // Validate required data
//        if (!recordId || !deleteAction) {
//            console.error('Delete button must have data-record-id and data-delete-action attributes');
//            return;
//        }

//        // Update modal content
//        document.getElementById('deleteRecordId').value = recordId;
//        document.getElementById('deleteRecordName').textContent = recordName;
//        document.getElementById('deleteItemType').textContent = itemType;
//        document.getElementById('deleteButtonText').textContent = buttonText;
//        document.getElementById('deleteForm').action = deleteAction;

//        // Show modal
//        const modal = new bootstrap.Modal(deleteModal);
//        modal.show();
//    }

//    // Use event delegation on document (more reliable approach)
//    document.addEventListener('click', function (e) {
//        // Check if clicked element or its parent has delete-btn class
//        const deleteButton = e.target.closest('.delete-btn');
//        if (deleteButton) {
//            e.preventDefault();
//            e.stopPropagation();
//            handleDeleteClick(deleteButton);
//        }
//    });

//    // Add loading state to delete form
//    if (deleteForm && deleteSubmitBtn) {
//        deleteForm.addEventListener('submit', function () {
//            deleteSubmitBtn.disabled = true;
//            deleteSubmitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Deleting...';
//        });

//        // Reset loading state when modal is hidden
//        deleteModal.addEventListener('hidden.bs.modal', function () {
//            // Reset form state
//            deleteSubmitBtn.disabled = false;
//            const buttonText = document.getElementById('deleteButtonText').textContent || 'Record';
//            deleteSubmitBtn.innerHTML = `<i class="fas fa-trash me-1"></i>Delete ${buttonText}`;

//            // Clear form data
//            document.getElementById('deleteRecordId').value = '';
//            document.getElementById('deleteRecordName').textContent = '';
//            document.getElementById('deleteItemType').textContent = 'the record';
//            document.getElementById('deleteButtonText').textContent = 'Record';
//            deleteForm.action = '';
//        });

//        // Also reset on modal show (in case of any lingering state)
//        deleteModal.addEventListener('show.bs.modal', function () {
//            deleteSubmitBtn.disabled = false;
//            const buttonText = document.getElementById('deleteButtonText').textContent || 'Record';
//            deleteSubmitBtn.innerHTML = `<i class="fas fa-trash me-1"></i>Delete ${buttonText}`;
//        });
//    }
//});