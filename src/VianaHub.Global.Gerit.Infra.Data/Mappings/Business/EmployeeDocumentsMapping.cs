using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Infra.Data.Mappings.Business;

public class EmployeeDocumentsMapping : IEntityTypeConfiguration<EmployeeDocumentsEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeDocumentsEntity> builder)
    {
        builder.ToTable("EmployeeDocuments", "dbo");
        builder.HasKey(x => x.Id).HasName("PK_EmployeeDocuments");
        builder.Property(x => x.Id).UseIdentityColumn(1, 1).IsRequired();
        builder.Property(x => x.TenantId).HasColumnType("INT").IsRequired();
        builder.Property(x => x.EmployeeId).HasColumnType("INT").IsRequired();
        builder.Property(x => x.DocumentTypeId).HasColumnType("INT").IsRequired();
        builder.Property(x => x.DocumentNumber).HasColumnType("NVARCHAR(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IssuingCountryCode).HasColumnType("CHAR(2)").HasMaxLength(2).IsRequired();
        builder.Property(x => x.IssuedAt).HasColumnType("DATE").IsRequired(false);
        builder.Property(x => x.ExpiresAt).HasColumnType("DATE").IsRequired(false);
        builder.Property(x => x.IsPrimary).HasColumnType("BIT").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsActive).HasColumnType("BIT").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnType("BIT").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnType("INT").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("DATETIME2(7)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        builder.Property(x => x.ModifiedBy).HasColumnType("INT").IsRequired(false);
        builder.Property(x => x.ModifiedAt).HasColumnType("DATETIME2(7)").IsRequired(false);

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId)
            .HasConstraintName("FK_EmployeeDocuments_Tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany(x => x.Documents)
            .HasForeignKey(x => new { x.EmployeeId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_EmployeeDocuments_Employee").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId)
            .HasConstraintName("FK_EmployeeDocuments_DocumentType").OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(x => new { x.Id, x.TenantId }).HasName("UQ_EmployeeDocuments_Id_Tenant");
        builder.HasIndex(x => new { x.TenantId, x.DocumentTypeId, x.IssuingCountryCode, x.DocumentNumber })
            .IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_EmployeeDocuments_Type_Country_Number");
        builder.HasIndex(x => new { x.TenantId, x.EmployeeId })
            .IsUnique().HasFilter("[IsPrimary] = 1 AND [IsActive] = 1 AND [IsDeleted] = 0").HasDatabaseName("UX_EmployeeDocuments_Primary");
        builder.HasIndex(x => new { x.TenantId, x.EmployeeId })
            .HasFilter("[IsDeleted] = 0").HasDatabaseName("IX_EmployeeDocuments_Employee");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_EmployeeDocuments_Primary_Active", "[IsPrimary] = 0 OR [IsActive] = 1");
            table.HasCheckConstraint("CK_EmployeeDocuments_Active_Deleted", "NOT ([IsActive] = 1 AND [IsDeleted] = 1)");
            table.HasCheckConstraint("CK_EmployeeDocuments_Modification", "([ModifiedAt] IS NULL AND [ModifiedBy] IS NULL) OR ([ModifiedAt] IS NOT NULL AND [ModifiedBy] IS NOT NULL)");
            table.HasCheckConstraint("CK_EmployeeDocuments_Dates", "[ExpiresAt] IS NULL OR [IssuedAt] IS NULL OR [ExpiresAt] >= [IssuedAt]");
        });
    }
}
