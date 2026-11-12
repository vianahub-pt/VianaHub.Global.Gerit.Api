using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Application.Dtos.Base;

public class PagedFilterRequest : Paging
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
