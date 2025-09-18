using ServerPMS.Abstractions.Infrastructure.Concurrency;
namespace ServerPMS.Infrastructure.Concurrency
{

	public class ResourceMapper : IResourceMapper
	{
        private Dictionary<string, object>[] resourceMaps;

        public ResourceMapper()
		{
            resourceMaps = new[]
            {
                new Dictionary<string,object>(),  // orders
                new Dictionary<string,object>(),  // units
                new Dictionary<string,object>()   // queues
            };
        }

        public async Task<object> GetValueAsync(string uniqueID)
        {
            ResourceType map = await FindResourceInMapsAsync(uniqueID);
            if (map!= ResourceType.Undefined)
            {
                return resourceMaps[(int)map][uniqueID];
            }
            else
                throw new KeyNotFoundException($"Resource {uniqueID} not found.");
        }

        private async Task<bool> IsOrderAsync(string uniqueID)
        {
            return await Task.Run(() => resourceMaps[0].ContainsKey(uniqueID));
        }

        private async Task<bool> IsUnitAsync(string uniqueID)
        {
            return await Task.Run(() => resourceMaps[1].ContainsKey(uniqueID));
        }

        private async Task<bool> IsQueueAsync(string uniqueID)
        {
            return await Task.Run(() => resourceMaps[2].ContainsKey(uniqueID));
        }

        public void MapOrder(string uniqueID, ProductionOrder value)
        {
            MapResource(uniqueID, value, ResourceType.Order);
        }

        public void MapUnit(string uniqueID, ProductionUnit value)
        {
            MapResource(uniqueID, value, ResourceType.Unit);
        }

        public void MapQueue(string uniqueID, UnitQueue value)
        {
            MapResource(uniqueID, value, ResourceType.Queue);
        }

        public void MapResource(string uniqueResourceID, object value, ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Order:
                    resourceMaps[0].Add(uniqueResourceID, value);
                    break;
                case ResourceType.Unit:
                    resourceMaps[1].Add(uniqueResourceID, value);
                    break;
                case ResourceType.Queue:
                    resourceMaps[2].Add(uniqueResourceID, value);
                    break;
            }
        }

        public async Task<ResourceType> FindResourceInMapsAsync(string uniqueID)
        {
            var tasks = new[]
                {
                    (Id: 0, Job: IsOrderAsync(uniqueID)),
                    (Id: 1, Job: IsUnitAsync(uniqueID)),
                    (Id: 2, Job: IsQueueAsync(uniqueID))
                };

            while (tasks.Length > 0)
            {
                var finishedTuple = await Task.WhenAny(tasks.Select(t => t.Job));

                var tuple = tasks.First(t => t.Job == finishedTuple);
                bool result = await tuple.Job;

                if (result)
                {
                    return (ResourceType)tuple.Id;
                }

                tasks = tasks.Where(t => t.Job != finishedTuple).ToArray();
            }
            return ResourceType.Undefined;
        }

        public bool DropMappedResource(string uniqueID)
        {
            ResourceType map = FindResourceInMapsAsync(uniqueID).Result;
            if (map == ResourceType.Undefined)
                return false;
            try
            {
                resourceMaps[(int)map].Remove(uniqueID);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

