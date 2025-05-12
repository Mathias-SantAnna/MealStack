using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealStack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Recipes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Recipes");
        }
    }
}
