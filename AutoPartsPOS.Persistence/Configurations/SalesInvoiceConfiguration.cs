using AutoPartsPOS.Domain.Sales;
using AutoPartsPOS.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("sales_invoices");

        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.Id)
            .ValueGeneratedOnAdd();

        builder.Property(invoice => invoice.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(100)
            .IsRequired();

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

        builder.Property(invoice => invoice.PaymentStatus)
            .HasColumnName("payment_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(InvoicePaymentStatus.Unpaid)
            .IsRequired();

        builder.Property(invoice => invoice.SubtotalAmount)
            .HasColumnName("subtotal_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(invoice => invoice.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(invoice => invoice.NetTotalAmount)
            .HasColumnName("net_total_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(invoice => invoice.PaidAmount)
            .HasColumnName("paid_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(invoice => invoice.RemainingAmount)
            .HasColumnName("remaining_amount")
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

        builder.HasMany(invoice => invoice.Items)
            .WithOne(item => item.SalesInvoice)
            .HasForeignKey(item => item.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invoice => invoice.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("ux_sales_invoices_invoice_number");

        builder.HasIndex(invoice => invoice.InvoiceDate)
            .HasDatabaseName("ix_sales_invoices_invoice_date");

        builder.HasIndex(invoice => invoice.Status)
            .HasDatabaseName("ix_sales_invoices_status");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_sales_invoices_amounts_non_negative", "subtotal_amount >= 0 AND discount_amount >= 0 AND net_total_amount >= 0");
            table.HasCheckConstraint("ck_sales_invoices_net_total_valid", "net_total_amount = subtotal_amount - discount_amount");
            table.HasCheckConstraint("ck_sales_invoices_status", "status IN ('Posted', 'Voided')");
            table.HasCheckConstraint("ck_sales_invoices_payment_status", "payment_status IN ('Paid', 'Unpaid', 'PartiallyPaid')");
            table.HasCheckConstraint("ck_sales_invoices_payment_amounts", "paid_amount >= 0 AND remaining_amount >= 0 AND paid_amount + remaining_amount = net_total_amount");
        });
    }
}

