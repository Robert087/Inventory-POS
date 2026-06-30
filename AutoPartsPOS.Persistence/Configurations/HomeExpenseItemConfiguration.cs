using AutoPartsPOS.Domain.HomeExpenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class HomeExpenseItemConfiguration : IEntityTypeConfiguration<HomeExpenseItem>
{
    public void Configure(EntityTypeBuilder<HomeExpenseItem> builder)
    {
        builder.ToTable("home_expense_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .ValueGeneratedOnAdd();

        builder.Property(item => item.HomeExpenseDayId)
            .HasColumnName("home_expense_day_id")
            .IsRequired();

        builder.Property(item => item.Note)
            .HasColumnName("note")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(item => item.Amount)
            .HasColumnName("amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(item => item.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(item => item.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(item => item.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.HasIndex(item => item.HomeExpenseDayId)
            .HasDatabaseName("ix_home_expense_items_home_expense_day_id");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_home_expense_items_amount_positive", "amount > 0");
        });
    }
}
