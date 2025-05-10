using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealStack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFavoritesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if foreign key exists before dropping
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Categories_AspNetUsers_CreatedById'
                    ) THEN
                        ALTER TABLE ""Categories"" DROP CONSTRAINT ""FK_Categories_AspNetUsers_CreatedById"";
                    END IF;
                END $$;
            ");

            // Check if UserFavoriteEntity foreign keys exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_name = 'userfavoriteentity'
                    ) THEN
                        IF EXISTS (
                            SELECT FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_UserFavoriteEntity_AspNetUsers_UserId'
                        ) THEN
                            ALTER TABLE ""UserFavoriteEntity"" DROP CONSTRAINT ""FK_UserFavoriteEntity_AspNetUsers_UserId"";
                        END IF;
                        
                        IF EXISTS (
                            SELECT FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_UserFavoriteEntity_Recipes_RecipeId'
                        ) THEN
                            ALTER TABLE ""UserFavoriteEntity"" DROP CONSTRAINT ""FK_UserFavoriteEntity_Recipes_RecipeId"";
                        END IF;
                    END IF;
                END $$;
            ");
            

            // Check if index exists before dropping
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM pg_indexes 
                        WHERE indexname = 'IX_Categories_CreatedById'
                    ) THEN
                        DROP INDEX ""IX_Categories_CreatedById"";
                    END IF;
                END $$;
            ");

            // Check if primary key exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_name = 'userfavoriteentity'
                    ) THEN
                        IF EXISTS (
                            SELECT FROM information_schema.table_constraints 
                            WHERE constraint_name = 'PK_UserFavoriteEntity' AND constraint_type = 'PRIMARY KEY'
                        ) THEN
                            ALTER TABLE ""UserFavoriteEntity"" DROP CONSTRAINT ""PK_UserFavoriteEntity"";
                        END IF;
                    END IF;
                END $$;
            ");
            

            // Check if CreatedById column exists before dropping
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

            // Check if CreatedDate column exists before dropping
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.columns 
                        WHERE table_name = 'categories' AND column_name = 'createddate'
                    ) THEN
                        ALTER TABLE ""Categories"" DROP COLUMN ""CreatedDate"";
                    END IF;
                END $$;
            ");

            // Check if UserFavoriteEntity exists before renaming
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_name = 'userfavoriteentity'
                    ) THEN
                        ALTER TABLE ""UserFavoriteEntity"" RENAME TO ""UserFavorites"";
                    ELSE
                        CREATE TABLE ""UserFavorites"" (
                            ""UserId"" text NOT NULL,
                            ""RecipeId"" integer NOT NULL,
                            ""DateAdded"" timestamp with time zone NOT NULL,
                            CONSTRAINT ""PK_UserFavorites"" PRIMARY KEY (""UserId"", ""RecipeId"")
                        );
                    END IF;
                END $$;
            ");

            // Check if index exists before renaming
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM pg_indexes 
                        WHERE indexname = 'IX_UserFavoriteEntity_RecipeId'
                    ) THEN
                        ALTER INDEX ""IX_UserFavoriteEntity_RecipeId"" RENAME TO ""IX_UserFavorites_RecipeId"";
                    END IF;
                END $$;
            ");
            

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            // Check if table exists before adding primary key
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_name = 'userfavorites'
                    ) AND NOT EXISTS (
                        SELECT FROM information_schema.table_constraints 
                        WHERE constraint_name = 'PK_UserFavorites' AND constraint_type = 'PRIMARY KEY'
                    ) THEN
                        ALTER TABLE ""UserFavorites"" ADD CONSTRAINT ""PK_UserFavorites"" PRIMARY KEY (""UserId"", ""RecipeId"");
                    END IF;
                END $$;
            ");
            

            // Conditionally add foreign keys
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_name = 'userfavorites'
                    ) THEN
                        -- Add FK to AspNetUsers if not exists
                        IF NOT EXISTS (
                            SELECT FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_UserFavorites_AspNetUsers_UserId'
                        ) THEN
                            ALTER TABLE ""UserFavorites"" ADD CONSTRAINT ""FK_UserFavorites_AspNetUsers_UserId""
                            FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE;
                        END IF;
                        
                        -- Add FK to Recipes if not exists
                        IF NOT EXISTS (
                            SELECT FROM information_schema.table_constraints 
                            WHERE constraint_name = 'FK_UserFavorites_Recipes_RecipeId'
                        ) THEN
                            ALTER TABLE ""UserFavorites"" ADD CONSTRAINT ""FK_UserFavorites_Recipes_RecipeId""
                            FOREIGN KEY (""RecipeId"") REFERENCES ""Recipes"" (""Id"") ON DELETE CASCADE;
                        END IF;
                    END IF;
                END $$;
            ");
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration can be left as-is since it would only run if the up migration succeeded
            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorites_AspNetUsers_UserId",
                table: "UserFavorites");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorites_Recipes_RecipeId",
                table: "UserFavorites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFavorites",
                table: "UserFavorites");

            migrationBuilder.RenameTable(
                name: "UserFavorites",
                newName: "UserFavoriteEntity");

            migrationBuilder.RenameIndex(
                name: "IX_UserFavorites_RecipeId",
                table: "UserFavoriteEntity",
                newName: "IX_UserFavoriteEntity_RecipeId");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFavoriteEntity",
                table: "UserFavoriteEntity",
                columns: new[] { "UserId", "RecipeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CreatedById",
                table: "Categories",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_CreatedById",
                table: "Categories",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavoriteEntity_AspNetUsers_UserId",
                table: "UserFavoriteEntity",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavoriteEntity_Recipes_RecipeId",
                table: "UserFavoriteEntity",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}