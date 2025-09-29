using System;
using ServerPMS.Infrastructure.ClientCommunication;

namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface IFeedManager
	{
        Task SubscribeChannelOrdersAsync(string id);

        Task SubscribeChannelUnitsAsync(string id);

        Task SubscribeChannelQueuesAsync(string id);

        Task UnsubscribeChannelOrdersAsync(string id);

        Task UnsubscribeChannelUnitsAsync(string id);

        Task UnsubscribeChannelQueuesAsync(string id);

        Task SubscribeAsync(string id, ChannelSubscriptions sub);

        Task UnsubscribeAsync(string id, ChannelSubscriptions sub);

        Task<OrdersReport> GenerateOrdersFullReportAsync();
        Task<UnitsReport> GenerateUnitsFullReportAsync();
        Task<QueuesReport> GenerateQueuesFullReportAsync();

        ChannelDescriptor AddChannelAsync(IClientHandler client, IConnectionHandler handler, params bool[] subcriptions);
    }
}

