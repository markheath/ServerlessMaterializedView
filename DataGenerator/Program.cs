using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace DataGenerator
{
    class Program
    {
		static Random random = new Random();

		const string productsContainerId = "products";
		const string ordersContainerId = "orders";

		public static async Task Main()
		{
			// materialized view experiment
			var databaseId = "investigate";

			var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONN_STR");
			var cosmosOptions = new CosmosClientOptions()
			{ ConnectionMode = ConnectionMode.Gateway }; // Gateway mode to get round firewall?

			using (var client = new CosmosClient(connectionString, cosmosOptions))
			{
				var database = client.GetDatabase(databaseId);  //client.CreateDatabaseIfNotExistsAsync(databaseId);

				// await CreateProducts(database); - one time operation


				//await SubmitRandomOrders(1000, database);
				var orders = await CountItems(database, ordersContainerId);
				Console.WriteLine($"Order count {orders}");
				var p = await FindRelatedProductsSlow(database, "product50");
				//p.Dump("Recommended");
			}

		}


		async Task SubmitRandomOrders(int orderCount, Database database)
		{
			var products = await CountItems(database, productsContainerId);
			//products.Dump("Product Count");

			var response = await database.CreateContainerIfNotExistsAsync(ordersContainerId, "/CustomerEmail");
			var container = response.Container;
			for (int n = 0; n < orderCount; n++)
			{
				var document = CreateRandomOrder(products);
				var r = await container.CreateItemAsync(document);
			}
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

		Order CreateRandomOrder(int productCount)
		{
			var order = new Order();
			order.Id = Guid.NewGuid();
			order.CustomerEmail = "customer@somewhere.com";
			order.OrderLines = new List<OrderLine>();
			var lines = random.Next(1, 8);
			for (int n = 0; n < lines; n++)
			{
				var line = new OrderLine();
				line.ProductId = $"product{random.Next(1, productCount + 1)}";
				line.Quantity = random.Next(1, 5);
				order.OrderLines.Add(line);
			}
			return order;
		}

		static async Task<List<string>> FindRelatedProductsSlow(Database database, string productId)
		{
			var lookup = new Dictionary<string, int>();
			// find all orders whose OrderLines
			var container = database.GetContainer(ordersContainerId);
			var query = container.GetItemQueryIterator<Order>($"SELECT o.OrderLines FROM o JOIN line IN o.OrderLines WHERE line.ProductId = '{productId}'");
			while (query.HasMoreResults)
			{
				var feedIterator = await query.ReadNextAsync();
				//feedIterator.RequestCharge.Dump("RUs"); // 10000: 3.6, 11000: 3.71, 12000: 3.88, 13000: 3.95, 14000: 4.12
				foreach (var order in feedIterator)
				{
					foreach (var line in order.OrderLines.Where(l => l.ProductId != productId))
					{
						if (lookup.ContainsKey(line.ProductId))
						{
							lookup[line.ProductId]++;
						}
						else
						{
							lookup[line.ProductId] = 1;
						}
					}
				}
			}
			//lookup.Dump();
			return lookup
					.OrderByDescending(l => l.Value)
					.Take(5)
					.Select(l => l.Key)
					.ToList();
		}

		static async Task<int> CountItems(Database database, string containerId)
		{
			var container = database.GetContainer(containerId);
			var it = container.GetItemQueryIterator<int>("SELECT VALUE COUNT(1) FROM c");
			var respo = await it.ReadNextAsync();
			var count = respo.Resource.First();
			return count;
		}

		async Task CreateProducts(Database database)
		{
			var dim1 = new[] { "Navy Blue", "Light Blue", "Green", "White", "Black", "Yellow", "Pink", "Purple", "Red", "Orange", "Brown" };
			var dim2 = new[] { "Men's", "Women's", "Children's" };
			var dim3 = new[] { "T-Shirt", "Sweater", "Shorts", "Trousers", "Scarf", "Hat", "Gloves", "Coat", "Jacket", "Socks" };
			var dim4 = new[] { "S", "M", "L", "XL", "XXL" };
			//(dim1.Length * dim2.Length * dim3.Length * dim4.Length).Dump();
			var products =
			from a in dim1
			from b in dim2
			from c in dim3
			from d in dim4
			select $"{a} {b} {c} (Size {d})";

			//products.Dump();

			var response = await database.CreateContainerIfNotExistsAsync(productsContainerId, "/Category");
			var container = response.Container;
			int n = 0;
			foreach (var p in products)
			{
				var document = new
				{
					id = $"product{++n}",
					Description = p,
					Category = "clothing",
					Price = random.Next(4, 30) + 0.99m,
					Rating = random.Next(1, 6)
				};
				var r = await container.UpsertItemAsync(document);
			}
		}

	}
}
