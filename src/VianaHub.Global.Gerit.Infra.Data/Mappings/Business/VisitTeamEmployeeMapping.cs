using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

public class VisitTeamEmployeeMapping : IEntityTypeConfiguration<VisitTeamEmployeeEntity>
{
    public void Configure(EntityTypeBuilder<VisitTeamEmployeeEntity> builder)
    {
        builder.ToTable("VisitTeamEmployee", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.VisitTeamId)
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.VisitTeamFunctionId)
            .IsRequired();

        builder.Property(x => x.IsLeader)
            .IsRequired();

        builder.Property(x => x.StartDateTime)
            .HasColumnType("DATETIME2")
            .IsRequired();

        builder.Property(x => x.EndDateTime)
            .HasColumnType("DATETIME2");

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

        // Relacionamentos
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VisitTeam)
            .WithMany()
            .HasForeignKey(x => new { x.VisitTeamId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => new { x.EmployeeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Function)
            .WithMany()
            .HasForeignKey(x => new { x.VisitTeamFunctionId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_VisitTeamEmployee_VisitTeamFunction")
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(x => new { x.Id, x.TenantId })
            .IsUnique()
            .HasDatabaseName("UQ_VisitTeamEmployee_Id_Tenant");

        builder.HasIndex(x => new { x.TenantId, x.VisitTeamId, x.EmployeeId })
            .HasFilter("[EndDateTime] IS NULL AND [IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("UX_VisitTeamEmployee_Active");

        builder.HasIndex(x => new { x.TenantId, x.VisitTeamId })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_VisitTeamEmployee_VisitTeamId");

        builder.HasIndex(x => new { x.TenantId, x.EmployeeId })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_VisitTeamEmployee_EmployeeId");

        // Constraints
        builder.ToTable(table => table.HasCheckConstraint("CK_VisitTeamEmployee_Active_Deleted",
            "NOT ([IsActive] = 1 AND [IsDeleted] = 1)"));

        builder.ToTable(table => table.HasCheckConstraint("CK_VisitTeamEmployee_EndDateTime",
            "[EndDateTime] IS NULL OR [EndDateTime] >= [StartDateTime]"));
    }
}
