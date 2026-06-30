using AutoPartsPOS.Domain.HomeExpenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class HomeExpenseDayConfiguration : IEntityTypeConfiguration<HomeExpenseDay>
{
    public void Configure(EntityTypeBuilder<HomeExpenseDay> builder)
    {
        builder.ToTable("home_expense_days");

        builder.HasKey(day => day.Id);

        builder.Property(day => day.Id)
            .ValueGeneratedOnAdd();

        builder.Property(day => day.ExpenseDate)
            .HasColumnName("expense_date")
            .IsRequired();

        builder.Property(day => day.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(day => day.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(day => day.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(day => day.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(day => day.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.HasMany(day => day.Items)
            .WithOne(item => item.HomeExpenseDay)
            .HasForeignKey(item => item.HomeExpenseDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(day => day.ExpenseDate)
            .IsUnique()
            .HasDatabaseName("ux_home_expense_days_expense_date");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_home_expense_days_total_amount_non_negative", "total_amount >= 0");
        });
    }
}
