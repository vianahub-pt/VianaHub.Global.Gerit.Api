namespace VianaHub.Global.Gerit.Domain.Tools.Pagination;

public class Order
{
    private string? sortBy;
    private string? sortDirection;

    public string? SortBy
    {
        get { return !string.IsNullOrWhiteSpace(sortBy) ? sortBy : "CreatedAt"; }
        set { sortBy = value; }
    }
    public string? SortDirection
    {
        get { return !string.IsNullOrWhiteSpace(sortDirection) && (sortDirection.ToLower().Equals("asc") || sortDirection.ToLower().Equals("desc")) ? sortDirection : "desc"; }
        set { sortDirection = value; }
    }
}

