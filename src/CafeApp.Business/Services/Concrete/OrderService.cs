using CafeApp.Business.DTOs.Order;
using CafeApp.DataAccess.Contexts;
using CafeApp.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeApp.Business.Services.Concrete;

public class OrderService
{
    private readonly CafeDbContext _context;

    public OrderService(CafeDbContext context)
    {
        _context = context;
    }

    // 1. Masaya Sipariş Ekleme / Yeni Adisyon Açma (Stok Düşümüyle Birlikte)
    public async Task<(bool Success, string Message, Guid? OrderId)> CreateOrAddItemsToOrderAsync(CreateOrderDto dto)
    {
        var table = await _context.Tables.FindAsync(dto.TableId);
        if (table == null)
            return (false, "Masa bulunamadı.", null);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Masanın açık (ödenmemiş) bir siparişi var mı?
            var activeOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.TableId == dto.TableId && o.Status == OrderStatus.Active);

            if (activeOrder == null)
            {
                activeOrder = new Order
                {
                    Id = Guid.NewGuid(),
                    TableId = dto.TableId,
                    Status = OrderStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = 0,
                    TotalCost = 0,
                    OrderItems = new List<OrderItem>()
                };

                _context.Orders.Add(activeOrder);
                table.Status = TableStatus.Occupied;

                // Siparişin Id'sinin netleşmesi için ara kayıt
                await _context.SaveChangesAsync();
            }
            else if (activeOrder.OrderItems == null)
            {
                activeOrder.OrderItems = new List<OrderItem>();
            }

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Ürün bulunamadı (ID: {itemDto.ProductId})", null);
                }

                // Stok yeterli mi kontrolü
                if (product.StockQuantity < itemDto.Quantity)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Yetersiz stok! '{product.Name}' için kalan stok: {product.StockQuantity}, istenen adet: {itemDto.Quantity}", null);
                }

                // Stoğu düş
                product.StockQuantity -= itemDto.Quantity;

                // Siparişte bu ürün daha önce eklenmiş mi?
                var existingItem = activeOrder.OrderItems.FirstOrDefault(oi => oi.ProductId == product.Id);
                if (existingItem != null)
                {
                    existingItem.Quantity += itemDto.Quantity;
                }
                else
                {
                    var orderItem = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = activeOrder.Id,
                        ProductId = product.Id,
                        Quantity = itemDto.Quantity,
                        UnitPrice = product.UnitPrice,
                        CostPrice = product.CostPrice
                    };

                    _context.OrderItems.Add(orderItem);
                    activeOrder.OrderItems.Add(orderItem);
                }

                // Toplam sipariş ve maliyet tutarlarını güncelle
                activeOrder.TotalAmount += (product.UnitPrice * itemDto.Quantity);
                activeOrder.TotalCost += (product.CostPrice * itemDto.Quantity);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Sipariş başarıyla işlendi ve stoktan düşüldü.", activeOrder.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Sipariş işlemi başarısız: {ex.Message}", null);
        }
    }

    // 2. Masanın Güncel Adisyonunu Getir
    // 2. Masanın Güncel Adisyonunu Getir
    public async Task<ActiveTableOrderDto?> GetActiveOrderDetailsByTableIdAsync(Guid tableId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.TableId == tableId && o.Status == OrderStatus.Active);

        if (order == null) return null;

        return new ActiveTableOrderDto
        {
            OrderId = order.Id,
            TableId = order.TableId,
            TableNumber = order.Table?.TableNumber ?? string.Empty,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemDetailDto
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? "Ürün Bilgisi Yok",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
                // TotalPrice salt okunur olduğu için buraya yazmıyoruz, otomatik Quantity * UnitPrice hesaplanıyor.
            }).ToList()
        };
    }

    // 3. Masanın Hesabını Kapatma (Ödeme Alma)
    public async Task<(bool Success, string Message)> CompletePaymentAsync(CompletePaymentDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null)
            return (false, "Sipariş bulunamadı.");

        if (order.Status == OrderStatus.Completed)
            return (false, "Bu siparişin ödemesi zaten alınmış.");

        order.Status = OrderStatus.Completed;
        order.ClosedAt = DateTime.UtcNow;

        // Masayı boşalt
        if (order.Table != null)
        {
            order.Table.Status = TableStatus.Empty;
        }

        await _context.SaveChangesAsync();
        return (true, "Ödeme başarıyla tamamlandı, masa boşaltıldı.");
    }
}