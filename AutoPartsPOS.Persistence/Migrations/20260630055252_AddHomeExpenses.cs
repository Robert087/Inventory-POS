using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsPOS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "home_expense_days",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    expense_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    total_amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_home_expense_days", x => x.Id);
                    table.CheckConstraint("ck_home_expense_days_total_amount_non_negative", "total_amount >= 0");
                });

            migrationBuilder.CreateTable(
                name: "home_expense_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    home_expense_day_id = table.Column<long>(type: "INTEGER", nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_home_expense_items", x => x.Id);
                    table.CheckConstraint("ck_home_expense_items_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "FK_home_expense_items_home_expense_days_home_expense_day_id",
                        column: x => x.home_expense_day_id,
                        principalTable: "home_expense_days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_home_expense_days_expense_date",
                table: "home_expense_days",
                column: "expense_date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_home_expense_items_home_expense_day_id",
                table: "home_expense_items",
                column: "home_expense_day_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "home_expense_items");

            migrationBuilder.DropTable(
                name: "home_expense_days");
        }
    }
}
