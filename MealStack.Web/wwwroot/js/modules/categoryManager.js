const CategoryManager = (function() {

    // Configuration
    const config = {
        selectors: {
            categorySelect: '.category-select',
            customInput: '.custom-category-input',
            adminControls: '.admin-category-controls',
            modal: '#createCategoryModal',
            saveButton: '#saveCategoryBtn',
            nameInput: '#newCategoryName',
            descriptionInput: '#newCategoryDescription',
            spinner: '.spinner-border',
            validation: '.category-validation'
        },
        endpoints: {
            getCategories: '/Ingredient/GetIngredientCategories',
            createCategory: '/Ingredient/CreateCategory'
        },
        standardCategories: [
            "Dairy & Eggs", "Meat & Seafood", "Vegetables", "Fruits",
            "Grains & Cereals", "Spices & Herbs", "Condiments & Oils",
            "Pantry Staples", "Beverages", "Baking", "Frozen", "Other"
        ]
    };

    // State
    let isAdmin = false;
    let categories = [];

    // Initialize the category manager //
    function init() {
        console.log('CategoryManager: Initializing...');

        if (!$(config.selectors.categorySelect).length) {
            console.log('CategoryManager: No category selects found');
            return;
        }

        isAdmin = $(config.selectors.adminControls).length > 0;
        console.log('CategoryManager: Admin mode:', isAdmin);

        loadCategories()
            .then(() => {
                setupEventHandlers();
                handleExistingValues();
                console.log('CategoryManager: Initialization complete');
            })
            .catch(error => {
                console.error('CategoryManager: Initialization failed:', error);
                fallbackToStandardCategories();
            });
    }

    /**
     * Load categories from server
     */
    async function loadCategories() {
        try {
            const response = await $.get(config.endpoints.getCategories);

            if (response.success) {
                categories = response.categories;
                console.log('CategoryManager: Loaded', categories.length, 'categories');
            } else {
                throw new Error(response.message || 'Failed to load categories');
            }
        } catch (error) {
            console.error('CategoryManager: Failed to load categories:', error);
            categories = config.standardCategories;
        }

        populateSelects();
    }

    /**
     * Populate all category select elements
     */
    function populateSelects() {
        $(config.selectors.categorySelect).each(function() {
            const $select = $(this);
            const currentValue = $select.data('current-value') || '';

            $select.empty();
            $select.append('<option value="">Select a category...</option>');

            // Add categories
            categories.forEach(category => {
                const selected = category === currentValue ? 'selected' : '';
                $select.append(`<option value="${category}" ${selected}>${category}</option>`);
            });

            // Add custom option
            $select.append('<option value="custom">➕ Custom Category...</option>');

            console.log('CategoryManager: Populated select with', categories.length, 'categories');
        });
    }

    /**
     * Handle existing values that might not be in the standard list
     */
    function handleExistingValues() {
        $(config.selectors.categorySelect).each(function() {
            const $select = $(this);
            const $customInput = $select.siblings(config.selectors.customInput);
            const currentValue = $select.data('current-value') || '';

            if (currentValue && !categories.includes(currentValue)) {
                // Show custom input for non-standard categories
                $customInput.val(currentValue).show();
                $select.val('custom');
                console.log('CategoryManager: Showing custom input for:', currentValue);
            }
        });
    }

    /**
     * Setup event handlers
     */
    function setupEventHandlers() {
        // Category selection change
        $(document).on('change', config.selectors.categorySelect, handleCategoryChange);

        // Custom input change
        $(document).on('input', config.selectors.customInput, handleCustomInput);

        // Form submission
        $(document).on('submit', 'form', handleFormSubmission);

        // Admin category creation
        if (isAdmin) {
            $(document).on('click', config.selectors.saveButton, handleCategoryCreation);
            $(document).on('shown.bs.modal', config.selectors.modal, focusModalInput);
            $(document).on('hidden.bs.modal', config.selectors.modal, clearModalForm);
        }

        console.log('CategoryManager: Event handlers setup complete');
    }

    /**
     * Handle category selection change
     */
    function handleCategoryChange(event) {
        const $select = $(event.target);
        const $customInput = $select.siblings(config.selectors.customInput);

        if ($select.val() === 'custom') {
            $customInput.show().focus();
            $select.val(''); // Clear select to let custom input take precedence
        } else {
            $customInput.hide().val('');
        }
    }

    /**
     * Handle custom input changes
     */
    function handleCustomInput(event) {
        const $input = $(event.target);
        const $select = $input.siblings(config.selectors.categorySelect);

        // Update the select value to match custom input
        $select.val($input.val());
    }

    /**
     * Handle form submission
     */
    function handleFormSubmission(event) {
        $(config.selectors.customInput).each(function() {
            const $input = $(this);
            const $select = $input.siblings(config.selectors.categorySelect);

            if ($input.is(':visible') && $input.val()) {
                $select.val($input.val());
            }
        });
    }

    /**
     * Handle admin category creation
     */
    async function handleCategoryCreation(event) {
        const $button = $(event.target);
        const $spinner = $button.find(config.selectors.spinner);
        const $nameInput = $(config.selectors.nameInput);
        const $descriptionInput = $(config.selectors.descriptionInput);

        const categoryName = $nameInput.val().trim();
        const categoryDescription = $descriptionInput.val().trim();

        // Validation
        if (!categoryName) {
            showFieldError($nameInput, 'Category name is required');
            return;
        }

        if (categories.includes(categoryName)) {
            showFieldError($nameInput, 'Category already exists');
            return;
        }

        // Clear previous errors
        clearFieldError($nameInput);

        // Show loading state
        $button.prop('disabled', true);
        $spinner.show();

        try {
            const response = await $.ajax({
                url: config.endpoints.createCategory,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    name: categoryName,
                    description: categoryDescription
                }),
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                }
            });

            if (response.success) {
                // Add to categories list
                categories.push(categoryName);
                categories.sort();

                // Update all selects
                populateSelects();

                // Set the new category as selected
                $(config.selectors.categorySelect).val(categoryName);

                // Close modal and show success
                $(config.selectors.modal).modal('hide');
                showSuccessMessage(`Category "${categoryName}" created successfully!`);

                console.log('CategoryManager: Category created:', categoryName);
            } else {
                showFieldError($nameInput, response.message || 'Failed to create category');
            }
        } catch (error) {
            console.error('CategoryManager: Error creating category:', error);
            showFieldError($nameInput, 'Failed to create category. Please try again.');
        } finally {
            // Hide loading state
            $button.prop('disabled', false);
            $spinner.hide();
        }
    }

    /**
     * Focus modal input when shown
     */
    function focusModalInput() {
        $(config.selectors.nameInput).focus();
    }

    /**
     * Clear modal form when hidden
     */
    function clearModalForm() {
        $(config.selectors.nameInput).val('').removeClass('is-invalid');
        $(config.selectors.descriptionInput).val('');
        $(config.selectors.nameInput).siblings('.invalid-feedback').text('');
    }

    /**
     * Show field error
     */
    function showFieldError($field, message) {
        $field.addClass('is-invalid');
        $field.siblings('.invalid-feedback').text(message);
    }

    /**
     * Clear field error
     */
    function clearFieldError($field) {
        $field.removeClass('is-invalid');
        $field.siblings('.invalid-feedback').text('');
    }

    /**
     * Show success message
     */
    function showSuccessMessage(message) {
        const alertHtml = `
            <div class="alert alert-success alert-dismissible fade show category-success-alert" role="alert">
                <i class="bi bi-check-circle me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>`;

        // Insert after the first h1 element
        $('h1').first().after(alertHtml);

        // Auto-dismiss after 3 seconds
        setTimeout(() => {
            $('.category-success-alert').fadeOut(400, function() {
                $(this).remove();
            });
        }, 3000);
    }

    /**
     * Fallback to standard categories if loading fails
     */
    function fallbackToStandardCategories() {
        console.log('CategoryManager: Using fallback categories');
        categories = config.standardCategories;
        populateSelects();
        setupEventHandlers();
        handleExistingValues();
    }

    /**
     * Public API
     */
    return {
        init: init,
        getCategories: () => categories,
        addCategory: (categoryName) => {
            if (!categories.includes(categoryName)) {
                categories.push(categoryName);
                categories.sort();
                populateSelects();
            }
        }
    };
})();

// Auto-initialize when DOM is ready
$(document).ready(() => {
    CategoryManager.init();
});

// Export for global access
window.CategoryManager = CategoryManager;