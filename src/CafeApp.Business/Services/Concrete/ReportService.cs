using CafeApp.Business.DTOs.Report;
using CafeApp.DataAccess.Contexts;
using CafeApp.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.Business.Services.Concrete;

public class ReportService
{
    private readonly CafeDbContext _context;

    public ReportService(CafeDbContext context)
    {
        _context = context;
    }

    public async Task<DailyDashboardSummaryDto> GetTodayLiveSummaryAsync()
    {
        var today = DateTime.UtcNow.Date;

        var completedOrdersToday = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed && o.ClosedAt >= today)
            .ToListAsync();

        var activeTables = await _context.Tables
            .AsNoTracking()
            .CountAsync(t => t.Status == TableStatus.Occupied);

        var totalRev = completedOrdersToday.Sum(o => o.TotalAmount);
        var totalCost = completedOrdersToday.Sum(o => o.TotalCost);

        return new DailyDashboardSummaryDto
        {
            TodayRevenue = totalRev,
            TodayNetProfit = totalRev - totalCost,
            TodayCompletedOrdersCount = completedOrdersToday.Count,
            ActiveTablesCount = activeTables
        };
    }

    public async Task<(bool Success, string Message, ZReportResultDto? Data)> CloseDayAndGenerateZReportAsync()
    {
        var openOrdersExist = await _context.Orders.AnyAsync(o => o.Status == OrderStatus.Active);
        if (openOrdersExist)
            return (false, "Gün sonu alınamaz! Masalarda açık adisyonlar var. Önce tüm hesapları kapatın.", null);

        var todayUtc = DateTime.UtcNow.Date;
        var todayDateOnly = DateOnly.FromDateTime(todayUtc);

        var completedOrders = await _context.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.ClosedAt >= todayUtc)
            .ToListAsync();

        var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
        var totalCost = completedOrders.Sum(o => o.TotalCost);
        var netProfit = totalRevenue - totalCost;

        var zReport = new DailyZReport
        {
            ReportDate = todayDateOnly,
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            NetProfit = netProfit,
            TotalOrdersCount = completedOrders.Count
        };

        _context.DailyZReports.Add(zReport);
        await _context.SaveChangesAsync();

        return (true, "Gün sonu başarıyla alındı ve Z raporu kaydedildi.", new ZReportResultDto
        {
            ReportDate = todayDateOnly.ToDateTime(TimeOnly.MinValue),
            TotalRevenue = zReport.TotalRevenue,
            TotalCost = zReport.TotalCost,
            NetProfit = zReport.NetProfit,
            TotalOrders = zReport.TotalOrdersCount
        });
    }
}