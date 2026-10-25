using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Billing;

/// <summary>
/// Mapeamento da entidade SubscriptionPlanFileRule.
/// Tabela: dbo.SubscriptionPlanFileRules — define tamanho máximo por FileType para cada plano.
/// </summary>
public class SubscriptionPlanFileRulesMapping : IEntityTypeConfiguration<SubscriptionPlanFileRulesEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanFileRulesEntity> builder)
    {
        builder.ToTable("SubscriptionPlanFileRules", "dbo");

        // Chave Primária
        builder.HasKey(x => x.Id)
            .HasName("PK_SubscriptionPlanFileRules");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Propriedades
        builder.Property(x => x.SubscriptionPlanId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.FileTypeId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.MaxFileSizeMB)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnType("BIT")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

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

        // Relacionamento com SubscriptionPlan
        builder.HasOne(x => x.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionPlanId)
            .HasConstraintName("FK_SubscriptionPlanFileRules_SubscriptionPlan")
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com FileType
        builder.HasOne(x => x.FileType)
            .WithMany()
            .HasForeignKey(x => x.FileTypeId)
            .HasConstraintName("FK_SubscriptionPlanFileRules_FileType")
            .OnDelete(DeleteBehavior.Restrict);

        // Constraint único composto: uma regra por (SubscriptionPlanId, FileTypeId)
        builder.HasIndex(x => new { x.SubscriptionPlanId, x.FileTypeId })
            .IsUnique()
            .HasDatabaseName("UQ_SubscriptionPlanFileRules_Plan_FileType");

        // Check constraint: MaxFileSizeMB deve ser maior que zero
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_SubscriptionPlanFileRules_MaxFileSizeMB",
            "[MaxFileSizeMB] > 0"));

        // Check constraint: não pode estar ativo e deletado ao mesmo tempo
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_SubscriptionPlanFileRules_Active_Deleted",
            "NOT ([IsActive] = 1 AND [IsDeleted] = 1)"));

        // Indice nao clusterizado: busca por SubscriptionPlan
        builder.HasIndex(x => x.SubscriptionPlanId)
            .HasDatabaseName("IX_SubscriptionPlanFileRules_SubscriptionPlan")
            .HasFilter("[IsDeleted] = 0");

        // Indice nao clusterizado: busca por FileType
        builder.HasIndex(x => x.FileTypeId)
            .HasDatabaseName("IX_SubscriptionPlanFileRules_FileType")
            .HasFilter("[IsDeleted] = 0");
    }
}
