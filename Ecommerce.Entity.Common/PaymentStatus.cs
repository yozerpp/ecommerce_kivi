namespace Ecommerce.Entity.Common;

public enum PaymentStatus
{
    Preparing = 1,
    Pending = 2,
    Completed = 4,
    Failed = 8,
    Refunded = 16
}