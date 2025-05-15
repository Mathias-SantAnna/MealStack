const ValidationService = (function() {
    const required = function(value, message = 'This field is required') {
        return value && value.trim() !== '' ? null : message;
    };

    const minLength = function(value, min, message = `Must be at least ${min} characters`) {
        return !value || value.length >= min ? null : message;
    };

    const maxLength = function(value, max, message = `Cannot exceed ${max} characters`) {
        return !value || value.length <= max ? null : message;
    };

    const range = function(value, min, max, message = `Value must be between ${min} and ${max}`) {
        if (!value && value !== 0) return null;

        const num = parseFloat(value);
        return (isNaN(num) || num < min || num > max) ? message : null;
    };

    const numeric = function(value, message = 'Must be a number') {
        if (!value && value !== 0) return null;

        return isNaN(parseFloat(value)) ? message : null;
    };

    // Apply validation to a form element
    const applyValidation = function(element, validationFn) {
        const $element = $(element);
        const value = $element.val();
        const error = validationFn(value);

        if (error) {
            $element.addClass('is-invalid');

            let $feedback = $element.next('.invalid-feedback');
            if ($feedback.length === 0) {
                $feedback = $('<div class="invalid-feedback"></div>');
                $element.after($feedback);
            }
            $feedback.text(error);

            return false;
        } else {
            $element.removeClass('is-invalid');
            return true;
        }
    };

    // Validate an entire form with multiple rules
    const validateForm = function(formSelector, validations) {
        let isValid = true;

        // Process each validation rule
        Object.entries(validations).forEach(([selector, validationFn]) => {
            const element = $(formSelector).find(selector);
            if (element.length) {
                const elementValid = applyValidation(element, validationFn);
                isValid = isValid && elementValid;
            }
        });

        return isValid;
    };

    // Setup real-time validation on input events
    const setupRealTimeValidation = function(formSelector, validations) {
        const form = $(formSelector);

        Object.entries(validations).forEach(([selector, validationFn]) => {
            const element = form.find(selector);
            if (element.length) {
                element.on('input blur change', function() {
                    applyValidation(this, validationFn);
                });
            }
        });
    };

    // Public API
    return {
        required,
        minLength,
        maxLength,
        range,
        numeric,
        applyValidation,
        validateForm,
        setupRealTimeValidation
    };
})();

window.ValidationService = ValidationService;