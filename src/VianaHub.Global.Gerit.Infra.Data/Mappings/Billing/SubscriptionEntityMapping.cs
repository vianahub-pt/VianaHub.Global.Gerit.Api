using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Billing;

public class SubscriptionEntityMapping : IEntityTypeConfiguration<SubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionEntity> builder)
    {
        builder.ToTable("Subscriptions", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.SubscriptionPlanId)
            .IsRequired();

        builder.Property(x => x.StatusDefinitionId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.StatusDomainId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.AgreedAmount)
            .HasColumnType("DECIMAL(19,4)")
            .IsRequired();

        builder.Property(x => x.BillingInterval)
            .HasColumnType("NVARCHAR(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .HasColumnType("CHAR(3)")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired(false);

        builder.Property(x => x.StripeId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.CurrentPeriodStart)
            .HasColumnType("DATETIME2")
            .IsRequired();

        builder.Property(x => x.CurrentPeriodEnd)
            .HasColumnType("DATETIME2")
            .IsRequired();

        builder.Property(x => x.TrialStart)
            .HasColumnType("DATETIME2")
            .IsRequired(false);

        builder.Property(x => x.TrialEnd)
            .HasColumnType("DATETIME2")
            .IsRequired(false);

        builder.Property(x => x.CancelAtPeriodEnd)
            .HasColumnType("BIT")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CanceledAt)
            .HasColumnType("DATETIME2")
            .IsRequired(false);

        builder.Property(x => x.CancellationReason)
            .HasColumnType("NVARCHAR(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.StripeCustomerId)
            .HasColumnType("NVARCHAR(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasColumnType("BIT")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsDeleted)
            .HasColumnType("BIT")
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

        // Constraint: Se IsDeleted = 1, ento IsActive = 0
        builder.ToTable(table => table.HasCheckConstraint("CK_Subscriptions_DeletedImpliesInactive", "[IsDeleted] = 0 OR [IsActive] = 0"));

        // Constraint: AgreedAmount >= 0
        builder.ToTable(table => table.HasCheckConstraint("CK_Subscriptions_AgreedAmount_NonNegative", "[AgreedAmount] >= 0"));

        // Navegao - Relacionamento com Tenant
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_Subscriptions_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

        // Navegao - Relacionamento com SubscriptionPlan (SubscriptionPlanEntity)
        builder.HasOne(x => x.SubscriptionPlan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(x => x.SubscriptionPlanId)
            .HasConstraintName("FK_Subscriptions_SubscriptionPlan")
            .OnDelete(DeleteBehavior.Restrict);

        // Navegao - Relacionamento com StatusDefinition (FK composta: StatusDefinitionId, TenantId, StatusDomainId)
        builder.HasOne(x => x.StatusDefinition)
            .WithMany()
            .HasForeignKey(x => new { x.StatusDefinitionId, x.TenantId, x.StatusDomainId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.StatusDomainId })
            .HasConstraintName("FK_Subscriptions_StatusDefinition")
            .OnDelete(DeleteBehavior.Restrict);

        // Chave alternativa (TenantId, Id)
        builder.HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_Subscriptions_TenantId_Id");

        // Constraint unica: Garantir que so pode haver um registro ativo por tenant
        builder.HasIndex(x => x.TenantId)
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_Subscriptions_Active");

        // ndice para performance em consultas por SubscriptionPlanId
        builder.HasIndex(x => x.SubscriptionPlanId)
            .HasDatabaseName("IX_Subscriptions_SubscriptionPlanId");

        // ndice para consultas por StatusDefinition
        builder.HasIndex(x => new { x.StatusDefinitionId, x.TenantId, x.StatusDomainId })
            .HasDatabaseName("IX_Subscriptions_StatusDefinition");

        // StripeId unico (quando preenchido)
        builder.HasIndex(x => x.StripeId)
            .IsUnique()
            .HasFilter("[StripeId] IS NOT NULL")
            .HasDatabaseName("UX_Subscriptions_StripeId");
    }
}
