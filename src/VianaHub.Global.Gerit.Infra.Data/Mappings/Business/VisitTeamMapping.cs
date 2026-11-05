using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

public class VisitTeamMapping : IEntityTypeConfiguration<VisitTeamEntity>
{
    public void Configure(EntityTypeBuilder<VisitTeamEntity> builder)
    {
        // Nome da tabela: singular (SQL: dbo.VisitTeam)
        builder.ToTable("VisitTeam", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Chave alternativa para suportar FKs compostas com TenantId
        builder.HasAlternateKey(x => new { x.Id, x.TenantId })
            .HasName("UQ_VisitTeam_Id_Tenant");

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.VisitId).IsRequired();
        builder.Property(x => x.TeamId).IsRequired();

        builder.Property(x => x.StartDateTime)
            .HasColumnType("DATETIME2")
            .IsRequired();

        builder.Property(x => x.EndDateTime)
            .HasColumnType("DATETIME2")
            .IsRequired(false);

        builder.Property(x => x.IsActive).HasColumnType("BIT").HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).HasColumnType("BIT").HasDefaultValue(false);

        builder.Property(x => x.CreatedAt).HasColumnType("DATETIME2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.ModifiedAt).HasColumnType("DATETIME2(7)");

        // Relacionamentos com FKs compostas (Id, TenantId) conforme SQL
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_VisitTeam_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Visit)
            .WithMany()
            .HasForeignKey(x => new { x.VisitId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_VisitTeam_Visit")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => new { x.TeamId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_VisitTeam_Team")
            .OnDelete(DeleteBehavior.Restrict);

        // Constraints
        builder.ToTable(table => table.HasCheckConstraint("CK_VisitTeam_Active_Deleted",
            "NOT ([IsActive] = 1 AND [IsDeleted] = 1)"));

        builder.ToTable(table => table.HasCheckConstraint("CK_VisitTeam_EndDateTime",
            "[EndDateTime] IS NULL OR [EndDateTime] >= [StartDateTime]"));

        // Índice único filtrado: apenas uma equipa ativa por visita
        builder.HasIndex(x => new { x.TenantId, x.VisitId, x.TeamId })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("UX_VisitTeam_Active");

        builder.HasIndex(x => new { x.TenantId, x.VisitId })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_VisitTeam_VisitId");
    }
}
