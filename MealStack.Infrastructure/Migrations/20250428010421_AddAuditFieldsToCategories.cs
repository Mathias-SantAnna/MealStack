using System; 
using Microsoft.EntityFrameworkCore.Migrations; 

#nullable disable

namespace MealStack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>( 
                name: "CreatedDate",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)); 

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Categories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Categories");
        }
    }
}