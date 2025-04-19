// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Recipe deletion confirmation
// Add this to your site.js file
function confirmDelete(id, name) {
    if (confirm(`Are you sure you want to delete the recipe "${name}"?`)) {
        document.getElementById(`delete-form-${id}`).submit();
    }
    return false;
}