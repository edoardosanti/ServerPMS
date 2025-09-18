// PMS Project V2.0
// LSData - all rights reserved
// OrdersBuffer.cs
//
//

using ServerPMS.Infrastructure.Generic;
using ServerPMS.Abstractions.Infrastructure.Concurrency;
using ServerPMS.Abstractions.Infrastructure.Database;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace ServerPMS.Core;


public class OrdersBuffer : SmartBuffer<ProductionOrder>
{
    private readonly IGlobalIDsManager GlobalIDsManager;
    private readonly IResourceMapper Mapper;

    public OrdersBuffer(IGlobalIDsManager globalIDManager, IResourceMapper mapper) : base()
    {
        Mapper = mapper; //map resource for concurrency handling
        GlobalIDsManager = globalIDManager;
    }

    public ProductionOrder this[string runtimeID] { get => Find(x => x.RuntimeID == runtimeID); }

    public override bool SmartAdd(ProductionOrder item)
    {
        Mapper.MapOrder(item.RuntimeID,item);
        GlobalIDsManager.AddOrderEntry(item.RuntimeID, item.DBId);

        return base.SmartAdd(item);
    }

    public override bool Remove(Predicate<ProductionOrder> predicate)
    {
        string orderId = base.Find(predicate).RuntimeID;
        Mapper.DropMappedResource(orderId);
        GlobalIDsManager.RemoveOrderEntry(orderId);
        return base.Remove(predicate);
    }

    public bool Remove(string runtimeID)
    {
        Mapper.DropMappedResource(runtimeID);
        GlobalIDsManager.RemoveOrderEntry(runtimeID);
        return base.Remove(x=>x.RuntimeID==runtimeID);
    }


}

