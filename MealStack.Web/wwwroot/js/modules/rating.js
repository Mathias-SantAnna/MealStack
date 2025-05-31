
console.log("🎨 Rating.js loaded - Visual Update Version");

$(document).ready(function() {
    console.log("🎨 Setting up rating system with visual updates...");

    const ratingWidgets = $('.rating-widget');
    console.log(`🎨 Found ${ratingWidgets.length} rating widgets`);

    if (ratingWidgets.length === 0) {
        console.log("ℹ️ No rating widgets found");
        return;
    }

    ratingWidgets.each(function() {
        const $widget = $(this);
        const currentUserRating = parseInt($widget.data('user-rating')) || 0;
        console.log(`🎨 Initializing widget with user rating: ${currentUserRating}`);
        updateStarDisplay($widget, currentUserRating);
    });

    $('.star-rating').off('click.rating').on('click.rating', function(e) {
        e.preventDefault();
        e.stopPropagation();

        console.log("⭐ STAR CLICKED!");

        const $star = $(this);
        const $widget = $star.closest('.rating-widget');
        const recipeId = $widget.data('recipe-id');
        const rating = parseInt($star.data('rating'));

        console.log(`⭐ Submitting rating ${rating} for recipe ${recipeId}`);

        if (!recipeId || !rating || rating < 1 || rating > 5) {
            alert("Error: Invalid rating data");
            return;
        }

        // Update stars Real Time
        console.log(`🎨 Immediate visual update: showing ${rating} stars`);
        updateStarDisplay($widget, rating);
        $widget.data('user-rating', rating);

        $widget.find('.star-rating').css('opacity', '0.7');

        submitRating(recipeId, rating, $widget);
    });

    $('.star-rating').hover(
        function() {
            const $star = $(this);
            const rating = parseInt($star.data('rating'));
            const $widget = $star.closest('.rating-widget');

            // Highlight stars for hover preview
            $widget.find('.star-rating').each(function() {
                const starValue = parseInt($(this).data('rating'));
                const $currentStar = $(this);

                if (starValue <= rating) {
                    $currentStar.removeClass('bi-star').addClass('bi-star-fill text-warning');
                } else {
                    $currentStar.removeClass('bi-star-fill text-warning').addClass('bi-star');
                }
            });
        },
        function() {
            const $widget = $(this).closest('.rating-widget');
            const userRating = parseInt($widget.data('user-rating')) || 0;
            updateStarDisplay($widget, userRating);
        }
    );

    console.log("🎨 Rating system setup complete");

    function updateStarDisplay($widget, userRating) {
        console.log(`🎨 Updating star display to ${userRating} stars`);

        $widget.find('.star-rating').each(function() {
            const starValue = parseInt($(this).data('rating'));
            const $star = $(this);

            $star.removeClass('bi-star bi-star-fill text-warning');

            if (starValue <= userRating) {
                $star.addClass('bi-star-fill text-warning');
                console.log(`🎨 Star ${starValue}: FILLED`);
            } else {
                $star.addClass('bi-star');
                console.log(`🎨 Star ${starValue}: EMPTY`);
            }
        });

        $widget.find('.star-rating').each(function() {
            this.style.display = 'none';
            this.offsetHeight; 
            this.style.display = '';
        });
    }

    function submitRating(recipeId, rating, $widget) {
        console.log("📡 Submitting rating to server...");

        const token = $('input[name="__RequestVerificationToken"]').val();

        if (!token) {
            alert("Security error: Please refresh the page.");
            return;
        }

        $.ajax({
            url: '/Recipe/RateRecipe',
            type: 'POST',
            data: {
                recipeId: recipeId,
                rating: rating,
                __RequestVerificationToken: token
            },
            success: function(result) {
                console.log("✅ Server response:", result);

                if (result && result.success) {
                    console.log(`🎨 Server confirmed rating ${rating} - ensuring visual state`);
                    updateStarDisplay($widget, rating);
                    $widget.data('user-rating', rating);

                    if (result.averageRating !== undefined) {
                        $('#average-rating').text(parseFloat(result.averageRating).toFixed(1));
                        console.log(`📊 Updated average rating: ${result.averageRating}`);
                    }

                    if (result.totalRatings !== undefined) {
                        $('#total-ratings').text(result.totalRatings);
                        console.log(`📊 Updated total ratings: ${result.totalRatings}`);
                    }

                    showSuccessMessage(`Rated ${rating} stars!`);

                    $widget.find('.star-rating').addClass('animate-pulse');
                    setTimeout(() => {
                        $widget.find('.star-rating').removeClass('animate-pulse');
                    }, 1000);

                } else {
                    console.error("❌ Server error:", result);
                    alert("Error: " + (result.message || "Failed to submit rating"));

                    const previousRating = parseInt($widget.data('user-rating')) || 0;
                    updateStarDisplay($widget, previousRating);
                }
            },
            error: function(xhr, status, error) {
                console.error("❌ AJAX Error:", error);
                alert("Failed to submit rating. Please try again.");

                const previousRating = parseInt($widget.data('user-rating')) || 0;
                updateStarDisplay($widget, previousRating);
            },
            complete: function() {
                $widget.find('.star-rating').css('opacity', '1');
                console.log("📡 AJAX request completed");
            }
        });
    }

    function showSuccessMessage(message) {
        $('.rating-success-message').remove();

        const messageHtml = `
            <div class="alert alert-success alert-dismissible fade show rating-success-message position-fixed" 
                 style="top: 20px; right: 20px; z-index: 9999; min-width: 300px;" role="alert">
                <i class="bi bi-check-circle me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        $('body').append(messageHtml);

        setTimeout(() => {
            $('.rating-success-message').fadeOut(500, function() {
                $(this).remove();
            });
        }, 3000);
    }
});

const style = document.createElement('style');
style.textContent = `
    .star-rating {
        transition: all 0.2s ease !important;
        cursor: pointer !important;
    }
    
    .star-rating:hover {
        transform: scale(1.1) !important;
    }
    
    .animate-pulse {
        animation: rating-pulse 0.6s ease-in-out !important;
    }
    
    @keyframes rating-pulse {
        0% { transform: scale(1); }
        50% { transform: scale(1.2); }
        100% { transform: scale(1); }
    }
`;
document.head.appendChild(style);

console.log("🎨 Rating system CSS added");