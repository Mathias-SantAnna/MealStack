const AjaxService = (function() {
    const getAntiForgeryToken = function() {
        return $('input[name="__RequestVerificationToken"]').val();
    };

    const handleError = function(message, error, onError) {
        console.error(`${message}: ${error}`);

        if (typeof onError === 'function') {
            onError(error);
        } else {
            alert(`${message}. Please try again.`);
        }
    };

    // Post data with AJAX
    const post = function(url, data, onSuccess, onError) {
        let requestData = data;
        if (!(data instanceof FormData)) {
            requestData = {
                ...data,
                __RequestVerificationToken: getAntiForgeryToken()
            };
        }

        // Make the AJAX request
        return $.ajax({
            url: url,
            type: 'POST',
            data: requestData,
            success: function(result) {
                if (typeof onSuccess === 'function') {
                    onSuccess(result);
                }
            },
            error: function(xhr, status, error) {
                handleError(`Error during request to ${url}`, error, onError);
            }
        });
    };

    // GET request with AJAX
    const get = function(url, data, onSuccess, onError) {
        return $.ajax({
            url: url,
            type: 'GET',
            data: data,
            success: function(result) {
                if (typeof onSuccess === 'function') {
                    onSuccess(result);
                }
            },
            error: function(xhr, status, error) {
                handleError(`Error during request to ${url}`, error, onError);
            }
        });
    };

    // Public API
    return {
        post,
        get
    };
})();

window.AjaxService = AjaxService;