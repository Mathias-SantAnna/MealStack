using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealStack.Infrastructure.Migrations
{
    public partial class DropCreatedByIdFromCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // drop the FK only if it's there
            migrationBuilder.Sql(
                "ALTER TABLE \"Categories\" " +
                "DROP CONSTRAINT IF EXISTS \"FK_Categories_AspNetUsers_CreatedById\";");

            // drop the index only if it's there
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Categories_CreatedById\";");

            // drop the column if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'categories' AND column_name = 'createdbyid'
                    ) THEN
                        ALTER TABLE ""Categories"" DROP COLUMN ""CreatedById"";
                    END IF;
                END $$;
            ");
            
            // Note: Removed the direct DropColumn operation
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // if you ever roll back, re-create the column
            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Categories",
                type: "text",
                nullable: true);

            // and the index/foreign key
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_Categories_CreatedById\" " +
                "ON \"Categories\" (\"CreatedById\");");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_CreatedById",
                table: "Categories",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}