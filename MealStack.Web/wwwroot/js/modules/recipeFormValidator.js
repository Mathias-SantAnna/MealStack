const RecipeFormValidator = {
    init: function() {
        console.log("RecipeFormValidator initializing...");

        $('#recipeForm').on('submit', function(e) {
            if (!RecipeFormValidator.validateForm()) {
                e.preventDefault();
                return false;
            }

            const $submitBtn = $(this).find('button[type="submit"]');
            const originalText = $submitBtn.text();
            $submitBtn.prop('disabled', true)
                .html('<i class="spinner-border spinner-border-sm me-1"></i>Saving...');

            setTimeout(() => {
                $submitBtn.prop('disabled', false).text(originalText);
            }, 10000);
        });

        setInterval(() => {
            const ingredientsVal = $('#Ingredients').val();
            if (ingredientsVal) {
                $('#Ingredients').removeClass('is-invalid');
            }
        }, 500);

        console.log("RecipeFormValidator initialized");
    },

    validateForm: function() {
        let isValid = true;

        $('.is-invalid').removeClass('is-invalid');
        $('.invalid-feedback').remove();

        const title = $('#Title').val().trim();
        if (!title) {
            this.showFieldError('#Title', 'Recipe title is required');
            isValid = false;
        }

        const instructions = $('#Instructions').val().trim();
        if (!instructions) {
            this.showFieldError('#Instructions', 'Instructions are required');
            isValid = false;
        }

        const ingredients = $('#Ingredients').val().trim();
        if (!ingredients) {
            this.showMessage('Please add at least one ingredient before saving the recipe.', 'danger');
            $('#ingredients-container').addClass('border-danger');
            isValid = false;
        } else {
            $('#ingredients-container').removeClass('border-danger');
        }

        const prepTime = parseInt($('#PrepTimeMinutes').val());
        if (isNaN(prepTime) || prepTime < 1) {
            this.showFieldError('#PrepTimeMinutes', 'Prep time must be at least 1 minute');
            isValid = false;
        }

        const servings = parseInt($('#Servings').val());
        if (isNaN(servings) || servings < 1) {
            this.showFieldError('#Servings', 'Servings must be at least 1');
            isValid = false;
        }

        if (!isValid) {
            this.showMessage('Please fix the errors before submitting.', 'danger');
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        return isValid;
    },

    showFieldError: function(fieldSelector, message) {
        const $field = $(fieldSelector);
        $field.addClass('is-invalid');

        const $feedback = $('<div class="invalid-feedback"></div>').text(message);
        $field.after($feedback);
    },

    showMessage: function(message, type = 'info') {
        const alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                <i class="bi bi-exclamation-triangle me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        $('.alert').not('.alert-info').remove();

        $('#recipeForm').prepend(alertHtml);
    }
};

$(document).ready(function() {
    if ($('#recipeForm').length) {
        RecipeFormValidator.init();
    }
});

window.RecipeFormValidator = RecipeFormValidator;