namespace CafeApp.Business.DTOs.Report;

public class DailyDashboardSummaryDto
{
    public decimal TodayRevenue { get; set; }
    public decimal TodayNetProfit { get; set; }
    public int TodayCompletedOrdersCount { get; set; }
    public int ActiveTablesCount { get; set; }
}

public class ZReportResultDto
{
    public DateTime ReportDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal NetProfit { get; set; }
    public int TotalOrders { get; set; }
}