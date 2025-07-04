// PMS Project V1.0
// LSData - all rights reserved
// OrderManager.cs
//
//


namespace ServerPMS
{

    public class OrdersBuffer : SmartBuffer<ProductionOrder>
    {
        public OrdersBuffer() : base() { }

        public ProductionOrder this[string runtimeID] { get => Find(x => x.RuntimeID == runtimeID); }

    public override bool SmartAdd(ProductionOrder item)
        {
            GlobalIDsManager.AddOrderEntry(item.RuntimeID, item.DBId);
            return base.SmartAdd(item);
        }

        public override bool Remove(Predicate<ProductionOrder> predicate)
        {
            GlobalIDsManager.RemoveOrderEntry(base.Find(predicate).RuntimeID);
            return base.Remove(predicate);
        }

        public bool Remove(string runtimeID)
        {
            GlobalIDsManager.RemoveOrderEntry(runtimeID);
            return base.Remove(x=>x.RuntimeID==runtimeID);
        }


    }
}

