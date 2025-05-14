// -------------------------------------------------------------------------------------------------------------------
// Recipe Form Manager - functionality and ingredient modal
// -------------------------------------------------------------------------------------------------------------------
const RecipeForm = (function() {
    let options = {};

    function init(config) {
        options = {
            isEdit: false,
            modalId: '#addIngredientModal',
            saveButtonSelector: '#save-new-ingredient',
            ...config
        };

        IngredientManager.init({
            parseExisting: options.isEdit
        });

        setupModalHandlers();
    }

    // Set up handlers for ingredient modal
    function setupModalHandlers() {
        $(options.addNewButtonSelector || '#add-new-ingredient-btn').on("click", function() {
            clearModalFields();
            $(options.modalId).modal("show");
        });

        // Save new ingredient from modal
        $(options.saveButtonSelector).on("click", function() {
            const name = $("#new-ingredient-name").val().trim();
            if (!name) {
                alert("Ingredient name is required");
                return;
            }

            const newIngredientData = {
                name: name,
                category: $("#new-ingredient-category").val().trim(),
                measurement: $("#new-ingredient-measurement").val().trim(),
                description: $("#new-ingredient-description").val().trim()
            };

            saveNewIngredient(newIngredientData);
        });
    }
    
    function clearModalFields() {
        $("#new-ingredient-name").val("");
        $("#new-ingredient-category").val("");
        $("#new-ingredient-measurement").val("");
        $("#new-ingredient-description").val("");
    }

    // Save a new ingredient
    function saveNewIngredient(ingredientData) {
        $.ajax({
            url: "/Ingredient/AddIngredientAjax",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(ingredientData),
            success: function(result) {
                if (result.success) {
                    IngredientManager.addIngredientChip(result.ingredient);
                    $(options.modalId).modal("hide");
                } else {
                    alert("Error saving ingredient: " + result.message);
                }
            },
            error: function(xhr, status, error) {
                console.error("Error saving ingredient:", error);
                try {
                    const response = JSON.parse(xhr.responseText);
                    if (response && response.message) {
                        alert("Error: " + response.message);
                    } else {
                        alert("Error saving ingredient. Please try again.");
                    }
                } catch (e) {
                    alert("Error saving ingredient. Please try again.");
                }
            }
        });
    }

    return {
        init
    };
})();