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

        // Show loading state
        btn.prop('disabled', true);
        const originalContent = btn.html();
        btn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');

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
                // Re-enable button
                btn.prop('disabled', false);
                btn.html(originalContent);
            },
            error: function(xhr, status, error) {
                console.error('Error toggling favorite:', error);
                alert('Failed to update favorite status. Please try again.');
                // Re-enable button
                btn.prop('disabled', false);
                btn.html(originalContent);
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

// Ingredient increment/decrement behavior - USE EVENT DELEGATION
$(document).on('input', '.quantity-input', function() {
    // Validate and format the input
    let value = parseFloat($(this).val());
    if (isNaN(value)) {
        $(this).val("");
    } else {
        // Round to 2 decimal places if needed
        $(this).val(Math.max(0, parseFloat(value.toFixed(2))));
    }
});

// Handle number input arrows
$(document).on('keydown', '.quantity-input', function(e) {
    // Allow: backspace, delete, tab, escape, enter
    if ($.inArray(e.keyCode, [46, 8, 9, 27, 13]) !== -1 ||
        // Allow: Ctrl+A, Command+A
        (e.keyCode === 65 && (e.ctrlKey === true || e.metaKey === true)) ||
        // Allow: home, end, left, right, down, up
        (e.keyCode >= 35 && e.keyCode <= 40)) {
        return;
    }

    // Ensure that it's a number and stop the keypress if not
    if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) &&
        (e.keyCode < 96 || e.keyCode > 105) &&
        e.keyCode !== 190 && e.keyCode !== 110) {
        e.preventDefault();
    }
});

// Add custom validation rules
$.validator.addMethod("ingredientsRequired", function(value, element) {
    return $("#ingredients-container").children().length > 0;
}, "Please add at least one ingredient");

// Apply the rule
$("#recipeForm").validate({
    rules: {
        Title: {
            required: true,
            minlength: 3
        },
        Instructions: {
            required: true,
            minlength: 10
        },
        // Custom validation for ingredients
        "ingredients-data": {
            ingredientsRequired: true
        }
    },
    messages: {
        Title: {
            required: "Please enter a recipe title",
            minlength: "Title must be at least 3 characters"
        }
        // Add other messages
    },
    errorPlacement: function(error, element) {
        // Custom placement of error messages
        if (element.attr("id") === "ingredients-data") {
            error.insertAfter("#ingredients-section");
        } else {
            error.insertAfter(element);
        }
    }
});