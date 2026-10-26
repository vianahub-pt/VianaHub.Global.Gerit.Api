using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Identity;

/// <summary>
/// Mapeamento da entidade ActionEntity
/// A��es poss�veis no sistema (n�o possui TenantId pois s�o globais)
/// </summary>
public class ActionMapping : IEntityTypeConfiguration<ActionEntity>
{
    public void Configure(EntityTypeBuilder<ActionEntity> builder)
    {
        builder.ToTable("Actions", "dbo");

        // Chave Prim�ria
        builder.HasKey(x => x.Id)
            .HasName("PK_Actions");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Propriedades
        builder.Property(x => x.Name)
            .HasColumnType("NVARCHAR(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnType("NVARCHAR(500)")
            .HasMaxLength(500)
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

        // Constraints �nicos
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("UX_Actions_Name")
            .HasFilter("[IsDeleted] = 0");

        // Relacionamentos
        builder.HasMany(x => x.RolePermissions)
            .WithOne(rp => rp.Action)
            .HasForeignKey(rp => rp.ActionId)
            .HasConstraintName("FK_RolePermissions_Action")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
