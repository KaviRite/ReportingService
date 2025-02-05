using System.Text.Json.Serialization;

namespace ReportingService.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public long Contact { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }

        public Address Address { get; set; }

        // Navigation Property
        [JsonIgnore] //Avoid Serialization Cycle
        public ICollection<Order> Orders { get; set; }
    }
}
