using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Domain.HomeExpenses;
using AutoPartsPOS.Domain.Catalog;
using AutoPartsPOS.Domain.Common;
using AutoPartsPOS.Domain.Inventory;
using AutoPartsPOS.Domain.Purchases;
using AutoPartsPOS.Domain.Sales;
using AutoPartsPOS.Domain.Settings;
using AutoPartsPOS.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService) : DbContext(options), IAppDbContext
{
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();

    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();

    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();

    public DbSet<HomeExpenseDay> HomeExpenseDays => Set<HomeExpenseDay>();

    public DbSet<HomeExpenseItem> HomeExpenseItems => Set<HomeExpenseItem>();

    IQueryable<AppSetting> IAppDbContext.AppSettings => AppSettings;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var now = dateTimeProvider.Now;
        var userName = currentUserService.UserName;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userName;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userName;
            }
        }
    }
}
