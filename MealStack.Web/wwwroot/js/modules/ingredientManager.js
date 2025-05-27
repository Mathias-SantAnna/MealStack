const IngredientManager = {
    config: {
        containerSelector: '#ingredients-container',
        addButtonSelector: '#add-ingredient-btn',
        ingredientInputSelector: '#ingredient-search',
        quantityInputSelector: '#ingredient-quantity',
        unitSelectSelector: '#ingredient-unit',
        newIngredientModalSelector: '#newIngredientModal',
        saveNewIngredientSelector: '#save-new-ingredient',
        autocompleteUrl: '/api/ingredients/search',
        createIngredientUrl: '/api/ingredients/create',
        debounceMs: 300
    },

    state: {
        ingredients: [],
        searchTimeout: null,
        currentIndex: 0
    },

    init: function(customConfig = {}) {
        this.config = { ...this.config, ...customConfig };
        this.setupEventListeners();
        this.loadExistingIngredients();
        this.initializeAutocomplete();
    },

    setupEventListeners: function() {
        $(this.config.addButtonSelector).on('click', () => {
            this.addIngredient();
        });

        $(document).on('click', '.remove-ingredient-btn', (e) => {
            this.removeIngredient(e.target.closest('.ingredient-item'));
        });

        $(this.config.ingredientInputSelector).on('input', (e) => {
            this.handleIngredientSearch(e.target.value);
        });

        $(this.config.ingredientInputSelector).on('keypress', (e) => {
            if (e.which === 13) {
                e.preventDefault();
                this.addIngredient();
            }
        });

        $(this.config.quantityInputSelector).on('keypress', (e) => {
            if (e.which === 13) {
                e.preventDefault();
                this.addIngredient();
            }
        });

        $(this.config.saveNewIngredientSelector).on('click', () => {
            this.saveNewIngredient();
        });

        $(this.config.newIngredientModalSelector).on('hidden.bs.modal', () => {
            this.clearNewIngredientForm();
        });
    },

    loadExistingIngredients: function() {
        const $container = $(this.config.containerSelector);
        const existingData = $container.data('existing-ingredients');

        if (existingData) {
            this.state.ingredients = JSON.parse(existingData);
            this.renderIngredients();
        } else {
            $container.find('.ingredient-item').each((index, element) => {
                const $element = $(element);
                this.state.ingredients.push({
                    id: $element.data('ingredient-id'),
                    name: $element.find('.ingredient-name').text(),
                    quantity: $element.find('.ingredient-quantity').text(),
                    unit: $element.find('.ingredient-unit').text()
                });
            });
        }
    },

    initializeAutocomplete: function() {
        $(this.config.ingredientInputSelector).autocomplete({
            source: (request, response) => {
                clearTimeout(this.state.searchTimeout);
                this.state.searchTimeout = setTimeout(() => {
                    this.fetchIngredientSuggestions(request.term, response);
                }, this.config.debounceMs);
            },
            minLength: 2,
            select: (event, ui) => {
                $(this.config.ingredientInputSelector).val(ui.item.label);
                return false;
            },
            appendTo: 'body',
            classes: {
                'ui-autocomplete': 'custom-autocomplete'
            }
        });
    },

    handleIngredientSearch: function(searchTerm) {
        if (searchTerm.length < 2) return;

        clearTimeout(this.state.searchTimeout);
        this.state.searchTimeout = setTimeout(() => {
            this.fetchIngredientSuggestions(searchTerm, (suggestions) => {
                this.displaySuggestions(suggestions);
            });
        }, this.config.debounceMs);
    },

    fetchIngredientSuggestions: function(term, callback) {
        $.ajax({
            url: this.config.autocompleteUrl,
            method: 'GET',
            data: { term: term },
            success: (data) => {
                const suggestions = data.map(item => ({
                    label: item.name,
                    value: item.name,
                    id: item.id,
                    defaultUnit: item.defaultMeasurement
                }));
                callback(suggestions);
            },
            error: (xhr, status, error) => {
                console.error('Failed to fetch ingredient suggestions:', error);
                callback([]);
            }
        });
    },

    displaySuggestions: function(suggestions) {
    },

    addIngredient: function() {
        const ingredientName = $(this.config.ingredientInputSelector).val().trim();
        const quantity = $(this.config.quantityInputSelector).val().trim();
        const unit = $(this.config.unitSelectSelector).val();

        if (!ingredientName) {
            this.showMessage('Please enter an ingredient name.', 'warning');
            return;
        }

        if (!quantity || isNaN(quantity) || parseFloat(quantity) <= 0) {
            this.showMessage('Please enter a valid quantity.', 'warning');
            return;
        }

        const existingIngredient = this.state.ingredients.find(ing =>
            ing.name.toLowerCase() === ingredientName.toLowerCase()
        );

        if (existingIngredient) {
            this.showMessage('This ingredient is already added to the recipe.', 'warning');
            return;
        }

        const ingredient = {
            id: null, 
            name: ingredientName,
            quantity: parseFloat(quantity),
            unit: unit || 'cup',
            tempId: this.state.currentIndex++
        };

        this.state.ingredients.push(ingredient);

        this.clearIngredientForm();

        this.renderIngredients();

        this.showMessage(`Added ${ingredient.name} to recipe.`, 'success');
    },

    removeIngredient: function(ingredientElement) {
        const $element = $(ingredientElement);
        const tempId = $element.data('temp-id');
        const ingredientName = $element.find('.ingredient-name').text();

        this.state.ingredients = this.state.ingredients.filter(ing => ing.tempId !== tempId);

        $element.fadeOut(300, () => {
            $element.remove();
            this.updateIngredientHiddenFields();
        });

        this.showMessage(`Removed ${ingredientName} from recipe.`, 'info');
    },

    renderIngredients: function() {
        const $container = $(this.config.containerSelector);
        $container.empty();

        if (this.state.ingredients.length === 0) {
            $container.html(`
                <div class="text-center text-muted py-4">
                    <i class="bi bi-inbox display-6"></i>
                    <p class="mt-2">No ingredients added yet.</p>
                </div>
            `);
            return;
        }

        this.state.ingredients.forEach((ingredient, index) => {
            const ingredientHtml = this.createIngredientHTML(ingredient, index);
            $container.append(ingredientHtml);
        });

        this.updateIngredientHiddenFields();
    },

    createIngredientHTML: function(ingredient, index) {
        return `
            <div class="ingredient-item card mb-2" data-temp-id="${ingredient.tempId}" data-ingredient-id="${ingredient.id || ''}">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-center">
                        <div class="flex-grow-1">
                            <div class="d-flex align-items-center">
                                <span class="ingredient-quantity fw-bold me-2">${ingredient.quantity}</span>
                                <span class="ingredient-unit text-muted me-2">${ingredient.unit}</span>
                                <span class="ingredient-name">${ingredient.name}</span>
                            </div>
                        </div>
                        <div class="ingredient-actions">
                            <button type="button" class="btn btn-sm btn-outline-danger remove-ingredient-btn" 
                                    title="Remove ingredient">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>
                    </div>
                </div>
                
                <input type="hidden" name="RecipeIngredients[${index}].IngredientName" value="${ingredient.name}">
                <input type="hidden" name="RecipeIngredients[${index}].Quantity" value="${ingredient.quantity}">
                <input type="hidden" name="RecipeIngredients[${index}].Unit" value="${ingredient.unit}">
                ${ingredient.id ? `<input type="hidden" name="RecipeIngredients[${index}].IngredientId" value="${ingredient.id}">` : ''}
            </div>
        `;
    },

    updateIngredientHiddenFields: function() {
        $('#ingredients-hidden-fields').remove();

        const $hiddenContainer = $('<div id="ingredients-hidden-fields"></div>');

        this.state.ingredients.forEach((ingredient, index) => {
            $hiddenContainer.append(`
                <input type="hidden" name="RecipeIngredients[${index}].IngredientName" value="${ingredient.name}">
                <input type="hidden" name="RecipeIngredients[${index}].Quantity" value="${ingredient.quantity}">
                <input type="hidden" name="RecipeIngredients[${index}].Unit" value="${ingredient.unit}">
                ${ingredient.id ? `<input type="hidden" name="RecipeIngredients[${index}].IngredientId" value="${ingredient.id}">` : ''}
            `);
        });

        $('form').append($hiddenContainer);
    },

    clearIngredientForm: function() {
        $(this.config.ingredientInputSelector).val('').focus();
        $(this.config.quantityInputSelector).val('');
        $(this.config.unitSelectSelector).val('cup');
    },

    saveNewIngredient: function() {
        const $modal = $(this.config.newIngredientModalSelector);
        const ingredientData = {
            name: $modal.find('#new-ingredient-name').val().trim(),
            description: $modal.find('#new-ingredient-description').val().trim(),
            categoryId: $modal.find('#new-ingredient-category').val(),
            defaultMeasurement: $modal.find('#new-ingredient-measurement').val()
        };

        if (!ingredientData.name) {
            this.showMessage('Please enter an ingredient name.', 'warning');
            return;
        }

        const $saveBtn = $(this.config.saveNewIngredientSelector);
        const originalText = $saveBtn.text();

        $saveBtn.prop('disabled', true).text('Saving...');

        $.ajax({
            url: this.config.createIngredientUrl,
            method: 'POST',
            data: ingredientData,
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: (response) => {
                if (response.success) {
                    $(this.config.ingredientInputSelector).val(response.ingredient.name);

                    $modal.modal('hide');

                    this.showMessage('New ingredient created successfully!', 'success');
                } else {
                    this.showMessage(response.message || 'Failed to create ingredient.', 'danger');
                }
            },
            error: (xhr, status, error) => {
                console.error('Failed to create ingredient:', error);
                this.showMessage('An error occurred while creating the ingredient.', 'danger');
            },
            complete: () => {
                $saveBtn.prop('disabled', false).text(originalText);
            }
        });
    },

    clearNewIngredientForm: function() {
        const $modal = $(this.config.newIngredientModalSelector);
        $modal.find('input, textarea, select').val('');
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
            <div class="alert ${alertClass} alert-dismissible fade show ingredient-alert" role="alert">
                <i class="bi ${icon} me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        $(this.config.containerSelector).before(alertHtml);

        setTimeout(() => {
            $('.ingredient-alert').fadeOut();
        }, 3000);
    },

    getIngredients: function() {
        return this.state.ingredients;
    },

    setIngredients: function(ingredients) {
        this.state.ingredients = ingredients;
        this.renderIngredients();
    }
};

$(document).ready(() => {
    if ($('#ingredients-container').length &&
        (window.location.pathname.includes('/Recipe/Create') ||
            window.location.pathname.includes('/Recipe/Edit'))) {
        IngredientManager.init();
    }
});

window.IngredientManager = IngredientManager;