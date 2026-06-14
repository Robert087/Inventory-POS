using AutoPartsPOS.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItem>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> builder)
    {
        builder.ToTable("purchase_invoice_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .ValueGeneratedOnAdd();

        builder.Property(item => item.PurchaseInvoiceId)
            .HasColumnName("purchase_invoice_id");

        builder.Property(item => item.ProductId)
            .HasColumnName("product_id");

        builder.Property(item => item.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(14, 3)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(item => item.TotalPrice)
            .HasColumnName("total_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.PurchaseInvoiceId)
            .HasDatabaseName("ix_purchase_invoice_items_purchase_invoice_id");

        builder.HasIndex(item => item.ProductId)
            .HasDatabaseName("ix_purchase_invoice_items_product_id");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_purchase_invoice_items_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_purchase_invoice_items_unit_price_non_negative", "unit_price >= 0");
            table.HasCheckConstraint("ck_purchase_invoice_items_total_price_non_negative", "total_price >= 0");
        });
    }
}

