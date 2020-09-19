using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using ServerlessMaterializedView.Models;

namespace DataGenerator
{
    class Program
    {
		static Random random = new Random();



		public static async Task Main()
		{
			// materialized view experiment
			var databaseId = "investigate";

			var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONN_STR");
			if (string.IsNullOrEmpty(connectionString))
            {
				Console.WriteLine("You need to set the COSMOS_CONN_STR environment variable");
            }
			var cosmosOptions = new CosmosClientOptions()
			{ ConnectionMode = ConnectionMode.Gateway }; // Gateway mode to get round firewall?

			using (var client = new CosmosClient(connectionString, cosmosOptions))
			{
				var database = client.GetDatabase(databaseId);  //client.CreateDatabaseIfNotExistsAsync(databaseId);
				Console.WriteLine("1. Generate Products");
				Console.WriteLine("2. Generate Random Orders");
				Console.WriteLine("3. Slow query for recommendations");
				var choice = Console.ReadKey();
				switch(choice.KeyChar)
                {
					case '1':
						Console.WriteLine("Generating products...");
						var response = await database.CreateContainerIfNotExistsAsync(Constants.ProductsContainerId, "/Category");
						var productsContainer = response.Container;
						var productGenerator = new ProductGenerator();
						await productGenerator.CreateProducts(productsContainer); 
						break;
					case '2':
						var orderGenerator = new OrderGenerator();
						await orderGenerator.SubmitRandomOrders(1000, database);
						var orders = await database.CountItems(Constants.OrdersContainerId);
						Console.WriteLine($"Order count {orders}");
						break;
					case '3':
						var sw = Stopwatch.StartNew();
						var p = await RecommendationQuery.FindRelatedProductsSlow(database, "product50");
						Console.WriteLine($"{p.Count} recommendations returned in {sw.ElapsedMilliseconds}ms");
						break;
					default:
						Console.WriteLine("Invalid selection");
						break;
				}
				//p.Dump("Recommended");
			}
		}
	}
}
