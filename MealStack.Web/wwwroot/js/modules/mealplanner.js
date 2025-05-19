const MealPlannerModule = (function() {
    const defaults = {
        // Shopping list selectors
        shoppingItemCheckboxSelector: '.shopping-item-checkbox',
        shoppingItemSelector: '.shopping-item',
        clearCheckedItemsSelector: '#clearCheckedItems',
        checkedItemsCountSelector: '#checkedItemsCount',
        searchItemsSelector: '#searchItems',

        // Recipe search selectors
        recipeSearchSelector: '#RecipeSearch',

        // Meal form selectors
        addMealFormSelector: '#addMealForm',
        editMealFormSelector: '#editMealForm',
        saveMealBtnSelector: '#saveMealBtn',
        updateMealBtnSelector: '#updateMealBtn',
        removeMealBtnSelector: '.remove-meal-btn',

        // API endpoints
        updateShoppingItemUrl: '/MealPlan/UpdateShoppingItem',
        addMealItemUrl: '/MealPlan/AddMealItem',
        updateMealItemUrl: '/MealPlan/UpdateMealItem',
        removeMealItemUrl: '/MealPlan/RemoveMealItem',
        getRecipesUrl: '/MealPlan/GetRecipes',
    };

    let options = {};

    const initShoppingList = function() {
        // Toggle shopping item checked state
        $(document).on('change', options.shoppingItemCheckboxSelector, function() {
            const checkbox = $(this);
            const isChecked = checkbox.prop('checked');
            const itemId = checkbox.data('item-id');
            const mealPlanId = checkbox.data('meal-plan-id');
            const item = checkbox.closest(options.shoppingItemSelector);

            // Optimistic update for better UX
            if (isChecked) {
                item.addClass('checked');
                updateCheckedCount(1);
            } else {
                item.removeClass('checked');
                updateCheckedCount(-1);
            }

            // Update on server
            $.ajax({
                url: options.updateShoppingItemUrl,
                type: 'POST',
                data: {
                    itemId: itemId,
                    isChecked: isChecked,
                    mealPlanId: mealPlanId
                },
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                error: function() {
                    // Revert on error
                    checkbox.prop('checked', !isChecked);
                    if (!isChecked) {
                        item.addClass('checked');
                        updateCheckedCount(1);
                    } else {
                        item.removeClass('checked');
                        updateCheckedCount(-1);
                    }
                    alert('Error updating item. Please try again.');
                }
            });
        });

        // Clear all checked items
        $(document).on('click', options.clearCheckedItemsSelector, function(e) {
            e.preventDefault();

            $(`${options.shoppingItemCheckboxSelector}:checked`).each(function() {
                $(this).prop('checked', false).trigger('change');
            });
        });

        // Search functionality
        $(document).on('input', options.searchItemsSelector, function() {
            const searchTerm = $(this).val().toLowerCase().trim();

            if (searchTerm === '') {
                $(options.shoppingItemSelector).show();
                $('.shopping-category').show();
                return;
            }

            $(options.shoppingItemSelector).each(function() {
                const itemName = $(this).data('item-name').toLowerCase();
                const matches = itemName.includes(searchTerm);
                $(this).toggle(matches);
            });

            // Hide empty categories
            $('.shopping-category').each(function() {
                const hasVisibleItems = $(this).find(`${options.shoppingItemSelector}:visible`).length > 0;
                $(this).toggle(hasVisibleItems);
            });
        });
    };

    const initMealPlanForms = function() {
        // Initialize Select2 for recipe search if exists
        if ($(options.recipeSearchSelector).length && typeof $.fn.select2 !== 'undefined') {
            $(options.recipeSearchSelector).select2({
                placeholder: 'Search for a recipe...',
                allowClear: true,
                ajax: {
                    url: options.getRecipesUrl,
                    dataType: 'json',
                    delay: 250,
                    data: function (params) {
                        return {
                            term: params.term || ''
                        };
                    },
                    processResults: function (data) {
                        return {
                            results: $.map(data, function (item) {
                                return {
                                    text: item.text,
                                    id: item.id,
                                    servings: item.servings
                                };
                            })
                        };
                    },
                    cache: true
                },
                minimumInputLength: 1
            }).on('select2:select', function (e) {
                const data = e.params.data;
                $('#RecipeId').val(data.id);
                if (data.servings) {
                    $('#Servings').val(data.servings);
                }
            });
        }

        // Add meal
        $(document).on('click', options.saveMealBtnSelector, function() {
            const form = $(options.addMealFormSelector);
            if (!form[0].checkValidity()) {
                form[0].reportValidity();
                return;
            }

            const formData = {
                MealPlanId: $('#MealPlanId').val(),
                RecipeId: $('#RecipeId').val(),
                PlannedDate: $('#PlannedDate').val(),
                MealType: $('#MealType').val(),
                Servings: $('#Servings').val(),
                Notes: $('#Notes').val()
            };

            $.ajax({
                url: options.addMealItemUrl,
                type: 'POST',
                data: formData,
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function(response) {
                    if (response.success) {
                        // Reload the page to show the updated meal plan
                        window.location.reload();
                    } else {
                        alert('Error: ' + (response.message || 'Failed to add meal. Please try again.'));
                    }
                },
                error: function() {
                    alert('Error: Failed to add meal. Please try again.');
                }
            });
        });

        // Update meal
        $(document).on('click', options.updateMealBtnSelector, function() {
            const form = $(options.editMealFormSelector);
            if (!form[0].checkValidity()) {
                form[0].reportValidity();
                return;
            }

            const formData = {
                Id: $('#EditMealId').val(),
                MealPlanId: $('#EditMealPlanId').val(),
                RecipeId: $('#EditRecipeId').val(),
                PlannedDate: $('#EditPlannedDate').val(),
                MealType: $('#EditMealType').val(),
                Servings: $('#EditServings').val(),
                Notes: $('#EditNotes').val()
            };

            $.ajax({
                url: options.updateMealItemUrl,
                type: 'POST',
                data: formData,
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function(response) {
                    if (response.success) {
                        window.location.reload();
                    } else {
                        alert('Error: ' + (response.message || 'Failed to update meal. Please try again.'));
                    }
                },
                error: function() {
                    alert('Error: Failed to update meal. Please try again.');
                }
            });
        });

        // Remove meal
        $(document).on('click', options.removeMealBtnSelector, function() {
            const itemId = $(this).data('meal-id');
            const mealPlanId = $(this).data('meal-plan-id');
            const recipeTitle = $(this).data('recipe-title');

            if (confirm(`Are you sure you want to remove "${recipeTitle}" from this meal plan?`)) {
                $.ajax({
                    url: options.removeMealItemUrl,
                    type: 'POST',
                    data: {
                        itemId: itemId,
                        mealPlanId: mealPlanId
                    },
                    headers: {
                        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                    },
                    success: function(response) {
                        if (response.success) {
                            $(`#meal-item-${itemId}`).fadeOut(300, function() {
                                $(this).remove();

                                // If no more meals for that day, remove the day header
                                $('.meal-day').each(function() {
                                    if ($(this).find('.meal-item').length === 0) {
                                        $(this).remove();
                                    }
                                });

                                // If no more meals at all, show empty state
                                if ($('.meal-item').length === 0) {
                                    window.location.reload();
                                }
                            });
                        } else {
                            alert('Error: ' + (response.message || 'Failed to remove meal. Please try again.'));
                        }
                    },
                    error: function() {
                        alert('Error: Failed to remove meal. Please try again.');
                    }
                });
            }
        });

        // Edit meal modal setup
        $('#editMealModal').on('show.bs.modal', function (event) {
            const button = $(event.relatedTarget);
            const modal = $(this);

            // Fill form with meal data
            modal.find('#EditMealId').val(button.data('meal-id'));
            modal.find('#EditRecipeId').val(button.data('recipe-id'));
            modal.find('#EditRecipeTitle').val(button.data('recipe-title'));
            modal.find('#EditPlannedDate').val(button.data('planned-date'));
            modal.find('#EditMealType').val(button.data('meal-type'));
            modal.find('#EditServings').val(button.data('servings'));
            modal.find('#EditNotes').val(button.data('notes'));
        });
    };

    // Helper function to update the checked items count
    const updateCheckedCount = function(change) {
        const countElement = $(options.checkedItemsCountSelector);
        if (countElement.length) {
            const currentCount = parseInt(countElement.text());
            countElement.text(currentCount + change);
        }
    };

    const init = function(config = {}) {
        console.log("Initializing MealPlannerModule...");
        options = $.extend({}, defaults, config);

        // Initialize shopping list functionality
        initShoppingList();

        // Initialize meal plan forms
        initMealPlanForms();

        console.log("MealPlannerModule initialized successfully");
    };

    return {
        init
    };
})();

window.MealPlannerModule = MealPlannerModule;