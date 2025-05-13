// site.js - Fixed version focusing on the recipe-card-wrapper class

// =============================================================================
// Recipe Operations
// =============================================================================
const RecipeApp = {
    // Initialize all recipe-related functionality
    init: function() {
        this.setupDeleteConfirmation();
        this.setupFavorites();
        this.setupRatings();
        this.setupNotesEditor();
        this.setupIngredientCheckboxes();
    },

    // Setup recipe deletion confirmation
    setupDeleteConfirmation: function() {
        window.confirmDelete = function(id, name) {
            if (confirm(`Are you sure you want to delete the recipe "${name}"?`)) {
                document.getElementById(`delete-form-${id}`).submit();
            }
            return false;
        };
    },

    // Setup favorites functionality 
    setupFavorites: function() {
        // Heart icon click handler
        $(document).on('click', '.favorite-container i.bi-heart, .favorite-container i.bi-heart-fill', function(e) {
            e.preventDefault();
            e.stopPropagation();

            const heartIcon = $(this);
            const container = heartIcon.closest('.favorite-container');
            const recipeId = container.data('recipe-id');

            if (heartIcon.data('processing')) return;
            heartIcon.data('processing', true);

            heartIcon.css({
                'transition': 'transform 0.2s ease',
                'transform': 'scale(1.2)'
            });

            const token = $('input[name="__RequestVerificationToken"]').val();

            $.ajax({
                url: '/Recipe/ToggleFavorite',
                type: 'POST',
                data: { recipeId: recipeId, __RequestVerificationToken: token },
                success: function(result) {
                    if (result.success) {
                        // Replace heart icons
                        container.find('i.bi-heart, i.bi-heart-fill').remove();
                        const newHeart = $('<i>').addClass(result.isFavorite ?
                            'bi bi-heart-fill text-danger fs-5' :
                            'bi bi-heart fs-5').css('cursor', 'pointer');
                        container.append(newHeart);

                        // Handle unfavorite on MyFavorites page
                        if (!result.isFavorite && window.location.pathname.includes('/Recipe/MyFavorites')) {
                            // Find the recipe card wrapper - explicitly look for this class
                            const recipeCard = heartIcon.closest('.recipe-card-wrapper');

                            if (recipeCard && recipeCard.length) {
                                console.log("Found recipe card:", recipeCard);

                                // Clear any existing animations or transitions
                                recipeCard.stop(true, true);

                                // Simple fadeOut with longer duration for smoother effect
                                recipeCard.fadeOut(900, function() {
                                    console.log("Fadeout complete, removing card");
                                    $(this).remove();

                                    // Check if we need to reload to show "No favorites" message
                                    const remainingCards = $('.recipe-card-wrapper:visible').length;
                                    console.log("Remaining cards:", remainingCards);

                                    if (remainingCards === 0) {
                                        console.log("No cards left, reloading page");
                                        location.reload();
                                    }
                                });
                            } else {
                                console.warn("Could not find recipe card wrapper - falling back to older selectors");
                                // Fallback to find any parent column
                                const cardCol = heartIcon.closest('.col-md-6, .col-lg-4');
                                if (cardCol && cardCol.length) {
                                    cardCol.fadeOut(900, function() {
                                        $(this).remove();
                                        if ($('.col-md-6:visible, .col-lg-4:visible').length === 0) {
                                            location.reload();
                                        }
                                    });
                                } else {
                                    console.error("Could not find recipe card container - reloading page");
                                    location.reload();
                                }
                            }
                        }
                    }
                },
                error: function(xhr, status, error) {
                    console.error('Error toggling favorite:', error);
                    alert('Failed to update favorite status. Please try again.');
                    heartIcon.css('transform', 'scale(1)');
                },
                complete: function() {
                    heartIcon.data('processing', false);
                }
            });
        });

        // Button-based favorite toggle handler (for backward compatibility)
        $(document).on('click', '.favorite-btn', function() {
            const btn = $(this);
            const container = btn.closest('.favorite-container');
            const recipeId = container.data('recipe-id');

            if (btn.data('processing')) {
                return;
            }
            btn.data('processing', true);

            const token = $('input[name="__RequestVerificationToken"]').val();
            const originalContent = btn.html();

            btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');

            $.ajax({
                url: '/Recipe/ToggleFavorite',
                type: 'POST',
                data: { recipeId: recipeId, __RequestVerificationToken: token },
                success: function(result) {
                    btn.prop('disabled', false);
                    if (result.success) {
                        btn.data('is-favorite', result.isFavorite);

                        if (result.isFavorite) {
                            btn.html('<i class="bi bi-heart-fill text-danger"></i>');
                        } else {
                            btn.html('<i class="bi bi-heart"></i>');

                            // Special handling for MyFavorites page
                            if (window.location.pathname.includes('/Recipe/MyFavorites')) {
                                // Look for the recipe card wrapper
                                const recipeCard = btn.closest('.recipe-card-wrapper');

                                if (recipeCard && recipeCard.length) {
                                    console.log("Found recipe card (button click):", recipeCard);

                                    // Clear any existing animations
                                    recipeCard.stop(true, true);

                                    // Use longer duration for smoother fadeout
                                    recipeCard.fadeOut(900, function() {
                                        console.log("Fadeout complete, removing card (button click)");
                                        $(this).remove();

                                        // Check remaining cards
                                        const remainingCards = $('.recipe-card-wrapper:visible').length;
                                        console.log("Remaining cards after button click:", remainingCards);

                                        if (remainingCards === 0) {
                                            console.log("No cards left after button click, reloading page");
                                            location.reload();
                                        }
                                    });
                                } else {
                                    console.warn("Could not find recipe card wrapper for button - using fallback");
                                    // Fallback to column
                                    const cardCol = btn.closest('.col-md-6, .col-lg-4');
                                    if (cardCol && cardCol.length) {
                                        cardCol.fadeOut(900, function() {
                                            $(this).remove();
                                            if ($('.col-md-6:visible, .col-lg-4:visible').length === 0) {
                                                location.reload();
                                            }
                                        });
                                    } else {
                                        console.error("Could not find recipe card container for button - reloading page");
                                        location.reload();
                                    }
                                }
                            }
                        }
                    } else {
                        btn.html(originalContent); // Restore on failure
                    }
                },
                error: function(xhr, status, error) {
                    console.error('Error toggling favorite (button click):', error);
                    alert('Failed to update favorite status. Please try again.');
                    btn.prop('disabled', false).html(originalContent);
                },
                complete: function() {
                    btn.data('processing', false);
                }
            });
        });
    },

    // Setup star ratings functionality
    setupRatings: function() {
        // Handle star click
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
                    RecipeApp.handleRatingResult(result, container, rating);
                },
                error: function(xhr, status, error) {
                    RecipeApp.handleAjaxError('Error rating recipe', error);
                }
            });
        });

        // Hover effect for stars
        $(document).on('mouseenter', '.star-rating', function() {
            const rating = $(this).data('rating');
            const container = $(this).closest('.rating-stars');
            RecipeApp.updateStarDisplay(container, rating);
        });

        $(document).on('mouseleave', '.rating-stars', function() {
            const container = $(this).closest('.rating-widget');
            const userRating = container.data('user-rating') || 0;
            RecipeApp.updateStarDisplay($(this), userRating);
        });

        // Initialize ratings on page load
        $('.rating-widget').each(function() {
            const container = $(this);
            const userRating = container.data('user-rating') || 0;
            if (userRating > 0) {
                RecipeApp.updateStarDisplay(container.find('.rating-stars'), userRating);
            }
        });
    },

    // Handle rating result
    handleRatingResult: function(result, container, rating) {
        if (result.success) {
            // Update stars display for this specific recipe
            RecipeApp.updateStarDisplay(container.find('.rating-stars'), rating);

            // Store the current user rating
            container.data('user-rating', rating);

            // Update average display if exists
            container.find('.average-rating').text(result.averageRating.toFixed(1));
            container.find('.total-ratings').text(result.totalRatings);

            // If we're on the recipe details page
            $('#average-rating').text(result.averageRating.toFixed(1));
            $('#total-ratings').text(result.totalRatings);

            // Update the recipe card's rating stars if it exists
            RecipeApp.updateRecipeCardRating(container.data('recipe-id'), result);
        }
    },

    // Update star display for a container based on rating
    updateStarDisplay: function(container, rating) {
        container.find('.star-rating').each(function(index) {
            if (index < rating) {
                $(this).addClass('bi-star-fill').removeClass('bi-star');
            } else {
                $(this).addClass('bi-star').removeClass('bi-star-fill');
            }
        });
    },

    // Update recipe card rating display
    updateRecipeCardRating: function(recipeId, result) {
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
    },

    // Setup notes editor
    setupNotesEditor: function() {
        $('#saveNotesBtn').on('click', function() {
            const recipeId = $(this).data('recipe-id');
            const notes = $('#recipeNotes').val();
            const token = $('input[name="__RequestVerificationToken"]').val();

            $.ajax({
                url: '/Recipe/SaveNotes',
                type: 'POST',
                data: {
                    recipeId: recipeId,
                    notes: notes,
                    __RequestVerificationToken: token
                },
                success: function(result) {
                    if (result.success) {
                        // Show success message
                        alert('Notes saved successfully!');
                    } else {
                        alert('Error: ' + result.message);
                    }
                },
                error: function(xhr, status, error) {
                    RecipeApp.handleAjaxError('Error saving notes', error);
                }
            });
        });
    },

    // Setup ingredient checkboxes on recipe details
    setupIngredientCheckboxes: function() {
        $('.ingredients-list input[type="checkbox"]').on('change', function() {
            const label = $(this).next('label');
            if (this.checked) {
                label.css('text-decoration', 'line-through');
            } else {
                label.css('text-decoration', 'none');
            }
        });
    },

    // Handle AJAX errors
    handleAjaxError: function(message, error, btn, originalContent) {
        console.error(`${message}: ${error}`);
        alert(`${message}. Please try again.`);

        // If button provided, restore it
        if (btn) {
            btn.prop('disabled', false);
            if (originalContent) {
                btn.html(originalContent);
            }
        }
    }
};

