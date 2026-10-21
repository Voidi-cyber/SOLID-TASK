using System;
using System.Collections.Generic;
using System.Linq;

namespace SolidLab.GoodCode
{
    //ISP: малі спеціалізовані інтерфейси товарів
    public interface IItem
    {
        string Name { get; }
        decimal Price { get; }
    }

    public interface IShippableItem : IItem
    {
        double Weight { get; }
    }

    // Фізичний товар: має вагу, потребує доставки
    public class PhysicalItem : IShippableItem
    {
        public string Name { get; }
        public decimal Price { get; }
        public double Weight { get; }

        public PhysicalItem(string name, decimal price, double weight)
        {
            Name = name;
            Price = price;
            Weight = weight;
        }
    }

    // Цифровий товар: ваги немає, доставка не потрібна
    public class DigitalItem : IItem
    {
        public string Name { get; }
        public decimal Price { get; }

        public DigitalItem(string name, decimal price)
        {
            Name = name;
            Price = price;
        }
    }

    // SRP: Order — лише модель стану замовлення

    public class Order
    {
        public List<IItem> Items { get; } = new();
        public string CustomerEmail { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
    }

    // SRP: PriceCalculator — лише обчислення сум

    public class PriceCalculator
    {
        private const decimal ShippingRatePerKg = 5.0m;

        public decimal CalculateItemsTotal(Order order)
            => order.Items.Sum(i => i.Price);

        // Доставка рахується лише для IShippableItem — цифрові товари ігноруються
        public decimal CalculateShipping(Order order)
            => (decimal)order.Items.OfType<IShippableItem>().Sum(i => i.Weight) * ShippingRatePerKg;

        public decimal CalculateFinalAmount(Order order)
            => CalculateItemsTotal(order) + CalculateShipping(order);
    }

    // OCP + LSP: абстракція оплати 

    /// <summary>
    /// Контракт (LSP): метод НІКОЛИ не кидає винятків через бізнес-причини
    /// (некоректна сума, відмова шлюзу тощо) — просто повертає false.
    /// true — оплата успішна, false — неуспішна. Стан програми не змінюється.
    /// </summary>
    public interface IPaymentProcessor
    {
        bool ProcessPayment(decimal amount);
    }

    public class CreditCardProcessor : IPaymentProcessor
    {
        public bool ProcessPayment(decimal amount)
        {
            if (amount <= 0) return false;
            Console.WriteLine($"Charging ${amount} via Credit Card Gateway...");
            return true;
        }
    }

    public class PayPalProcessor : IPaymentProcessor
    {
        public bool ProcessPayment(decimal amount)
        {
            if (amount <= 0) return false;
            Console.WriteLine($"Charging ${amount} via PayPal API...");
            return true;
        }
    }

    // Новий спосіб оплати додається НОВИМ класом, без змін OrderProcessor (OCP):
    public class CryptoProcessor : IPaymentProcessor
    {
        public bool ProcessPayment(decimal amount)
        {
            if (amount <= 0) return false;
            Console.WriteLine($"Charging ${amount} via Crypto Wallet...");
            return true;
        }
    }

    // SRP + DIP: збереження та сповіщення за абстракціями

    public interface IOrderRepository
    {
        void Save(Order order);
    }

    public class MySqlOrderRepository : IOrderRepository
    {
        public void Save(Order order)
            => Console.WriteLine("Saving order to MySQL Database (localhost:3306)...");
    }

    public interface INotifier
    {
        void Notify(string recipient, string message);
    }

    public class EmailNotifier : INotifier
    {
        public void Notify(string recipient, string message)
            => Console.WriteLine($"Sending email to {recipient}: {message}");
    }

    // DIP: OrderProcessor залежить лише від абстракцій 

    public class OrderProcessor
    {
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly IOrderRepository _orderRepository;
        private readonly INotifier _notifier;
        private readonly PriceCalculator _priceCalculator;

        public OrderProcessor(
            IPaymentProcessor paymentProcessor,
            IOrderRepository orderRepository,
            INotifier notifier,
            PriceCalculator priceCalculator)
        {
            _paymentProcessor = paymentProcessor;
            _orderRepository = orderRepository;
            _notifier = notifier;
            _priceCalculator = priceCalculator;
        }

        // Лише оркестрація: сама нічого не рахує, не оплачує, не зберігає
        public bool Checkout(Order order)
        {
            decimal finalAmount = _priceCalculator.CalculateFinalAmount(order);

            order.IsPaid = _paymentProcessor.ProcessPayment(finalAmount);
            if (!order.IsPaid) return false;

            _orderRepository.Save(order);
            _notifier.Notify(order.CustomerEmail, $"Your order is confirmed. Total: ${finalAmount}");

            return true;
        }
    }

    // Composition Root — єдине місце, де створюються конкретні класи

    public static class SOLID_TASK
    {
        public static void Main()
        {
            var order = new Order { CustomerEmail = "client@example.com" };
            order.Items.Add(new PhysicalItem("Keyboard", 100m, 1.2));
            order.Items.Add(new DigitalItem("C# E-Book", 20m));

            // Спосіб оплати обирається при складанні залежностей
            var processor = new OrderProcessor(
                new PayPalProcessor(),          
                new MySqlOrderRepository(),
                new EmailNotifier(),
                new PriceCalculator());

            bool ok = processor.Checkout(order);
            Console.WriteLine($"Result: {ok}, IsPaid: {order.IsPaid}");
        }
    }
}