using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Subscription;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Function;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Subscription;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Billing;

public class SubscriptionAppService : ISubscriptionAppService
{
    private readonly ISubscriptionDataRepository _repo;
    private readonly ISubscriptionDomainService _domain;
    private readonly ICurrentUserService _currentUser;
    private readonly INotify _notify;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localization;
    private readonly ILogger<SubscriptionAppService> _logger;
    private readonly IFileValidationService _fileValidation;

    public SubscriptionAppService(
        ISubscriptionDataRepository repo,
        ISubscriptionDomainService domain,
        INotify notify,
        IMapper mapper,
        ICurrentUserService currentUser,
        ILocalizationService localization,
        ILogger<SubscriptionAppService> logger,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _domain = domain;
        _notify = notify;
        _mapper = mapper;
        _currentUser = currentUser;
        _localization = localization;
        _logger = logger;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<SubscriptionResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _domain.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<SubscriptionResponse>>(entities);
    }

    public async Task<SubscriptionDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _domain.GetByIdAsync(id, ct);
        return _mapper.Map<SubscriptionDetailResponse>(entity);
    }

    public async Task<SubscriptionDetailResponse> GetByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        var entity = await _domain.GetByTenantIdAsync(tenantId, ct);
        return _mapper.Map<SubscriptionDetailResponse>(entity);
    }

    public async Task<ListPageResponse<SubscriptionResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _domain.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<SubscriptionResponse>>(paged);
    }

    public async Task<IEnumerable<SubscriptionResponse>> GetActiveSubscriptionsAsync(CancellationToken ct)
    {
        var entities = await _domain.GetActiveSubscriptionsAsync(ct);
        return _mapper.Map<IEnumerable<SubscriptionResponse>>(entities);
    }

    public async Task<IEnumerable<SubscriptionResponse>> GetExpiringSubscriptionsAsync(int daysBeforeExpiration, CancellationToken ct)
    {
        var entities = await _domain.GetExpiringSubscriptionsAsync(daysBeforeExpiration, ct);
        return _mapper.Map<IEnumerable<SubscriptionResponse>>(entities);
    }

    public async Task<int> CreateAsync(CreateSubscriptionRequest request, CancellationToken ct)
    {
        // Verifica se j� existe uma subscription ativa para o tenant
        var exists = await _domain.ExistsByTenantIdAsync(request.TenantId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.Create.TenantAlreadyHasSubscription"), 400);
            return 0;
        }

        var entity = new SubscriptionEntity(
            request.TenantId,
            request.SubscriptionPlanId,
            request.StatusDefinitionId,
            request.StatusDomainId,
            request.AgreedAmount,
            request.BillingInterval,
            request.CurrencyCode,
            request.CurrentPeriodStart,
            request.CurrentPeriodEnd,
            request.TrialStart,
            request.TrialEnd,
            request.StripeCustomerId,
            _currentUser.GetUserId()
        );

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateSubscriptionRequest request, CancellationToken ct)
    {
        var entity = await _domain.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.Update.ResourceNotFound"), 404);
            return false;
        }

        entity.Update(
            request.SubscriptionPlanId,
            request.StatusDefinitionId,
            request.StatusDomainId,
            request.AgreedAmount,
            request.BillingInterval,
            request.CurrencyCode,
            request.CurrentPeriodStart,
            request.CurrentPeriodEnd,
            request.TrialStart,
            request.TrialEnd,
            _currentUser.GetUserId()
        );

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _domain.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.Activate.ResourceNotFound"), 404);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct)
    {
        var entity = await _domain.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.Deactivate.ResourceNotFound"), 404);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await _domain.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.Delete.ResourceNotFound"), 404);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> CancelAsync(int id, CancelSubscriptionRequest request, CancellationToken ct)
    {
        var entity = await _domain.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.Cancel.ResourceNotFound"), 404);
            return false;
        }

        entity.Cancel(request.CancellationReason, request.CancelAtPeriodEnd, _currentUser.GetUserId());
        return await _domain.CancelAsync(entity, ct);
    }

    public async Task<bool> RenewAsync(int id, RenewSubscriptionRequest request, CancellationToken ct)
    {
        var entity = await _domain.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.Renew.ResourceNotFound"), 404);
            return false;
        }

        entity.Renew(request.NewPeriodEnd, _currentUser.GetUserId());
        return await _domain.RenewAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct)
    {
        // Valida arquivo usando servi�o centralizado
        if (!_fileValidation.ValidateFile(file))
            return false;

        // L� itens do CSV
        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadSubscriptionItem>? ReadCsvFile(IFormFile file)
    {
        try
        {
            // Cria StreamReader com encoding UTF-8 for�ado
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";", // CSV usa ponto e v�rgula como delimitador
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null // Ignora linhas mal formatadas ao inv�s de lan�ar exce��o
            };

            using var csv = new CsvReader(reader, config);
            var records = new List<BulkUploadSubscriptionItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadSubscriptionItem>();
                    if (record != null)
                    {
                        // Valida se os campos n�o cont�m conte�do perigoso
                        if (record.TenantId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.TenantId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.SubscriptionPlanId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.SubscriptionPlanId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.StatusDefinitionId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.StatusDefinitionId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.StatusDomainId <= 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.StatusDomainId.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.AgreedAmount < 0)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.AgreedAmount.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.CurrentPeriodStart <= DateTime.MinValue)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.CurrentPeriodStart.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.CurrentPeriodEnd <= DateTime.MinValue)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.CurrentPeriodEnd.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }
                        if (record.CurrentPeriodEnd < record.CurrentPeriodStart)
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.CurrentPeriodEnd.CurrentPeriodStart.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    // Log linha com erro mas continua processamento
                    _logger.LogWarning("Erro ao processar linha {RowNumber} do CSV de Subscriptions | Exception: {ExceptionDetails}", rowCount + 2, LogValueSanitizer.ExceptionDetails(ex));
                    _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao ler arquivo CSV de Subscriptions: {Message} | Exception: {ExceptionDetails}", LogValueSanitizer.Sanitize(ex.Message), LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadSubscriptionItem> items, CancellationToken ct)
    {
        var hasErrors = false;
        var tenantId = _currentUser.GetTenantId();

        foreach (var item in items)
        {
            // Valida campos obrigat�rios
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            // Verifica duplicidade
            var exists = await _repo.ExistsByTenantIdAsync(tenantId, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Subscription.ProcessBulkItems.ExistsByTenantId", item.TenantId), 400);
                hasErrors = true;
                continue;
            }

            // Cria a entidade
            var entity = new SubscriptionEntity(tenantId, item.SubscriptionPlanId, item.StatusDefinitionId, item.StatusDomainId, item.AgreedAmount, item.BillingInterval, item.CurrencyCode, item.CurrentPeriodStart, item.CurrentPeriodEnd, item.TrialStart, item.TrialEnd, item.StripeCustomerId, _currentUser.GetUserId());

            // Tenta criar no dom�nio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Subscription.ProcessBulkItems.FailedToCreate"), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadSubscriptionItem item)
    {
        if (item.TenantId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ValidateBulkItem.TenantId", item.TenantId), 400);
            return false;
        }
        if (item.SubscriptionPlanId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ValidateBulkItem.SubscriptionPlanId", item.SubscriptionPlanId), 400);
            return false;
        }
        if (item.StatusDefinitionId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ValidateBulkItem.StatusDefinitionId", item.StatusDefinitionId), 400);
            return false;
        }
        if (item.StatusDomainId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ValidateBulkItem.StatusDomainId", item.StatusDomainId), 400);
            return false;
        }
        if (item.AgreedAmount < 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Subscription.ValidateBulkItem.AgreedAmountNonNegative"), 400);
            return false;
        }
        return true;
    }
}
