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
        newIngredientCategorySelectSelector: '#new-ingredient-category-select',
        addCustomCategoryBtnSelector: '#add-custom-category-btn',
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

        // API endpoints
        searchIngredientsUrl: "/Ingredient/SearchIngredients",
        getAllIngredientsUrl: "/Ingredient/GetAllIngredients",
        addIngredientAjaxUrl: "/Ingredient/AddIngredientAjax",
        getIngredientCategoriesUrl: "/Ingredient/GetIngredientCategories"
    };

    let ingredientsList = [];
    let selectedIngredients = [];
    let addedIngredientIds = new Set();
    let ingredientCategories = [];

    function addIngredientChip(ingredient) {
        console.log("Adding ingredient chip:", ingredient);

        // Better duplicate prevention
        const isDuplicate = selectedIngredients.some(i => {
            if (ingredient.id && !ingredient.id.toString().startsWith('temp')) {
                return i.id === ingredient.id;
            } else {
                return i.name.toLowerCase() === ingredient.name.toLowerCase();
            }
        });

        if (isDuplicate) {
            console.log("Ingredient already exists, skipping:", ingredient.name);
            return;
        }

        // Ensure unique ID for new ingredients
        if (!ingredient.id || ingredient.id.toString().startsWith('temp')) {
            ingredient.id = 'temp_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        }

        const ingredientWithQuantity = {
            ...ingredient,
            quantity: ingredient.quantity || "",
            unit: ingredient.unit || ingredient.measurement || ""
        };

        selectedIngredients.push(ingredientWithQuantity);
        addedIngredientIds.add(ingredient.id);

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
        addedIngredientIds.delete(id.toString());
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

    // Updates the hidden input field with the formatted ingredient list
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
        console.log("Hidden field updated:", ingredientsText);
    }

    // Parse existing ingredients from hidden field
    function parseExistingIngredients() {
        const existingIngredientsText = $(options.dataFieldSelector).val();
        if (!existingIngredientsText || existingIngredientsText.trim() === "") {
            return;
        }

        console.log("Parsing existing ingredients:", existingIngredientsText);

        // Clear existing arrays to prevent duplication
        selectedIngredients = [];
        addedIngredientIds.clear();
        $(options.containerSelector).empty();

        const lines = existingIngredientsText.split('\n').filter(line => line.trim() !== '');

        lines.forEach(line => {
            if (!line.trim()) return;

            const parts = line.trim().split(' ');
            let quantity = '';
            let unit = '';
            let nameParts = [];

            if (parts.length > 0) {
                if (!isNaN(parseFloat(parts[0])) && parts.length > 1) {
                    quantity = parts[0];
                    const potentialUnit = parts[1].toLowerCase();

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

    // Load ingredient categories for dropdown
    function loadIngredientCategories() {
        if (typeof AjaxService === 'undefined') {
            console.error("AjaxService not available for loading categories");
            return;
        }

        AjaxService.get(options.getIngredientCategoriesUrl, null,
            function(data) {
                if (data.success) {
                    ingredientCategories = data.categories;
                    populateCategoryDropdowns();
                } else {
                    console.error("Error loading categories:", data.message);
                }
            },
            function(error) {
                console.error("Error loading ingredient categories:", error);
            }
        );
    }

    // Populate category dropdowns
    function populateCategoryDropdowns() {
        const $categorySelect = $(options.newIngredientCategorySelectSelector);
        const $categoryInput = $(options.newIngredientCategorySelector);

        if ($categorySelect.length) {
            // Admin dropdown
            $categorySelect.empty().append('<option value="">Select existing category...</option>');
            ingredientCategories.forEach(category => {
                $categorySelect.append(`<option value="${category}">${category}</option>`);
            });
        }

        if ($categoryInput.is('select')) {
            // Regular user dropdown
            $categoryInput.empty().append('<option value="">Select a category...</option>');
            ingredientCategories.forEach(category => {
                $categoryInput.append(`<option value="${category}">${category}</option>`);
            });
        }
    }

    // Loads all ingredients from the server
    function loadAllIngredients(callback) {
        if (typeof AjaxService === 'undefined') {
            showError("Required dependency AjaxService is not available. Please reload the page and try again.");
            return;
        }

        try {
            AjaxService.get(options.getAllIngredientsUrl, null,
                function(data) {
                    ingredientsList = data;
                    console.log("Loaded ingredients:", ingredientsList.length);

                    loadIngredientCategories();

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

    function clearModalFields() {
        $(options.newIngredientNameSelector).val("");
        $(options.newIngredientMeasurementSelector).val("");
        $(options.newIngredientDescriptionSelector).val("");

        const $categorySelect = $(options.newIngredientCategorySelectSelector);
        const $categoryInput = $(options.newIngredientCategorySelector);

        if ($categorySelect.length) {
            $categorySelect.val("");
            $categoryInput.addClass('d-none').val("");
        } else if ($categoryInput.is('select')) {
            $categoryInput.val("");
        } else {
            $categoryInput.val("");
        }
    }

    function saveNewIngredientViaModal() {
        const name = $(options.newIngredientNameSelector).val().trim();
        if (!name) {
            showError("Ingredient name is required");
            return;
        }

        let category = '';
        const $categorySelect = $(options.newIngredientCategorySelectSelector);
        const $categoryInput = $(options.newIngredientCategorySelector);

        if ($categorySelect.length && $categorySelect.val()) {
            category = $categorySelect.val().trim();
        } else if ($categoryInput.val()) {
            category = $categoryInput.val().trim();
        }

        const newIngredientData = {
            name: name,
            category: category,
            measurement: $(options.newIngredientMeasurementSelector).val().trim(),
            description: $(options.newIngredientDescriptionSelector).val().trim()
        };

        console.log("Saving new ingredient:", newIngredientData);

        // AJAX call with correct headers
        $.ajax({
            url: options.addIngredientAjaxUrl,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(newIngredientData),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(result) {
                console.log("Save ingredient result:", result);
                if (result.success && result.ingredient) {
                    ingredientsList.push(result.ingredient);
                    addIngredientChip(result.ingredient);
                    $(options.modalId).modal('hide');
                    clearModalFields();

                    if (newIngredientData.category && !ingredientCategories.includes(newIngredientData.category)) {
                        loadIngredientCategories();
                    }
                } else {
                    showError("Error saving ingredient: " + (result.message || "Unknown error"));
                }
            },
            error: function(jqXHR, textStatus, errorThrown) {
                console.error("AJAX Error:", jqXHR.responseText);
                let errorMsg = "Error saving ingredient. Please try again.";
                try {
                    const response = JSON.parse(jqXHR.responseText);
                    if (response && response.message) errorMsg = "Error: " + response.message;
                } catch (e) {
                    errorMsg = "Server error: " + textStatus;
                }
                showError(errorMsg);
            }
        });
    }

    function setupEventHandlers() {
        if (typeof $ === 'undefined') {
            showError("Required dependency jQuery is not available");
            return;
        }

        console.log("Setting up event handlers...");

        // Main add ingredient button
        $(document).off('click', options.addButtonSelector).on('click', options.addButtonSelector, handleAddIngredient);

        // Add new ingredient modal button
        $(document).off('click', options.addNewButtonSelector).on('click', options.addNewButtonSelector, function() {
            console.log("Add new ingredient button clicked");
            clearModalFields();
            $(options.modalId).modal('show');
        });

        // Custom category toggle (admin only)
        $(document).off('click', options.addCustomCategoryBtnSelector).on('click', options.addCustomCategoryBtnSelector, function() {
            const $categorySelect = $(options.newIngredientCategorySelectSelector);
            const $categoryInput = $(options.newIngredientCategorySelector);

            if ($categoryInput.hasClass('d-none')) {
                $categoryInput.removeClass('d-none').focus();
                $categorySelect.val("");
                $(this).find('i').removeClass('bi-plus').addClass('bi-dash');
                $(this).attr('title', 'Use existing categories');
            } else {
                $categoryInput.addClass('d-none').val("");
                $(this).find('i').removeClass('bi-dash').addClass('bi-plus');
                $(this).attr('title', 'Add custom category');
            }
        });

        function handleAddIngredient() {
            const searchField = $(options.searchFieldSelector);
            const searchTerm = searchField.val().trim();
            if (!searchTerm) return;

            console.log("Adding ingredient:", searchTerm);

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
        const searchField = $(options.searchFieldSelector);
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
                searchField.off('keypress').on('keypress', function(e) {
                    if (e.which === 13) {
                        e.preventDefault();
                        handleAddIngredient();
                    }
                });
            }
        }

        // Event delegation for dynamically added chips
        $(document).off('click', `.${options.ingredientChipClass} .${options.removeIngredientBtnClass}`)
            .on('click', `.${options.ingredientChipClass} .${options.removeIngredientBtnClass}`, function() {
                const chipId = $(this).closest(`.${options.ingredientChipClass}`).data('id');
                removeIngredientChip(chipId);
            });

        $(document).off('change', `.${options.ingredientChipClass} .${options.quantityInputClass}`)
            .on('change', `.${options.ingredientChipClass} .${options.quantityInputClass}`, function() {
                const chipId = $(this).closest(`.${options.ingredientChipClass}`).data('id');
                updateIngredientQuantity(chipId, $(this).val().trim());
            });

        $(document).off('change', `.${options.ingredientChipClass} .${options.unitSelectClass}`)
            .on('change', `.${options.ingredientChipClass} .${options.unitSelectClass}`, function() {
                const chipId = $(this).closest(`.${options.ingredientChipClass}`).data('id');
                updateIngredientUnit(chipId, $(this).val().trim());
            });

        $(document).off('click', options.saveModalButtonSelector)
            .on('click', options.saveModalButtonSelector, function() {
                console.log("Save new ingredient modal button clicked");
                saveNewIngredientViaModal();
            });

        $('form').has(options.containerSelector).off("submit").on("submit", function() {
            console.log("Form submitting, updating hidden data...");
            updateHiddenIngredientsData();
            return true;
        });

        console.log("Event handlers setup complete");
    }

    function showError(message) {
        console.error(message);
        alert(message);
    }

    // Initializes the module with optional configuration
    const init = function(config = {}) {
        console.log("IngredientModule initializing...");

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
            console.log("IngredientModule initialized successfully");
        });
    };

    return {
        init: init,
        addIngredientChip: addIngredientChip,
        updateHiddenIngredientsData: updateHiddenIngredientsData
    };
})();

window.IngredientModule = IngredientModule;