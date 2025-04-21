using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealStack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, copy DisplayName values to UserName where UserName is email-like
            migrationBuilder.Sql(@"
                UPDATE ""AspNetUsers""
                SET ""UserName"" = ""DisplayName"", ""NormalizedUserName"" = UPPER(""DisplayName"")
                WHERE ""DisplayName"" IS NOT NULL AND ""UserName"" LIKE '%@%';
            ");
    
            // Then drop DisplayName column
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add DisplayName column back
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        
            // Copy UserName to DisplayName for restored compatibility
            migrationBuilder.Sql(@"
                UPDATE ""AspNetUsers""
                SET ""DisplayName"" = ""UserName""
                WHERE ""UserName"" NOT LIKE '%@%';
            ");
        }
    }
}