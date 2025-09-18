using System;
using ServerPMS.Infrastructure.Concurrency;
namespace ServerPMS.Abstractions.Infrastructure.Concurrency
{
    public interface IResourceMapper
    {
        // Mapping
        void MapOrder(string uniqueID, ProductionOrder value);
        void MapUnit(string uniqueID, ProductionUnit value);
        void MapQueue(string uniqueID, UnitQueue value);
        void MapResource(string uniqueResourceID, object value, ResourceType type);

        // Lookup
        Task<ResourceType> FindResourceInMapsAsync(string uniqueID);
        Task<object> GetValueAsync(string uniqueID);
        // Drop
        bool DropMappedResource(string uniqueID);
    }
}

