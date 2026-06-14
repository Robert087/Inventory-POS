using AutoPartsPOS.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsPOS.Persistence.Configurations;

public sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings");

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Id)
            .ValueGeneratedOnAdd();

        builder.Property(setting => setting.Key)
            .HasColumnName("setting_key")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(setting => setting.Value)
            .HasColumnName("setting_value");

        builder.Property(setting => setting.Description);

        builder.Property(setting => setting.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false);

        builder.Property(setting => setting.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(setting => setting.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(setting => setting.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(setting => setting.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.HasIndex(setting => setting.Key)
            .IsUnique();
    }
}

