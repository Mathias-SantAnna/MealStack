$(document).ready(function() {
    $("#ingredientSearch").on("keyup", function() {
        filterIngredients();
    });
    
    $("#categoryFilter").on("change", function() {
        filterIngredients();
    });
    
    $("#measurementFilter").on("change", function() {
        filterIngredients();
    });
    
    function filterIngredients() {
        const searchTerm = $("#ingredientSearch").val().toLowerCase();
        const categoryFilter = $("#categoryFilter").val();
        const measurementFilter = $("#measurementFilter").val();

        $("#ingredientsTable tbody tr").each(function() {
            const $row = $(this);
            const name = $row.find("td:first").text().toLowerCase();
            const category = $row.data("category");
            const measurement = $row.data("measurement");

            const matchesSearch = name.includes(searchTerm);
            const matchesCategory = !categoryFilter || category === categoryFilter;
            const matchesMeasurement = !measurementFilter || measurement === measurementFilter;

            if (matchesSearch && matchesCategory && matchesMeasurement) {
                $row.show();
            } else {
                $row.hide();
            }
        });
    }
});