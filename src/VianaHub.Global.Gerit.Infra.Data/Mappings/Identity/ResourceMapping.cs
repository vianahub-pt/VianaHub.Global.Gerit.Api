using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Identity;

/// <summary>
/// Mapeamento da entidade Resource
/// Recursos do sistema (n�o possui TenantId pois s�o globais)
/// </summary>
public class ResourceMapping : IEntityTypeConfiguration<ResourceEntity>
{
    public void Configure(EntityTypeBuilder<ResourceEntity> builder)
    {
        builder.ToTable("Resources", "dbo");

        // Chave Prim�ria
        builder.HasKey(r => r.Id)
            .HasName("PK_Resources");

        builder.Property(r => r.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Propriedades
        builder.Property(r => r.Name)
            .HasColumnType("NVARCHAR(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.Description)
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
            .HasDatabaseName("UX_Resources_Name")
            .HasFilter("[IsDeleted] = 0");

        // Relacionamentos
        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Resource)
            .HasForeignKey(rp => rp.ResourceId)
            .HasConstraintName("FK_RolePermissions_Resource")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
