const MealPlannerModule = (function() {
    const defaults = {
        shoppingItemCheckboxSelector: '.shopping-item-checkbox',
        shoppingItemSelector:         '.shopping-item',
        clearCheckedItemsSelector:    '#clearCheckedItems',
        checkedItemsCountSelector:    '#checkedItemsCount',
        searchItemsSelector:          '#searchItems',

        // Add Meal modal (updated IDs)
        recipeSearchSelector:    '#addMeal_RecipeId',
        addMealFormSelector:     '#addMealForm',
        saveMealBtnSelector:     '#saveAddMealItemBtn',
        addMealModalSelector:    '#addMealModal',
        plannedDateInputSelector:'#addMeal_PlannedDate',

        editMealFormSelector:    '#editMealForm',
        updateMealBtnSelector:   '#updateMealBtn',
        removeMealBtnSelector:   '.remove-meal-btn',

        // API endpoints
        updateShoppingItemUrl: '/MealPlan/UpdateShoppingItem',
        addMealItemUrl:        '/MealPlan/AddMealItem',
        updateMealItemUrl:     '/MealPlan/UpdateMealItem',
        removeMealItemUrl:     '/MealPlan/RemoveMealItem',
        getRecipesUrl:         '/MealPlan/GetRecipes'
    };

    let options = {};
    let eventHandlersInitialized = false; // Flag to prevent duplicate

    function getRequestVerificationToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    function displayModalErrors(formSelector, errors, generalSelector) {
        // clear previous
        $(`${formSelector} .text-danger`).text('');
        $(generalSelector).text('');
        if (errors && typeof errors === 'object') {
            $.each(errors, (key, msgs) => {
                const span = $(`${formSelector} #addMeal_${key}_Error`);
                if (span.length) {
                    span.text(msgs.join(', '));
                } else {
                    $(generalSelector).append(`<div>${msgs.join(', ')}</div>`);
                }
            });
        } else if (errors) {
            $(generalSelector).text(errors);
        }
    }

    const initDatepickers = function() {
        if ($.fn.datepicker) {
            $('.datepicker').each(function() {
                if (!$(this).hasClass('hasDatepicker')) {
                    $(this).datepicker({
                        dateFormat: 'dd/mm/yy',
                        changeMonth: true,
                        changeYear: true,
                        firstDay: 1,
                        autoclose: true,
                        todayHighlight: true,
                        showOtherMonths: true,
                        selectOtherMonths: true,
                        beforeShow: function(input, inst) {
                            // Ensure proper positioning relative to the input
                            setTimeout(function() {
                                inst.dpDiv.css({
                                    top: $(input).offset().top + $(input).outerHeight() + 5,
                                    left: $(input).offset().left
                                });
                            }, 0);
                        }
                    });
                }
            });
        }
    };

    const initRecipeSearch = function() {
        const $el = $(options.recipeSearchSelector);
        if ($el.length && $.fn.select2 && !$el.data('select2')) { 
            $el.select2({
                dropdownParent: $(options.addMealModalSelector),
                placeholder:    'Search for a recipe...',
                allowClear:     true,
                minimumInputLength: 1,
                ajax: {
                    url: options.getRecipesUrl,
                    dataType: 'json',
                    delay: 250,
                    data: params => ({ term: params.term || '' }),
                    processResults: data => ({
                        results: data.map(item => ({ id: item.id, text: item.text }))
                    }),
                    cache: true
                }
            });
        }
    };

    const initShoppingList = function() {
        if (!$('.shopping-item').length) {
            return;
        }

        const token = getRequestVerificationToken();
        // toggle single
        $(document).off('change', options.shoppingItemCheckboxSelector).on('change', options.shoppingItemCheckboxSelector, function() {
            const checkbox   = $(this);
            const isChecked  = checkbox.prop('checked');
            const itemId     = checkbox.data('item-id');
            const mealPlanId = checkbox.data('meal-plan-id');
            const item       = checkbox.closest(options.shoppingItemSelector);

            // optimistic UI
            if (isChecked) {
                item.addClass('checked');
                updateCheckedCount(1);
            } else {
                item.removeClass('checked');
                updateCheckedCount(-1);
            }

            // persist
            $.ajax({
                url:    options.updateShoppingItemUrl,
                method: 'POST',
                data:   { itemId, isChecked, mealPlanId },
                headers:{ 'RequestVerificationToken': token },
                error: function() {
                    // revert
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

        $(document).off('click', options.clearCheckedItemsSelector).on('click', options.clearCheckedItemsSelector, function(e) {
            e.preventDefault();
            $(`${options.shoppingItemCheckboxSelector}:checked`).each(function() {
                $(this).prop('checked', false).trigger('change');
            });
        });

        // filter
        $(document).off('input', options.searchItemsSelector).on('input', options.searchItemsSelector, function() {
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
            $('.shopping-category').each(function() {
                const hasVisible = $(this)
                    .find(`${options.shoppingItemSelector}:visible`)
                    .length > 0;
                $(this).toggle(hasVisible);
            });
        });
    };

    const setupMealPlanEventListeners = function() {
        if (eventHandlersInitialized) {
            return; 
        }

        const token = getRequestVerificationToken();

        // Add Meal via modal form
        $(document).off('submit', options.addMealFormSelector).on('submit', options.addMealFormSelector, function(e) {
            e.preventDefault();
            console.log("Add meal form submitted");

            $('#addMealGeneralError').text('');
            $(`${options.addMealFormSelector} .text-danger`).text('');

            const form = $(options.addMealFormSelector)[0];
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const data = {
                MealPlanId: $('#addMeal_MealPlanId').val(),
                RecipeId: $(options.recipeSearchSelector).val(),
                PlannedDate: $(options.plannedDateInputSelector).val(),
                MealType: $('#addMeal_MealType').val(),
                Servings: $('#addMeal_Servings').val(),
                Notes: $('#addMeal_Notes').val()
            };

            // Validate required fields on client side
            let hasError = false;
            if (!data.RecipeId) {
                $('#addMeal_RecipeId_Error').text('Please select a recipe');
                hasError = true;
            }

            if (!data.PlannedDate) {
                $('#addMeal_PlannedDate_Error').text('Please select a date');
                hasError = true;
            }

            if (!data.MealType) {
                $('#addMeal_MealType_Error').text('Please select a meal type');
                hasError = true;
            }

            if (hasError) {
                return;
            }

            const submitBtn = $(options.saveMealBtnSelector);
            submitBtn.prop('disabled', true);
            submitBtn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Adding...');

            // Submit form data via AJAX
            $.ajax({
                url: options.addMealItemUrl,
                type: 'POST',
                data: data,
                headers: { 'RequestVerificationToken': token },
                success: function(resp) {
                    if (resp.success) {
                        window.location.reload();
                    } else {
                        submitBtn.prop('disabled', false);
                        submitBtn.text('Add Meal');
                        displayModalErrors(
                            options.addMealFormSelector,
                            resp.errors || resp.message,
                            '#addMealGeneralError'
                        );
                    }
                },
                error: function(xhr) {
                    submitBtn.prop('disabled', false);
                    submitBtn.text('Add Meal');
                    let msg = 'An unexpected error occurred.';
                    try {
                        msg = JSON.parse(xhr.responseText).message || msg;
                    } catch(e) { /* Ignore parsing error */ }
                    $('#addMealGeneralError').text(msg);
                }
            });
        });

        // Button click handler for saving meal - Use once handler to prevent multiple bindings
        $(document).off('click', options.saveMealBtnSelector).on('click', options.saveMealBtnSelector, function() {
            $(options.addMealFormSelector).submit();
        });

        // reset form when opening
        $(options.addMealModalSelector).off('show.bs.modal').on('show.bs.modal', function() {
            $(`${options.addMealFormSelector} .text-danger`).text('');
            $('#addMealGeneralError').text('');
            $(options.recipeSearchSelector).val(null).trigger('change');

            const today = new Date();
            const formattedDate = today.getDate()  + '/' + (today.getMonth() + 1) + '/' + today.getFullYear();
            $(options.plannedDateInputSelector).val(formattedDate);

            setTimeout(function() {
                initDatepickers();
            }, 300);
        });

        $('#editMealModal').off('show.bs.modal').on('show.bs.modal', function(event) {
            const btn   = $(event.relatedTarget);
            const modal = $(this);
            modal.find('#EditMealId').val(btn.data('meal-id'));
            modal.find('#EditMealPlanId').val(btn.data('meal-plan-id'));
            modal.find('#EditRecipeId').val(btn.data('recipe-id'));
            modal.find('#EditRecipeTitle').val(btn.data('recipe-title'));
            modal.find('#EditPlannedDate').val(btn.data('planned-date'));
            modal.find('#EditMealType').val(btn.data('meal-type'));
            modal.find('#EditServings').val(btn.data('servings'));
            modal.find('#EditNotes').val(btn.data('notes'));

            setTimeout(function() {
                initDatepickers();
            }, 300);
        });

        $(document).off('click', options.updateMealBtnSelector).on('click', options.updateMealBtnSelector, function() {
            const form = $(options.editMealFormSelector)[0];
            if (!form.checkValidity()) {
                form.reportValidity();
                return;
            }

            const updateBtn = $(this);
            updateBtn.prop('disabled', true);
            updateBtn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Updating...');

            const data = {
                Id:          $('#EditMealId').val(),
                MealPlanId:  $('#EditMealPlanId').val(),
                RecipeId:    $('#EditRecipeId').val(),
                PlannedDate: $('#EditPlannedDate').val(),
                MealType:    $('#EditMealType').val(),
                Servings:    $('#EditServings').val(),
                Notes:       $('#EditNotes').val()
            };

            $.ajax({
                url:     options.updateMealItemUrl,
                type:    'POST',
                data:    data,
                headers: { 'RequestVerificationToken': token },
                success: function(resp) {
                    if (resp.success) {
                        window.location.reload();
                    } else {
                        updateBtn.prop('disabled', false);
                        updateBtn.text('Save Changes');
                        alert('Error: ' + (resp.message || (resp.errors ? resp.errors.join('\n') : 'Failed to update meal.')));
                    }
                },
                error: function() {
                    updateBtn.prop('disabled', false);
                    updateBtn.text('Save Changes');
                    alert('Error: Failed to update meal. Please try again.');
                }
            });
        });

        $(document).off('click', options.removeMealBtnSelector).on('click', options.removeMealBtnSelector, function() {
            const btn         = $(this);
            const itemId      = btn.data('meal-id');
            const mealPlanId  = btn.data('meal-plan-id');
            const recipeTitle = btn.data('recipe-title');

            if (confirm(`Remove "${recipeTitle}" from this plan?`)) {
                btn.prop('disabled', true);

                $.ajax({
                    url:     options.removeMealItemUrl,
                    type:    'POST',
                    data:    { itemId, mealPlanId },
                    headers: { 'RequestVerificationToken': token },
                    success: function(resp) {
                        if (resp.success) {
                            $(`#meal-item-${itemId}`)
                                .fadeOut(300, function() {
                                    $(this).remove();
                                    // clean up empty days
                                    $('.meal-day').each(function() {
                                        if (!$(this).find('.meal-item').length) {
                                            $(this).remove();
                                        }
                                    });
                                    // if none left, reload page
                                    if (!$('.meal-item').length) {
                                        window.location.reload();
                                    }
                                });
                        } else {
                            btn.prop('disabled', false);
                            alert('Error: ' + (resp.message || 'Failed to remove meal.'));
                        }
                    },
                    error: function() {
                        btn.prop('disabled', false);
                        alert('Error: Failed to remove meal. Please try again.');
                    }
                });
            }
        });

        eventHandlersInitialized = true;
    };

    const updateCheckedCount = function(change) {
        const el = $(options.checkedItemsCountSelector);
        if (!el.length) return;
        el.text(parseInt(el.text() || '0', 10) + change);
    };

    const init = function(config = {}) {
        console.log("Initializing MealPlannerModule...");
        options = $.extend({}, defaults, config);

        if ($('#addMealModal').length || $('.meal-day').length || $('.shopping-item').length) {
            initDatepickers();
            initRecipeSearch();
            initShoppingList();
            setupMealPlanEventListeners();
            console.log("MealPlannerModule initialized successfully");
        } else {
            console.log("MealPlannerModule skipped: not on a meal planner page");
        }
    };

    return { init };
})();

window.MealPlannerModule = MealPlannerModule;