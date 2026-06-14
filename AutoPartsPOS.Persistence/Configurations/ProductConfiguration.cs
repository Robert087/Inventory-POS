using AutoPartsPOS.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .ValueGeneratedOnAdd();

        builder.Property(product => product.ProductCode)
            .HasColumnName("product_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(product => product.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(100);

        builder.Property(product => product.NameAr)
            .HasColumnName("name_ar")
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(product => product.CategoryId)
            .HasColumnName("category_id");

        builder.Property(product => product.PurchasePrice)
            .HasColumnName("purchase_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(product => product.SellingPrice)
            .HasColumnName("selling_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(product => product.CurrentStock)
            .HasColumnName("current_stock")
            .HasPrecision(14, 3)
            .IsRequired();

        builder.Property(product => product.MinimumStock)
            .HasColumnName("minimum_stock")
            .HasPrecision(14, 3)
            .IsRequired();

        builder.Property(product => product.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(product => product.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(product => product.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(product => product.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(product => product.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(product => product.ProductCode)
            .IsUnique()
            .HasDatabaseName("ux_products_product_code");

        builder.HasIndex(product => product.Barcode)
            .IsUnique()
            .HasFilter("barcode IS NOT NULL")
            .HasDatabaseName("ux_products_barcode");

        builder.HasIndex(product => product.NameAr)
            .HasDatabaseName("ix_products_name_ar");

        builder.HasIndex(product => product.CategoryId)
            .HasDatabaseName("ix_products_category_id");

        builder.HasIndex(product => product.IsActive)
            .HasDatabaseName("ix_products_is_active");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_products_purchase_price_non_negative", "purchase_price >= 0");
            table.HasCheckConstraint("ck_products_selling_price_non_negative", "selling_price >= 0");
            table.HasCheckConstraint("ck_products_current_stock_non_negative", "current_stock >= 0");
            table.HasCheckConstraint("ck_products_minimum_stock_non_negative", "minimum_stock >= 0");
        });
    }
}

