using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPaymentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "paid_amount",
                table: "sales_invoices",
                type: "TEXT",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "remaining_amount",
                table: "sales_invoices",
                type: "TEXT",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                "UPDATE sales_invoices SET paid_amount = 0, remaining_amount = net_total_amount");

            if (ActiveProvider != "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.AddCheckConstraint(
                    name: "ck_sales_invoices_payment_amounts",
                    table: "sales_invoices",
                    sql: "paid_amount >= 0 AND remaining_amount >= 0 AND paid_amount + remaining_amount = net_total_amount");
            }

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider != "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.DropCheckConstraint(
                    name: "ck_sales_invoices_payment_amounts",
                    table: "sales_invoices");
            }

            migrationBuilder.DropColumn(
                name: "paid_amount",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "remaining_amount",
                table: "sales_invoices");
        }
    }
}
