const FavoriteModule = (function() {
    let options = {
        favoriteButtonSelector: '.favorite-btn',
        favoriteContainerSelector: '.favorite-container',
        toggleFavoriteUrl: '/Recipe/ToggleFavorite'
    };

    const init = function(config = {}) {
        console.log("Initializing FavoriteModule...");

        options = {
            ...options,
            ...config
        };

        console.log("Favorite button selector:", options.favoriteButtonSelector);
        console.log("Favorite container selector:", options.favoriteContainerSelector);

        setupFavorites();
    };

    const setupFavorites = function() {
        console.log("Setting up favorites functionality");

        $(document).on('click', options.favoriteButtonSelector, function(e) {
            e.preventDefault();
            e.stopPropagation();

            console.log("Favorite button clicked");

            const btn = $(this);
            const container = btn.closest(options.favoriteContainerSelector);

            const recipeId = container.length ? container.data('recipe-id') : btn.data('recipe-id');

            console.log("Recipe ID:", recipeId);

            if (!recipeId) {
                console.error("No recipe ID found!");
                return;
            }

            if (btn.data('processing')) {
                console.log("Already processing, ignoring click");
                return;
            }

            btn.data('processing', true);

            const originalContent = btn.html();
            btn.prop('disabled', true);
            btn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');

            // Prepare anti-forgery token
            const token = $('input[name="__RequestVerificationToken"]').val();

            if (!token) {
                console.warn("No anti-forgery token found, this might cause issues");
            }

            // AJAX call to toggle favorite status
            $.ajax({
                url: '/Recipe/ToggleFavorite',
                type: 'POST',
                data: {
                    recipeId: recipeId,
                    __RequestVerificationToken: token
                },
                success: function(result) {
                    console.log("Toggle favorite result:", result);
                    handleFavoriteToggleSuccess(result, btn);
                },
                error: function(xhr, status, error) {
                    console.error("Error toggling favorite:", error);
                    console.error("Response:", xhr.responseText);
                    handleFavoriteToggleError(error, btn, originalContent);
                },
                complete: function() {
                    btn.prop('disabled', false);
                    btn.data('processing', false);
                }
            });
        });
    };

    // Handle successful favorite toggle
    const handleFavoriteToggleSuccess = function(result, btn) {
        if (result.success) {
            btn.data('is-favorite', result.isFavorite);

            if (result.isFavorite) {
                btn.html('<i class="bi bi-heart-fill text-danger fs-5"></i>');
                animateHeartFill(btn);
            } else {
                btn.html('<i class="bi bi-heart fs-5"></i>');

                // Recipe card disappears if unfavorited in MyFavorites page
                if (window.location.pathname.includes('/Recipe/MyFavorites')) {
                    const recipeCard = btn.closest('.recipe-card-wrapper');

                    if (recipeCard && recipeCard.length) {
                        recipeCard.fadeOut(900, function() {
                            $(this).remove();

                            if ($('.recipe-card-wrapper:visible').length === 0) {
                                location.reload();
                            }
                        });
                    }
                }
            }
        } else {
            console.warn("Favorite toggle returned success=false");
            alert(result.message || "Failed to update favorite status");
        }
    };

    const handleFavoriteToggleError = function(error, btn, originalContent) {
        console.error('Error toggling favorite:', error);
        alert('Failed to update favorite status. Please try again.');
        btn.html(originalContent);
    };

    const animateHeartFill = function(btn) {
        const heart = btn.find('i');
        heart.addClass('animate-heart');
        setTimeout(() => heart.removeClass('animate-heart'), 600);
    };

    // Public API
    return {
        init
    };
})();

window.FavoriteModule = FavoriteModule;