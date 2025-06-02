const IngredientManager = {
    config: {
        containerSelector: '#ingredients-container',
        addButtonSelector: '#add-ingredient-btn',
        ingredientInputSelector: '#ingredient-search',
        quantityInputSelector: '#ingredient-quantity',
        unitSelectSelector: '#ingredient-unit',
        newIngredientModalSelector: '#addIngredientModal',
        newIngredientBtnSelector: '#add-new-ingredient-btn',
        saveNewIngredientSelector: '#save-new-ingredient',
        ingredientsTextareaSelector: '#Ingredients',
        autocompleteUrl: '/Ingredient/SearchIngredients',
        createIngredientUrl: '/Ingredient/AddIngredientAjax'
    },

    ingredients: [],
    initialized: false,
    isProcessing: false, // ADD THIS - it was missing!
    currentIndex: 0,
    lastClickTime: 0, // ADD THIS for click throttling

    init: function(customConfig = {}) {
        if (this.initialized) {
            console.log("IngredientManager already initialized - SKIPPING");
            return true;
        }

        console.log("IngredientManager initializing...");
        Object.assign(this.config, customConfig);

        if (!$(this.config.containerSelector).length) {
            console.error("Ingredients container not found");
            return false;
        }

        this.setupEventListeners();
        this.loadExistingIngredients();
        this.initializeAutocomplete();
        this.setupModal();

        this.initialized = true;
        console.log("IngredientManager initialized successfully");
        return true;
    },

    setupEventListeners: function() {
        const self = this;

        // Remove ALL existing event listeners to prevent duplicates
        $(document).off('.ingredientManager');
        $(this.config.addButtonSelector).off('.ingredientManager');
        $(this.config.ingredientInputSelector).off('.ingredientManager');
        $(this.config.quantityInputSelector).off('.ingredientManager');

        // Add button click with throttling
        $(document).on('click.ingredientManager', this.config.addButtonSelector, function(e) {
            e.preventDefault();
            e.stopPropagation();

            // Throttle clicks to prevent rapid firing
            if (self.lastClickTime && (Date.now() - self.lastClickTime) < 500) {
                console.log("Ignoring rapid click");
                return;
            }
            self.lastClickTime = Date.now();

            console.log("Add ingredient button clicked");
            self.addIngredient();
        });

        // Remove ingredient button
        $(document).on('click.ingredientManager', '.remove-ingredient-btn', function(e) {
            e.preventDefault();
            e.stopPropagation();
            self.removeIngredient($(this).closest('.ingredient-item'));
        });

        // Enter key handlers
        $(this.config.ingredientInputSelector + ', ' + this.config.quantityInputSelector)
            .on('keypress.ingredientManager', function(e) {
                if (e.which === 13) {
                    e.preventDefault();
                    self.addIngredient();
                }
            });

        // New ingredient modal button
        $(document).on('click.ingredientManager', this.config.newIngredientBtnSelector, function(e) {
            e.preventDefault();
            e.stopPropagation();
            self.openNewIngredientModal();
        });

        console.log("Event listeners setup complete");
    },

    loadExistingIngredients: function() {
        const $container = $(this.config.containerSelector);
        const $textarea = $(this.config.ingredientsTextareaSelector);

        if ($textarea.length && $textarea.val().trim()) {
            const lines = $textarea.val().trim().split('\n');
            this.ingredients = [];

            lines.forEach(line => {
                const match = line.match(/^(\d+(?:\.\d+)?)\s+(\w+)\s+(.+)$/);
                if (match) {
                    this.ingredients.push({
                        quantity: parseFloat(match[1]),
                        unit: match[2],
                        name: match[3],
                        tempId: this.currentIndex++
                    });
                }
            });

            this.renderIngredientsQuiet(); // Use quiet version
        }

        console.log(`Loaded ${this.ingredients.length} ingredients`);
    },

    initializeAutocomplete: function() {
        if (typeof $.ui === 'undefined' || !$.ui.autocomplete) {
            console.warn("jQuery UI autocomplete not available");
            return;
        }

        const self = this;

        // Remove existing autocomplete first
        if ($(this.config.ingredientInputSelector).hasClass('ui-autocomplete-input')) {
            $(this.config.ingredientInputSelector).autocomplete('destroy');
        }

        $(this.config.ingredientInputSelector).autocomplete({
            source: function(request, response) {
                $.ajax({
                    url: self.config.autocompleteUrl,
                    data: { term: request.term },
                    success: function(data) {
                        response(data.map(item => ({
                            label: item.name,
                            value: item.name,
                            measurement: item.measurement
                        })));
                    },
                    error: function() {
                        response([]);
                    }
                });
            },
            minLength: 2,
            select: function(event, ui) {
                $(self.config.ingredientInputSelector).val(ui.item.value);
                if (ui.item.measurement) {
                    $(self.config.unitSelectSelector).val(ui.item.measurement);
                }
                return false;
            }
        });
    },

    addIngredient: function() {
        // Prevent multiple calls during processing
        if (this.isProcessing) {
            console.log("IngredientManager is processing, ignoring duplicate call");
            return;
        }
        this.isProcessing = true;

        try {
            const name = $(this.config.ingredientInputSelector).val().trim();
            const quantity = parseFloat($(this.config.quantityInputSelector).val() || '1');
            const unit = $(this.config.unitSelectSelector).val() || 'cup';

            if (!name) {
                this.showMessage('Please enter an ingredient name.', 'warning');
                return;
            }

            if (isNaN(quantity) || quantity <= 0) {
                this.showMessage('Please enter a valid quantity.', 'warning');
                return;
            }

            if (this.ingredients.some(ing => ing.name.toLowerCase() === name.toLowerCase())) {
                this.showMessage('This ingredient is already added.', 'warning');
                return;
            }

            this.ingredients.push({
                name: name,
                quantity: quantity,
                unit: unit,
                tempId: this.currentIndex++
            });

            // Clear form BEFORE updating display to prevent retriggering
            this.clearIngredientForm();

            // Update display without triggering events
            this.renderIngredientsQuiet();
            this.updateIngredientsTextareaQuiet();

            this.showMessage(`Added ${quantity} ${unit} ${name}`, 'success');

        } catch (error) {
            console.error("Error in addIngredient:", error);
        } finally {
            // Reset processing flag after a short delay
            setTimeout(() => {
                this.isProcessing = false;
            }, 100);
        }
    },

    // QUIET methods that don't trigger events
    renderIngredientsQuiet: function() {
        const $container = $(this.config.containerSelector);
        $container.empty();

        if (this.ingredients.length === 0) {
            $container.html(`
                <div class="text-center text-muted py-4">
                    <i class="bi bi-basket display-6"></i>
                    <p class="mt-2">No ingredients added yet. Add ingredients above to get started!</p>
                </div>
            `);
            return;
        }

        this.ingredients.forEach(ingredient => {
            $container.append(this.createIngredientHTML(ingredient));
        });
    },

    updateIngredientsTextareaQuiet: function() {
        const $textarea = $(this.config.ingredientsTextareaSelector);

        if (!$textarea.length) {
            console.error("Ingredients textarea not found!");
            return;
        }

        const ingredientsText = this.ingredients
            .map(ing => `${ing.quantity} ${ing.unit} ${ing.name}`)
            .join('\n');

        // Update value WITHOUT triggering change events
        $textarea.prop('value', ingredientsText);

        // Hide the textarea (it's for form submission only)
        $textarea.removeClass('d-none').hide();

        console.log(`Updated ingredients textarea with ${this.ingredients.length} ingredients (quiet mode)`);
    },

    removeIngredient: function($element) {
        if (this.isProcessing) {
            console.log("Currently processing, ignoring remove request");
            return;
        }
        this.isProcessing = true;

        try {
            const tempId = $element.data('temp-id');
            const name = $element.find('.ingredient-name').text();

            this.ingredients = this.ingredients.filter(ing => ing.tempId !== tempId);

            $element.fadeOut(300, () => {
                $element.remove();
                this.updateIngredientsTextareaQuiet(); // Use quiet version

                if (this.ingredients.length === 0) {
                    this.renderIngredientsQuiet(); // Use quiet version
                }
            });

            this.showMessage(`Removed ${name}`, 'info');
        } finally {
            setTimeout(() => {
                this.isProcessing = false;
            }, 100);
        }
    },

    // Keep the old methods for backwards compatibility but make them safer
    renderIngredients: function() {
        this.renderIngredientsQuiet();
    },

    createIngredientHTML: function(ingredient) {
        const quantity = ingredient.quantity % 1 === 0 ?
            ingredient.quantity.toString() :
            ingredient.quantity.toFixed(2).replace(/\.?0+$/, '');

        return `
            <div class="ingredient-item card mb-2" data-temp-id="${ingredient.tempId}">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-center">
                        <div class="flex-grow-1">
                            <span class="ingredient-quantity fw-bold text-primary">${quantity}</span>
                            <span class="ingredient-unit text-muted">${ingredient.unit}</span>
                            <span class="ingredient-name">${ingredient.name}</span>
                        </div>
                        <button type="button" class="btn btn-sm btn-outline-danger remove-ingredient-btn" 
                                title="Remove ${ingredient.name}">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
    },

    updateIngredientsTextarea: function() {
        this.updateIngredientsTextareaQuiet();
    },

    clearIngredientForm: function() {
        $(this.config.ingredientInputSelector).val('').focus();
        $(this.config.quantityInputSelector).val('1');
        $(this.config.unitSelectSelector).val('cup');
    },

    setupModal: function() {
        const self = this;
        const $modal = $(this.config.newIngredientModalSelector);

        if (!$modal.length) {
            console.warn("New ingredient modal not found");
            return;
        }

        $(document).off('click.ingredientManager', this.config.saveNewIngredientSelector)
            .on('click.ingredientManager', this.config.saveNewIngredientSelector, function(e) {
                e.preventDefault();
                self.saveNewIngredient();
            });

        $modal.off('hidden.bs.modal.ingredientManager')
            .on('hidden.bs.modal.ingredientManager', function() {
                self.clearNewIngredientForm();
                self.cleanupModal();
            });

        $modal.off('hide.bs.modal.ingredientManager')
            .on('hide.bs.modal.ingredientManager', function() {
                setTimeout(() => {
                    $('.modal-backdrop').remove();
                    $('body').removeClass('modal-open').css('padding-right', '');
                }, 100);
            });
    },

    openNewIngredientModal: function() {
        const $modal = $(this.config.newIngredientModalSelector);

        if (!$modal.length) {
            this.showMessage("Modal not found", 'danger');
            return;
        }

        this.cleanupModal();
        this.clearNewIngredientForm();

        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            const bsModal = bootstrap.Modal.getOrCreateInstance($modal[0]);
            bsModal.show();
        } else {
            $modal.modal('show');
        }
    },

    saveNewIngredient: function() {
        const name = $('#new-ingredient-name').val().trim();
        const category = $('#new-ingredient-category').val();
        const measurement = $('#new-ingredient-measurement').val();
        const description = $('#new-ingredient-description').val().trim();

        if (!name) {
            this.showModalMessage('Please enter an ingredient name.', 'danger');
            return;
        }

        const $saveBtn = $(this.config.saveNewIngredientSelector);
        const originalText = $saveBtn.text();
        $saveBtn.prop('disabled', true).text('Saving...');

        $.ajax({
            url: this.config.createIngredientUrl,
            method: 'POST',
            data: {
                name: name,
                category: category,
                measurement: measurement,
                description: description,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response && response.success) {
                    $(this.config.ingredientInputSelector).val(response.ingredient.name);

                    if (response.ingredient.measurement) {
                        $(this.config.unitSelectSelector).val(response.ingredient.measurement);
                    }

                    this.closeModal();

                    $(this.config.quantityInputSelector).focus();

                    this.showMessage('New ingredient created! Now set the quantity and click Add.', 'success');
                } else {
                    this.showModalMessage(response?.message || 'Failed to create ingredient.', 'danger');
                }
            },
            error: (xhr, status, error) => {
                console.error('Failed to create ingredient:', error);
                this.showModalMessage('Error creating ingredient.', 'danger');
            },
            complete: () => {
                $saveBtn.prop('disabled', false).text(originalText);
            }
        });
    },

    closeModal: function() {
        const $modal = $(this.config.newIngredientModalSelector);

        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
            const bsModal = bootstrap.Modal.getInstance($modal[0]);
            if (bsModal) {
                bsModal.hide();
            }
        } else {
            $modal.modal('hide');
        }

        setTimeout(() => this.cleanupModal(), 300);
    },

    cleanupModal: function() {
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css('padding-right', '');
        $(this.config.newIngredientModalSelector).removeClass('show').css('display', '');
    },

    clearNewIngredientForm: function() {
        $('#new-ingredient-name, #new-ingredient-description').val('');
        $('#new-ingredient-category, #new-ingredient-measurement').val('');
        $('#modal-messages').empty();
    },

    showMessage: function(message, type = 'info') {
        const alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show ingredient-alert" role="alert">
                <i class="bi bi-${type === 'success' ? 'check-circle' : 'info-circle'} me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        $('.ingredient-alert').remove();
        $(this.config.containerSelector).before(alertHtml);

        setTimeout(() => $('.ingredient-alert').fadeOut(), 4000);
    },

    showModalMessage: function(message, type = 'info') {
        $('#modal-messages').html(`
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `);
    },

    getIngredients: function() {
        return [...this.ingredients];
    },

    setIngredients: function(ingredients) {
        this.ingredients = ingredients || [];
        this.renderIngredientsQuiet();
        this.updateIngredientsTextareaQuiet();
    }
};

// Export to global scope - NO automatic initialization
window.IngredientManager = IngredientManager;