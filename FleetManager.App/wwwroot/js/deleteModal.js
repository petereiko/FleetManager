(function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.delete-btn');
        if (!btn) return;

        e.preventDefault();
        var id = btn.getAttribute('data-item-id'),
            name = btn.getAttribute('data-item-name'),
            modal = new bootstrap.Modal(document.getElementById('deleteModal'));

        document.getElementById('deleteItemId').value = id;
        document.getElementById('deleteItemName').textContent = name;
        // the form’s action was rendered from the Partial’s model
        modal.show();
    }, false);
})();
