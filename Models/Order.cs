using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServerlessMaterializedView.Models
{
    public class Order
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public string CustomerEmail { get; set; }
        public List<OrderLine> OrderLines { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
