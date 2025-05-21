const MealPlannerModule = (function() {
    // Default selectors & endpoints
    const defaults = {
        // Shopping list
        shoppingItemCheckboxSelector: '.shopping-item-checkbox',
        shoppingItemSelector:         '.shopping-item',
        clearCheckedItemsSelector:    '#clearCheckedItems',
        checkedItemsCountSelector:    '#checkedItemsCount',
        searchItemsSelector:          '#searchItems',

        // Recipe search
        recipeSearchSelector: '#RecipeSearch',

        // Meal plan forms
        addMealFormSelector:    '#addMealForm',
        editMealFormSelector:   '#editMealForm',
        saveMealBtnSelector:    '#saveMealBtn',
        updateMealBtnSelector:  '#updateMealBtn',
        removeMealBtnSelector:  '.remove-meal-btn',

        // API endpoints
        updateShoppingItemUrl: '/MealPlan/UpdateShoppingItem',
        addMealItemUrl:        '/MealPlan/AddMealItem',
        updateMealItemUrl:     '/MealPlan/UpdateMealItem',
        removeMealItemUrl:     '/MealPlan/RemoveMealItem',
        getRecipesUrl:         '/MealPlan/GetRecipes'
    };

    let options = {};

    // 1) Initialize jQuery-UI datepickers in dd/mm/yy format
    const initDatepickers = function() {
        if ($.fn.datepicker) {
            $('.datepicker').each(function() {
                $(this).datepicker({
                    dateFormat:  'dd/mm/yy',   // European format
                    changeMonth: true,         // month dropdown
                    changeYear:  true,         // year dropdown
                    firstDay:    1             // Monday as first day
                });
            });
        }
    };

    // 2) Initialize Select2 for recipe search
    const initRecipeSearch = function() {
        if ($(options.recipeSearchSelector).length && $.fn.select2) {
            $(options.recipeSearchSelector).select2({
                placeholder: 'Search for a recipe...',
                allowClear: true,
                dropdownParent: $('#addMealModal'),
                ajax: {
                    url: options.getRecipesUrl,
                    dataType: 'json',
                    delay: 250,
                    data: function (params) {
                        return { term: params.term || '' };
                    },
                    processResults: function (data) {
                        return {
                            results: $.map(data, function (item) {
                                return {
                                    text: item.text,
                                    id: item.id,
                                    servings: item.servings,
                                    imagePath: item.imagePath
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
                $('#AddServings').val(data.servings || 2);
            });
        }
    };

    // 3) Shopping-list functionality
    const initShoppingList = function() {
        const requestVerificationToken = $('input[name="__RequestVerificationToken"]').val();

        // Toggle item checked state
        $(document).on('change', options.shoppingItemCheckboxSelector, function() {
            const checkbox   = $(this);
            const isChecked  = checkbox.prop('checked');
            const itemId     = checkbox.data('item-id');
            const mealPlanId = checkbox.data('meal-plan-id');
            const item       = checkbox.closest(options.shoppingItemSelector);

            // Optimistic UI update
            if (isChecked) {
                item.addClass('checked');
                updateCheckedCount(1);
            } else {
                item.removeClass('checked');
                updateCheckedCount(-1);
            }

            // Persist change
            $.ajax({
                url:     options.updateShoppingItemUrl,
                method:  'POST',
                data:    {
                    itemId:     itemId,
                    isChecked:  isChecked,
                    mealPlanId: mealPlanId
                },
                headers: {
                    'RequestVerificationToken': requestVerificationToken
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
                    alert('Error updating shopping item. Please try again.');
                }
            });
        });

        // Clear all checked
        $(document).on('click', options.clearCheckedItemsSelector, function(e) {
            e.preventDefault();
            $(`${options.shoppingItemCheckboxSelector}:checked`).each(function() {
                $(this).prop('checked', false).trigger('change');
            });
        });

        // Search filter
        $(document).on('input', options.searchItemsSelector, function() {
            const term = $(this).val().toLowerCase().trim();
            if (!term) {
                $(options.shoppingItemSelector).show();
                $('.shopping-category').show();
                return;
            }
            $(options.shoppingItemSelector).each(function() {
                const name = $(this).data('item-name').toLowerCase();
                $(this).toggle(name.indexOf(term) !== -1);
            });
            // Hide empty categories
            $('.shopping-category').each(function() {
                const hasVisible = $(this)
                    .find(`${options.shoppingItemSelector}:visible`)
                    .length > 0;
                $(this).toggle(hasVisible);
            });
        });
    };

    // 4) Meal-plan add/edit/remove functionality
    const setupMealPlanEventListeners = function() {
        const requestVerificationToken = $('input[name="__RequestVerificationToken"]').val();

        // Add meal
        $(document).on('click', options.saveMealBtnSelector, function() {
            const form = $(options.addMealFormSelector)[0];
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const formData = {
                MealPlanId:  $('#AddMealPlanId').val(),
                RecipeId:    $('#RecipeSearch').val(),
                PlannedDate: $('#AddPlannedDate').val(),
                MealType:    $('#AddMealType').val(),
                Servings:    $('#AddServings').val(),
                Notes:       $('#AddNotes').val()
            };

            $.ajax({
                url: options.addMealItemUrl,
                type: 'POST',
                data: formData,
                headers: { 'RequestVerificationToken': requestVerificationToken },
                success: function(response) {
                    if (response.success) {
                        window.location.reload();
                    } else {
                        alert('Error: ' + (response.message || (response.errors ? response.errors.join('\n') : 'Failed to add meal. Please try again.')));
                    }
                },
                error: function() {
                    alert('Error: Failed to add meal. Please try again.');
                }
            });
        });

        // Edit meal modal setup
        $('#editMealModal').on('show.bs.modal', function (event) {
            const button = $(event.relatedTarget);
            const modal = $(this);

            modal.find('#EditMealId').val(button.data('meal-id'));
            modal.find('#EditRecipeId').val(button.data('recipe-id'));
            modal.find('#EditRecipeTitle').val(button.data('recipe-title'));
            modal.find('#EditPlannedDate').val(button.data('planned-date'));
            modal.find('#EditMealType').val(button.data('meal-type'));
            modal.find('#EditServings').val(button.data('servings'));
            modal.find('#EditNotes').val(button.data('notes'));
        });

        // Update meal
        $(document).on('click', options.updateMealBtnSelector, function() {
            const form = $(options.editMealFormSelector)[0];
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const formData = {
                Id:          $('#EditMealId').val(),
                MealPlanId:  $('#EditMealPlanId').val(),
                RecipeId:    $('#EditRecipeId').val(),
                PlannedDate: $('#EditPlannedDate').val(),
                MealType:    $('#EditMealType').val(),
                Servings:    $('#EditServings').val(),
                Notes:       $('#EditNotes').val()
            };

            $.ajax({
                url: options.updateMealItemUrl,
                type: 'POST',
                data: formData,
                headers: { 'RequestVerificationToken': requestVerificationToken },
                success: function(response) {
                    if (response.success) {
                        window.location.reload();
                    } else {
                        alert('Error: ' + (response.message || (response.errors ? response.errors.join('\n') : 'Failed to update meal. Please try again.')));
                    }
                },
                error: function() {
                    alert('Error: Failed to update meal. Please try again.');
                }
            });
        });

        // Remove meal
        $(document).on('click', options.removeMealBtnSelector, function() {
            const button = $(this);
            const itemId = button.data('meal-id');
            const mealPlanId = button.data('meal-plan-id');
            const recipeTitle = button.data('recipe-title');

            if (confirm(`Are you sure you want to remove "${recipeTitle}" from this meal plan?`)) {
                $.ajax({
                    url: options.removeMealItemUrl,
                    type: 'POST',
                    data: { itemId: itemId, mealPlanId: mealPlanId },
                    headers: { 'RequestVerificationToken': requestVerificationToken },
                    success: function(response) {
                        if (response.success) {
                            $(`#meal-item-${itemId}`).fadeOut(300, function() {
                                $(this).remove();
                                $('.meal-day').each(function() {
                                    if ($(this).find('.meal-item').length === 0) {
                                        $(this).remove();
                                    }
                                });

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
    };

    // Helper function to update checked items count
    const updateCheckedCount = function(change) {
        const el = $(options.checkedItemsCountSelector);
        if (!el.length) return;
        el.text(parseInt(el.text() || '0', 10) + change);
    };

    // Public init
    const init = function(config = {}) {
        options = $.extend({}, defaults, config);
        console.log("Initializing MealPlannerModule...");

        // Initialize components
        initDatepickers();
        initRecipeSearch();
        initShoppingList();
        setupMealPlanEventListeners();

        console.log("MealPlannerModule initialized successfully");
    };

    return { init };
})();

window.MealPlannerModule = MealPlannerModule;