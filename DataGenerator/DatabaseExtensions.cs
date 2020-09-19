using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace DataGenerator
{
    public static class DatabaseExtensions
    {
		public static async Task<int> CountItems(this Database database, string containerId)
		{
			var container = database.GetContainer(containerId);
			var it = container.GetItemQueryIterator<int>("SELECT VALUE COUNT(1) FROM c");
			var respo = await it.ReadNextAsync();
			var count = respo.Resource.First();
			return count;
		}

	}
}
