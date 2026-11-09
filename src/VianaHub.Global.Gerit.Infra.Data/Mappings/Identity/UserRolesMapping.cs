using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Identity;

/// <summary>
/// Mapeamento da entidade UserRole
/// Rela��o usu�rio x role com suporte a Row Level Security
/// </summary>
public class UserRolesMapping : IEntityTypeConfiguration<UserRolesEntity>
{
    public void Configure(EntityTypeBuilder<UserRolesEntity> builder)
    {
        builder.ToTable("UserRoles", "dbo");

        // Chave prim�ria (Identity PK conforme SQL)
        builder.HasKey(x => x.Id)
            .HasName("PK_UserRoles");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Propriedades
        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.RoleId)
            .IsRequired();

        // Índices
        // Unique constraint: nao pode duplicar (Tenant, User, Role)
        builder.HasAlternateKey(x => new { x.TenantId, x.UserId, x.RoleId })
            .HasName("UQ_UserRoles");

        // Relacionamentos
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_UserRoles_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

    }
}
