// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Recipe deletion confirmation
function confirmDelete(id, name) {
    if (confirm(`Are you sure you want to delete the recipe "${name}"?`)) {
        document.getElementById(`delete-form-${id}`).submit();
    }
    return false;
}

// Favorites functionality
$(document).ready(function() {
    $('.favorite-btn').on('click', function() {
        const btn = $(this);
        const recipeId = btn.closest('.favorite-container').data('recipe-id');
        const isFavorite = btn.data('is-favorite') === true;

        // Prepare anti-forgery token
        const token = $('input[name="__RequestVerificationToken"]').val();

        // AJAX call to toggle favorite status
        $.ajax({
            url: '/Recipe/ToggleFavorite',
            type: 'POST',
            data: {
                recipeId: recipeId,
                __RequestVerificationToken: token
            },
            success: function(result) {
                if (result.success) {
                    // Update button appearance
                    if (result.isFavorite) {
                        btn.data('is-favorite', true);
                        btn.html('<i class="bi bi-heart-fill me-1"></i>');
                    } else {
                        btn.data('is-favorite', false);
                        btn.html('<i class="bi bi-heart me-1"></i>');

                        // If on favorites page, fade out the card
                        if (window.location.pathname.includes('/Recipe/MyFavorites')) {
                            btn.closest('.col-md-6').fadeOut(400, function() {
                                $(this).remove();

                                // If it was the last favorite, show the "no favorites" message
                                if ($('.col-md-6').length === 0) {
                                    location.reload();
                                }
                            });
                        }
                    }
                }
            }
        });
    });
});