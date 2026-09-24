using CafeApp.Business.DTOs.Order;
using CafeApp.Business.Services.Concrete;
using CafeApp.DataAccess.Contexts;
using CafeApp.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly CafeDbContext _context;

    public OrdersController(OrderService orderService, CafeDbContext context)
    {
        _orderService = orderService;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrAddItems([FromBody] CreateOrderDto dto)
    {
        var result = await _orderService.CreateOrAddItemsToOrderAsync(dto);
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message, orderId = result.OrderId });
    }

    [HttpGet("table/{tableId}/active")]
    public async Task<IActionResult> GetActiveOrder(Guid tableId)
    {
        var order = await _orderService.GetActiveOrderDetailsByTableIdAsync(tableId);
        if (order == null)
            return NotFound(new { message = "Bu masada açık bir adisyon bulunmuyor." });

        return Ok(order);
    }

    // Masadaki açık adisyondan ürünün adedini azaltma veya tamamen silme (Stok iadesiyle)
    [HttpPost("update-item-quantity")]
    public async Task<IActionResult> UpdateItemQuantity([FromBody] UpdateOrderItemDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.Status == OrderStatus.Active);

        if (order == null) return NotFound(new { message = "Aktif sipariş bulunamadı." });

        var item = order.OrderItems.FirstOrDefault(oi => oi.ProductId == dto.ProductId);
        if (item == null) return NotFound(new { message = "Ürün adisyonda bulunamadı." });

        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null) return NotFound(new { message = "Ürün bulunamadı." });

        if (dto.Delta > 0)
        {
            if (product.StockQuantity < dto.Delta)
                return BadRequest(new { message = $"Yetersiz stok! Kalan stok: {product.StockQuantity}" });

            product.StockQuantity -= dto.Delta;
            item.Quantity += dto.Delta;
            order.TotalAmount += (product.UnitPrice * dto.Delta);
            order.TotalCost += (product.CostPrice * dto.Delta);
        }
        else if (dto.Delta < 0)
        {
            int removeQty = Math.Abs(dto.Delta);
            if (item.Quantity <= removeQty)
            {
                product.StockQuantity += item.Quantity;
                order.TotalAmount -= (product.UnitPrice * item.Quantity);
                order.TotalCost -= (product.CostPrice * item.Quantity);
                _context.OrderItems.Remove(item);
            }
            else
            {
                product.StockQuantity += removeQty;
                item.Quantity -= removeQty;
                order.TotalAmount -= (product.UnitPrice * removeQty);
                order.TotalCost -= (product.CostPrice * removeQty);
            }
        }

        if (!order.OrderItems.Any())
        {
            order.Status = OrderStatus.Cancelled;
            var table = await _context.Tables.FindAsync(order.TableId);
            if (table != null) table.Status = TableStatus.Empty;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Adisyon güncellendi." });
    }

    [HttpPost("complete-payment")]
    public async Task<IActionResult> CompletePayment([FromBody] CompletePaymentDto dto)
    {
        var result = await _orderService.CompletePaymentAsync(dto);
        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // Günlük Kasa & Fiş Dökümü
    [HttpGet("daily-summary")]
    public async Task<IActionResult> GetDailySummary()
    {
        var today = DateTime.UtcNow.Date;

        var closedOrdersToday = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.Status == OrderStatus.Completed && o.ClosedAt >= today)
            .ToListAsync();

        var totalRevenue = closedOrdersToday.Sum(o => o.TotalAmount);
        var totalCost = closedOrdersToday.Sum(o => o.TotalCost);
        var netProfit = totalRevenue - totalCost;

        var productSales = closedOrdersToday
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.UnitPrice })
            .Select(g => new
            {
                ProductName = g.Key.Name,
                Quantity = g.Sum(x => x.Quantity),
                UnitPrice = g.Key.UnitPrice,
                Total = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.Quantity)
            .ToList();

        return Ok(new
        {
            TotalRevenue = totalRevenue,
            CashTotal = totalRevenue,
            CardTotal = 0m,
            NetProfit = netProfit,
            CompletedTables = closedOrdersToday.Count,
            ProductSales = productSales
        });
    }

    // Müdür Paneli İçin Aylık ve Yıllık Kâr/Zarar
    [HttpGet("financial-analytics")]
    public async Task<IActionResult> GetFinancialAnalytics()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var allClosedOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed && o.ClosedAt.HasValue)
            .ToListAsync();

        var monthlyOrders = allClosedOrders.Where(o => o.ClosedAt >= startOfMonth).ToList();
        var yearlyOrders = allClosedOrders.Where(o => o.ClosedAt >= startOfYear).ToList();

        var monthlyRevenue = monthlyOrders.Sum(o => o.TotalAmount);
        var monthlyCost = monthlyOrders.Sum(o => o.TotalCost);
        var monthlyProfit = monthlyRevenue - monthlyCost;

        var yearlyRevenue = yearlyOrders.Sum(o => o.TotalAmount);
        var yearlyCost = yearlyOrders.Sum(o => o.TotalCost);
        var yearlyProfit = yearlyRevenue - yearlyCost;

        return Ok(new
        {
            Monthly = new { Revenue = monthlyRevenue, Cost = monthlyCost, NetProfit = monthlyProfit, OrderCount = monthlyOrders.Count },
            Yearly = new { Revenue = yearlyRevenue, Cost = yearlyCost, NetProfit = yearlyProfit, OrderCount = yearlyOrders.Count }
        });
    }
}

public class UpdateOrderItemDto
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Delta { get; set; }
}