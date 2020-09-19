using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ServerlessMaterializedView.Models
{
    public class RelatedProduct
    {
        public int Count { get; set; }
        public DateTime MostRecentPurchase { get; set; }

    }
}
