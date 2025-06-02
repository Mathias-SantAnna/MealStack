using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealStack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrphanedCategoryIdFromRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if CategoryId column exists before trying to drop it
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name = 'Recipes' AND column_name = 'CategoryId') THEN
                        ALTER TABLE ""Recipes"" DROP COLUMN ""CategoryId"";
                        RAISE NOTICE 'CategoryId column dropped from Recipes table';
                    ELSE
                        RAISE NOTICE 'CategoryId column does not exist in Recipes table, skipping drop';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Recipes",
                type: "integer",
                nullable: true);
        }
    }
}