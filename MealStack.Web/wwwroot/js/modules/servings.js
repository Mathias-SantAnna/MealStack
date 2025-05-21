const ServingsModule = (function() {
    const defaults = {
        servingsInputSelector: '#servings-input',
        ingredientListSelector: '.ingredients-list',
        applyButtonSelector: '#apply-servings',
        resetButtonSelector: '#reset-servings',
        originalServingsAttr: 'data-original-servings',
        originalValueAttr: 'data-original-value'
    };

    let options = {};
    let originalServings = 0;
    let currentServings = 0;
    let ingredientOriginalData = [];

    const init = function(config = {}) {
        console.log("Initializing ServingsModule...");

        options = { ...defaults, ...config };

        const servingsInput = document.querySelector(options.servingsInputSelector);
        if (!servingsInput) {
            console.error("Servings input element not found");
            return;
        }

        originalServings = parseInt(servingsInput.getAttribute(options.originalServingsAttr), 10);
        if (isNaN(originalServings)) {
            console.error("Invalid original servings value");
            return;
        }

        currentServings = originalServings;
        servingsInput.value = currentServings;

        processIngredients();
        setupEventListeners();

        console.log("ServingsModule initialized with", originalServings, "servings");
    };

    const processIngredients = function() {
        const ingredients = document.querySelectorAll(`${options.ingredientListSelector} li`);

        ingredients.forEach((item, index) => {
            const label = item.querySelector('label');
            const text = label ? label.textContent.trim() : item.textContent.trim();

            ingredientOriginalData[index] = {
                element: item,
                originalText: text,
                label: label,
                processed: false
            };
            
            // Extract quantity from text (match numbers at the beginning)
            const match = text.match(/^(\d+(?:\.\d+)?|\d+\/\d+)\s+(\w+)/);

            if (match) {
                let quantity = match[1];
                const unit = match[2];
                
                // Handle fractions
                if (quantity.includes('/')) {
                    const [numerator, denominator] = quantity.split('/');
                    quantity = parseFloat(numerator) / parseFloat(denominator);
                } else {
                    quantity = parseFloat(quantity);
                }

                ingredientOriginalData[index].quantity = quantity;
                ingredientOriginalData[index].quantityText = match[1];
                ingredientOriginalData[index].unit = unit;
                ingredientOriginalData[index].processed = true;
            }
        });

        console.log("Processed ingredient data:", ingredientOriginalData);
    };

    const setupEventListeners = function() {
        const applyBtn = document.querySelector(options.applyButtonSelector);
        const resetBtn = document.querySelector(options.resetButtonSelector);
        const servingsInput = document.querySelector(options.servingsInputSelector);

        if (applyBtn) {
            applyBtn.addEventListener('click', function() {
                const newServings = parseInt(servingsInput.value, 10);
                if (!isNaN(newServings) && newServings > 0) {
                    updateServings(newServings);
                } else {
                    alert("Please enter a valid number of servings (greater than 0)");
                    servingsInput.value = currentServings;
                }
            });
        }

        if (resetBtn) {
            resetBtn.addEventListener('click', function() {
                if (servingsInput) {
                    servingsInput.value = originalServings;
                }
                resetToOriginal();
            });
        }

        if (servingsInput) {
            servingsInput.addEventListener('keypress', function(e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    const applyBtn = document.querySelector(options.applyButtonSelector);
                    if (applyBtn) {
                        applyBtn.click();
                    }
                }
            });
        }
    };

    const resetToOriginal = function() {
        console.log("Resetting to original servings: " + originalServings);
        currentServings = originalServings;

        ingredientOriginalData.forEach(data => {
            if (data.label) {
                data.label.textContent = data.originalText;
            } else if (data.element) {
                data.element.textContent = data.originalText;
            }

            data.element.classList.add('highlight');
            setTimeout(() => {
                data.element.classList.remove('highlight');
            }, 500);
        });
    };

    const updateServings = function(newServings) {
        if (newServings < 1) return;

        currentServings = newServings;
        recalculateIngredients(newServings);

        console.log(`Updated servings from ${originalServings} to ${newServings}`);
    };

    // escape special regex chars for any string
    const escapeRegExp = function(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    };

    // multiply qty by ratio, one decimal, strip trailing .0
    const calculateNewQuantity = function(originalQty, ratio) {
        const newQty = originalQty * ratio;
        return newQty.toFixed(1).replace(/\.0$/, '');
    };

    // loop ingredients and update
    const recalculateIngredients = function(newServings) {
        const ratio = newServings / originalServings;

        ingredientOriginalData.forEach(data => {
            if (!data.processed || !data.quantity) return;

            try {
                const newQuantity = calculateNewQuantity(data.quantity, ratio);
                // To properly update content
                if (data.label) {
                    // For checkboxes with labels
                    const updatedText = data.originalText.replace(
                        new RegExp(`^\\s*${escapeRegExp(data.quantityText)}\\s+`, 'i'),
                        `${newQuantity} `
                    );
                    data.label.textContent = updatedText;
                } else if (data.element) {
                    // For list items no checkboxes
                    const updatedText = data.originalText.replace(
                        new RegExp(`^\\s*${escapeRegExp(data.quantityText)}\\s+`, 'i'),
                        `${newQuantity} `
                    );
                    data.element.textContent = updatedText;
                }

                data.element.classList.add('highlight');
                setTimeout(() => {
                    data.element.classList.remove('highlight');
                }, 500);
            } catch (e) {
                console.error("Error recalculating ingredient:", e);
            }
        });
    };

    return {
        init,
        updateServings,
        resetToOriginal
    };
})();

window.ServingsModule = ServingsModule;