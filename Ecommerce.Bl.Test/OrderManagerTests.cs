using Ecommerce.Entity;
using Ecommerce.Entity.Common;

namespace Ecommerce.Bl.Test;

public class OrderManagerTests
{
    [Test]
    public void CreateOrder() {
        var payment = new Payment(){ TransactionId = "21345", Amount = 100, Id = 0, PaymentMethod = PaymentMethod.CARD };
        payment = TestContext._paymentRepository.Add(payment);
        var offer = TestContext._offerRepository.Find(f => true);
        TestContext._cartManager.Add(offer);
        var o = TestContext._orderManager.CreateOrder(payment);
        var o1=TestContext._orderRepository.Find(or => or.Id == o.Id);
        Assert.That(TestContext.DeepEquals(o, o1), Is.True);
    }
}