using FluentValidation;
using Cronos;
using VianaHub.Global.Gerit.Domain.Entities.Job;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Job;

public class JobDefinitionUpdateValidator : AbstractValidator<JobDefinitionsEntity>
{
    public JobDefinitionUpdateValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description))
            .WithMessage(localization.GetMessage("Domain.Job.DescriptionMaxLength", 1000));

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

        RuleFor(x => x.CronExpression)
            .Must(cron => IsValidCron(cron) || string.IsNullOrWhiteSpace(cron))
            .WithMessage(localization.GetMessage("Domain.Job.CronInvalid"));

        RuleFor(x => x.TimeZoneId)
            .Must(tz => IsValidTimeZone(tz) || string.IsNullOrWhiteSpace(tz))
            .WithMessage(localization.GetMessage("Domain.Job.TimeZoneInvalid"));

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
        var t = Type.GetType(typeName);
        if (t != null) return t;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }
            catch
            {
            }
        }

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
            }
        }

        return null;
    }
}
