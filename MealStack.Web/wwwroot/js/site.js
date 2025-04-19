// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Recipe deletion confirmation
function confirmDelete(recipeId, recipeName) {
    if (confirm(`Are you sure you want to delete "${recipeName}"?`)) {
        document.getElementById(`delete-form-${recipeId}`).submit();
    }
    return false;
}