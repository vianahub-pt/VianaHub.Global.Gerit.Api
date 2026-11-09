using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Identity;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Identity;

/// <summary>
/// Mapeamento da entidade User
/// Usu�rios do sistema com suporte a Row Level Security
/// </summary>
public class UserMapping : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users", "dbo");

        // Chave Prim�ria
        builder.HasKey(x => x.Id)
            .HasName("PK_Users");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Propriedades
        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnType("NVARCHAR(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnType("NVARCHAR(256)")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.NormalizedEmail)
            .HasColumnType("NVARCHAR(256)")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.EmailConfirmed)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasColumnType("NVARCHAR(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.PhoneNumberConfirmed)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.LastAccessAt)
            .HasColumnType("DATETIME2")
            .IsRequired(false);

        builder.Property(x => x.PasswordHash)
            .HasColumnType("NVARCHAR(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.UrlImage)
            .HasColumnType("NVARCHAR(500)")
            .HasMaxLength(500)
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
            .IsConcurrencyToken()
            .IsRequired(false);

        // Constraints �nicos
        // Adiciona alternate key (Id, TenantId) para compatibilidade com FKs compostas no banco
        builder.HasAlternateKey(x => new { x.Id, x.TenantId })
            .HasName("UQ_Users_Id_Tenant");

        builder.HasIndex(x => new { x.TenantId, x.NormalizedEmail })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Users_Tenant_NormalizedEmail");

        // �ndices
        builder.HasIndex(x => x.NormalizedEmail)
            .HasDatabaseName("IX_Users_Login")
            .IncludeProperties(x => new { x.Id, x.TenantId, x.IsActive })
            .HasFilter("[IsDeleted] = 0");

        // Relacionamentos
        // NOTA: O relacionamento User -> Tenant j� est� configurado no TenantMapping.cs
        // atrav�s de HasMany(t => t.Users).WithOne(x => x.Tenant).HasForeignKey(x => x.TenantId)
        // N�o configurar novamente aqui para evitar propriedades sombra (shadow properties)

        builder.HasMany(x => x.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .HasConstraintName("FK_UserRoles_User")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
