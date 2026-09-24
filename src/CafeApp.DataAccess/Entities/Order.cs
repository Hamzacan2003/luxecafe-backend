namespace CafeApp.DataAccess.Entities;

public enum OrderStatus
{
    Active = 0,
    Completed = 1,
    Cancelled = 2
}

public class Order : BaseEntity
{
    public Guid TableId { get; set; }
    public RestaurantTable Table { get; set; } = null!;

    public Guid? CashierId { get; set; }
    public AppUser? Cashier { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Active;
    public decimal TotalAmount { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2
}