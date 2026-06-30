using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsPOS.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260615035130_AddInvoicePaymentStatus")]
public sealed class AddInvoicePaymentStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "payment_status",
            table: "sales_invoices",
            type: "TEXT",
            maxLength: 30,
            nullable: false,
            defaultValue: "Unpaid");

        migrationBuilder.AddColumn<string>(
            name: "payment_status",
            table: "purchase_invoices",
            type: "TEXT",
            maxLength: 30,
            nullable: false,
            defaultValue: "Unpaid");

        migrationBuilder.AddCheckConstraint(
            name: "ck_sales_invoices_payment_status",
            table: "sales_invoices",
            sql: "payment_status IN ('Paid', 'Unpaid', 'PartiallyPaid')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_purchase_invoices_payment_status",
            table: "purchase_invoices",
            sql: "payment_status IN ('Paid', 'Unpaid', 'PartiallyPaid')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_sales_invoices_payment_status",
            table: "sales_invoices");

        migrationBuilder.DropCheckConstraint(
            name: "ck_purchase_invoices_payment_status",
            table: "purchase_invoices");

        migrationBuilder.DropColumn(
            name: "payment_status",
            table: "sales_invoices");

        migrationBuilder.DropColumn(
            name: "payment_status",
            table: "purchase_invoices");
    }
}
