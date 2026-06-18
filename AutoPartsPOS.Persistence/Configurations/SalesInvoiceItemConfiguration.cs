using AutoPartsPOS.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class SalesInvoiceItemConfiguration : IEntityTypeConfiguration<SalesInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceItem> builder)
    {
        builder.ToTable("sales_invoice_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .ValueGeneratedOnAdd();

        builder.Property(item => item.SalesInvoiceId)
            .HasColumnName("sales_invoice_id");

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

        builder.Property(item => item.UnitCost)
            .HasColumnName("unit_cost")
            .HasPrecision(14, 4)
            .IsRequired();

        builder.Property(item => item.TotalCost)
            .HasColumnName("total_cost")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.SalesInvoiceId)
            .HasDatabaseName("ix_sales_invoice_items_sales_invoice_id");

        builder.HasIndex(item => item.ProductId)
            .HasDatabaseName("ix_sales_invoice_items_product_id");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_sales_invoice_items_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_sales_invoice_items_unit_price_non_negative", "unit_price >= 0");
            table.HasCheckConstraint("ck_sales_invoice_items_total_price_non_negative", "total_price >= 0");
            table.HasCheckConstraint("ck_sales_invoice_items_unit_cost_non_negative", "unit_cost >= 0");
            table.HasCheckConstraint("ck_sales_invoice_items_total_cost_non_negative", "total_cost >= 0");
        });
    }
}

