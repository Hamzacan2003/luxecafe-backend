using CafeApp.DataAccess.Entities;

namespace CafeApp.Business.DTOs.Order;

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderDto
{
    public Guid TableId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CompletePaymentDto
{
    public Guid OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}

public class ActiveTableOrderDto
{
    public Guid OrderId { get; set; }
    public Guid TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDetailDto> Items { get; set; } = new();
}

public class OrderItemDetailDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}