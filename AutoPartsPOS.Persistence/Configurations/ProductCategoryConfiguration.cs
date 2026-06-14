using AutoPartsPOS.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .ValueGeneratedOnAdd();

        builder.Property(category => category.NameAr)
            .HasColumnName("name_ar")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasColumnName("description");

        builder.Property(category => category.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(category => category.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(category => category.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(category => category.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.HasIndex(category => category.NameAr)
            .IsUnique()
            .HasDatabaseName("ux_product_categories_name_ar");

        builder.HasIndex(category => category.IsActive)
            .HasDatabaseName("ix_product_categories_is_active");
    }
}

