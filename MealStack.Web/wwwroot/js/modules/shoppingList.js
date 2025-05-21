const ShoppingListModule = (function() {
    // Private variables
    let options = {
        checkboxSelector: '.shopping-item-checkbox',
        itemSelector: '.shopping-item',
        searchInputSelector: '#searchItems',
        clearCheckedButtonSelector: '#clearCheckedItems',
        checkedItemsCountSelector: '#checkedItemsCount',
        copyButtonSelector: '#copyToClipboard',
        saveButtonSelector: '#saveShoppingItemBtn',
        formSelector: '#addShoppingItemForm',
        updateShoppingItemUrl: '',
        addShoppingItemUrl: ''
    };

    const init = function(config) {
        // Merge configuration with defaults
        options = {...options, ...config};

        setupCheckboxHandlers();
        setupClearCheckedHandler();
        setupSearchHandler();
        setupCopyToClipboardHandler();
        setupAddItemHandler();

        updateCheckedCount();

        console.log("Shopping List Module initialized");
    };

    const setupCheckboxHandlers = function() {
        $(document).on('change', options.checkboxSelector, function() {
            const checkbox = $(this);
            const isChecked = checkbox.prop('checked');
            const itemId = checkbox.data('item-id');
            const mealPlanId = checkbox.data('meal-plan-id');
            const item = checkbox.closest(options.itemSelector);

            // Optimistic update for better UX
            if (isChecked) {
                item.addClass('checked');
            } else {
                item.removeClass('checked');
            }
            updateCheckedCount();

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
                    } else {
                        item.removeClass('checked');
                    }
                    updateCheckedCount();
                    alert('Error updating item. Please try again.');
                }
            });
        });
    };

    const setupClearCheckedHandler = function() {
        $(document).on('click', options.clearCheckedButtonSelector, function(e) {
            e.preventDefault();
            $(`${options.checkboxSelector}:checked`).each(function() {
                $(this).prop('checked', false).trigger('change');
            });
        });
    };

    const setupSearchHandler = function() {
        $(document).on('input', options.searchInputSelector, function() {
            const searchTerm = $(this).val().toLowerCase().trim();

            if (searchTerm === '') {
                $(options.itemSelector).show();
                $('.shopping-category').show();
                return;
            }

            $(options.itemSelector).each(function() {
                const itemName = $(this).data('item-name').toLowerCase();
                const matches = itemName.includes(searchTerm);
                $(this).toggle(matches);
            });

            $('.shopping-category').each(function() {
                const hasVisibleItems = $(this).find(`${options.itemSelector}:visible`).length > 0;
                $(this).toggle(hasVisibleItems);
            });
        });
    };

    const setupCopyToClipboardHandler = function() {
        $(document).on('click', options.copyButtonSelector, function() {
            let listText = '';
            $('.shopping-category:visible').each(function() {
                const categoryName = $(this).find('.shopping-category-header').text();
                let categoryHasItems = false;
                let categoryText = categoryName + ':\n';

                $(this).find(`${options.itemSelector}:visible`).each(function() {
                    if (!$(this).hasClass('checked')) {
                        const quantity = $(this).find('.shopping-item-quantity').text().trim();
                        const name = $(this).data('item-name');
                        categoryText += `  • ${quantity} ${name}\n`;
                        categoryHasItems = true;
                    }
                });

                if(categoryHasItems){
                    listText += categoryText + '\n';
                }
            });

            if (listText.trim() === '') {
                alert('No unchecked items to copy.');
                return;
            }

            navigator.clipboard.writeText(listText.trim())
                .then(() => {
                    const originalText = $(this).html();
                    $(this).html('<i class="bi bi-check-lg"></i> Copied!');
                    setTimeout(() => {
                        $(this).html(originalText);
                    }, 2000);
                })
                .catch(err => {
                    console.error('Failed to copy: ', err);
                    alert('Failed to copy to clipboard. Please try again.');
                });
        });
    };

    const setupAddItemHandler = function() {
        $(document).on('click', options.saveButtonSelector, function() {
            const form = $(options.formSelector);
            if (!form[0].checkValidity()) {
                form[0].reportValidity();
                return;
            }

            const formData = {
                MealPlanId: $('#AddItemMealPlanId').val(),
                IngredientName: $('#ItemName').val(),
                Quantity: $('#ItemQuantity').val(),
                Unit: $('#ItemUnit').val(),
                Category: $('#ItemCategory').val()
            };

            $.ajax({
                url: options.addShoppingItemUrl,
                type: 'POST',
                data: formData,
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function(response) {
                    if (response.success) {
                        window.location.reload();
                    } else {
                        alert('Error: ' + (response.message || (response.errors ? response.errors.join('\n') : 'Failed to add item. Please try again.')));
                    }
                },
                error: function() {
                    alert('Error: Failed to add item. Please try again.');
                }
            });
        });
    };

    const updateCheckedCount = function() {
        const checkedCount = $(`${options.checkboxSelector}:checked`).length;
        $(options.checkedItemsCountSelector).text(checkedCount);

        if (checkedCount > 0) {
            $(options.clearCheckedButtonSelector).prop('disabled', false);
        } else {
            $(options.clearCheckedButtonSelector).prop('disabled', true);
        }
    };

    return {
        init: init
    };
})();

window.ShoppingListModule = ShoppingListModule;