using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

/// <summary>
/// Mapeamento da entidade EmployeeFiscalData
/// Dados fiscais do funcionário com suporte a Row Level Security
/// </summary>
public class EmployeeFiscalDataMapping : IEntityTypeConfiguration<EmployeeFiscalDataEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeFiscalDataEntity> builder)
    {
        builder.ToTable("EmployeeFiscalData", "dbo");

        // Chave Primária
        builder.HasKey(x => x.Id)
            .HasName("PK_EmployeeFiscalData");

        builder.Property(x => x.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Propriedades
        builder.Property(x => x.TenantId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(x => x.TaxNumber)
            .HasColumnType("NVARCHAR(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.VatNumber)
            .HasColumnType("NVARCHAR(20)")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.FiscalCountry)
            .HasColumnType("CHAR(2)")
            .HasMaxLength(2)
            .HasDefaultValue("PT")
            .IsRequired();

        builder.Property(x => x.IsVatRegistered)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IBAN)
            .HasColumnType("NVARCHAR(34)")
            .HasMaxLength(34)
            .IsRequired(false);

        builder.Property(x => x.FiscalEmail)
            .HasColumnType("NVARCHAR(255)")
            .HasMaxLength(255)
            .IsRequired(false);

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

        // Relacionamento com Employee (FK composta EmployeeId + TenantId)
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => new { x.EmployeeId, x.TenantId })
            .HasPrincipalKey(e => new { e.Id, e.TenantId })
            .HasConstraintName("FK_EmployeeFiscalData_Employee")
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com Tenant
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_EmployeeFiscalData_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

        // Constraint única: garantir apenas um registro ativo por funcionário (soft delete)
        builder.HasIndex(x => new { x.TenantId, x.EmployeeId })
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_EmployeeFiscalData_Active");

        // TaxNumber unico por tenant + pais fiscal
        builder.HasIndex(x => new { x.TenantId, x.FiscalCountry, x.TaxNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_EmployeeFiscalData_TaxNumber");

        // Check constraint
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EmployeeFiscalData_Active_Deleted",
            "NOT ([IsActive] = 1 AND [IsDeleted] = 1)"));
    }
}
