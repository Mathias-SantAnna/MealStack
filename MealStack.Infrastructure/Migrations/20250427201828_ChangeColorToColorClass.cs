// 20250427201828_ChangeColorToColorClass.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealStack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeColorToColorClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorClass",
                table: "Categories",
                type: "text",
                nullable: true);

            // Copy data from Color to ColorClass with appropriate mapping
            migrationBuilder.Sql(@"
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'primary' 
                WHERE ""Color"" = '#0d6efd';
                
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'secondary' 
                WHERE ""Color"" = '#6c757d';
                
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'success' 
                WHERE ""Color"" = '#198754';
                
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'danger' 
                WHERE ""Color"" = '#dc3545';
                
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'warning' 
                WHERE ""Color"" = '#ffc107';
                
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'info' 
                WHERE ""Color"" = '#0dcaf0';
                
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'light' 
                WHERE ""Color"" = '#f8f9fa';
                
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'dark' 
                WHERE ""Color"" = '#212529';
                
                -- For any other colors, default to 'secondary'
                UPDATE ""Categories"" 
                SET ""ColorClass"" = 'secondary' 
                WHERE ""ColorClass"" IS NULL;
            ");

            // Remove the old Color column
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the Color column
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Categories",
                type: "text",
                nullable: true);

            // Restore basic color mapping 
            migrationBuilder.Sql(@"
                UPDATE ""Categories"" 
                SET ""Color"" = '#0d6efd' 
                WHERE ""ColorClass"" = 'primary';
                
                UPDATE ""Categories"" 
                SET ""Color"" = '#6c757d' 
                WHERE ""ColorClass"" = 'secondary';
                
                UPDATE ""Categories"" 
                SET ""Color"" = '#198754' 
                WHERE ""ColorClass"" = 'success';
                
                UPDATE ""Categories"" 
                SET ""Color"" = '#dc3545' 
                WHERE ""ColorClass"" = 'danger';
                
                UPDATE ""Categories"" 
                SET ""Color"" = '#ffc107' 
                WHERE ""ColorClass"" = 'warning';
                
                UPDATE ""Categories"" 
                SET ""Color"" = '#0dcaf0' 
                WHERE ""ColorClass"" = 'info';
                
                UPDATE ""Categories"" 
                SET ""Color"" = '#f8f9fa' 
                WHERE ""ColorClass"" = 'light';
                
                UPDATE ""Categories"" 
                SET ""Color"" = '#212529' 
                WHERE ""ColorClass"" = 'dark';
            ");
            
            migrationBuilder.DropColumn(
                name: "ColorClass",
                table: "Categories");
        }
    }
}