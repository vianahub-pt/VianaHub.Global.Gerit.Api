namespace VianaHub.Global.Gerit.Domain.Tools.Pagination;

public class ListPage<T>
{
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }

    public ListPage() { }
    public ListPage(IEnumerable<T> items, int pageNumber, int pageSize, int totalItems, int totalPages)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = totalPages;
    }
}
