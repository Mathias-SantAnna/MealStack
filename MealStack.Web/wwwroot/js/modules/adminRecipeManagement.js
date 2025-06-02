const AdminRecipeManagement = (function() {
    let options = {
        searchFormSelector: '#searchFilterForm',
        searchInputSelector: '#searchTerm',
        clearSearchSelector: '#clearSearch',
        resetFiltersSelector: '#resetFilters',
        sortSelectSelector: '#sortBy',
        difficultySelectSelector: '#difficultyFilter',
        timeFilterSelector: '#timeFilter',

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

        manageUrl: '/Admin/ManageRecipes',
        bulkActionUrl: '/Admin/BulkAction'
    };

    let selectedItems = new Set();
    let categories = [];

    const init = function(config = {}) {
        console.log("AdminRecipeManagement initializing...");

        // Merge config with defaults
        options = { ...options, ...config };

        if (typeof $ === 'undefined') {
            console.error("jQuery is required for AdminRecipeManagement");
            return;
        }

        setupSearch();
        setupBulkActions();
        setupDeleteButtons();
        setupAdvancedFilters();

        console.log("AdminRecipeManagement initialized successfully");
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
            searchForm.submit();
        });

        searchForm.on('submit', function() {
            const submitButton = $(this).find('button[type="submit"]');
            const originalText = submitButton.html();

            submitButton.prop('disabled', true)
                .html('<span class="spinner-border spinner-border-sm me-1"></span>Searching...');

            // Let the form submit naturally
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
            case 'addCategories':
            case 'removeCategories':
                if (categories.length > 0) {
                    let categoryOptions = '';
                    categories.forEach(category => {
                        categoryOptions += `<option value="${category.id}">${category.name}</option>`;
                    });

                    optionsContainer.html(`
                        <label class="form-label">Select Categories</label>
                        <select class="form-select" name="categoryIds" multiple required>
                            ${categoryOptions}
                        </select>
                        <small class="form-text text-muted">Hold Ctrl/Cmd to select multiple</small>
                    `);
                } else {
                    optionsContainer.html(`
                        <div class="alert alert-info mb-0">
                            <i class="bi bi-info-circle me-2"></i>
                            No categories available. Create categories first.
                        </div>
                    `);
                }
                break;

            case 'changeDifficulty':
                optionsContainer.html(`
                    <label class="form-label">New Difficulty</label>
                    <select class="form-select" name="newDifficulty" required>
                        <option value="">Select difficulty...</option>
                        <option value="Easy">Easy</option>
                        <option value="Medium">Medium</option>
                        <option value="Masterchef">Masterchef</option>
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
                        <option value="pdf">PDF Report</option>
                    </select>
                    <div class="form-check mt-2">
                        <input class="form-check-input" type="checkbox" name="includeImages" id="includeImages">
                        <label class="form-check-label" for="includeImages">
                            Include recipe images (PDF only)
                        </label>
                    </div>
                `);
                break;

            case 'delete':
                optionsContainer.html(`
                    <div class="alert alert-warning mb-0">
                        <i class="bi bi-exclamation-triangle me-2"></i>
                        <strong>Warning:</strong> This will permanently delete all selected recipes.
                        <br><small>This action cannot be undone. All ratings, favorites, and meal plan references will also be removed.</small>
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
            showError('Please select an action and at least one recipe.');
            return;
        }

        if (action === 'delete') {
            const confirmMessage = `Are you sure you want to delete ${selectedIds.length} recipes?\n\nThis action cannot be undone and will:\n• Remove all selected recipes permanently\n• Delete all ratings and favorites for these recipes\n• Remove them from meal plans\n\nType "DELETE" to confirm:`;

            const userInput = prompt(confirmMessage);
            if (userInput !== 'DELETE') {
                showError('Delete action cancelled. You must type "DELETE" to confirm.');
                return;
            }
        }

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
                    showSuccess(response.message || `Successfully processed ${selectedIds.length} recipes`);

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
                console.error("Missing recipe data on delete button");
                showError("Error: Missing recipe information");
                return;
            }

            const confirmMessage = `Are you sure you want to delete "${itemName}"?\n\nThis action cannot be undone and will:\n• Remove the recipe permanently\n• Delete all ratings and favorites\n• Remove from meal plans`;

            if (confirm(confirmMessage)) {
                button.prop('disabled', true).html('<i class="bi bi-hourglass-split"></i>');

                try {
                    const deleteForm = $(`#${options.deleteFormPrefix}${itemId}`);

                    if (deleteForm.length === 0) {
                        throw new Error(`Delete form not found for recipe ${itemId}`);
                    }

                    deleteForm.submit();
                } catch (error) {
                    console.error("Error submitting delete form:", error);
                    showError("Error deleting recipe. Please try again.");

                    button.prop('disabled', false).html('<i class="bi bi-trash"></i>');
                }
            }
        });

        console.log("Delete buttons setup complete");
    };

    const setupAdvancedFilters = function() {
        $(options.difficultySelectSelector + ', ' + options.timeFilterSelector).on('change', function() {
            $(options.searchFormSelector).submit();
        });

        $(document).on('click', '.category-pill', function(e) {
            e.preventDefault();
            const categoryId = $(this).data('category-id');
            const currentUrl = new URL(window.location);

            if (categoryId) {
                currentUrl.searchParams.set('categoryId', categoryId);
            } else {
                currentUrl.searchParams.delete('categoryId');
            }

            window.location.href = currentUrl.toString();
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

    const refreshTable = function() {
        window.location.reload();
    };

    const exportRecipes = function(format = 'csv', selectedOnly = false) {
        const params = new URLSearchParams();
        params.set('format', format);

        if (selectedOnly && selectedItems.size > 0) {
            params.set('recipeIds', Array.from(selectedItems).join(','));
        }

        const exportUrl = `${options.manageUrl}/Export?${params.toString()}`;
        window.open(exportUrl, '_blank');
    };

    return {
        init: init,
        setCategories: setCategories,
        refreshTable: refreshTable,
        exportRecipes: exportRecipes,
        showSuccess: showSuccess,
        showError: showError,
        getSelectedItems: () => Array.from(selectedItems)
    };
})();

window.AdminRecipeManagement = AdminRecipeManagement;