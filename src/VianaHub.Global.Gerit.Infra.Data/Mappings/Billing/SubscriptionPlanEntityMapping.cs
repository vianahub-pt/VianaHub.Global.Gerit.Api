using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Billing;

public class SubscriptionPlanEntityMapping : IEntityTypeConfiguration<SubscriptionPlanEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanEntity> builder)
    {
        builder.ToTable("SubscriptionPlans", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code)
            .HasColumnType("NVARCHAR(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PricePerHour)
            .HasColumnType("DECIMAL(19,4)")
            .IsRequired(false);

        builder.Property(x => x.PricePerDay)
            .HasColumnType("DECIMAL(19,4)")
            .IsRequired(false);

        builder.Property(x => x.PricePerMonth)
            .HasColumnType("DECIMAL(19,4)")
            .IsRequired(false);

        builder.Property(x => x.PricePerYear)
            .HasColumnType("DECIMAL(19,4)")
            .IsRequired(false);

        builder.Property(x => x.Currency)
            .HasColumnType("NVARCHAR(3)")
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("EUR");

        builder.Property(x => x.MaxUsers)
            .IsRequired();

        builder.Property(x => x.MaxPhotosPerVisit)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedBy)
              .HasColumnType("INT")
              .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("DATETIME2(7)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(x => x.ModifiedBy)
            .HasColumnType("INT")
            .IsRequired(false);

        builder.Property(x => x.ModifiedAt)
            .HasColumnType("DATETIME2(7)")
            .IsRequired(false);

        // Constraints
        builder.ToTable(table => table.HasCheckConstraint("CK_SubscriptionPlans_Active_Deleted",
            "NOT ([IsActive] = 1 AND [IsDeleted] = 1)"));

        // Índice único filtrado para Code
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_SubscriptionPlans_Code");
    }
}
