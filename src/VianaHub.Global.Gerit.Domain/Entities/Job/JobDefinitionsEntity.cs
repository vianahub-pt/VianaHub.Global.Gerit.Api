using VianaHub.Global.Gerit.Domain.Base;

namespace VianaHub.Global.Gerit.Domain.Entities.Job;

public class JobDefinitionsEntity : Entity, IAggregateRoot
{
    // Propriedades principais
    public string? JobCategory { get; private set; }
    public string? JobName { get; private set; }
    public string? Description { get; private set; }
    public string? JobPurpose { get; private set; }
    public string? JobType { get; private set; }
    public string? JobMethod { get; private set; }
    public string? CronExpression { get; private set; }
    public string? TimeZoneId { get; private set; }
    public bool ExecuteOnlyOnce { get; private set; }
    public int TimeoutMinutes { get; private set; }
    public int Priority { get; private set; }
    public string? Queue { get; private set; }
    public int MaxRetries { get; private set; }
    public string? JobConfiguration { get; private set; }
    public bool IsSystemJob { get; private set; }
    public string? HangfireJobId { get; private set; }
    public DateTime? LastRegisteredAt { get; private set; }

    // Estado l�gico
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    // Construtor protegido para EF
    protected JobDefinitionsEntity() { }

    // F�brica de cria��o com valores padr�o conforme regras de neg�cio
    public JobDefinitionsEntity(
        string? jobCategory,
        string? jobName,
        string? jobType,
        int createdBy,
        string? description = null,
        string? jobPurpose = null,
        string? jobMethod = "Execute",
        string? cronExpression = null,
        string? timeZoneId = "GMT Standard Time",
        bool executeOnlyOnce = false,
        int timeoutMinutes = 5,
        int priority = 5,
        string? queue = "default",
        int maxRetries = 3,
        string? jobConfiguration = null,
        bool isSystemJob = false)
        : base()
    {
        JobCategory = jobCategory;
        JobName = jobName;
        Description = description;
        JobPurpose = jobPurpose;
        JobType = jobType;
        JobMethod = string.IsNullOrWhiteSpace(jobMethod) ? "Execute" : jobMethod;
        CronExpression = cronExpression;
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "GMT Standard Time" : timeZoneId;
        ExecuteOnlyOnce = executeOnlyOnce;
        TimeoutMinutes = timeoutMinutes;
        Priority = priority;
        Queue = string.IsNullOrWhiteSpace(queue) ? "default" : queue;
        MaxRetries = maxRetries;
        JobConfiguration = jobConfiguration;
        IsSystemJob = isSystemJob;

        IsActive = true;
        IsDeleted = false;

        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? description,
        string? jobPurpose,
        string? cronExpression,
        string? timeZoneId,
        int timeoutMinutes,
        int priority,
        string? queue,
        int maxRetries,
        string? jobConfiguration,
        bool isActive,
        int? modifiedBy)
    {
        Description = description;
        JobPurpose = jobPurpose;
        CronExpression = cronExpression;
        TimeZoneId = timeZoneId;
        TimeoutMinutes = timeoutMinutes;
        Priority = priority;
        Queue = queue;
        MaxRetries = maxRetries;
        JobConfiguration = jobConfiguration;
        IsActive = isActive;

        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Activate(int? modifiedBy)
    {
        IsActive = true;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate(int? modifiedBy)
    {
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Delete(int? modifiedBy)
    {
        if (IsSystemJob)
            throw new InvalidOperationException("Cannot delete system job");

        IsDeleted = true;
        IsActive = false;
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetHangfireRegistration(string? hangfireJobId)
    {
        HangfireJobId = hangfireJobId;
        LastRegisteredAt = DateTime.UtcNow;
    }
}
