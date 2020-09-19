using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using ServerlessMaterializedView.Models;

namespace ChangeFeedProcessor
{
    public static class RecommendationViewBuilder
    {
        private const string databaseName = "investigate";
        private const string connectionStringSettingName = "OrdersDatabase";

        [FunctionName("UpdateRecommendationView")]
        public static async Task Run([CosmosDBTrigger(
                databaseName: databaseName,
                collectionName: Constants.OrdersContainerId,
                ConnectionStringSetting = connectionStringSettingName,
                LeaseCollectionName = "leases",
                StartFromBeginning = true,
                CreateLeaseCollectionIfNotExists = true)]
            IReadOnlyList<Document> input,

            [CosmosDB(
                databaseName: databaseName,
                collectionName: Constants.ProductsContainerId,
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
                        var docUri = UriFactory.CreateDocumentUri(databaseName, Constants.ProductsContainerId, orderLine.ProductId);
                        var document = await productsClient.ReadDocumentAsync(docUri,
                            new RequestOptions { PartitionKey = new PartitionKey("clothing") }); // partition key is "/Category"
                        Product p = (dynamic) document.Resource;
                        if (p.Recommendations == null)
                        {
                            p.Recommendations = new Dictionary<string, RelatedProduct>();
                        }

                        log.LogInformation("Product:" + orderLine.ProductId);
                        foreach (var boughtWith in order.OrderLines.Where(l => l.ProductId != orderLine.ProductId))
                        {
                            if (p.Recommendations.ContainsKey(boughtWith.ProductId))
                            {
                                p.Recommendations[boughtWith.ProductId].Count++;
                                p.Recommendations[boughtWith.ProductId].MostRecentPurchase = order.OrderDate;
                            }
                            else
                            {
                                p.Recommendations[boughtWith.ProductId] = new RelatedProduct()
                                {
                                    Count = 1,
                                    MostRecentPurchase = order.OrderDate
                                };
                            }
                        }                        
                        if (p.Recommendations.Count > 50)
                        {
                            log.LogInformation("Truncating recommendations");
                            p.Recommendations = p.Recommendations
                                .OrderBy(kvp => kvp.Value.Count)
                                .ThenByDescending(kvp => kvp.Value.MostRecentPurchase)
                                .Take(50)
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        }
                        await productsClient.ReplaceDocumentAsync(document.Resource.SelfLink, p);

                    }
                }
            }
        }
    }
}
