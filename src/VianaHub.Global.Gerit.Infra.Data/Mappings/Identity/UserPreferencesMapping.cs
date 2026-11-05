using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Identity;

public class UserPreferencesMapping : IEntityTypeConfiguration<UserPreferencesEntity>
{
    public void Configure(EntityTypeBuilder<UserPreferencesEntity> builder)
    {
        builder.ToTable("UserPreferences", "dbo");

        builder.HasKey(x => x.Id).HasName("PK_UserPreferences");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        builder.HasAlternateKey(x => new { x.Id, x.TenantId }).HasName("UQ_UserPreferences_Id_Tenant");

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Appearance)
            .HasColumnType("NVARCHAR(10)")
            .HasMaxLength(10)
            .HasDefaultValue("light")
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnType("NVARCHAR(3)")
            .HasMaxLength(3)
            .HasDefaultValue("EUR")
            .IsRequired();

        builder.Property(x => x.Locale)
            .HasColumnType("NVARCHAR(10)")
            .HasMaxLength(10)
            .HasDefaultValue("pt-PT")
            .IsRequired();

        builder.Property(x => x.Timezone)
            .HasColumnType("NVARCHAR(100)")
            .HasMaxLength(100)
            .HasDefaultValue("Europe/Lisbon")
            .IsRequired();

        builder.Property(x => x.DateFormat)
            .HasColumnType("NVARCHAR(20)")
            .HasMaxLength(20)
            .HasDefaultValue("DD-MM-YYYY")
            .IsRequired();

        builder.Property(x => x.TimeFormat)
            .HasColumnType("NVARCHAR(10)")
            .HasMaxLength(10)
            .HasDefaultValue("24h")
            .IsRequired();

        builder.Property(x => x.DayStart)
            .HasColumnType("TIME(0)")
            .HasDefaultValueSql("('09:00')")
            .IsRequired();

        builder.Property(x => x.DayEnd)
            .HasColumnType("TIME(0)")
            .HasDefaultValueSql("('18:00')")
            .IsRequired();

        builder.Property(x => x.EmailNewsletter)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.EmailWeeklyReport)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.EmailApproval)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.EmailAlerts)
            .HasColumnType("BIT")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.EmailReminders)
            .HasColumnType("BIT")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.EmailPlanner)
            .HasColumnType("BIT")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("DATETIME2(7)")
            .IsRequired();

        builder.Property(x => x.ModifiedBy)
            .HasColumnType("INT")
            .IsRequired(false);

        builder.Property(x => x.ModifiedAt)
            .HasColumnType("DATETIME2(7)")
            .IsRequired(false);

        // Check constraints to match SQL table
        builder.ToTable(table => table.HasCheckConstraint("CK_UserPreferences_Active_Deleted", "NOT (IsActive = 1 AND IsDeleted = 1)"));
        builder.ToTable(table => table.HasCheckConstraint("CK_UserPreferences_Appearance", "Appearance IN ('light', 'dark')"));
        builder.ToTable(table => table.HasCheckConstraint("CK_UserPreferences_TimeFormat", "TimeFormat IN ('24h', '12h')"));

        // Relationships
        // Composite FK (UserId, TenantId) targeting Users(Id, TenantId) as defined in SQL
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => new { x.UserId, x.TenantId })
            .HasPrincipalKey(u => new { u.Id, u.TenantId })
            .HasConstraintName("FK_UserPreferences_User")
            .OnDelete(DeleteBehavior.Restrict);

        // FK to Tenant (use explicit navigation to avoid EF Core creating shadow property)
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_UserPreferences_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index TenantId + UserId active (match SQL index name and filter)
        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasDatabaseName("UX_UserPreferences_Tenant_User_Active")
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
    }
}
