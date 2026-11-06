using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

/// <summary>
/// Mapeamento da entidade Employee
/// Membros do time com suporte a Row Level Security
/// </summary>
public class EmployeeMapping : IEntityTypeConfiguration<EmployeeEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeEntity> builder)
    {
        builder.ToTable("Employees", "dbo");

        // Chave Prim�ria
        builder.HasKey(tm => tm.Id)
            .HasName("PK_Employees");

        builder.Property(tm => tm.Id)
            .UseIdentityColumn(1, 1)
            .IsRequired();

        // Chave alternativa para suportar FKs compostas com TenantId
        builder.HasAlternateKey(tm => new { tm.Id, tm.TenantId })
            .HasName("UQ_Employees_Id_Tenant");

        // Propriedades
        builder.Property(tm => tm.TenantId)
            .IsRequired();

        builder.Property(tm => tm.Name)
            .HasColumnType("NVARCHAR(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(tm => tm.StatusDefinitionId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(tm => tm.StatusDomainId)
            .HasColumnType("INT")
            .IsRequired();

        builder.Property(tm => tm.PhoneNumber)
            .HasColumnType("NVARCHAR(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(tm => tm.CellPhoneNumber)
            .HasColumnType("NVARCHAR(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(tm => tm.IsCellPhoneWhatsapp)
            .HasColumnType("BIT")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(tm => tm.Email)
            .HasColumnType("NVARCHAR(320)")
            .HasMaxLength(320)
            .IsRequired(false);

        builder.Property(tm => tm.ImageUrl)
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
            .IsRequired(false);

        // Relacionamentos
        builder.HasOne(tm => tm.Tenant)
            .WithMany()
            .HasForeignKey(tm => tm.TenantId)
            .HasConstraintName("FK_Employees_Tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tm => tm.StatusDefinition)
            .WithMany()
            .HasForeignKey(tm => new { tm.StatusDefinitionId, tm.TenantId, tm.StatusDomainId })
            .HasPrincipalKey(s => new { s.Id, s.TenantId, s.StatusDomainId })
            .HasConstraintName("FK_Employees_StatusDefinitions")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(tm => tm.Contacts)
            .WithOne(tmc => tmc.Employee)
            .HasForeignKey(tmc => new { tmc.EmployeeId, tmc.TenantId })
            .HasPrincipalKey(tm => new { tm.Id, tm.TenantId })
            .HasConstraintName("FK_EmployeeContacts_Employee")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(tm => tm.Addresses)
            .WithOne(x => x.Employee)
            .HasForeignKey(x => new { x.EmployeeId, x.TenantId })
            .HasPrincipalKey(tm => new { tm.Id, tm.TenantId })
            .HasConstraintName("FK_EmployeeAddresses_Employee")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(tm => tm.Documents)
            .WithOne(x => x.Employee)
            .HasForeignKey(x => new { x.EmployeeId, x.TenantId })
            .HasPrincipalKey(tm => new { tm.Id, tm.TenantId })
            .HasConstraintName("FK_EmployeeDocuments_Employee")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
