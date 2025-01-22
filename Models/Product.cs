﻿namespace ReportingService.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int InStock { get; set; }
        public int OrdersReceived { get; set; }

        // Navigation Property
        public ICollection<Order> Orders { get; set; }
    }
}
