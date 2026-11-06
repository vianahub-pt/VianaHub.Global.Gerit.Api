using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IEmployeeDocumentDataRepository
{
    Task<EmployeeDocumentsEntity?> GetByIdAsync(int employeeId, int id, CancellationToken ct = default);
    Task<IEnumerable<EmployeeDocumentsEntity>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);
    Task<bool> ExistsIdentityAsync(int tenantId, int documentTypeId, string issuingCountryCode, string documentNumber, int? excludeId, CancellationToken ct = default);
    Task<bool> ExistsPrimaryAsync(int tenantId, int employeeId, int? excludeId, CancellationToken ct = default);
    Task<bool> AddAsync(EmployeeDocumentsEntity entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(EmployeeDocumentsEntity entity, CancellationToken ct = default);
}
