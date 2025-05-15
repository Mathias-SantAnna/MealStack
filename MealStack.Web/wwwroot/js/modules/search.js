const SearchModule = (function() {
    let options = {
        searchTermInput: '#searchTerm',          
        searchForm: '#searchForm',              
        advancedSearchOptionsToggle: '#advancedSearchOptions', 
        difficultySelect: '#difficulty',        
        sortBySelect: '#sortBy',                
        matchAllIngredientsCheckbox: '#matchAllIngredients', 
        suggestionsUrl: "/Recipe/GetSearchSuggestions" 
    };

    function initializeAutocomplete() {
        if ($(options.searchTermInput).length && typeof $.ui !== 'undefined' && $.ui.autocomplete) {
            $(options.searchTermInput).autocomplete({
                source: function(request, response) {
                    console.log("Autocomplete request for: " + request.term);
                    AjaxService.get(options.suggestionsUrl, { term: request.term }, function(data) {
                        console.log("Received " + data.length + " suggestions");
                        response(data); // Assuming data is already an array of strings or {label, value}
                    });
                },
                minLength: 2,
                delay: 300,
                select: function(event, ui) {
                    $(options.searchTermInput).val(ui.item.value);
                    $(options.searchForm).submit();
                    return false;
                }
            });
        } else {
            console.warn("Search term input or jQuery UI autocomplete not found/available.");
        }
    }

    function toggleAdvancedSearchOptions() {
        $(options.advancedSearchOptionsToggle).collapse('hide');
    }

    const init = function(config = {}) {
        console.log("SearchModule initializing...");
        options = $.extend({}, options, config);

        initializeAutocomplete();

        $(options.advancedSearchOptionsToggle).collapse('hide');

        $(options.difficultySelect + ', ' + options.sortBySelect + ', ' + options.matchAllIngredientsCheckbox).on('change', function(){
            toggleAdvancedSearchOptions();
        });

        console.log("SearchModule initialized.");
    };

    return {
        init: init
    };
})();

window.SearchModule = SearchModule;