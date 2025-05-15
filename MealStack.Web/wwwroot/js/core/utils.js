const Utils = (function() {
    // Debounce function to limit how often a function can fire
    const debounce = function(func, wait, immediate) {
        let timeout;
        return function() {
            const context = this, args = arguments;
            const later = function() {
                timeout = null;
                if (!immediate) func.apply(context, args);
            };
            const callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func.apply(context, args);
        };
    };

    const formatTime = function(minutes) {
        if (!minutes) return '0m';

        const hours = Math.floor(minutes / 60);
        const mins = minutes % 60;

        if (hours > 0) {
            return `${hours}h${mins > 0 ? ` ${mins}m` : ''}`;
        } else {
            return `${mins}m`;
        }
    };

    const formatDate = function(dateString) {
        if (!dateString) return '';

        const date = new Date(dateString);
        return date.toLocaleDateString();
    };

    // Get URL parameter by name
    const getUrlParameter = function(name) {
        name = name.replace(/[\[]/, '\\[').replace(/[\]]/, '\\]');
        const regex = new RegExp('[\\?&]' + name + '=([^&#]*)');
        const results = regex.exec(location.search);
        return results === null ? '' : decodeURIComponent(results[1].replace(/\+/g, ' '));
    };

    // Parse JSON safely
    const parseJson = function(jsonString, defaultValue = null) {
        try {
            return JSON.parse(jsonString);
        } catch (e) {
            console.error('Error parsing JSON:', e);
            return defaultValue;
        }
    };

    // Public API
    return {
        debounce,
        formatTime,
        formatDate,
        getUrlParameter,
        parseJson
    };
})();

window.Utils = Utils;