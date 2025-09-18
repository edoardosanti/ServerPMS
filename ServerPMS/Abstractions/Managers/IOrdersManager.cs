using System;
using ServerPMS.Infrastructure.External;
using ServerPMS.Core;

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
		Task LoadOrdersFromDBAsync();
		void Import(IEnumerable<ProductionOrder> orders);
        Task LoadFromExcelFileAsync(string filename, ExcelOrderParserParams parserParams);

    }
}

