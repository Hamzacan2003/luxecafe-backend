namespace CafeApp.DataAccess.Entities;

public class DailyZReport : BaseEntity
{
    public DateOnly ReportDate { get; set; }
    public DateTime ClosedAt { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal NetProfit { get; set; }
    public int TotalOrdersCount { get; set; }

    public Guid ClosedByUserId { get; set; }
    public AppUser ClosedByUser { get; set; } = null!;
}