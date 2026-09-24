namespace CafeApp.DataAccess.Entities;

public enum TableStatus
{
    Empty = 0,
    Occupied = 1,
    Reserved = 2
}

public class RestaurantTable : BaseEntity
{
    public string TableNumber { get; set; } = string.Empty;
    public string QrToken { get; set; } = Guid.NewGuid().ToString("N");
    public TableStatus Status { get; set; } = TableStatus.Empty;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}