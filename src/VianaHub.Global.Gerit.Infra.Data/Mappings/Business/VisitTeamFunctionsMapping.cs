using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

/// <summary>
/// Mapeamento da entidade VisitTeamFunctionsEntity para a tabela dbo.VisitTeamFunctions.
/// </summary>
public class VisitTeamFunctionsMapping : IEntityTypeConfiguration<VisitTeamFunctionsEntity>
{
    public void Configure(EntityTypeBuilder<VisitTeamFunctionsEntity> builder)
    {
        builder.ToTable("VisitTeamFunctions", "dbo");

        // Chave Primária
        builder.HasKey(x => x.Id)
            .HasName("PK_VisitTeamFunctions");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Chave alternativa para suportar FKs compostas com TenantId
        builder.HasAlternateKey(x => new { x.Id, x.TenantId })
            .HasName("UQ_VisitTeamFunctions_Id_Tenant");

        // Propriedades
        builder.Property(x => x.TenantId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnType("NVARCHAR(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnType("NVARCHAR(500)")
            .HasMaxLength(500)
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

        // Relacionamento com Tenant
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_VisitTeamFunctions_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

        // Constraints
        builder.ToTable(table => table.HasCheckConstraint("CK_VisitTeamFunctions_Active_Deleted",
            "NOT ([IsActive] = 1 AND [IsDeleted] = 1)"));
    }
}
