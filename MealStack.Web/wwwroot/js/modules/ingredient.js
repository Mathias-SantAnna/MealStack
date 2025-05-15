const IngredientModule = (function() {
    let options = {
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
        ingredientChipClass: 'ingredient-chip',
        quantityInputClass: 'quantity-input',
        unitSelectClass: 'unit-select',
        removeIngredientBtnClass: 'remove-ingredient',
        parseExisting: false, 
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
        searchIngredientsUrl: "/Ingredient/SearchIngredients",
        getAllIngredientsUrl: "/Ingredient/GetAllIngredients",
        addIngredientAjaxUrl: "/Ingredient/AddIngredientAjax"
    };

    let ingredientsList = []; 
    let selectedIngredients = []; 

    function addIngredientChip(ingredient) {
        if (selectedIngredients.some(i => i.id === ingredient.id && ingredient.id !== null && !ingredient.id.toString().startsWith('temp'))) {
            console.warn("Ingredient already selected:", ingredient.name);
            return;
        }

        const ingredientWithQuantity = {
            ...ingredient,
            quantity: ingredient.quantity || "", 
            unit: ingredient.unit || ingredient.measurement || "" 
        };
        selectedIngredients.push(ingredientWithQuantity);

        let measurementOptionsHtml = '<option value="">Select unit</option>';
        options.measurementUnits.forEach(unit => {
            const selected = unit.value === (ingredientWithQuantity.unit || ingredient.measurement) ? 'selected' : '';
            measurementOptionsHtml += `<option value="${unit.value}" ${selected}>${unit.label}</option>`;
        });

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

    function parseExistingIngredients() {
        const existingIngredientsText = $(options.dataFieldSelector).val();
        if (existingIngredientsText && existingIngredientsText.trim() !== "") {
            const lines = existingIngredientsText.split('\n');
            lines.forEach(line => {
                if (line.trim()) {
                    const parts = line.trim().split(' ');
                    let quantity = '';
                    let unit = '';
                    let nameParts = [];

                    if (parts.length > 0) {
                        if (!isNaN(parseFloat(parts[0])) && parts.length > 1) { 
                            quantity = parts[0];
                            const potentialUnit = parts[1].toLowerCase();
                            const isUnit = options.measurementUnits.some(u => u.value === potentialUnit || potentialUnit.startsWith(u.value));
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

                        // Try to match or create a temp one
                        let matchedIngredient = ingredientsList.find(i => i.name.toLowerCase() === name.toLowerCase());
                        if (matchedIngredient) {
                            addIngredientChip({ ...matchedIngredient, quantity: quantity, unit: unit });
                        } else {
                            // If not found, add as a new temporary ingredient
                            addIngredientChip({
                                id: 'temp_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9), 
                                name: name,
                                isNew: true, 
                                quantity: quantity,
                                unit: unit
                            });
                        }
                    }
                }
            });
        }
    }

    function loadAllIngredients(callback) {
        AjaxService.get(options.getAllIngredientsUrl, null,
            function(data) {
                ingredientsList = data;
                if (options.parseExisting) {
                    parseExistingIngredients(); 
                }
                if (typeof callback === 'function') callback();
            },
            function() { console.error("Error loading all ingredients."); }
        );
    }

    function clearModalFields() {
        $(options.newIngredientNameSelector).val("");
        $(options.newIngredientCategorySelector).val("");
        $(options.newIngredientMeasurementSelector).val("");
        $(options.newIngredientDescriptionSelector).val("");
    }

    function saveNewIngredientViaModal() {
        const name = $(options.newIngredientNameSelector).val().trim();
        if (!name) {
            alert("Ingredient name is required");
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
                    alert("Error saving ingredient: " + (result.message || "Unknown error"));
                }
            },
            function(jqXHR) {
                let errorMsg = "Error saving ingredient. Please try again.";
                try {
                    const response = JSON.parse(jqXHR.responseText);
                    if (response && response.message) errorMsg = "Error: " + response.message;
                } catch (e) { /* Ignore parsing error, use default message */ }
                alert(errorMsg);
            }
        );
    }

    function setupEventHandlers() {
        console.log("Setting up ingredient event handlers...");
        console.log("searchFieldSelector:", options.searchFieldSelector);
        console.log("addButtonSelector:", options.addButtonSelector);
        console.log("addNewButtonSelector:", options.addNewButtonSelector);

        // Autocomplete for ingredient search
        if ($(options.searchFieldSelector).length && typeof $.ui !== 'undefined' && $.ui.autocomplete) {
            $(options.searchFieldSelector).autocomplete({
                source: function(request, response) {
                    AjaxService.get(options.searchIngredientsUrl, { term: request.term }, function(data) {
                        response($.map(data, function(item) {
                            return { label: item.name, value: item.name, itemData: item }; 
                        }));
                    });
                },
                minLength: 2,
                select: function(event, ui) {
                    addIngredientChip(ui.item.itemData); 
                    setTimeout(function() { $(options.searchFieldSelector).val(""); }, 100);
                    return false;
                }
            });
        } else {
            console.warn("jQuery UI Autocomplete not available or search field not found.");
        }

        // Add ingredient button (after typing in search field and hitting a button)
        $(document).on('click', options.addButtonSelector, function() {
            console.log("Add ingredient button clicked");
            const searchTerm = $(options.searchFieldSelector).val().trim();
            if (searchTerm) {
                const existing = ingredientsList.find(i => i.name.toLowerCase() === searchTerm.toLowerCase());
                if (existing) {
                    addIngredientChip(existing);
                } else {
                    // If not found, try searching via AJAX
                    AjaxService.get(options.searchIngredientsUrl, { term: searchTerm }, function(data) {
                        if (data && data.length > 0) {
                            addIngredientChip(data[0]); 
                        } else {
                            // If no match, add as a new temporary ingredient
                            addIngredientChip({
                                id: 'temp_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9),
                                name: searchTerm,
                                isNew: true
                            });
                        }
                        $(options.searchFieldSelector).val("");
                    });
                }
                $(options.searchFieldSelector).val("");
            }
        });

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

        $('form').has(options.containerSelector).on("submit", function() {
            updateHiddenIngredientsData();
            return true;
        });

        // Modal Event Handlers
        $(document).on('click', options.addNewButtonSelector, function() {
            console.log("Add new ingredient button clicked");
            clearModalFields();
            $(options.modalId).modal('show');
        });

        $(document).on('click', options.saveModalButtonSelector, function() {
            console.log("Save new ingredient button clicked");
            saveNewIngredientViaModal();
        });
    }

    const init = function(config = {}) {
        console.log("IngredientModule initializing...");
        options = $.extend(true, {}, options, config);

        // Debug options
        console.log("Ingredient container selector:", options.containerSelector);
        console.log("Add button selector:", options.addButtonSelector);
        console.log("Modal button selector:", options.addNewButtonSelector);
        
        loadAllIngredients(function() {
            setupEventHandlers();
            console.log("IngredientModule initialized successfully.");
        });
    };

    return {
        init: init,
        addIngredientChip: addIngredientChip 
    };
})();

window.IngredientModule = IngredientModule;