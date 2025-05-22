const RecipeForm = (function() {
    let options = {
        // Form selectors
        formSelector: '#recipeForm',
        titleSelector: '#Title',
        descriptionSelector: '#Description',
        instructionsSelector: '#Instructions',
        prepTimeSelector: '#PrepTimeMinutes',
        cookTimeSelector: '#CookTimeMinutes',
        servingsSelector: '#Servings',
        difficultySelector: '#Difficulty',
        notesSelector: '#Notes',

        // Image upload
        imageFileSelector: 'input[name="ImageFile"]',
        currentImageSelector: '.img-thumbnail',

        // Categories
        categoryCheckboxSelector: 'input[name="selectedCategories"]',

        // Configuration
        isEdit: false
    };

    // Initialize the recipe form
    const init = function(config = {}) {
        console.log("RecipeForm initializing...");

        // Merge config with defaults
        options = { ...options, ...config };

        if (!$(options.formSelector).length) {
            console.warn("Recipe form not found, skipping initialization");
            return;
        }

        setupFormValidation();
        setupImagePreview();
        setupFormSubmission();
        setupCharacterCounters();

        console.log("RecipeForm initialized successfully");
    };

    // Setup form validation
    const setupFormValidation = function() {
        // Title validation
        $(options.titleSelector).on('blur', function() {
            const value = $(this).val().trim();
            if (!value) {
                showFieldError(this, 'Recipe title is required');
            } else if (value.length < 3) {
                showFieldError(this, 'Title must be at least 3 characters');
            } else if (value.length > 100) {
                showFieldError(this, 'Title cannot exceed 100 characters');
            } else {
                clearFieldError(this);
            }
        });

        // Instructions validation
        $(options.instructionsSelector).on('blur', function() {
            const value = $(this).val().trim();
            if (!value) {
                showFieldError(this, 'Instructions are required');
            } else if (value.length < 10) {
                showFieldError(this, 'Instructions must be at least 10 characters');
            } else {
                clearFieldError(this);
            }
        });

        // Numeric field validation
        $(options.prepTimeSelector + ', ' + options.cookTimeSelector + ', ' + options.servingsSelector).on('blur', function() {
            const value = parseInt($(this).val());
            const fieldName = $(this).attr('id');

            if (isNaN(value) || value < 0) {
                showFieldError(this, 'Please enter a valid number');
            } else {
                if (fieldName === 'PrepTimeMinutes' && value < 1) {
                    showFieldError(this, 'Prep time must be at least 1 minute');
                } else if (fieldName === 'Servings' && value < 1) {
                    showFieldError(this, 'Servings must be at least 1');
                } else if (value > 1440 && fieldName !== 'Servings') {
                    showFieldError(this, 'Time cannot exceed 1440 minutes (24 hours)');
                } else if (fieldName === 'Servings' && value > 100) {
                    showFieldError(this, 'Servings cannot exceed 100');
                } else {
                    clearFieldError(this);
                }
            }
        });

        console.log("Form validation setup complete");
    };

    // Setup image preview functionality
    const setupImagePreview = function() {
        $(options.imageFileSelector).on('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                // Validate file type
                if (!file.type.startsWith('image/')) {
                    alert('Please select a valid image file');
                    $(this).val('');
                    return;
                }

                // Validate file size (max 5MB)
                if (file.size > 5 * 1024 * 1024) {
                    alert('Image file size must be less than 5MB');
                    $(this).val('');
                    return;
                }

                // Create preview
                const reader = new FileReader();
                reader.onload = function(e) {
                    // Remove existing preview
                    $('#image-preview').remove();

                    // Create new preview
                    const preview = $(`
                        <div id="image-preview" class="mt-2">
                            <img src="${e.target.result}" alt="Recipe Image Preview" class="img-thumbnail" style="max-height: 200px;" />
                            <div class="form-text">New image preview</div>
                        </div>
                    `);

                    $(options.imageFileSelector).after(preview);
                };
                reader.readAsDataURL(file);
            }
        });

        console.log("Image preview setup complete");
    };

    // Setup form submission handling
    const setupFormSubmission = function() {
        $(options.formSelector).on('submit', function(e) {
            // Validate form before submission
            if (!validateForm()) {
                e.preventDefault();
                showFormError('Please correct the errors below before submitting');
                return false;
            }

            // Show loading state
            const submitBtn = $(this).find('button[type="submit"]');
            const originalText = submitBtn.text();
            submitBtn.prop('disabled', true)
                .html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

            // Re-enable button after 10 seconds as fallback
            setTimeout(() => {
                submitBtn.prop('disabled', false).text(originalText);
            }, 10000);
        });

        console.log("Form submission handling setup complete");
    };

    // Setup character counters for text fields
    const setupCharacterCounters = function() {
        // Title counter
        setupFieldCounter(options.titleSelector, 100);

        // Description counter  
        setupFieldCounter(options.descriptionSelector, 500);

        console.log("Character counters setup complete");
    };

    // Setup counter for a specific field
    const setupFieldCounter = function(selector, maxLength) {
        const $field = $(selector);
        if (!$field.length) return;

        const counterId = $field.attr('id') + '-counter';
        const counter = $(`<small id="${counterId}" class="form-text text-muted"></small>`);
        $field.after(counter);

        const updateCounter = function() {
            const current = $field.val().length;
            counter.text(`${current}/${maxLength} characters`);

            if (current > maxLength * 0.9) {
                counter.removeClass('text-muted').addClass('text-warning');
            } else if (current > maxLength) {
                counter.removeClass('text-muted text-warning').addClass('text-danger');
            } else {
                counter.removeClass('text-warning text-danger').addClass('text-muted');
            }
        };

        $field.on('input', updateCounter);
        updateCounter(); // Initialize
    };

    // Validate entire form
    const validateForm = function() {
        let isValid = true;

        // Clear previous errors
        $('.is-invalid').removeClass('is-invalid');
        $('.invalid-feedback').remove();

        // Validate title
        const title = $(options.titleSelector).val().trim();
        if (!title) {
            showFieldError(options.titleSelector, 'Recipe title is required');
            isValid = false;
        } else if (title.length < 3) {
            showFieldError(options.titleSelector, 'Title must be at least 3 characters');
            isValid = false;
        }

        // Validate instructions
        const instructions = $(options.instructionsSelector).val().trim();
        if (!instructions) {
            showFieldError(options.instructionsSelector, 'Instructions are required');
            isValid = false;
        } else if (instructions.length < 10) {
            showFieldError(options.instructionsSelector, 'Instructions must be at least 10 characters');
            isValid = false;
        }

        // Validate prep time
        const prepTime = parseInt($(options.prepTimeSelector).val());
        if (isNaN(prepTime) || prepTime < 1) {
            showFieldError(options.prepTimeSelector, 'Prep time must be at least 1 minute');
            isValid = false;
        }

        // Validate servings
        const servings = parseInt($(options.servingsSelector).val());
        if (isNaN(servings) || servings < 1) {
            showFieldError(options.servingsSelector, 'Servings must be at least 1');
            isValid = false;
        }

        return isValid;
    };

    // Show field error
    const showFieldError = function(field, message) {
        const $field = $(field);
        $field.addClass('is-invalid');

        let $feedback = $field.next('.invalid-feedback');
        if ($feedback.length === 0) {
            $feedback = $('<div class="invalid-feedback"></div>');
            $field.after($feedback);
        }
        $feedback.text(message);
    };

    // Clear field error
    const clearFieldError = function(field) {
        const $field = $(field);
        $field.removeClass('is-invalid');
        $field.next('.invalid-feedback').remove();
    };

    // Show form-level error
    const showFormError = function(message) {
        // Remove existing form errors
        $('.form-error').remove();

        // Add new error message
        const errorAlert = $(`
            <div class="alert alert-danger alert-dismissible fade show form-error" role="alert">
                <i class="bi bi-exclamation-triangle me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `);

        $(options.formSelector).prepend(errorAlert);

        // Scroll to top of form
        $(options.formSelector)[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
    };

    // Auto-save functionality (optional)
    const setupAutoSave = function() {
        if (!options.isEdit) return; // Only for edit mode

        let autoSaveTimer;
        const autoSaveDelay = 30000; // 30 seconds

        const triggerAutoSave = function() {
            clearTimeout(autoSaveTimer);
            autoSaveTimer = setTimeout(() => {
                saveFormData();
            }, autoSaveDelay);
        };

        // Trigger auto-save on field changes
        $(options.formSelector).find('input, textarea, select').on('change input', triggerAutoSave);

        console.log("Auto-save setup complete");
    };

    // Save form data to localStorage
    const saveFormData = function() {
        if (!options.isEdit) return;

        try {
            const formData = {
                title: $(options.titleSelector).val(),
                description: $(options.descriptionSelector).val(),
                instructions: $(options.instructionsSelector).val(),
                prepTime: $(options.prepTimeSelector).val(),
                cookTime: $(options.cookTimeSelector).val(),
                servings: $(options.servingsSelector).val(),
                difficulty: $(options.difficultySelector).val(),
                notes: $(options.notesSelector).val(),
                timestamp: new Date().toISOString()
            };

            localStorage.setItem('recipeForm_autoSave', JSON.stringify(formData));
            console.log("Form data auto-saved");
        } catch (e) {
            console.warn("Could not auto-save form data:", e);
        }
    };

    // Restore form data from localStorage
    const restoreFormData = function() {
        if (!options.isEdit) return;

        try {
            const saved = localStorage.getItem('recipeForm_autoSave');
            if (!saved) return;

            const formData = JSON.parse(saved);
            const savedTime = new Date(formData.timestamp);
            const now = new Date();

            // Only restore if saved within last hour
            if ((now - savedTime) > 3600000) {
                localStorage.removeItem('recipeForm_autoSave');
                return;
            }

            // Ask user if they want to restore
            if (confirm('Found auto-saved changes. Do you want to restore them?')) {
                $(options.titleSelector).val(formData.title || '');
                $(options.descriptionSelector).val(formData.description || '');
                $(options.instructionsSelector).val(formData.instructions || '');
                $(options.prepTimeSelector).val(formData.prepTime || '');
                $(options.cookTimeSelector).val(formData.cookTime || '');
                $(options.servingsSelector).val(formData.servings || '');
                $(options.difficultySelector).val(formData.difficulty || '');
                $(options.notesSelector).val(formData.notes || '');

                console.log("Form data restored from auto-save");
            }

            localStorage.removeItem('recipeForm_autoSave');
        } catch (e) {
            console.warn("Could not restore form data:", e);
        }
    };

    // Public API
    return {
        init: init,
        validateForm: validateForm,
        showFormError: showFormError
    };
})();

// Make RecipeForm available globally
window.RecipeForm = RecipeForm;