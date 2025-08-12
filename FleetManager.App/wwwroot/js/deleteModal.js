



class DeleteModal {
    constructor(text, action, controller,area, id) {
        this.text = text;
        this.action = action;
        this.controller = controller;
        this.id = id;
        this.area = area;
    }

    openDeleteModal() {
        const element = document.getElementById('deleteGenericModal');
        const modal = new bootstrap.Modal(element);
        document.getElementById('deleteRecordId').value = this.id;
        document.getElementById('deleteRecordName').textContent = this.text;

        document.getElementById('action').value = this.action;
        document.getElementById('controller').value = this.controller;

        modal.show();
    }

    confirmDelete() {
        $.ajax({
            url: `/${this.area}/${this.controller}/${this.action}/${this.id}`,
            type: 'get',
            dataType: 'json',
            success: function (response) {
                if (response.success) {
                    showToast(response.message, 'success');
                } else {
                    showToast(response.message, 'danger');
                }

                setTimeout(() => {
                    location.reload();
                }, 3000);
            },
            error: function (err) {
                showToast(err.responseText, 'danger');
            }
        });
    }
}







