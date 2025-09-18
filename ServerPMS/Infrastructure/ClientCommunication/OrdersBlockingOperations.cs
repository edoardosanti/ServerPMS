using System;
namespace ServerPMS.Infrastructure.ClientCommunication
{
	public enum OrdersBlockingOperations
	{
		UpdateCustomerName,
		UpdatePartCode,
		UpdateQty,
		UpdateDescription,
		UpdateMoldID,
		UpdateMoldPosition,
		UpdateDeliveryDate,
		UpdateDeliveryFacility,
		UpdateNotes,
		Import
	}
}

