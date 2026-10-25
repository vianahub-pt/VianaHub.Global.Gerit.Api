using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Entities.Billing;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

public class ClientFiscalDataMapping : IEntityTypeConfiguration<ClientFiscalDataEntity>
{
    public void Configure(EntityTypeBuilder<ClientFiscalDataEntity> builder)
    {
        builder.ToTable("ClientFiscalData", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.ClientId)
            .IsRequired();

        // Relacionamento com Client (FK composta)
        builder.HasOne(x => x.Client)
            .WithOne(c => c.FiscalData)
            .HasForeignKey<ClientFiscalDataEntity>(x => new { x.ClientId, x.TenantId })
            .HasPrincipalKey<ClientEntity>(c => new { c.Id, c.TenantId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ClientFiscalData_Client");

        // Relacionamento com Tenant
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_ClientFiscalData_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.TaxNumber)
            .HasColumnType("NVARCHAR(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.VatNumber)
            .HasColumnType("NVARCHAR(20)")
            .HasMaxLength(20);

        builder.Property(x => x.FiscalCountry)
            .HasColumnType("CHAR(2)")
            .HasMaxLength(2)
            .IsRequired()
            .HasDefaultValue("PT");

        builder.Property(x => x.IsVatRegistered)
            .HasDefaultValue(false);

        builder.Property(x => x.IBAN)
            .HasColumnType("NVARCHAR(34)")
            .HasMaxLength(34);

        builder.Property(x => x.FiscalEmail)
            .HasColumnType("NVARCHAR(255)")
            .HasMaxLength(255);

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

        // Índice único conforme banco
        builder.HasIndex(x => new { x.ClientId, x.TenantId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_ClientFiscalData_Client");

        // TaxNumber unico por tenant + pais fiscal
        builder.HasIndex(x => new { x.TenantId, x.FiscalCountry, x.TaxNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_ClientFiscalData_TaxNumber");

        // Apenas um registro fiscal ativo por tenant+cliente
        builder.HasIndex(x => new { x.TenantId, x.ClientId })
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_ClientFiscalData_Active");

        // Check constraint
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ClientFiscalData_Active_Deleted",
            "NOT ([IsActive] = 1 AND [IsDeleted] = 1)"));
    }
}

