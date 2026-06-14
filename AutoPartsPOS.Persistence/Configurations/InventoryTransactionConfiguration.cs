using AutoPartsPOS.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .ValueGeneratedOnAdd();

        builder.Property(transaction => transaction.ProductId)
            .HasColumnName("product_id");

        builder.Property(transaction => transaction.TransactionType)
            .HasColumnName("transaction_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(transaction => transaction.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(14, 3)
            .IsRequired();

        builder.Property(transaction => transaction.BalanceAfter)
            .HasColumnName("balance_after")
            .HasPrecision(14, 3)
            .IsRequired();

        builder.Property(transaction => transaction.ReferenceType)
            .HasColumnName("reference_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(transaction => transaction.ReferenceId)
            .HasColumnName("reference_id");

        builder.Property(transaction => transaction.TransactionDate)
            .HasColumnName("transaction_date")
            .IsRequired();

        builder.Property(transaction => transaction.Notes)
            .HasColumnName("notes");

        builder.HasOne(transaction => transaction.Product)
            .WithMany()
            .HasForeignKey(transaction => transaction.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transaction => transaction.ProductId)
            .HasDatabaseName("ix_inventory_transactions_product_id");

        builder.HasIndex(transaction => transaction.TransactionDate)
            .HasDatabaseName("ix_inventory_transactions_transaction_date");

        builder.HasIndex(transaction => new { transaction.ReferenceType, transaction.ReferenceId })
            .HasDatabaseName("ix_inventory_transactions_reference");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_inventory_transactions_quantity_not_zero", "quantity <> 0");
            table.HasCheckConstraint("ck_inventory_transactions_balance_after_non_negative", "balance_after >= 0");
            table.HasCheckConstraint("ck_inventory_transactions_transaction_type", "transaction_type IN ('Purchase', 'Sale', 'Adjustment', 'VoidPurchase', 'VoidSale')");
            table.HasCheckConstraint("ck_inventory_transactions_reference_type", "reference_type IN ('PurchaseInvoice', 'SalesInvoice', 'ManualAdjustment')");
        });
    }
}

