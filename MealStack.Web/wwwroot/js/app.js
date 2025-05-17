const MealStack = (function() {
    const init = function() {
        console.log('Starting MealStack application initialization...');

        initializeCommon();

        initializePageSpecific();

        console.log('MealStack application initialized successfully');
    };

    const initializeCommon = function() {
        initBootstrapComponents();

        initMessageAlerts();
    };

    const initBootstrapComponents = function() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function(tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });

        const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.map(function(popoverTriggerEl) {
            return new bootstrap.Popover(popoverTriggerEl);
        });
    };

    const initMessageAlerts = function() {
        setTimeout(function() {
            $('.alert-success.alert-dismissible').alert('close');
        }, 5000);
    };

    const initializePageSpecific = function() {
        const path = window.location.pathname.toLowerCase();

        console.log("Current path:", path);

        if ($('.favorite-btn').length) {
            console.log("Initializing FavoriteModule - found favorite buttons on page.");
            FavoriteModule.init();
        }

        // --- Recipe Module ---
        if (path.includes('/recipe')) {
            console.log("Initializing RecipeModule.");
            RecipeModule.init();
        }

        // --- Ingredient Module ---
        if (path.includes('/recipe/create') || path.includes('/recipe/edit')) {
            console.log("Initializing IngredientModule for Recipe Form.");
            IngredientModule.init({
                parseExisting: path.includes('/recipe/edit'),
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
                newIngredientDescriptionSelector: '#new-ingredient-description'
            });
        }

        if (path.includes('/recipe/details/')) {
            console.log("Initializing RatingModule for Recipe Details.");
            RatingModule.init();

            if (typeof ServingsModule !== 'undefined') {
                console.log("Initializing ServingsModule for Recipe Details.");
                ServingsModule.init();
            }
        }

        if ($('#searchForm').length > 0) {
            console.log("Initializing SearchModule.");
            SearchModule.init();
        }
    };

    // Public API
    return {
        init
    };
})();

$(document).ready(function() {
    console.log("Document ready, initializing MealStack application...");
    MealStack.init();
});