using AutoPartsPOS.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("purchase_invoices");

        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.Id)
            .ValueGeneratedOnAdd();

        builder.Property(invoice => invoice.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(invoice => invoice.SupplierId)
            .HasColumnName("supplier_id");

        builder.Property(invoice => invoice.InvoiceDate)
            .HasColumnName("invoice_date")
            .IsRequired();

        builder.Property(invoice => invoice.Notes)
            .HasColumnName("notes");

        builder.Property(invoice => invoice.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(invoice => invoice.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(invoice => invoice.VoidedAt)
            .HasColumnName("voided_at");

        builder.Property(invoice => invoice.VoidReason)
            .HasColumnName("void_reason");

        builder.Property(invoice => invoice.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(invoice => invoice.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(invoice => invoice.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(invoice => invoice.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.HasOne(invoice => invoice.Supplier)
            .WithMany()
            .HasForeignKey(invoice => invoice.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(invoice => invoice.Items)
            .WithOne(item => item.PurchaseInvoice)
            .HasForeignKey(item => item.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invoice => invoice.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("ux_purchase_invoices_invoice_number");

        builder.HasIndex(invoice => invoice.SupplierId)
            .HasDatabaseName("ix_purchase_invoices_supplier_id");

        builder.HasIndex(invoice => invoice.InvoiceDate)
            .HasDatabaseName("ix_purchase_invoices_invoice_date");

        builder.HasIndex(invoice => invoice.Status)
            .HasDatabaseName("ix_purchase_invoices_status");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_purchase_invoices_total_non_negative", "total_amount >= 0");
            table.HasCheckConstraint("ck_purchase_invoices_status", "status IN ('Posted', 'Voided')");
        });
    }
}

