using AutoPartsPOS.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(supplier => supplier.Id);

        builder.Property(supplier => supplier.Id)
            .ValueGeneratedOnAdd();

        builder.Property(supplier => supplier.NameAr)
            .HasColumnName("name_ar")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(supplier => supplier.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50);

        builder.Property(supplier => supplier.Address)
            .HasColumnName("address");

        builder.Property(supplier => supplier.Notes)
            .HasColumnName("notes");

        builder.Property(supplier => supplier.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(supplier => supplier.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(supplier => supplier.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(supplier => supplier.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(supplier => supplier.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.HasIndex(supplier => supplier.NameAr)
            .IsUnique()
            .HasDatabaseName("ux_suppliers_name_ar");

        builder.HasIndex(supplier => supplier.Phone)
            .HasDatabaseName("ix_suppliers_phone");

        builder.HasIndex(supplier => supplier.IsActive)
            .HasDatabaseName("ix_suppliers_is_active");
    }
}

