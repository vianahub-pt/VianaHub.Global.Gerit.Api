using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

public class EmployeeTeamMapping : IEntityTypeConfiguration<EmployeeTeamEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeTeamEntity> builder)
    {
        // Nome da tabela: singular (SQL: dbo.EmployeeTeam)
        builder.ToTable("EmployeeTeam", "dbo");

        builder.HasKey(x => x.Id).HasName("PK_EmployeeTeam");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnType("INT")
            .IsRequired();
        builder.Property(x => x.TeamId)
            .HasColumnType("INT")
            .IsRequired();
        builder.Property(x => x.EmployeeId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.IsLeader)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.StartDateTime)
            .HasColumnType("DATETIME2")
            .IsRequired();

        builder.Property(x => x.EndDateTime)
            .HasColumnType("DATETIME2")
            .IsRequired(false);

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

        // Relacionamentos com FKs compostas (Id, TenantId)
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_EmployeeTeam_Tenant")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => new { x.TeamId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EmployeeTeam_Team")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => new { x.EmployeeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EmployeeTeam_Member")
            .OnDelete(DeleteBehavior.NoAction);

        // Constraints
        builder.ToTable(table => table.HasCheckConstraint("CK_EmployeeTeam_Active_Deleted",
            "NOT ([IsActive] = 1 AND [IsDeleted] = 1)"));

        builder.ToTable(table => table.HasCheckConstraint("CK_EmployeeTeam_EndDateTime",
            "[EndDateTime] IS NULL OR [EndDateTime] >= [StartDateTime]"));

        // Índice filtrado: apenas uma associação ativa por (Team, Employee)
        builder.HasIndex(x => new { x.TenantId, x.TeamId, x.EmployeeId })
            .HasFilter("[EndDateTime] IS NULL AND [IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("UX_EmployeeTeam_Active");
    }
}
