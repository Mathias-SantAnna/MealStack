const DateUtils = (function() {
    const formatISODate = function(date) {
        if (!date) date = new Date();

        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');

        return `${year}-${month}-${day}`;
    };
    
    const formatLocalDate = function(date) {
        if (!date) return '';

        if (typeof date === 'string') {
            date = new Date(date);
        }

        return date.toLocaleDateString(undefined, {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };
    
    const parseDate = function(dateString) {
        if (!dateString) return null;

        let date = new Date(dateString);
        if (!isNaN(date.getTime())) {
            return date;
        }

        const formats = [
            // ISO format
            {
                regex: /^(\d{4})-(\d{2})-(\d{2})$/,
                parse: function(matches) {
                    return new Date(
                        parseInt(matches[1], 10),
                        parseInt(matches[2], 10) - 1,
                        parseInt(matches[3], 10)
                    );
                }
            },
            // MM/DD/YYYY format
            {
                regex: /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/,
                parse: function(matches) {
                    return new Date(
                        parseInt(matches[3], 10),
                        parseInt(matches[1], 10) - 1,
                        parseInt(matches[2], 10)
                    );
                }
            },
            // DD/MM/YYYY format
            {
                regex: /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/,
                parse: function(matches) {
                    return new Date(
                        parseInt(matches[3], 10),
                        parseInt(matches[2], 10) - 1,
                        parseInt(matches[1], 10)
                    );
                }
            }
        ];

        for (const format of formats) {
            const matches = dateString.match(format.regex);
            if (matches) {
                date = format.parse(matches);
                if (!isNaN(date.getTime())) {
                    return date;
                }
            }
        }

        return null;
    };
    
    const daysBetween = function(startDate, endDate) {
        if (typeof startDate === 'string') startDate = new Date(startDate);
        if (typeof endDate === 'string') endDate = new Date(endDate);

        // Reset hours to ensure correct day count
        startDate = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate());
        endDate = new Date(endDate.getFullYear(), endDate.getMonth(), endDate.getDate());

        const millisecondsPerDay = 1000 * 60 * 60 * 24;
        const millisecondsDifference = Math.abs(endDate - startDate);

        return Math.round(millisecondsDifference / millisecondsPerDay) + 1;
    };
    
    const dateRange = function(startDate, endDate) {
        if (typeof startDate === 'string') startDate = new Date(startDate);
        if (typeof endDate === 'string') endDate = new Date(endDate);

        const dates = [];
        const currentDate = new Date(startDate);

        while (currentDate <= endDate) {
            dates.push(new Date(currentDate));
            currentDate.setDate(currentDate.getDate() + 1);
        }

        return dates;
    };

    return {
        formatISODate,
        formatLocalDate,
        parseDate,
        daysBetween,
        dateRange
    };
})();

window.DateUtils = DateUtils;