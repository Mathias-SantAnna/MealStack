// -------------------------------------------------------------------------------------------------------------------
// Ingredient Manager - functionality for recipe forms
// -------------------------------------------------------------------------------------------------------------------
const IngredientManager = (function() {
    let ingredientsList = [];
    let selectedIngredients = [];
    let options = {};

    // Default measurement
    const defaultMeasurementUnits = [
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
    ];

    function init(config) {
        options = {
            containerSelector: '#ingredients-container',
            dataFieldSelector: '#ingredients-data',
            searchFieldSelector: '#ingredient-search',
            addButtonSelector: '#add-ingredient-btn',
            addNewButtonSelector: '#add-new-ingredient-btn',
            measurementUnits: defaultMeasurementUnits,
            ...config
        };

        setupEventHandlers();
        loadIngredients();
    }

    // Set up event handlers
    function setupEventHandlers() {
        // Autocomplete for ingredient search
        $(options.searchFieldSelector).autocomplete({
            source: function(request, response) {
                $.getJSON("/Ingredient/SearchIngredients", { term: request.term }, function(data) {
                    response($.map(data, function(item) {
                        return {
                            label: item.name,
                            value: item.name,
                            item: item
                        };
                    }));
                });
            },
            minLength: 2,
            select: function(event, ui) {
                setTimeout(function() {
                    $(options.searchFieldSelector).val("");
                }, 100);
                addIngredientChip(ui.item.item);
                return false;
            }
        });

        // Add ingredient button
        $(options.addButtonSelector).on("click", function() {
            const searchTerm = $(options.searchFieldSelector).val().trim();
            if (searchTerm) {
                $.getJSON("/Ingredient/SearchIngredients", { term: searchTerm }, function(data) {
                    if (data && data.length > 0) {
                        addIngredientChip(data[0]);
                    } else {
                        const newIngredient = {
                            id: 'temp_' + Date.now(),
                            name: searchTerm,
                            isNew: true
                        };
                        addIngredientChip(newIngredient);
                    }
                });
                $(options.searchFieldSelector).val("");
            }
        });

        // Form submission handler
        $("form").on("submit", function(e) {
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
            return true;
        });
    }

    // Add an ingredient chip to the container
    function addIngredientChip(ingredient) {
        if (selectedIngredients.some(i => i.id === ingredient.id)) {
            return;
        }

        const ingredientWithQuantity = {
            ...ingredient,
            quantity: "",
            unit: ingredient.measurement || ""
        };
        selectedIngredients.push(ingredientWithQuantity);

        let measurementOptionsHtml = '<option value="">Select unit</option>';
        options.measurementUnits.forEach(unit => {
            const selected = unit.value === ingredient.measurement ? 'selected' : '';
            measurementOptionsHtml += `<option value="${unit.value}" ${selected}>${unit.label}</option>`;
        });

        const chipHtml = `
            <div class="ingredient-chip d-inline-flex align-items-center bg-light rounded p-2 me-2 mb-2" data-id="${ingredient.id}">
                <input type="number" class="quantity-input form-control form-control-sm me-1" 
                       style="width: 60px;" placeholder="Qty" aria-label="Quantity" min="0" step="0.01">
                <select class="unit-select form-select form-select-sm me-1" style="width: 100px;">
                    ${measurementOptionsHtml}
                </select>
                <span class="ingredient-name">${ingredient.name}</span>
                <button type="button" class="btn-close ms-2 remove-ingredient" aria-label="Remove" style="font-size: 0.6rem;"></button>
            </div>
        `;

        $(options.containerSelector).append(chipHtml);
        setupChipEventHandlers();
    }

    // Set up event handlers for ingredient chips
    function setupChipEventHandlers() {
        $(".remove-ingredient").off("click").on("click", function() {
            const chipId = $(this).closest(".ingredient-chip").data("id");
            removeIngredientChip(chipId);
        });

        $(".quantity-input").off("change").on("change", function() {
            const chipId = $(this).closest(".ingredient-chip").data("id");
            const quantity = $(this).val().trim();
            updateIngredientQuantity(chipId, quantity);
        });

        $(".unit-select").off("change").on("change", function() {
            const chipId = $(this).closest(".ingredient-chip").data("id");
            const unit = $(this).val().trim();
            updateIngredientUnit(chipId, unit);
        });
    }

    // Remove an ingredient chip
    function removeIngredientChip(id) {
        selectedIngredients = selectedIngredients.filter(i => i.id !== id);
        $(`.ingredient-chip[data-id="${id}"]`).remove();
    }

    // Update quantity
    function updateIngredientQuantity(id, quantity) {
        const ingredient = selectedIngredients.find(i => i.id === id);
        if (ingredient) {
            ingredient.quantity = quantity;
        }
    }

    // Update unit
    function updateIngredientUnit(id, unit) {
        const ingredient = selectedIngredients.find(i => i.id === id);
        if (ingredient) {
            ingredient.unit = unit;
        }
    }

    // Load ingredients from API
    function loadIngredients() {
        $.ajax({
            url: "/Ingredient/GetAllIngredients",
            type: "GET",
            success: function(data) {
                ingredientsList = data;
                if (typeof options.onIngredientsLoaded === 'function') {
                    options.onIngredientsLoaded(data);
                }
                // Parse existing ingredients if needed
                if (options.parseExisting) {
                    parseExistingIngredients();
                }
            },
            error: function(err) {
                console.error("Error loading ingredients:", err);
            }
        });
    }

    // Parse existing ingredients
    function parseExistingIngredients() {
        const existingIngredientsText = $(options.dataFieldSelector).val();
        if (existingIngredientsText) {
            const lines = existingIngredientsText.split('\n');

            lines.forEach(line => {
                if (line.trim()) {
                    const parts = line.trim().split(' ');

                    if (parts.length > 0) {
                        let quantity = '';
                        let unit = '';
                        let name = '';

                        if (!isNaN(parseFloat(parts[0]))) {
                            quantity = parts[0];

                            if (parts.length > 1) {
                                const potentialUnit = parts[1].toLowerCase();
                                const isUnit = options.measurementUnits.some(u =>
                                    u.value === potentialUnit ||
                                    potentialUnit.startsWith(u.value)
                                );

                                if (isUnit) {
                                    unit = potentialUnit;
                                    name = parts.slice(2).join(' ');
                                } else {
                                    name = parts.slice(1).join(' ');
                                }
                            }
                        } else {
                            name = line.trim();
                        }

                        const matchedIngredient = ingredientsList.find(i =>
                            i.name.toLowerCase() === name.toLowerCase());

                        if (matchedIngredient) {
                            const ingredientWithQuantity = {
                                ...matchedIngredient,
                                quantity: quantity,
                                unit: unit
                            };

                            selectedIngredients.push(ingredientWithQuantity);

                            let measurementOptionsHtml = '<option value="">Select unit</option>';
                            options.measurementUnits.forEach(u => {
                                const selected = u.value === unit ? 'selected' : '';
                                measurementOptionsHtml += `<option value="${u.value}" ${selected}>${u.label}</option>`;
                            });

                            const chipHtml = `
                                <div class="ingredient-chip d-inline-flex align-items-center bg-light rounded p-2 me-2 mb-2" data-id="${matchedIngredient.id}">
                                    <input type="number" class="quantity-input form-control form-control-sm me-1" 
                                        style="width: 60px;" placeholder="Qty" aria-label="Quantity" min="0" step="0.01" value="${quantity}">
                                    <select class="unit-select form-select form-select-sm me-1" style="width: 100px;">
                                        ${measurementOptionsHtml}
                                    </select>
                                    <span class="ingredient-name">${matchedIngredient.name}</span>
                                    <button type="button" class="btn-close ms-2 remove-ingredient" aria-label="Remove" style="font-size: 0.6rem;"></button>
                                </div>
                            `;

                            $(options.containerSelector).append(chipHtml);
                        } else {
                            const tempIngredient = {
                                id: 'temp_' + Date.now() + Math.random(),
                                name: name,
                                isNew: true,
                                quantity: quantity,
                                unit: unit
                            };

                            selectedIngredients.push(tempIngredient);

                            let measurementOptionsHtml = '<option value="">Select unit</option>';
                            options.measurementUnits.forEach(u => {
                                const selected = u.value === unit ? 'selected' : '';
                                measurementOptionsHtml += `<option value="${u.value}" ${selected}>${u.label}</option>`;
                            });

                            const chipHtml = `
                                <div class="ingredient-chip d-inline-flex align-items-center bg-light rounded p-2 me-2 mb-2" data-id="${tempIngredient.id}">
                                    <input type="number" class="quantity-input form-control form-control-sm me-1" 
                                        style="width: 60px;" placeholder="Qty" aria-label="Quantity" min="0" step="0.01" value="${quantity}">
                                    <select class="unit-select form-select form-select-sm me-1" style="width: 100px;">
                                        ${measurementOptionsHtml}
                                    </select>
                                    <span class="ingredient-name">${tempIngredient.name}</span>
                                    <button type="button" class="btn-close ms-2 remove-ingredient" aria-label="Remove" style="font-size: 0.6rem;"></button>
                                </div>
                            `;

                            $(options.containerSelector).append(chipHtml);
                        }
                    }
                }
            });

            setupChipEventHandlers();
        }
    }

    return {
        init,
        addIngredientChip,
        removeIngredientChip
    };
})();