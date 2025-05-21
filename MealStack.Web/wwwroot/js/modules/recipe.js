const RecipeModule = (function() {
    let options = {
        deleteConfirmButtonSelector: '.delete-recipe-btn',
        deleteFormPrefix: 'delete-form-',
        printButtonSelector: '#print-recipe-btn',
        shareButtonSelector: '#share-recipe-btn'
    };

    const init = function(config = {}) {
        console.log("Initializing RecipeModule...");

        options = {
            ...options,
            ...config
        };

        setupEventHandlers();

        console.log("RecipeModule initialized successfully");
    };

    const setupEventHandlers = function() {
        $(document).on('click', 'a[onclick*="confirmDelete"]', function(e) {
            e.preventDefault();
            const recipeId = $(this).data('recipe-id') ||
                $(this).attr('onclick').match(/confirmDelete\((\d+)/)?.[1];

            const recipeName = $(this).data('recipe-name') ||
                $(this).attr('onclick').match(/confirmDelete\(\d+,\s*['"]([^'"]+)['"]/)?.[1] ||
                'this recipe';

            console.log(`Confirm delete for recipe ID: ${recipeId}, Name: ${recipeName}`);

            if (recipeId && confirm(`Are you sure you want to delete "${recipeName}"?`)) {
                $(`#delete-form-${recipeId}`).submit();
            }
        });

        // Print recipe
        $(document).on('click', options.printButtonSelector, function() {
            window.print();
        });

        // Share recipe
        $(document).on('click', options.shareButtonSelector, function() {
            if (navigator.share) {
                const title = $('h1').first().text();
                const url = window.location.href;

                navigator.share({
                    title: title,
                    url: url
                })
                    .then(() => console.log('Shared successfully'))
                    .catch(err => console.error('Error sharing:', err));
            } else {
                alert('Share functionality is not supported in your browser. You can copy the URL manually.');
            }
        });
    };

    const confirmDelete = function(id, name) {
        console.log(`Delete request for recipe ID: ${id}, Name: ${name}`);
        if (confirm(`Are you sure you want to delete the recipe "${name}"?`)) {
            $(`#delete-form-${id}`).submit();
            return true;
        }
        return false;
    };

    return {
        init,
        confirmDelete
    };
})();

window.RecipeModule = RecipeModule;