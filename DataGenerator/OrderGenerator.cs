using Microsoft.Azure.Cosmos;
using ServerlessMaterializedView.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataGenerator
{
    class OrderGenerator
    {
		private Random random = new Random();

		public async Task SubmitRandomOrders(int orderCount, Database database)
		{
			var products = await database.CountItems(Constants.ProductsContainerId);
			//products.Dump("Product Count");

			var response = await database.CreateContainerIfNotExistsAsync(Constants.OrdersContainerId, "/CustomerEmail");
			var container = response.Container;
			for (int n = 0; n < orderCount; n++)
			{
				var document = CreateRandomOrder(products);
				var r = await container.CreateItemAsync(document);
			}
		}

		Order CreateRandomOrder(int productCount)
		{
			var order = new Order();
			order.Id = Guid.NewGuid();
			order.CustomerEmail = "customer@somewhere.com";
			order.OrderLines = new List<OrderLine>();
			order.OrderDate = DateTime.Now;
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
	}
}
