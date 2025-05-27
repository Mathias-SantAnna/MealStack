const BulkActions = {
    config: {
        containerSelector: '#bulk-actions-panel',
        tableSelector: '#management-table',
        selectAllSelector: '#select-all-items',
        itemCheckboxSelector: '.item-checkbox',
        actionFormSelector: '#bulk-action-form',
        selectedCountSelector: '#selected-count',
        bulkActionUrl: '/api/bulk-action', 
        confirmDestructive: true
    },

    state: {
        selectedItems: new Set(),
        isSelectingAll: false,
        currentAction: null
    },

    init: function(customConfig = {}) {
        this.config = { ...this.config, ...customConfig };
        this.setupEventListeners();
        this.updateUI();
    },

    setupEventListeners: function() {
        $(document).on('change', this.config.selectAllSelector, (e) => {
            this.handleSelectAll(e.target.checked);
        });

        $(document).on('change', this.config.itemCheckboxSelector, (e) => {
            this.handleItemSelect(e.target);
        });

        $(document).on('click', `${this.config.tableSelector} tbody tr`, (e) => {
            if (!$(e.target).is('input, button, a, .btn')) {
                const checkbox = $(e.currentTarget).find(this.config.itemCheckboxSelector);
                if (checkbox.length) {
                    checkbox.prop('checked', !checkbox.prop('checked')).trigger('change');
                }
            }
        });

        $(document).on('change', '#bulk-action-select', (e) => {
            this.handleActionChange(e.target.value);
        });

        $(document).on('click', '#execute-bulk-action', () => {
            this.executeBulkAction();
        });

        $(document).on('click', '#clear-selection', () => {
            this.clearSelection();
        });
    },

    handleSelectAll: function(isChecked) {
        this.state.isSelectingAll = isChecked;

        $(this.config.itemCheckboxSelector).each((index, checkbox) => {
            const $checkbox = $(checkbox);
            const itemId = $checkbox.val();

            $checkbox.prop('checked', isChecked);

            if (isChecked) {
                this.state.selectedItems.add(itemId);
                $checkbox.closest('tr').addClass('table-primary');
            } else {
                this.state.selectedItems.delete(itemId);
                $checkbox.closest('tr').removeClass('table-primary');
            }
        });

        this.updateUI();
    },

    handleItemSelect: function(checkbox) {
        const $checkbox = $(checkbox);
        const itemId = $checkbox.val();
        const isChecked = $checkbox.prop('checked');

        if (isChecked) {
            this.state.selectedItems.add(itemId);
            $checkbox.closest('tr').addClass('table-primary');
        } else {
            this.state.selectedItems.delete(itemId);
            $checkbox.closest('tr').removeClass('table-primary');
        }

        this.updateSelectAllState();
        this.updateUI();
    },

    updateSelectAllState: function() {
        const totalCheckboxes = $(this.config.itemCheckboxSelector).length;
        const selectedCount = this.state.selectedItems.size;
        const $selectAll = $(this.config.selectAllSelector);

        if (selectedCount === 0) {
            $selectAll.prop('indeterminate', false).prop('checked', false);
        } else if (selectedCount === totalCheckboxes) {
            $selectAll.prop('indeterminate', false).prop('checked', true);
        } else {
            $selectAll.prop('indeterminate', true).prop('checked', false);
        }
    },

    handleActionChange: function(action) {
        this.state.currentAction = action;
        this.showActionOptions(action);
    },

    showActionOptions: function(action) {
        const $optionsContainer = $('#bulk-action-options');

        if (!action) {
            $optionsContainer.hide().empty();
            return;
        }

        let optionsHtml = '';

        switch (action) {
            case 'assignCategory':
                optionsHtml = this.getCategoryAssignmentOptions();
                break;
            case 'changeDifficulty':
                optionsHtml = this.getDifficultyOptions();
                break;
            case 'setMeasurement':
                optionsHtml = this.getMeasurementOptions();
                break;
            case 'export':
                optionsHtml = this.getExportOptions();
                break;
            case 'delete':
                optionsHtml = this.getDeleteOptions();
                break;
            default:
                optionsHtml = '<p class="text-muted">No additional options required.</p>';
        }

        $optionsContainer.html(optionsHtml).show();
    },

    getCategoryAssignmentOptions: function() {
        const categories = window.availableCategories || [];

        return `
            <div class="mb-3">
                <label for="bulk-category" class="form-label">Select Category</label>
                <select class="form-select" id="bulk-category" required>
                    <option value="">Choose category...</option>
                    ${categories.map(cat =>
            `<option value="${cat.id}">${cat.name}</option>`
        ).join('')}
                </select>
            </div>
            <div class="form-check">
                <input class="form-check-input" type="checkbox" id="create-new-category">
                <label class="form-check-label" for="create-new-category">
                    Or create new category
                </label>
            </div>
            <div id="new-category-input" class="mt-2" style="display: none;">
                <input type="text" class="form-control" id="new-category-name" 
                       placeholder="Enter new category name">
            </div>
        `;
    },

    getDifficultyOptions: function() {
        return `
            <div class="mb-3">
                <label for="bulk-difficulty" class="form-label">Select Difficulty</label>
                <select class="form-select" id="bulk-difficulty" required>
                    <option value="">Choose difficulty...</option>
                    <option value="Easy">Easy</option>
                    <option value="Medium">Medium</option>
                    <option value="Hard">Hard</option>
                </select>
            </div>
        `;
    },

    getMeasurementOptions: function() {
        const measurements = ['cup', 'tbsp', 'tsp', 'oz', 'lb', 'g', 'kg', 'ml', 'l'];

        return `
            <div class="mb-3">
                <label for="bulk-measurement" class="form-label">Default Measurement</label>
                <select class="form-select" id="bulk-measurement" required>
                    <option value="">Choose measurement...</option>
                    ${measurements.map(unit =>
            `<option value="${unit}">${unit}</option>`
        ).join('')}
                </select>
            </div>
        `;
    },

    getExportOptions: function() {
        return `
            <div class="mb-3">
                <label for="export-format" class="form-label">Export Format</label>
                <select class="form-select" id="export-format" required>
                    <option value="">Choose format...</option>
                    <option value="csv">CSV</option>
                    <option value="json">JSON</option>
                    <option value="pdf">PDF</option>
                </select>
            </div>
            <div class="form-check">
                <input class="form-check-input" type="checkbox" id="include-usage-data" checked>
                <label class="form-check-label" for="include-usage-data">
                    Include usage statistics
                </label>
            </div>
        `;
    },

    getDeleteOptions: function() {
        return `
            <div class="alert alert-warning">
                <i class="bi bi-exclamation-triangle me-2"></i>
                <strong>Warning:</strong> This action cannot be undone. 
                Items currently used in recipes will be preserved.
            </div>
            <div class="form-check">
                <input class="form-check-input" type="checkbox" id="confirm-delete" required>
                <label class="form-check-label" for="confirm-delete">
                    I understand this action cannot be undone
                </label>
            </div>
        `;
    },

    executeBulkAction: function() {
        if (this.state.selectedItems.size === 0) {
            this.showMessage('Please select at least one item.', 'warning');
            return;
        }

        if (!this.state.currentAction) {
            this.showMessage('Please select an action.', 'warning');
            return;
        }

        const actionData = this.gatherActionData();

        if (!actionData) {
            return; 
        }

        if (this.config.confirmDestructive &&
            ['delete'].includes(this.state.currentAction)) {
            const confirmMessage = `Are you sure you want to ${this.state.currentAction} ${this.state.selectedItems.size} items?`;
            if (!confirm(confirmMessage)) {
                return;
            }
        }

        this.performBulkAction(actionData);
    },

    gatherActionData: function() {
        const baseData = {
            action: this.state.currentAction,
            selectedIds: Array.from(this.state.selectedItems),
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        };

        switch (this.state.currentAction) {
            case 'assignCategory':
                const categoryId = $('#bulk-category').val();
                const newCategoryName = $('#new-category-name').val();

                if (!categoryId && !newCategoryName) {
                    this.showMessage('Please select or enter a category.', 'warning');
                    return null;
                }

                return {
                    ...baseData,
                    categoryId: categoryId || null,
                    newCategoryName: newCategoryName || null
                };

            case 'changeDifficulty':
                const difficulty = $('#bulk-difficulty').val();
                if (!difficulty) {
                    this.showMessage('Please select a difficulty level.', 'warning');
                    return null;
                }
                return { ...baseData, difficulty };

            case 'setMeasurement':
                const measurement = $('#bulk-measurement').val();
                if (!measurement) {
                    this.showMessage('Please select a measurement unit.', 'warning');
                    return null;
                }
                return { ...baseData, measurement };

            case 'export':
                const format = $('#export-format').val();
                if (!format) {
                    this.showMessage('Please select an export format.', 'warning');
                    return null;
                }
                return {
                    ...baseData,
                    format,
                    includeUsageData: $('#include-usage-data').prop('checked')
                };

            case 'delete':
                if (!$('#confirm-delete').prop('checked')) {
                    this.showMessage('Please confirm the deletion.', 'warning');
                    return null;
                }
                return baseData;

            default:
                return baseData;
        }
    },

    performBulkAction: function(actionData) {
        const $executeBtn = $('#execute-bulk-action');
        const originalText = $executeBtn.text();

        $executeBtn.prop('disabled', true)
            .html('<i class="bi bi-arrow-clockwise spin me-1"></i>Processing...');

        if (actionData.action === 'export') {
            this.handleExport(actionData);
            return;
        }

        $.ajax({
            url: this.config.bulkActionUrl,
            method: 'POST',
            data: actionData,
            success: (response) => {
                if (response.success) {
                    this.showMessage(response.message || 'Action completed successfully.', 'success');
                    this.clearSelection();

                    setTimeout(() => {
                        window.location.reload();
                    }, 1500);
                } else {
                    this.showMessage(response.message || 'Action failed.', 'danger');
                }
            },
            error: (xhr, status, error) => {
                console.error('Bulk action failed:', error);
                this.showMessage('An error occurred while processing the action.', 'danger');
            },
            complete: () => {
                $executeBtn.prop('disabled', false).text(originalText);
            }
        });
    },

    handleExport: function(actionData) {
        const params = new URLSearchParams(actionData);
        const exportUrl = `${this.config.bulkActionUrl}?${params.toString()}`;

        const link = document.createElement('a');
        link.href = exportUrl;
        link.download = `export_${Date.now()}.${actionData.format}`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        this.showMessage('Export started. Download should begin shortly.', 'info');
        $('#execute-bulk-action').prop('disabled', false).text('Execute Action');
    },

    clearSelection: function() {
        this.state.selectedItems.clear();
        $(this.config.itemCheckboxSelector).prop('checked', false);
        $(this.config.selectAllSelector).prop('checked', false).prop('indeterminate', false);
        $(`${this.config.tableSelector} tbody tr`).removeClass('table-primary');
        $('#bulk-action-select').val('');
        $('#bulk-action-options').hide().empty();
        this.state.currentAction = null;
        this.updateUI();
    },

    updateUI: function() {
        const selectedCount = this.state.selectedItems.size;
        const $panel = $(this.config.containerSelector);
        const $selectedCount = $(this.config.selectedCountSelector);

        $selectedCount.text(selectedCount);

        if (selectedCount > 0) {
            $panel.slideDown();
        } else {
            $panel.slideUp();
        }

        const hasAction = this.state.currentAction && selectedCount > 0;
        $('#execute-bulk-action').prop('disabled', !hasAction);
    },

    showMessage: function(message, type = 'info') {
        const alertClass = `alert-${type}`;
        const icon = {
            success: 'bi-check-circle',
            danger: 'bi-exclamation-triangle',
            warning: 'bi-exclamation-triangle',
            info: 'bi-info-circle'
        }[type] || 'bi-info-circle';

        const alertHtml = `
            <div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                <i class="bi ${icon} me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        const $container = $('.container').first();
        $container.prepend(alertHtml);

        setTimeout(() => {
            $('.alert').not('.alert-permanent').fadeOut();
        }, 5000);
    }
};

$(document).ready(() => {
    if (window.bulkActionsConfig) {
        BulkActions.init(window.bulkActionsConfig);
    }
});

window.BulkActions = BulkActions;