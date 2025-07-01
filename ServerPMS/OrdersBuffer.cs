// PMS Project V1.0
// LSData - all rights reserved
// OrderManager.cs
//
//


namespace ServerPMS
{
    public class OrderAddedArgs : EventArgs
    {
        public ProductionOrder order;
        public int index;
    }

    public class OrdersBuffer : SmartBuffer<ProductionOrder>
    {
        public OrdersBuffer() : base() { }

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

