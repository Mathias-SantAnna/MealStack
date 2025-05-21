const RatingModule = (function() {
    let options = {
        ratingWidgetSelector: '.rating-widget',
        starSelector: '.star-rating',
        averageRatingSelector: '#average-rating',
        totalRatingsSelector: '#total-ratings',
        rateRecipeUrl: '/Recipe/RateRecipe'
    };

    const init = function(config = {}) {
        console.log("Initializing RatingModule...");

        options = {
            ...options,
            ...config
        };

        setupRatingStars();

        console.log("RatingModule initialized successfully");
    };

    const setupRatingStars = function() {
        $(document).on('click', options.starSelector, function() {
            const $star = $(this);
            const $widget = $star.closest(options.ratingWidgetSelector);
            const recipeId = $widget.data('recipe-id');
            const rating = $star.data('rating');

            if (!recipeId) {
                console.error("No recipe ID found on rating widget");
                return;
            }

            console.log(`Rating recipe ID: ${recipeId} with ${rating} stars`);
            submitRating(recipeId, rating, $widget);
        });
    };

    // Submit rating via AJAX
    const submitRating = function(recipeId, rating, $widget) {
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: options.rateRecipeUrl,
            type: 'POST',
            data: {
                recipeId: recipeId,
                rating: rating,
                __RequestVerificationToken: token
            },
            success: function(result) {
                if (result.success) {
                    updateRatingDisplay($widget, rating, result.averageRating, result.totalRatings);
                } else {
                    alert(result.message || "Error submitting rating");
                }
            },
            error: function(xhr, status, error) {
                console.error("Error submitting rating:", error);
                alert("Failed to submit rating. Please try again.");
            }
        });
    };

    const updateRatingDisplay = function($widget, userRating, averageRating, totalRatings) {
        $widget.find(options.starSelector).each(function() {
            const starValue = parseInt($(this).data('rating'));
            if (starValue <= userRating) {
                $(this).removeClass('bi-star').addClass('bi-star-fill');
            } else {
                $(this).removeClass('bi-star-fill').addClass('bi-star');
            }
        });

        const $avgRating = $widget.find(options.averageRatingSelector);
        if ($avgRating.length) {
            $avgRating.text(parseFloat(averageRating).toFixed(1));
        }

        const $totalRatings = $widget.find(options.totalRatingsSelector);
        if ($totalRatings.length) {
            $totalRatings.text(totalRatings);
        }

        // Store the user's rating as data attribute
        $widget.data('user-rating', userRating);
    };

    return {
        init
    };
})();

window.RatingModule = RatingModule;