namespace VianaHub.Global.Gerit.Domain.ReadModels;

public class JobPagedFilter : PagedFilter
{
    public string? Category { get; set; }
    public bool? IsSystemJob { get; set; }
    public string? Queue { get; set; }

    public JobPagedFilter(string? search = null, bool? isActive = true, int? pageNumber = null, int? pageSize = null, string? sortBy = null, string? sortDirection = null)
        : base(search, isActive, pageNumber, pageSize, sortBy, sortDirection)
    {
    }
}
