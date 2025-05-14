// -------------------------------------------------------------------------------------------------------------------
// Recipe Operations
// -------------------------------------------------------------------------------------------------------------------
const RecipeApp = {
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
        $(document).on('click', '.favorite-btn', function(e) {
            e.preventDefault();
            e.stopPropagation();

            const btn = $(this);
            const container = btn.closest('.favorite-container');
            const recipeId = container.data('recipe-id');

            if (btn.data('processing')) return;
            btn.data('processing', true);

            // Visual feedback
            btn.prop('disabled', true);
            const originalContent = btn.html();
            btn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');

            const token = $('input[name="__RequestVerificationToken"]').val();

            $.ajax({
                url: '/Recipe/ToggleFavorite',
                type: 'POST',
                data: { recipeId: recipeId, __RequestVerificationToken: token },
                success: function(result) {
                    if (result.success) {
                        // Update button state and appearance
                        btn.data('is-favorite', result.isFavorite);

                        if (result.isFavorite) {
                            btn.html('<i class="bi bi-heart-fill text-danger fs-5"></i>');
                        } else {
                            btn.html('<i class="bi bi-heart fs-5"></i>');

                            // Special handling for MyFavorites page
                            if (window.location.pathname.includes('/Recipe/MyFavorites')) {
                                const recipeCard = btn.closest('.recipe-card-wrapper');

                                if (recipeCard && recipeCard.length) {
                                    recipeCard.fadeOut(900, function() {
                                        $(this).remove();

                                        if ($('.recipe-card-wrapper:visible').length === 0) {
                                            location.reload();
                                        }
                                    });
                                }
                            }
                        }
                    } else {
                        // Restore original content on failure
                        btn.html(originalContent);
                    }
                },
                error: function(xhr, status, error) {
                    console.error('Error toggling favorite:', error);
                    alert('Failed to update favorite status. Please try again.');
                    btn.html(originalContent);
                },
                complete: function() {
                    btn.prop('disabled', false);
                    btn.data('processing', false);
                }
            });
        });
    },

    // Setup star ratings functionality
    setupRatings: function() {
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

        // Hover effect
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
            RecipeApp.updateStarDisplay(container.find('.rating-stars'), rating);
            
            container.data('user-rating', rating);
            
            container.find('.average-rating').text(result.averageRating.toFixed(1));
            container.find('.total-ratings').text(result.totalRatings);

            // Recipe details page
            $('#average-rating').text(result.averageRating.toFixed(1));
            $('#total-ratings').text(result.totalRatings);

            // Update rating if exists
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
    
    handleAjaxError: function(message, error, btn, originalContent) {
        console.error(`${message}: ${error}`);
        alert(`${message}. Please try again.`);
        
        if (btn) {
            btn.prop('disabled', false);
            if (originalContent) {
                btn.html(originalContent);
            }
        }
    }
};

// -------------------------------------------------------------------------------------------------------------------
// Ingredient Form Validation
// -------------------------------------------------------------------------------------------------------------------
const IngredientApp = {
    init: function() {
        this.setupQuantityValidation();
        this.setupCustomValidation();
        this.setupDuplicateHandling();
    },

    // Handle ingredient quantity input validation
    setupQuantityValidation: function() {
        $(document).on('input', '.quantity-input', function() {
            let value = parseFloat($(this).val());
            if (isNaN(value)) {
                $(this).val("");
            } else {
                // Round to 2 decimal
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

    // Duplicate handling for ingredient modal
    setupDuplicateHandling: function() {
        $("#save-new-ingredient").on("click", function() {
            const name = $("#new-ingredient-name").val().trim();
            if (!name) {
                alert("Ingredient name is required");
                return;
            }

            // Create new ingredient data
            const newIngredientData = {
                name: name,
                category: $("#new-ingredient-category").val().trim(),
                measurement: $("#new-ingredient-measurement").val().trim(),
                description: $("#new-ingredient-description").val().trim()
            };

            // Save to database via AJAX
            $.ajax({
                url: "/Ingredient/AddIngredientAjax",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(newIngredientData),
                success: function(result) {
                    if (result.success) {
                        ingredientsList.push(result.ingredient);
                        
                        addIngredientChip(result.ingredient);
                        
                        $("#addIngredientModal").modal("hide");
                    } else {
                        alert("Error saving ingredient: " + result.message);
                    }
                },
                error: function(xhr, status, error) {
                    console.error("Error saving ingredient:", error);
                    // Try to parse the error response for more detailed message
                    try {
                        const response = JSON.parse(xhr.responseText);
                        if (response && response.message) {
                            alert("Error: " + response.message);
                        } else {
                            alert("Error saving ingredient. Please try again.");
                        }
                    } catch (e) {
                        alert("Error saving ingredient. Please try again.");
                    }
                }
            });
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

// -------------------------------------------------------------------------------------------------------------------
// Search Functionality
// -------------------------------------------------------------------------------------------------------------------
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

$(document).ready(function() {
    RecipeApp.init();
    IngredientApp.init();
    SearchApp.init();
});