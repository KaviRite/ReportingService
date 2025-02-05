namespace ReportingService.Models
{
    public class Address
    {
        public int AddressId { get; set; }
        public string ShippingAddress { get; set; }
        public string BillingAddress { get; set; }
        public string City { get; set; }
        public string State {  get; set; }
    }
}
