using FluentValidation;
using Cronos;
using System.Reflection;
using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Job;

public class JobDefinitionCreateValidator : AbstractValidator<JobDefinitionsEntity>
{
    public JobDefinitionCreateValidator(ILocalizationService localization)
    {
        RuleFor(x => x.JobCategory)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.Job.JobCategoryRequired"));

        RuleFor(x => x.JobName)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.Job.JobNameRequired"))
            .MaximumLength(150)
            .WithMessage(localization.GetMessage("Domain.Job.JobNameMaxLength", 150));

        RuleFor(x => x.JobType)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.Job.JobTypeRequired"))
            .MaximumLength(200)
            .WithMessage(localization.GetMessage("Domain.Job.JobTypeMaxLength", 200));

        RuleFor(x => x.JobMethod)
            .MaximumLength(100)
            .WithMessage(localization.GetMessage("Domain.Job.JobMethodMaxLength", 100));

        RuleFor(x => x.TimeoutMinutes)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.Job.TimeoutMustBeGreaterThanZero"));

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 10)
            .WithMessage(localization.GetMessage("Domain.Job.PriorityRange", 1, 10));

        RuleFor(x => x.MaxRetries)
            .GreaterThanOrEqualTo(0)
            .WithMessage(localization.GetMessage("Domain.Job.MaxRetriesNonNegative"));

        RuleFor(x => x)
            .Custom((entity, context) =>
            {
                if (entity.ExecuteOnlyOnce && !string.IsNullOrWhiteSpace(entity.CronExpression))
                {
                    context.AddFailure(localization.GetMessage("Domain.Job.ExecuteOnlyOnceMustNotHaveCron"));
                }

                if (!entity.ExecuteOnlyOnce && string.IsNullOrWhiteSpace(entity.CronExpression))
                {
                    context.AddFailure(localization.GetMessage("Domain.Job.CronRequiredForRecurring"));
                }
            });

        RuleFor(x => x.JobConfiguration)
            .Must(IsValidJsonOrNull)
            .WithMessage(localization.GetMessage("Domain.Job.JobConfigurationInvalid"));

        // Advanced validations: Cron format and TimeZone existence
        RuleFor(x => x.CronExpression)
            .Must(cron => IsValidCron(cron) || string.IsNullOrWhiteSpace(cron))
            .WithMessage(localization.GetMessage("Domain.Job.CronInvalid"));

        RuleFor(x => x.TimeZoneId)
            .Must(tz => IsValidTimeZone(tz) || string.IsNullOrWhiteSpace(tz))
            .WithMessage(localization.GetMessage("Domain.Job.TimeZoneInvalid"));

        // Validate JobType and JobMethod via reflection and whitelist
        RuleFor(x => x)
            .Custom((entity, context) =>
            {
                if (!IsValidJobTypeAndMethod(entity.JobType, entity.JobMethod))
                {
                    context.AddFailure(localization.GetMessage("Domain.Job.JobTypeOrMethodInvalid"));
                }
            });
    }

    private bool IsValidJsonOrNull(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidCron(string? cron)
    {
        if (string.IsNullOrWhiteSpace(cron)) return true;
        try
        {
            var _ = CronExpression.Parse(cron);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidTimeZone(string? tz)
    {
        if (string.IsNullOrWhiteSpace(tz)) return true;
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(tz);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidJobTypeAndMethod(string? jobType, string? jobMethod)
    {
        if (string.IsNullOrWhiteSpace(jobType)) return false;

        // Whitelist namespaces
        var allowedPrefixes = new[] { "VianaHub.Global.Gerit.Infra.Job.Jobs" };
        if (!allowedPrefixes.Any(p => jobType.StartsWith(p)))
            return false;

        try
        {
            var type = ResolveType(jobType);
            if (type == null) return false;
            var methodName = string.IsNullOrWhiteSpace(jobMethod) ? "Execute" : jobMethod;
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
            return method != null;
        }
        catch
        {
            return false;
        }
    }

    private Type? ResolveType(string typeName)
    {
        // Try Type.GetType first (works if assembly-qualified)
        var t = Type.GetType(typeName);
        if (t != null) return t;

        // Search all loaded assemblies for the type full name
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }
            catch
            {
                // ignore assembly load exceptions
            }
        }

        // As a last resort, try to find by name only (type name without namespace)
        var simpleName = typeName.Split('.').Last();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var found = asm.GetTypes().FirstOrDefault(x => x.Name == simpleName);
                if (found != null) return found;
            }
            catch
            {
                // ignore
            }
        }

        return null;
    }
}
