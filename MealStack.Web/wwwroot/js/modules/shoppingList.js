const ShoppingListModule = (function() {
    let options = {
        checkboxSelector: '.shopping-item-checkbox',
        itemSelector: '.shopping-item',
        searchInputSelector: '#searchItems',
        clearCheckedButtonSelector: '#clearCheckedItems',
        checkedItemsCountSelector: '#checkedItemsCount',
        copyButtonSelector: '#copyToClipboard',
        saveButtonSelector: '#saveShoppingItemBtn',
        formSelector: '#addShoppingItemForm',
        progressBarSelector: '.progress-bar',
        updateShoppingItemUrl: '',
        addShoppingItemUrl: ''
    };

    const init = function(config) {
        options = {...options, ...config};

        setupCheckboxHandlers();
        setupClearCheckedHandler();
        setupSearchHandler();
        setupCopyToClipboardHandler();
        setupAddItemHandler();

        updateAllProgress();

        console.log("Shopping List Module initialized with real-time updates");
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

            updateAllProgress();

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
                    checkbox.prop('checked', !isChecked);
                    if (!isChecked) {
                        item.addClass('checked');
                    } else {
                        item.removeClass('checked');
                    }
                    updateAllProgress();
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

    // Master function to update all progress elements
    const updateAllProgress = function() {
        console.log("Updating all progress elements...");
        updateCheckedCount();
        updateOverallProgress();
        updateProgressMessage();
    };

    const updateCheckedCount = function() {
        const checkedCount = $(`${options.checkboxSelector}:checked`).length;
        const totalCount = $(options.checkboxSelector).length;
        const remaining = totalCount - checkedCount;

        console.log(`Progress: ${checkedCount}/${totalCount} checked`);

        $(options.checkedItemsCountSelector).text(checkedCount);

        $('.badge:contains("done")').each(function() {
            $(this).html(`<span id="checkedItemsCount">${checkedCount}</span> done`);
        });

        $('.badge.bg-secondary').text(`${remaining} left`);

        const clearButton = $(options.clearCheckedButtonSelector);
        if (checkedCount > 0) {
            clearButton.prop('disabled', false);
            clearButton.removeClass('disabled');
        } else {
            clearButton.prop('disabled', true);
            clearButton.addClass('disabled');
        }
    };

    // Update the main progress bar
    const updateOverallProgress = function() {
        const totalItems = $(options.checkboxSelector).length;
        const checkedItems = $(`${options.checkboxSelector}:checked`).length;
        const progress = totalItems > 0 ? (checkedItems / totalItems) * 100 : 0;

        console.log(`Progress bar: ${progress.toFixed(1)}%`);

        const progressBar = $(options.progressBarSelector);
        if (progressBar.length) {
            progressBar.css('width', `${progress}%`);
            progressBar.attr('aria-valuenow', progress);

            if (progress < 100) {
                progressBar.addClass('progress-bar-animated');
            } else {
                progressBar.removeClass('progress-bar-animated');
            }
        }

        // Update progress text
        $('.card.bg-light .text-muted').each(function() {
            const text = $(this).text();
            if (text.includes('of') && text.includes('collected')) {
                $(this).text(`${checkedItems} of ${totalItems} collected`);
            }
        });
    };

    const updateProgressMessage = function() {
        const totalItems = $(options.checkboxSelector).length;
        const checkedItems = $(`${options.checkboxSelector}:checked`).length;
        const progress = totalItems > 0 ? (checkedItems / totalItems) * 100 : 0;
        const remaining = totalItems - checkedItems;

        let icon, message, textClass;

        if (progress >= 100) {
            icon = 'bi-check-circle-fill';
            message = 'All done! Ready to shop.';
            textClass = 'text-success';
        } else if (progress >= 75) {
            icon = 'bi-clock';
            message = `Almost finished - ${remaining} items left.`;
            textClass = 'text-primary';
        } else if (progress >= 25) {
            icon = 'bi-arrow-right';
            message = `Making progress - ${remaining} items to go.`;
            textClass = 'text-warning';
        } else {
            icon = 'bi-cart';
            message = `Let's get shopping! ${remaining} items to collect.`;
            textClass = 'text-muted';
        }

        // Update the progress message
        $('.card.bg-light small').each(function() {
            if ($(this).find('i').length > 0 || $(this).text().includes('shop')) {
                $(this).html(`<i class="${icon} ${textClass} me-1"></i><span>${message}</span>`);
            }
        });

        console.log(`Progress message: ${message}`);
    };

    return {
        init: init,
        updateAllProgress: updateAllProgress,
        updateCheckedCount: updateCheckedCount,
        updateOverallProgress: updateOverallProgress,
        updateProgressMessage: updateProgressMessage
    };
})();

window.ShoppingListModule = ShoppingListModule;