// =============================================================================
// Ingredient Form Validation
// =============================================================================
const IngredientApp = {
    init: function() {
        this.setupQuantityValidation();
        this.setupCustomValidation();
    },

    // Handle ingredient quantity input validation
    setupQuantityValidation: function() {
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
    },

    // Add custom validation rules
    setupCustomValidation: function() {
        if ($.validator) {
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
        }
    }
};

// =============================================================================
// Search Functionality
// =============================================================================
const SearchApp = {
    init: function() {
        this.setupAutocomplete();
        this.handleAdvancedSearch();
    },

    // Initialize autocomplete for search inputs
    setupAutocomplete: function() {
        $("#searchTerm").autocomplete({
            source: function(request, response) {
                console.log("Autocomplete request for: " + request.term);
                $.ajax({
                    url: "/Recipe/GetSearchSuggestions",
                    dataType: "json",
                    data: { term: request.term },
                    success: function(data) {
                        console.log("Received " + data.length + " suggestions");
                        response(data);
                    },
                    error: function(xhr, status, error) {
                        console.error("Autocomplete error: " + error);
                    }
                });
            },
            minLength: 2,
            delay: 300,
            select: function(event, ui) {
                $("#searchTerm").val(ui.item.value);
                $("#searchForm").submit();
                return false;
            }
        });
    },

    // Show advanced search options if any filter is applied
    handleAdvancedSearch: function() {
        if ($("#difficulty").val() || $("#sortBy").val() !== "newest" ||
            $("#matchAllIngredients").prop("checked") === false) {
            $("#advancedSearchOptions").collapse("show");
        }
    }
};

// Initialize all apps when document is ready
$(document).ready(function() {
    RecipeApp.init();
    IngredientApp.init();
    SearchApp.init();
});