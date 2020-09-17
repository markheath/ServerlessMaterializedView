using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ChangeFeedProcessor
{
    public static class Function1
    {
        private const string databaseName = "investigate";
        private const string ordersCollectionName = "orders";
        private const string productsCollectionName = "products";
        private const string connectionStringSettingName = "OrdersDatabase";

        [FunctionName("Function1")]
        public static async Task Run([CosmosDBTrigger(
                databaseName: databaseName,
                collectionName: ordersCollectionName,
                ConnectionStringSetting = connectionStringSettingName,
                LeaseCollectionName = "leases",
                StartFromBeginning = true,
                CreateLeaseCollectionIfNotExists = true)]
            IReadOnlyList<Document> input,

            [CosmosDB(
                databaseName: databaseName,
                collectionName: productsCollectionName,
                ConnectionStringSetting = "OrdersDatabase")] DocumentClient productsClient,

            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].Id);
                foreach (var doc in input)
                {
                    Order order = (dynamic) doc;
                    log.LogInformation("Order: " + order.Id);

                    if (order.OrderLines.Count <= 1)
                    {
                        // no related products to process
                        continue;
                    }
                    foreach (var orderLine in order.OrderLines)
                    {
                        //var docLink = $"dbs/{databaseName}/colls/{productsCollectionName}/docs/{orderLine.ProductId}";
                        var docUri = UriFactory.CreateDocumentUri(databaseName, productsCollectionName, orderLine.ProductId);
                        var document = await productsClient.ReadDocumentAsync(docUri,
                            new RequestOptions { PartitionKey = new PartitionKey("clothing") }); // partition key is "/Category"
                        Product p = (dynamic) document.Resource;
                        if (p.RelatedProducts == null)
                        {
                            p.RelatedProducts = new Dictionary<string, int>();
                        }

                        log.LogInformation("Product:" + orderLine.ProductId);
                        foreach (var boughtWith in order.OrderLines.Where(l => l.ProductId != orderLine.ProductId))
                        {
                            if (p.RelatedProducts.ContainsKey(boughtWith.ProductId))
                            {
                                p.RelatedProducts[boughtWith.ProductId]++;
                            }
                            else
                            {
                                p.RelatedProducts[boughtWith.ProductId] = 1;
                            }
                        }

                        await productsClient.ReplaceDocumentAsync(document.Resource.SelfLink, p);

                    }
                }
            }
        }
    }

    public class RelatedProduct
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public Dictionary<string, int> RelatedProducts { get; set; }
    }

    public class Product
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Rating { get; set; }
        public Dictionary<string, int> RelatedProducts { get; set; }
    }

    public class Order
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        public string CustomerEmail { get; set; }
        public List<OrderLine> OrderLines { get; set; }
    }

    public class OrderLine
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
