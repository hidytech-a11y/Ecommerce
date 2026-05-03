using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCartUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH ranked_carts AS (
                    SELECT
                        "Id",
                        "UserId",
                        FIRST_VALUE("Id") OVER (PARTITION BY "UserId" ORDER BY "Id") AS keep_id,
                        ROW_NUMBER() OVER (PARTITION BY "UserId" ORDER BY "Id") AS row_number
                    FROM "Carts"
                )
                UPDATE "CartItem" AS item
                SET "CartId" = ranked.keep_id
                FROM ranked_carts AS ranked
                WHERE item."CartId" = ranked."Id"
                  AND ranked.row_number > 1;

                WITH ranked_carts AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (PARTITION BY "UserId" ORDER BY "Id") AS row_number
                    FROM "Carts"
                )
                DELETE FROM "Carts" AS cart
                USING ranked_carts AS ranked
                WHERE cart."Id" = ranked."Id"
                  AND ranked.row_number > 1;

                WITH ranked_items AS (
                    SELECT
                        "Id",
                        SUM("Quantity") OVER (PARTITION BY "CartId", "ProductId")::integer AS total_quantity,
                        ROW_NUMBER() OVER (PARTITION BY "CartId", "ProductId" ORDER BY "Id") AS row_number
                    FROM "CartItem"
                )
                UPDATE "CartItem" AS item
                SET "Quantity" = ranked.total_quantity
                FROM ranked_items AS ranked
                WHERE item."Id" = ranked."Id"
                  AND ranked.row_number = 1;

                WITH ranked_items AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (PARTITION BY "CartId", "ProductId" ORDER BY "Id") AS row_number
                    FROM "CartItem"
                )
                DELETE FROM "CartItem" AS item
                USING ranked_items AS ranked
                WHERE item."Id" = ranked."Id"
                  AND ranked.row_number > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId_ProductId",
                table: "CartItem",
                columns: new[] { "CartId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItem_CartId_ProductId",
                table: "CartItem");
        }
    }
}
