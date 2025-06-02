using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealStack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateRecipeCategoriesJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if RecipeCategories table already exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'RecipeCategories') THEN
                        CREATE TABLE ""RecipeCategories"" (
                            ""RecipeId"" integer NOT NULL,
                            ""CategoryId"" integer NOT NULL,
                            CONSTRAINT ""PK_RecipeCategories"" PRIMARY KEY (""RecipeId"", ""CategoryId""),
                            CONSTRAINT ""FK_RecipeCategories_Categories_CategoryId"" FOREIGN KEY (""CategoryId"") REFERENCES ""Categories"" (""Id"") ON DELETE RESTRICT,
                            CONSTRAINT ""FK_RecipeCategories_Recipes_RecipeId"" FOREIGN KEY (""RecipeId"") REFERENCES ""Recipes"" (""Id"") ON DELETE CASCADE
                        );
                        
                        CREATE INDEX ""IX_RecipeCategories_CategoryId"" ON ""RecipeCategories"" (""CategoryId"");
                        
                        RAISE NOTICE 'RecipeCategories table created successfully';
                    ELSE
                        RAISE NOTICE 'RecipeCategories table already exists, skipping creation';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeCategories");
        }
    }
}