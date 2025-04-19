// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

/**
 * Handles confirmation for deleting recipes
 * @param {number} recipeId - The ID of the recipe to delete
 * @param {string} recipeName - The name of the recipe
 * @returns {boolean} - False to prevent default link behavior
 */
function confirmDelete(recipeId, recipeName) {
    // Show confirmation dialog
    if (confirm(`Are you sure you want to delete "${recipeName}"?`)) {
        // If confirmed, submit the form
        document.getElementById(`delete-form-${recipeId}`).submit();
    }

    // Return false to prevent the default link behavior
    return false;
}