using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqliteSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    setting_key = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    setting_value = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    is_system = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name_ar = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    invoice_number = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    net_total_amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    voided_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    void_reason = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_invoices", x => x.Id);
                    table.CheckConstraint("ck_sales_invoices_amounts_non_negative", "subtotal_amount >= 0 AND discount_amount >= 0 AND net_total_amount >= 0");
                    table.CheckConstraint("ck_sales_invoices_net_total_valid", "net_total_amount = subtotal_amount - discount_amount");
                    table.CheckConstraint("ck_sales_invoices_status", "status IN ('Posted', 'Voided')");
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name_ar = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    product_code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    barcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    name_ar = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    category_id = table.Column<long>(type: "INTEGER", nullable: false),
                    purchase_price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    selling_price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    current_stock = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    minimum_stock = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.CheckConstraint("ck_products_current_stock_non_negative", "current_stock >= 0");
                    table.CheckConstraint("ck_products_minimum_stock_non_negative", "minimum_stock >= 0");
                    table.CheckConstraint("ck_products_purchase_price_non_negative", "purchase_price >= 0");
                    table.CheckConstraint("ck_products_selling_price_non_negative", "selling_price >= 0");
                    table.ForeignKey(
                        name: "FK_products_product_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "product_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    invoice_number = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    supplier_id = table.Column<long>(type: "INTEGER", nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    total_amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    voided_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    void_reason = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_invoices", x => x.Id);
                    table.CheckConstraint("ck_purchase_invoices_status", "status IN ('Posted', 'Voided')");
                    table.CheckConstraint("ck_purchase_invoices_total_non_negative", "total_amount >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_invoices_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    product_id = table.Column<long>(type: "INTEGER", nullable: false),
                    transaction_type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    balance_after = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    reference_type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    reference_id = table.Column<long>(type: "INTEGER", nullable: false),
                    transaction_date = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transactions", x => x.Id);
                    table.CheckConstraint("ck_inventory_transactions_balance_after_non_negative", "balance_after >= 0");
                    table.CheckConstraint("ck_inventory_transactions_quantity_not_zero", "quantity <> 0");
                    table.CheckConstraint("ck_inventory_transactions_reference_type", "reference_type IN ('PurchaseInvoice', 'SalesInvoice', 'ManualAdjustment')");
                    table.CheckConstraint("ck_inventory_transactions_transaction_type", "transaction_type IN ('Purchase', 'Sale', 'Adjustment', 'VoidPurchase', 'VoidSale')");
                    table.ForeignKey(
                        name: "FK_inventory_transactions_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoice_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    sales_invoice_id = table.Column<long>(type: "INTEGER", nullable: false),
                    product_id = table.Column<long>(type: "INTEGER", nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_invoice_items", x => x.Id);
                    table.CheckConstraint("ck_sales_invoice_items_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_sales_invoice_items_total_price_non_negative", "total_price >= 0");
                    table.CheckConstraint("ck_sales_invoice_items_unit_price_non_negative", "unit_price >= 0");
                    table.ForeignKey(
                        name: "FK_sales_invoice_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_invoice_items_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoice_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    purchase_invoice_id = table.Column<long>(type: "INTEGER", nullable: false),
                    product_id = table.Column<long>(type: "INTEGER", nullable: false),
                    quantity = table.Column<decimal>(type: "TEXT", precision: 14, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_invoice_items", x => x.Id);
                    table.CheckConstraint("ck_purchase_invoice_items_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_purchase_invoice_items_total_price_non_negative", "total_price >= 0");
                    table.CheckConstraint("ck_purchase_invoice_items_unit_price_non_negative", "unit_price >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_invoice_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_invoice_items_purchase_invoices_purchase_invoice_id",
                        column: x => x.purchase_invoice_id,
                        principalTable: "purchase_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_settings_setting_key",
                table: "app_settings",
                column: "setting_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_product_id",
                table: "inventory_transactions",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_reference",
                table: "inventory_transactions",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_transactions_transaction_date",
                table: "inventory_transactions",
                column: "transaction_date");

            migrationBuilder.CreateIndex(
                name: "ix_product_categories_is_active",
                table: "product_categories",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_product_categories_name_ar",
                table: "product_categories",
                column: "name_ar",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_is_active",
                table: "products",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_products_name_ar",
                table: "products",
                column: "name_ar");

            migrationBuilder.CreateIndex(
                name: "ux_products_barcode",
                table: "products",
                column: "barcode",
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_products_product_code",
                table: "products",
                column: "product_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_items_product_id",
                table: "purchase_invoice_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_items_purchase_invoice_id",
                table: "purchase_invoice_items",
                column: "purchase_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_invoice_date",
                table: "purchase_invoices",
                column: "invoice_date");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_status",
                table: "purchase_invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_supplier_id",
                table: "purchase_invoices",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_purchase_invoices_invoice_number",
                table: "purchase_invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoice_items_product_id",
                table: "sales_invoice_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoice_items_sales_invoice_id",
                table: "sales_invoice_items",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_invoice_date",
                table: "sales_invoices",
                column: "invoice_date");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_status",
                table: "sales_invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_sales_invoices_invoice_number",
                table: "sales_invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_is_active",
                table: "suppliers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_phone",
                table: "suppliers",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ux_suppliers_name_ar",
                table: "suppliers",
                column: "name_ar",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "inventory_transactions");

            migrationBuilder.DropTable(
                name: "purchase_invoice_items");

            migrationBuilder.DropTable(
                name: "sales_invoice_items");

            migrationBuilder.DropTable(
                name: "purchase_invoices");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "sales_invoices");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "product_categories");
        }
    }
}
