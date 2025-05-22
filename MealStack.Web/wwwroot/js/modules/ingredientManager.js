const IngredientModule = (function() {
    // Default config
    let options = {
        // DOM selectors
        containerSelector: '#ingredients-container',
        dataFieldSelector: '#ingredients-data',
        searchFieldSelector: '#ingredient-search',
        addButtonSelector: '#add-ingredient-btn',
        addNewButtonSelector: '#add-new-ingredient-btn',
        modalId: '#addIngredientModal',
        saveModalButtonSelector: '#save-new-ingredient',
        newIngredientNameSelector: '#new-ingredient-name',
        newIngredientCategorySelector: '#new-ingredient-category',
        newIngredientMeasurementSelector: '#new-ingredient-measurement',
        newIngredientDescriptionSelector: '#new-ingredient-description',

        // CSS classes
        ingredientChipClass: 'ingredient-chip',
        quantityInputClass: 'quantity-input',
        unitSelectClass: 'unit-select',
        removeIngredientBtnClass: 'remove-ingredient',

        // Configuration
        parseExisting: false,

        // Data
        measurementUnits: [
            { value: 'grams', label: 'Grams (g)' },
            { value: 'kilograms', label: 'Kilograms (kg)' },
            { value: 'milliliters', label: 'Milliliters (ml)' },
            { value: 'liters', label: 'Liters (L)' },
            { value: 'teaspoons', label: 'Teaspoons (tsp)' },
            { value: 'tablespoons', label: 'Tablespoons (tbsp)' },
            { value: 'cups', label: 'Cups' },
            { value: 'pieces', label: 'Pieces' },
            { value: 'units', label: 'Units' },
            { value: 'ounces', label: 'Ounces (oz)' },
            { value: 'pounds', label: 'Pounds (lb)' }
        ],

        // API endpoints
        searchIngredientsUrl: "/Ingredient/SearchIngredients",
        getAllIngredientsUrl: "/Ingredient/GetAllIngredients",
        addIngredientAjaxUrl: "/Ingredient/AddIngredientAjax"
    };

    let ingredientsList = []; 
    let selectedIngredients = []; 
     
    function addIngredientChip(ingredient) {
        if (selectedIngredients.some(i => i.id === ingredient.id &&
            ingredient.id !== null &&
            !ingredient.id.toString().startsWith('temp'))) {
            return;
        }

        const ingredientWithQuantity = {
            ...ingredient,
            quantity: ingredient.quantity || "",
            unit: ingredient.unit || ingredient.measurement || ""
        };
        selectedIngredients.push(ingredientWithQuantity);

        // Generate the measurement options HTML
        let measurementOptionsHtml = '<option value="">Select unit</option>';
        options.measurementUnits.forEach(unit => {
            const selected = unit.value === (ingredientWithQuantity.unit || ingredient.measurement) ? 'selected' : '';
            measurementOptionsHtml += `<option value="${unit.value}" ${selected}>${unit.label}</option>`;
        });

        // Create the ingredient chip HTML
        const chipHtml = `
            <div class="${options.ingredientChipClass} d-inline-flex align-items-center bg-light rounded p-2 me-2 mb-2" data-id="${ingredientWithQuantity.id}">
                <input type="number" class="${options.quantityInputClass} form-control form-control-sm me-1" style="width: 70px;" placeholder="Qty" aria-label="Quantity" min="0" step="0.01" value="${ingredientWithQuantity.quantity}">
                <select class="${options.unitSelectClass} form-select form-select-sm me-1" style="width: 120px;">
                    ${measurementOptionsHtml}
                </select>
                <span class="ingredient-name me-2">${ingredientWithQuantity.name}</span>
                <button type="button" class="btn-close ${options.removeIngredientBtnClass}" aria-label="Remove" style="font-size: 0.6rem;"></button>
            </div>`;

        $(options.containerSelector).append(chipHtml);
        updateHiddenIngredientsData();
    }

    function removeIngredientChip(id) {
        selectedIngredients = selectedIngredients.filter(i => i.id.toString() !== id.toString());
        $(`.${options.ingredientChipClass}[data-id="${id}"]`).remove();
        updateHiddenIngredientsData();
    }

    function updateIngredientQuantity(id, quantity) {
        const ingredient = selectedIngredients.find(i => i.id.toString() === id.toString());
        if (ingredient) {
            ingredient.quantity = quantity;
            updateHiddenIngredientsData();
        }
    }

    function updateIngredientUnit(id, unit) {
        const ingredient = selectedIngredients.find(i => i.id.toString() === id.toString());
        if (ingredient) {
            ingredient.unit = unit;
            updateHiddenIngredientsData();
        }
    }

    // Updates the hidden input field with the formatted ingredient list - submits the correct ingredient data
    function updateHiddenIngredientsData() {
        const ingredientsText = selectedIngredients.map(i => {
            if (i.quantity && i.unit) {
                return `${i.quantity} ${i.unit} ${i.name}`;
            } else if (i.quantity) {
                return `${i.quantity} ${i.name}`;
            } else {
                return i.name;
            }
        }).join("\n");

        $(options.dataFieldSelector).val(ingredientsText);
    }

    // Parses existing ingredients from the hidden input when editing a recipe
    function parseExistingIngredients() {
        const existingIngredientsText = $(options.dataFieldSelector).val();
        if (!existingIngredientsText || existingIngredientsText.trim() === "") {
            return;
        }

        const lines = existingIngredientsText.split('\n');
        lines.forEach(line => {
            if (!line.trim()) return;

            const parts = line.trim().split(' ');
            let quantity = '';
            let unit = '';
            let nameParts = [];

            if (parts.length > 0) {
                // Check if the first part is a number (quantity)
                if (!isNaN(parseFloat(parts[0])) && parts.length > 1) {
                    quantity = parts[0];
                    const potentialUnit = parts[1].toLowerCase();

                    // Check if second part is a unit
                    const isUnit = options.measurementUnits.some(u =>
                        u.value === potentialUnit || potentialUnit.startsWith(u.value));

                    if (isUnit && parts.length > 2) {
                        unit = potentialUnit;
                        nameParts = parts.slice(2);
                    } else {
                        nameParts = parts.slice(1);
                    }
                } else {
                    nameParts = parts;
                }

                const name = nameParts.join(' ');

                // Try to find in known ingredients or create a temporary one
                let matchedIngredient = ingredientsList.find(i =>
                    i.name.toLowerCase() === name.toLowerCase());

                if (matchedIngredient) {
                    addIngredientChip({
                        ...matchedIngredient,
                        quantity: quantity,
                        unit: unit
                    });
                } else {
                    // Create a temporary ingredient
                    addIngredientChip({
                        id: 'temp_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9),
                        name: name,
                        isNew: true,
                        quantity: quantity,
                        unit: unit
                    });
                }
            }
        });
    }

    // Loads all ingredients from the server - Function to call after loading
    function loadAllIngredients(callback) {
        if (typeof AjaxService === 'undefined') {
            showError("Required dependency AjaxService is not available. Please reload the page and try again.");
            return;
        }

        try {
            AjaxService.get(options.getAllIngredientsUrl, null,
                function(data) {
                    ingredientsList = data;

                    if (options.parseExisting) {
                        parseExistingIngredients();
                    }

                    if (typeof callback === 'function') callback();
                },
                function(error) {
                    showError("Failed to load ingredients. Please refresh the page and try again.");
                }
            );
        } catch (error) {
            showError("Error initializing ingredients. Please refresh the page.");
            if (typeof callback === 'function') callback();
        }
    }

    // Clears the new ingredient modal fields
    function clearModalFields() {
        $(options.newIngredientNameSelector).val("");
        $(options.newIngredientCategorySelector).val("");
        $(options.newIngredientMeasurementSelector).val("");
        $(options.newIngredientDescriptionSelector).val("");
    }

    // Saves a new ingredient via the modal
    function saveNewIngredientViaModal() {
        const name = $(options.newIngredientNameSelector).val().trim();
        if (!name) {
            showError("Ingredient name is required");
            return;
        }

        const newIngredientData = {
            name: name,
            category: $(options.newIngredientCategorySelector).val().trim(),
            measurement: $(options.newIngredientMeasurementSelector).val().trim(),
            description: $(options.newIngredientDescriptionSelector).val().trim()
        };

        AjaxService.post(options.addIngredientAjaxUrl, newIngredientData,
            function(result) {
                if (result.success && result.ingredient) {
                    ingredientsList.push(result.ingredient);
                    addIngredientChip(result.ingredient);
                    $(options.modalId).modal('hide');
                } else {
                    showError("Error saving ingredient: " + (result.message || "Unknown error"));
                }
            },
            function(jqXHR) {
                let errorMsg = "Error saving ingredient. Please try again.";
                try {
                    const response = JSON.parse(jqXHR.responseText);
                    if (response && response.message) errorMsg = "Error: " + response.message;
                } catch (e) { /* Ignore parsing error, use default message */ }
                showError(errorMsg);
            }
        );
    }

    // Sets up all event handlers
    function setupEventHandlers() {
        if (typeof $ === 'undefined') {
            showError("Required dependency jQuery is not available");
            return;
        }

        // Check if required elements exist in the DOM
        const searchField = $(options.searchFieldSelector);
        const addButton = $(options.addButtonSelector);
        const addNewButton = $(options.addNewButtonSelector);

        if (!searchField.length || !addButton.length || !addNewButton.length) {
            console.warn("Some required elements for ingredient management were not found in the DOM");
        }

        addButton.on('click', handleAddIngredient);

        addNewButton.on('click', function() {
            clearModalFields();
            $(options.modalId).modal('show');
        });

        function handleAddIngredient() {
            const searchTerm = searchField.val().trim();
            if (!searchTerm) return;

            // Check if ingredient already exists in our list
            const existing = ingredientsList.find(i =>
                i.name.toLowerCase() === searchTerm.toLowerCase());

            if (existing) {
                addIngredientChip(existing);
                searchField.val("");
            } else {
                AjaxService.get(options.searchIngredientsUrl, { term: searchTerm }, function(data) {
                    if (data && data.length > 0) {
                        addIngredientChip(data[0]);
                    } else {
                        // Create a temporary ingredient
                        addIngredientChip({
                            id: 'temp_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9),
                            name: searchTerm,
                            isNew: true
                        });
                    }
                    searchField.val("");
                });
            }
        }

        // Setup autocomplete if jQuery UI is available
        if (searchField.length) {
            if (typeof $.ui !== 'undefined' && $.ui.autocomplete) {
                searchField.autocomplete({
                    source: function(request, response) {
                        AjaxService.get(options.searchIngredientsUrl, { term: request.term }, function(data) {
                            response($.map(data, function(item) {
                                return {
                                    label: item.name,
                                    value: item.name,
                                    itemData: item
                                };
                            }));
                        });
                    },
                    minLength: 2,
                    select: function(event, ui) {
                        addIngredientChip(ui.item.itemData);
                        setTimeout(function() { searchField.val(""); }, 100);
                        return false;
                    }
                });
            } else {
                // Fallback for when jQuery UI is not available
                searchField.on('keypress', function(e) {
                    if (e.which === 13) {
                        e.preventDefault();
                        handleAddIngredient();
                    }
                });
            }
        }

        // Event delegation for dynamically added chips
        $(document).on('click', `.${options.ingredientChipClass} .${options.removeIngredientBtnClass}`, function() {
            const chipId = $(this).closest(`.${options.ingredientChipClass}`).data('id');
            removeIngredientChip(chipId);
        });

        $(document).on('change', `.${options.ingredientChipClass} .${options.quantityInputClass}`, function() {
            const chipId = $(this).closest(`.${options.ingredientChipClass}`).data('id');
            updateIngredientQuantity(chipId, $(this).val().trim());
        });

        $(document).on('change', `.${options.ingredientChipClass} .${options.unitSelectClass}`, function() {
            const chipId = $(this).closest(`.${options.ingredientChipClass}`).data('id');
            updateIngredientUnit(chipId, $(this).val().trim());
        });

        $(document).on('click', options.saveModalButtonSelector, saveNewIngredientViaModal);

        $('form').has(options.containerSelector).on("submit", function() {
            updateHiddenIngredientsData();
            return true;
        });
    }

    function showError(message) {
        console.error(message);
        alert(message);
    }
    
     // Initializes the module with optional configuration
    const init = function(config = {}) {
        // Merge config with default options
        options = $.extend(true, {}, options, config);

        if (typeof $ === 'undefined') {
            showError("Required dependency jQuery is not available");
            return;
        }

        if (typeof AjaxService === 'undefined') {
            showError("Required dependency AjaxService is not available");
            return;
        }

        loadAllIngredients(function() {
            setupEventHandlers();
        });
    };

    return {
        init: init,
        addIngredientChip: addIngredientChip,
        updateHiddenIngredientsData: updateHiddenIngredientsData
    };
})();

window.IngredientModule = IngredientModule;

 // RecipeForm - Handles recipe form submission and integration with IngredientModule
if (typeof window.RecipeForm === 'undefined') {
    window.RecipeForm = (function() {
        const init = function(config = {}) {
            if (typeof IngredientModule !== 'undefined') {
                IngredientModule.init({
                    parseExisting: config.isEdit || false
                });

                $("#recipeForm").on("submit", function() {
                    if (typeof IngredientModule.updateHiddenIngredientsData === 'function') {
                        IngredientModule.updateHiddenIngredientsData();
                    }
                    return true;
                });
            } else {
                console.error("Required dependency IngredientModule is not available");
            }
        };

        // Public API
        return {
            init: init
        };
    })();
}