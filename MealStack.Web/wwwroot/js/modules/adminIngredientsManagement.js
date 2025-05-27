const AdminIngredientsManagement = (function() {
    let options = {
        searchFormSelector: '#searchFilterForm',
        searchInputSelector: '#searchTerm',
        clearSearchSelector: '#clearSearch',
        resetFiltersSelector: '#resetFilters',
        sortSelectSelector: '#sortBy',
        categorySelectSelector: '#categoryFilter',
        measurementSelectSelector: '#measurementFilter',
        authorSelectSelector: '#authorFilter',
        hasDescriptionSelectSelector: '#hasDescriptionFilter',

        // Bulk actions
        selectAllSelector: '#selectAllItems, #selectAllHeader',
        bulkPanelSelector: '#bulkActionsPanel',
        selectedCountSelector: '#selectedCount',
        clearBulkSelectionSelector: '#clearBulkSelection',
        bulkActionFormSelector: '#bulkActionForm',
        bulkActionSelectSelector: '#bulkAction',
        bulkActionOptionsSelector: '#bulkActionOptions',
        itemCheckboxSelector: '.item-checkbox',

        deleteButtonSelector: '.delete-item-btn',
        deleteFormPrefix: 'delete-form-',

        manageUrl: '/Ingredient/ManageIngredients',
        bulkActionUrl: '/Ingredient/BulkAction',
        searchIngredientsUrl: '/Ingredient/SearchIngredients'
    };

    let selectedItems = new Set();
    let categories = [];
    let measurements = [];

    const init = function(config = {}) {
        console.log("AdminIngredientsManagement initializing...");

        // Merge config with defaults
        options = { ...options, ...config };

        if (typeof $ === 'undefined') {
            console.error("jQuery is required for AdminIngredientsManagement");
            return;
        }

        setupSearch();
        setupBulkActions();
        setupDeleteButtons();
        setupAdvancedFilters();
        setupAutocomplete();

        console.log("AdminIngredientsManagement initialized successfully");
    };

    const setupAutocomplete = function() {
        if (typeof $.ui !== 'undefined' && $.ui.autocomplete) {
            $(options.searchInputSelector).autocomplete({
                source: function(request, response) {
                    $.ajax({
                        url: options.searchIngredientsUrl,
                        type: 'GET',
                        data: { term: request.term },
                        success: function(data) {
                            response($.map(data, function(item) {
                                return {
                                    label: item.name,
                                    value: item.name
                                };
                            }));
                        },
                        error: function() {
                            response([]);
                        }
                    });
                },
                minLength: 2,
                select: function(event, ui) {
                    $(this).val(ui.item.value);
                    $(options.searchFormSelector).submit();
                    return false;
                }
            });
        } else {
            console.warn("jQuery UI Autocomplete not available for ingredients search.");
        }
    };

    const setupSearch = function() {
        const searchForm = $(options.searchFormSelector);
        const searchInput = $(options.searchInputSelector);
        const clearBtn = $(options.clearSearchSelector);
        const resetBtn = $(options.resetFiltersSelector);

        // Real-time search with debounce
        let searchTimeout;
        searchInput.on('input', function() {
            clearTimeout(searchTimeout);
            const searchTerm = $(this).val().trim();

            if (searchTerm.length >= 2 || searchTerm.length === 0) {
                searchTimeout = setTimeout(() => {
                    searchForm.submit();
                }, 500);
            }
        });

        clearBtn.on('click', function() {
            searchInput.val('').focus();
            searchForm.submit();
        });

        resetBtn.on('click', function() {
            searchForm[0].reset();
            window.location.href = options.manageUrl;
        });

        searchForm.on('submit', function() {
            const submitButton = $(this).find('button[type="submit"]');
            const originalText = submitButton.html();

            submitButton.prop('disabled', true)
                .html('<span class="spinner-border spinner-border-sm me-1"></span>Searching...');

            return true;
        });

        console.log("Search functionality setup complete");
    };

    const setupBulkActions = function() {
        const selectAllCheckbox = $(options.selectAllSelector);
        const bulkPanel = $(options.bulkPanelSelector);
        const selectedCountBadge = $(options.selectedCountSelector);
        const clearSelectionBtn = $(options.clearBulkSelectionSelector);
        const bulkActionForm = $(options.bulkActionFormSelector);
        const bulkActionSelect = $(options.bulkActionSelectSelector);

        selectAllCheckbox.on('change', function() {
            const isChecked = this.checked;
            $(options.itemCheckboxSelector).prop('checked', isChecked);
            selectAllCheckbox.prop('checked', isChecked);
            updateBulkSelection();
        });

        $(document).on('change', options.itemCheckboxSelector, function() {
            updateBulkSelection();
        });

        clearSelectionBtn.on('click', function() {
            $(options.itemCheckboxSelector + ', ' + options.selectAllSelector).prop('checked', false);
            updateBulkSelection();
        });

        bulkActionForm.on('submit', function(e) {
            e.preventDefault();
            executeBulkAction();
        });

        bulkActionSelect.on('change', function() {
            showBulkActionOptions($(this).val());
        });

        console.log("Bulk actions setup complete");
    };

    const updateBulkSelection = function() {
        const selectedCheckboxes = $(options.itemCheckboxSelector + ':checked');
        const selectedCount = selectedCheckboxes.length;
        const totalCheckboxes = $(options.itemCheckboxSelector).length;

        selectedItems.clear();
        selectedCheckboxes.each(function() {
            selectedItems.add($(this).val());
        });

        $(options.selectedCountSelector).text(selectedCount);

        if (selectedCount > 0) {
            $(options.bulkPanelSelector).removeClass('d-none');
        } else {
            $(options.bulkPanelSelector).addClass('d-none');
        }

        const selectAllCheckboxes = $(options.selectAllSelector);
        if (selectedCount === 0) {
            selectAllCheckboxes.prop('indeterminate', false).prop('checked', false);
        } else if (selectedCount === totalCheckboxes) {
            selectAllCheckboxes.prop('indeterminate', false).prop('checked', true);
        } else {
            selectAllCheckboxes.prop('indeterminate', true).prop('checked', false);
        }
    };

    const showBulkActionOptions = function(action) {
        const optionsContainer = $(options.bulkActionOptionsSelector);
        optionsContainer.empty();

        switch(action) {
            case 'assignCategory':
            case 'changeCategory':
                optionsContainer.html(`
                    <label class="form-label">Category Name</label>
                    <input type="text" class="form-control" name="categoryName" 
                           placeholder="Enter category name..." required>
                `);
                break;

            case 'setMeasurement':
                optionsContainer.html(`
                    <label class="form-label">Measurement Unit</label>
                    <select class="form-select" name="measurementUnit" required>
                        <option value="">Select unit...</option>
                        <option value="grams">Grams (g)</option>
                        <option value="kilograms">Kilograms (kg)</option>
                        <option value="milliliters">Milliliters (ml)</option>
                        <option value="liters">Liters (L)</option>
                        <option value="teaspoons">Teaspoons (tsp)</option>
                        <option value="tablespoons">Tablespoons (tbsp)</option>
                        <option value="cups">Cups</option>
                        <option value="pieces">Pieces</option>
                        <option value="units">Units</option>
                        <option value="ounces">Ounces (oz)</option>
                        <option value="pounds">Pounds (lb)</option>
                    </select>
                `);
                break;

            case 'export':
                optionsContainer.html(`
                    <label class="form-label">Export Format</label>
                    <select class="form-select" name="exportFormat" required>
                        <option value="">Select format...</option>
                        <option value="csv">CSV (Excel)</option>
                        <option value="json">JSON</option>
                    </select>
                `);
                break;

            case 'delete':
                optionsContainer.html(`
                    <div class="alert alert-warning mb-0">
                        <i class="bi bi-exclamation-triangle me-2"></i>
                        <strong>Warning:</strong> This will permanently delete selected ingredients.
                        <br><small>Ingredients used in recipes will be preserved.</small>
                    </div>
                `);
                break;

            case 'clearCategory':
                optionsContainer.html(`
                    <div class="alert alert-info mb-0">
                        <i class="bi bi-info-circle me-2"></i>
                        This will clear the category for selected ingredients.
                    </div>
                `);
                break;

            case 'clearMeasurement':
                optionsContainer.html(`
                    <div class="alert alert-info mb-0">
                        <i class="bi bi-info-circle me-2"></i>
                        This will clear the measurement for selected ingredients.
                    </div>
                `);
                break;

            default:
                optionsContainer.empty();
        }
    };

    const executeBulkAction = function() {
        const action = $(options.bulkActionSelectSelector).val();
        const selectedIds = Array.from(selectedItems);

        if (!action || selectedIds.length === 0) {
            showError('Please select an action and at least one ingredient.');
            return;
        }

        if (action === 'delete') {
            const confirmMessage = `Are you sure you want to delete ${selectedIds.length} ingredients?\n\nThis action cannot be undone. Ingredients used in recipes will be preserved.`;
            if (!confirm(confirmMessage)) {
                return;
            }
        }

        // Update hidden field
        $('#selectedIds').val(selectedIds.join(','));

        const executeButton = $('#executeBulkAction');
        const originalText = executeButton.html();
        executeButton.prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Processing...');

        const formData = $(options.bulkActionFormSelector).serialize();

        $.ajax({
            url: options.bulkActionUrl,
            type: 'POST',
            data: formData,
            success: function(response) {
                if (response.success) {
                    showSuccess(response.message || `Successfully processed ${selectedIds.length} ingredients`);
                    setTimeout(() => {
                        window.location.reload();
                    }, 1500);
                } else {
                    showError(response.message || 'Bulk action failed');
                }
            },
            error: function(xhr, status, error) {
                console.error("Bulk action error:", error);
                showError('An error occurred while processing the bulk action. Please try again.');
            },
            complete: function() {
                executeButton.prop('disabled', false).html(originalText);
            }
        });
    };

    const setupDeleteButtons = function() {
        $(document).on('click', options.deleteButtonSelector, function(e) {
            e.preventDefault();

            const button = $(this);
            const itemId = button.data('item-id');
            const itemName = button.data('item-name');

            if (!itemId || !itemName) {
                console.error("Missing ingredient data on delete button");
                showError("Error: Missing ingredient information");
                return;
            }

            const confirmMessage = `Are you sure you want to delete "${itemName}"?\n\nThis action cannot be undone.`;

            if (confirm(confirmMessage)) {
                button.prop('disabled', true).html('<i class="bi bi-hourglass-split"></i>');

                try {
                    const deleteForm = $(`#${options.deleteFormPrefix}${itemId}`);

                    if (deleteForm.length === 0) {
                        throw new Error(`Delete form not found for ingredient ${itemId}`);
                    }

                    deleteForm.submit();
                } catch (error) {
                    console.error("Error submitting delete form:", error);
                    showError("Error deleting ingredient. Please try again.");
                    button.prop('disabled', false).html('<i class="bi bi-trash"></i>');
                }
            }
        });

        console.log("Delete buttons setup complete");
    };

    const setupAdvancedFilters = function() {
        $(options.categorySelectSelector + ', ' + options.measurementSelectSelector + ', ' +
            options.authorSelectSelector + ', ' + options.hasDescriptionSelectSelector + ', ' +
            options.sortSelectSelector).on('change', function() {
            $(options.searchFormSelector).submit();
        });

        console.log("Advanced filters setup complete");
    };

    const showSuccess = function(message) {
        showAlert(message, 'success');
    };

    const showError = function(message) {
        showAlert(message, 'danger');
    };

    const showAlert = function(message, type) {
        $('.admin-alert').remove();

        const alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show admin-alert" 
                 role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
                <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-triangle'} me-2"></i>
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        $('body').append(alertHtml);

        setTimeout(() => {
            $('.admin-alert').fadeOut(400, function() {
                $(this).remove();
            });
        }, 5000);
    };

    const setCategories = function(categoryList) {
        categories = categoryList;
    };

    const setMeasurements = function(measurementList) {
        measurements = measurementList;
    };

    const refreshTable = function() {
        window.location.reload();
    };

    const exportIngredients = function(format = 'csv', selectedOnly = false) {
        const params = new URLSearchParams();
        params.set('format', format);

        if (selectedOnly && selectedItems.size > 0) {
            params.set('ingredientIds', Array.from(selectedItems).join(','));
        }

        const exportUrl = `${options.manageUrl}/Export?${params.toString()}`;
        window.open(exportUrl, '_blank');
    };

    return {
        init: init,
        setCategories: setCategories,
        setMeasurements: setMeasurements,
        refreshTable: refreshTable,
        exportIngredients: exportIngredients,
        showSuccess: showSuccess,
        showError: showError,
        getSelectedItems: () => Array.from(selectedItems)
    };
})();

window.AdminIngredientsManagement = AdminIngredientsManagement;