using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightedAverageCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "total_cost",
                table: "sales_invoice_items",
                type: "TEXT",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                table: "sales_invoice_items",
                type: "TEXT",
                precision: 14,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "current_average_cost",
                table: "products",
                type: "TEXT",
                precision: 14,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE products SET current_average_cost = purchase_price;");
            migrationBuilder.Sql("""
                UPDATE sales_invoice_items
                SET unit_cost = COALESCE((SELECT current_average_cost FROM products WHERE products.Id = sales_invoice_items.product_id), 0),
                    total_cost = ROUND(quantity * COALESCE((SELECT current_average_cost FROM products WHERE products.Id = sales_invoice_items.product_id), 0), 2);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_invoice_items_total_cost_non_negative",
                table: "sales_invoice_items",
                sql: "total_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_invoice_items_unit_cost_non_negative",
                table: "sales_invoice_items",
                sql: "unit_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_current_average_cost_non_negative",
                table: "products",
                sql: "current_average_cost >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_invoice_items_total_cost_non_negative",
                table: "sales_invoice_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_invoice_items_unit_cost_non_negative",
                table: "sales_invoice_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_current_average_cost_non_negative",
                table: "products");

            migrationBuilder.DropColumn(
                name: "total_cost",
                table: "sales_invoice_items");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                table: "sales_invoice_items");

            migrationBuilder.DropColumn(
                name: "current_average_cost",
                table: "products");
        }
    }
}
