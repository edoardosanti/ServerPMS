using System;
namespace ServerPMS.Abstractions.Managers
{
	public interface IOrdersBuffer
    {

        bool SmartAdd(ProductionOrder item);
        bool Remove(Predicate<ProductionOrder> predicate);
        bool Remove(string runtimeID);
        
    }
}

