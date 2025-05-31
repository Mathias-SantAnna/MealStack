const RecipePageInit = {
    init: function() {
        console.log("RecipePageInit starting...");

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setup());
        } else {
            this.setup();
        }
    },

    setup: function() {
        setTimeout(() => {
            this.initializeIngredientManager();
            this.initializeRecipeForm();
            this.setupModalFixes();
            console.log("RecipePageInit completed successfully");
        }, 200);
    },

    initializeIngredientManager: function() {
        if (typeof IngredientManager !== 'undefined' && $('#ingredients-container').length) {
            console.log("Initializing IngredientManager...");

            IngredientManager.init({
                autocompleteUrl: '/Ingredient/SearchIngredients',
                createIngredientUrl: '/Ingredient/AddIngredientAjax'
            });

            console.log("IngredientManager initialized");
        }
    },

    initializeRecipeForm: function() {
        if (typeof RecipeForm !== 'undefined' && $('#recipeForm').length) {
            console.log("Initializing RecipeForm...");

            const isEdit = window.location.pathname.includes('/Edit');
            RecipeForm.init({ isEdit: isEdit });

            console.log("RecipeForm initialized");
        }
    },

    setupModalFixes: function() {
        const modal = document.getElementById('addIngredientModal');
        if (modal) {
            document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
            document.body.classList.remove('modal-open');
            document.body.style.paddingRight = '';
            document.body.style.overflow = '';

            console.log("Modal fixes applied");
        }
    }
};

RecipePageInit.init();

window.RecipePageInit = RecipePageInit;