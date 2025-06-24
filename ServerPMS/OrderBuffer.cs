// PMS Project V1.0
// LSData - all rights reserved
// OrderBuffer.cs
//
//
using System;
using System.Collections;
using System.Data;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ServerPMS
{
    public class OrderBuffer:IEnumerable<ProductionOrder>
    {
        List<ProductionOrder> MainBuffer;

        public OrderBuffer()
        {
            MainBuffer = new List<ProductionOrder>();

        }

        public void SmartAdd(ProductionOrder order)
        {
            if (!IsInBuffer(order))
                MainBuffer.Add(order);
        }

        private bool IsInBuffer(ProductionOrder order)
        {
            if (MainBuffer.Find(x => x.Equals(order)) == null)
                return false;
            else
                return true;
        }

        public void SmartAdd(ProductionOrder[] orders)
        {
            foreach(ProductionOrder order in orders)
            {
                if (!IsInBuffer(order))
                    MainBuffer.Add(order);
            }
        }

        public bool Remove(Predicate<ProductionOrder> predicate)
        {
            return MainBuffer.RemoveAll(predicate)>0? true:false;
        }

        public void SmartAdd(List<ProductionOrder> orders)
        {
            SmartAdd(orders.ToArray());
        }

        public IEnumerator<ProductionOrder> GetEnumerator()
        {
            return MainBuffer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)MainBuffer).GetEnumerator();
        }
    }
}

