using Microsoft.Azure.Cosmos;
using ServerlessMaterializedView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataGenerator
{
    class RecommendationQuery
    {
		public static async Task<List<string>> FindRelatedProductsSlow(Database database, string productId)
		{
			var lookup = new Dictionary<string, int>();
			// find all orders whose OrderLines
			var container = database.GetContainer(Constants.OrdersContainerId);
			var query = container.GetItemQueryIterator<Order>($"SELECT o.OrderLines FROM o JOIN line IN o.OrderLines WHERE line.ProductId = '{productId}'");
			while (query.HasMoreResults)
			{
				var feedIterator = await query.ReadNextAsync();
				Console.WriteLine($"RUs {feedIterator.RequestCharge}");
				// 10000: 3.6, 11000: 3.71, 12000: 3.88, 13000: 3.95, 14000: 4.12
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
	}
}
