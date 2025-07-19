using System;
using ServerPMS.Infrastructure.External;

namespace ServerPMS.Abstractions.Managers
{
	public interface IOrdersManager
	{

		//properties
		OrdersBuffer OrdersBuffer { get; }
		ProductionOrder this[string id] { get;}

		//methods
		void UpdateOrderState(string runtimeID, OrderState newState);
		bool RemoveNotEOFOrder(Predicate<ProductionOrder> predicate);
		bool RemoveNotEOFOrder(string runtimeID);
		void LoadOrdersFromDB();
		void LoadFromExcelFile(string filename, ExcelOrderParserParams parserParams);

    }
}

