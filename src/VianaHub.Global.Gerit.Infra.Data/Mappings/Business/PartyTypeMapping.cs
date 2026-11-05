using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

/// <summary>
/// Mapeamento da entidade PartyType (catálogo global).
/// Tabela: dbo.PartyTypes — Id TINYINT (PK), Code NVARCHAR(50) UK.
/// Não é multi-tenant: dados globais compartilhados entre tenants.
/// </summary>
public class PartyTypeMapping : IEntityTypeConfiguration<PartyTypeEntity>
{
    public void Configure(EntityTypeBuilder<PartyTypeEntity> builder)
    {
        builder.ToTable("PartyTypes", "dbo");

        // Chave Primária
        builder.HasKey(x => x.Id)
            .HasName("PK_PartyTypes");

        // Id TINYINT IDENTITY: o SQL Server gera os IDs das novas criações.
        // IDs históricos dos seeds preservados: 1=Individual, 2=Organization.
        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .HasColumnType("TINYINT")
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnType("NVARCHAR(50)")
            .HasMaxLength(50)
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

        // Constraint único no Code
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UQ_PartyTypes_Code");

        // Nota: Relacionamento com Translations (1:N) está configurado exclusivamente
        // em PartyTypeTranslationsMapping para evitar shadow FK PartyTypeEntityId por convenção.
    }
}
