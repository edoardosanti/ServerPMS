using System;
namespace ServerPMS.Abstractions.Infrastructure.External
{
	public interface IExcelOrderParser
	{
		List<ProductionOrder> ParseOrders();

	}
}

