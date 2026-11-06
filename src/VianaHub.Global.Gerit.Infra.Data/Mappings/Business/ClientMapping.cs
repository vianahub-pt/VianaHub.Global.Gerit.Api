using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

/// <summary>
/// Mapeamento da entidade Client unificada
/// Clientes do tenant com suporte a Row Level Security
/// </summary>
public class ClientMapping : IEntityTypeConfiguration<ClientEntity>
{
    public void Configure(EntityTypeBuilder<ClientEntity> builder)
    {
        builder.ToTable("Clients", "dbo");

        // Chave Primária
        builder.HasKey(x => x.Id)
            .HasName("PK_Clients");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Chave alternativa para suportar FKs compostas com TenantId
        builder.HasAlternateKey(x => new { x.Id, x.TenantId })
            .HasName("UQ_Clients_Id_Tenant");

        // Propriedades base
        builder.Property(x => x.TenantId)
            .HasColumnType("INT")
            .IsRequired(true);

        builder.Property(x => x.PartyTypeId)
            .HasColumnType("TINYINT")
            .IsRequired(true);

        builder.Property(x => x.AcquisitionSourceTypeId)
            .HasColumnType("INT")
            .IsRequired(true);

        builder.Property(x => x.ImageUrl)
            .HasColumnType("NVARCHAR(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.Note)
            .HasColumnType("NVARCHAR(1000)")
            .HasMaxLength(1000)
            .IsRequired(false);

        // Campos unificados
        builder.Property(x => x.Name)
            .HasColumnType("NVARCHAR(500)")
            .HasMaxLength(500)
            .IsRequired(true);

        builder.Property(x => x.PhoneNumber)
            .HasColumnType("NVARCHAR(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.CellPhoneNumber)
            .HasColumnType("NVARCHAR(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.IsCellPhoneWhatsapp)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnType("NVARCHAR(320)")
            .HasMaxLength(320)
            .IsRequired(false);

        builder.Property(x => x.WebsiteUrl)
            .HasColumnType("NVARCHAR(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.BirthDate)
            .HasColumnType("DATE")
            .IsRequired(false);

        builder.Property(x => x.Gender)
            .HasColumnType("NVARCHAR(30)")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.Nationality)
            .HasColumnType("NVARCHAR(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.CompanyRegistrationNumber)
            .HasColumnType("NVARCHAR(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.EconomicActivityCode)
            .HasColumnType("NVARCHAR(20)")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.NumberOfEmployees)
            .HasColumnType("INT")
            .IsRequired(false);

        builder.Property(x => x.StatusDefinitionId)
            .HasColumnType("INT")
            .IsRequired(true);

        builder.Property(x => x.StatusDomainId)
            .HasColumnType("INT")
            .IsRequired(true);

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
            .HasConstraintName("FK_Clients_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AcquisitionSourceType)
            .WithMany()
            .HasForeignKey(x => x.AcquisitionSourceTypeId)
            .HasConstraintName("FK_Clients_AcquisitionSourceType")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PartyType)
            .WithMany()
            .HasForeignKey(x => x.PartyTypeId)
            .HasConstraintName("FK_Clients_PartyType")
            .OnDelete(DeleteBehavior.Restrict);

        // FK composta para StatusDefinition: (StatusDefinitionId, TenantId, StatusDomainId)
        builder.HasOne(x => x.StatusDefinition)
            .WithMany()
            .HasForeignKey(x => new { x.StatusDefinitionId, x.TenantId, x.StatusDomainId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.StatusDomainId })
            .HasConstraintName("FK_Clients_StatusDefinition")
            .OnDelete(DeleteBehavior.Restrict);

        // Nota: StatusDomain não tem relação direta — acede-se via StatusDefinition.StatusDomain
        builder.Ignore(x => x.StatusDomain);

        builder.HasMany(x => x.Contacts)
            .WithOne(cc => cc.Client)
            .HasForeignKey(cc => new { cc.ClientId, cc.TenantId })
            .HasPrincipalKey(c => new { c.Id, c.TenantId })
            .HasConstraintName("FK_ClientContacts_Client")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Addresses)
            .WithOne(ca => ca.Client)
            .HasForeignKey(ca => new { ca.ClientId, ca.TenantId })
            .HasPrincipalKey(c => new { c.Id, c.TenantId })
            .HasConstraintName("FK_ClientAddresses_Client")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FiscalData)
            .WithOne()
            .HasForeignKey<ClientFiscalDataEntity>(cf => new { cf.ClientId, cf.TenantId })
            .HasPrincipalKey<ClientEntity>(c => new { c.Id, c.TenantId })
            .HasConstraintName("FK_ClientFiscalData_Client")
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_Clients_TenantId")
            .HasFilter("[IsDeleted] = 0");
    }
}
