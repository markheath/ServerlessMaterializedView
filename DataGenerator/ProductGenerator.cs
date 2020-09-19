using Microsoft.Azure.Cosmos;
using ServerlessMaterializedView.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataGenerator
{
    class ProductGenerator
    {
		private Random random = new Random();

		public async Task CreateProducts(Container container)
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

			int n = 0;
			foreach (var p in products)
			{
				var document = new Product
				{
					Id = $"product{++n}",
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
