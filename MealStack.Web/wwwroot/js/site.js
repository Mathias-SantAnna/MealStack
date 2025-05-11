// Recipe deletion confirmation
function confirmDelete(id, name) {
    if (confirm(`Are you sure you want to delete the recipe "${name}"?`)) {
        document.getElementById(`delete-form-${id}`).submit();
    }
    return false;
}

// Favorites functionality
$(document).ready(function() {
    // Use event delegation to handle dynamically loaded content
    $(document).on('click', '.favorite-btn', function() {
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
                        btn.find('i').removeClass('bi-heart').addClass('bi-heart-fill text-danger');
                    } else {
                        btn.data('is-favorite', false);
                        btn.find('i').removeClass('bi-heart-fill text-danger').addClass('bi-heart');

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
            },
            error: function(xhr, status, error) {
                console.error('Error toggling favorite:', error);
                alert('Failed to update favorite status. Please try again.');
            }
        });
    });
});

// Star rating functionality
$(document).on('click', '.star-rating', function() {
    const rating = $(this).data('rating');
    const container = $(this).closest('.rating-widget');
    const recipeId = container.data('recipe-id');
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Recipe/RateRecipe',
        type: 'POST',
        data: {
            recipeId: recipeId,
            rating: rating,
            __RequestVerificationToken: token
        },
        success: function(result) {
            if (result.success) {
                // Update stars display for this specific recipe
                container.find('.star-rating').each(function(index) {
                    if (index < rating) {
                        $(this).removeClass('bi-star').addClass('bi-star-fill');
                    } else {
                        $(this).removeClass('bi-star-fill').addClass('bi-star');
                    }
                });

                // Store the current user rating
                container.data('user-rating', rating);

                // Update average display if exists
                container.find('.average-rating').text(result.averageRating.toFixed(1));
                container.find('.total-ratings').text(result.totalRatings);

                // If we're on the recipe details page
                $('#average-rating').text(result.averageRating.toFixed(1));
                $('#total-ratings').text(result.totalRatings);

                // Update the recipe card's rating stars if it exists
                const recipeCard = $(`[data-recipe-id="${recipeId}"]`).closest('.card');
                if (recipeCard.length) {
                    recipeCard.find('.rating-stars .bi').each(function(index) {
                        if (index < Math.floor(result.averageRating)) {
                            $(this).removeClass('bi-star bi-star-half').addClass('bi-star-fill');
                        } else if (index < Math.floor(result.averageRating) + 0.5 && result.averageRating % 1 >= 0.5) {
                            $(this).removeClass('bi-star bi-star-fill').addClass('bi-star-half');
                        } else {
                            $(this).removeClass('bi-star-fill bi-star-half').addClass('bi-star');
                        }
                    });

                    // Update rating count
                    recipeCard.find('.rating-count').text(`(${result.totalRatings})`);
                }
            }
        },
        error: function(xhr, status, error) {
            console.error('Error rating recipe:', error);
            alert('Failed to update rating. Please try again.');
        }
    });
});

// Hover effect for stars - scoped to container
$(document).on('mouseenter', '.star-rating', function() {
    const rating = $(this).data('rating');
    const container = $(this).closest('.rating-stars');

    container.find('.star-rating').each(function(index) {
        if (index < rating) {
            $(this).addClass('bi-star-fill').removeClass('bi-star');
        } else {
            $(this).addClass('bi-star').removeClass('bi-star-fill');
        }
    });
});

$(document).on('mouseleave', '.rating-stars', function() {
    const container = $(this).closest('.rating-widget');
    const userRating = container.data('user-rating') || 0;

    // Restore to the actual user rating
    $(this).find('.star-rating').each(function(index) {
        if (index < userRating) {
            $(this).addClass('bi-star-fill').removeClass('bi-star');
        } else {
            $(this).addClass('bi-star').removeClass('bi-star-fill');
        }
    });
});

// Initialize ratings and favorites on page load
$(document).ready(function() {
    // Initialize user ratings if available
    $('.rating-widget').each(function() {
        const container = $(this);
        const userRating = container.data('user-rating') || 0;

        if (userRating > 0) {
            container.find('.star-rating').each(function(index) {
                if (index < userRating) {
                    $(this).addClass('bi-star-fill').removeClass('bi-star');
                }
            });
        }
    });
});