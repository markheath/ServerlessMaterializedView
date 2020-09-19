using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServerlessMaterializedView.Models
{
    public class Product
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Rating { get; set; }
        public Dictionary<string, RelatedProduct> Recommendations { get; set; }
    }
}
