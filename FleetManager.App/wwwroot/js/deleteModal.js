// wwwroot/js/delete-modal.js
// Generic Delete Modal Handler
class DeleteModal {
    constructor() {
        console.log('DeleteModal constructor called'); // Temporary debug line
        this.modal = document.getElementById('deleteModal');
        this.form = document.getElementById('deleteForm');
        this.recordIdInput = document.getElementById('deleteRecordId');
        this.recordNameSpan = document.getElementById('deleteRecordName');
        this.itemTypeSpan = document.getElementById('deleteItemType');
        this.buttonTextSpan = document.getElementById('deleteButtonText');
        this.submitBtn = document.getElementById('deleteSubmitBtn');

        console.log('Modal element found:', this.modal); // Temporary debug line
        this.init();
    }

    init() {
        // Handle all delete buttons with class 'delete-btn'
        document.addEventListener('click', (e) => {
            if (e.target.matches('.delete-btn') || e.target.closest('.delete-btn')) {
                const btn = e.target.matches('.delete-btn') ? e.target : e.target.closest('.delete-btn');
                this.handleDeleteClick(btn);
            }
        });

        // Handle form submission with loading state
        this.form.addEventListener('submit', (e) => {
            this.setLoadingState(true);
        });

        // Reset loading state when modal is hidden
        this.modal.addEventListener('hidden.bs.modal', () => {
            this.setLoadingState(false);
        });
    }

    handleDeleteClick(btn) {
        const recordId = btn.dataset.recordId;
        const recordName = btn.dataset.recordName || 'this item';
        const deleteAction = btn.dataset.deleteAction;
        const itemType = btn.dataset.itemType || 'the record';
        const buttonText = btn.dataset.buttonText || 'Record';

        // Validate required data
        if (!recordId || !deleteAction) {
            console.error('Delete button must have data-record-id and data-delete-action attributes');
            return;
        }

        // Set modal content
        this.recordIdInput.value = recordId;
        this.recordNameSpan.textContent = recordName;
        this.itemTypeSpan.textContent = itemType;
        this.buttonTextSpan.textContent = buttonText;
        this.form.action = deleteAction;

        // Show modal
        new bootstrap.Modal(this.modal).show();
    }

    setLoadingState(isLoading) {
        if (isLoading) {
            this.submitBtn.disabled = true;
            this.submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Deleting...';
        } else {
            this.submitBtn.disabled = false;
            this.submitBtn.innerHTML = `<i class="fas fa-trash me-1"></i>Delete ${this.buttonTextSpan.textContent}`;
        }
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    console.log('Delete modal script loaded'); // Temporary debug line
    new DeleteModal();
});















//class DeleteModal {
//    constructor(text, action, controller,area, id) {
//        this.text = text;
//        this.action = action;
//        this.controller = controller;
//        this.id = id;
//        this.area = area;
//    }

//    openDeleteModal() {
//        const element = document.getElementById('deleteGenericModal');
//        const modal = new bootstrap.Modal(element);
//        document.getElementById('deleteRecordId').value = this.id;
//        document.getElementById('deleteRecordName').textContent = this.text;

//        document.getElementById('action').value = this.action;
//        document.getElementById('controller').value = this.controller;

//        modal.show();
//    }

//    confirmDelete() {
//        $.ajax({
//            url: `/${this.area}/${this.controller}/${this.action}/${this.id}`,
//            type: 'get',
//            dataType: 'json',
//            success: function (response) {
//                if (response.success) {
//                    showToast(response.message, 'success');
//                } else {
//                    showToast(response.message, 'danger');
//                }

//                setTimeout(() => {
//                    location.reload();
//                }, 3000);
//            },
//            error: function (err) {
//                showToast(err.responseText, 'danger');
//            }
//        });
//    }
//}







