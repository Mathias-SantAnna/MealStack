// search.js - Script for search functionality
$(document).ready(function() {
    console.log("Search.js loaded");

    // Initialize autocomplete for search inputs
    $("#searchTerm").autocomplete({
        source: function(request, response) {
            console.log("Autocomplete request for: " + request.term);
            $.ajax({
                url: "/Recipe/GetSearchSuggestions",
                dataType: "json",
                data: { term: request.term },
                success: function(data) {
                    console.log("Received " + data.length + " suggestions");
                    response(data);
                },
                error: function(xhr, status, error) {
                    console.error("Autocomplete error: " + error);
                }
            });
        },
        minLength: 2,
        delay: 300,
        select: function(event, ui) {
            $("#searchTerm").val(ui.item.value);
            $("#searchForm").submit();
            return false;
        }
    });

    // Show advanced search options if any filter is applied
    if ($("#difficulty").val() || $("#sortBy").val() !== "newest" ||
        $("#matchAllIngredients").prop("checked") === false) {
        $("#advancedSearchOptions").collapse("show");
    }
});