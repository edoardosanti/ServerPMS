using System.Collections.Concurrent;
using System.Net.Security;
using Microsoft.Extensions.Logging;
using ServerPMS.Managers;
using ServerPMS.Abstractions.Infrastructure.Concurrency;
using ServerPMS.Infrastructure.Concurrency;
using ServerPMS.Abstractions.Managers;
using System.Timers;
using ServerPMS.Abstractions.Infrastructure.ClientCommunication;

namespace ServerPMS.Infrastructure.ClientCommunication
{
	public enum ChannelSubscriptions
	{
		Orders,
		Units,
		Queues,
		Systems
	}

	public class ChannelDescriptor
	{
        public IClientHandler Client { private set; get; }
		public IConnectionHandler Channel { private set; get; }
		private bool[] subs = {false,false,false,false};

		public bool IsSubscribedOrders => subs[0];
        public bool IsSubscribedUnits => subs[1];
        public bool IsSubscribedQueues => subs[2];
        public bool IsSubscribedSystem => subs[3];

        public ChannelDescriptor(IClientHandler client, IConnectionHandler channel, bool[]? subs)
		{
			if (subs != null&&subs?.Length==4)
				this.subs = subs;
			Client = client;
			Channel = channel;
		}

		public void Subscribe(ChannelSubscriptions subscription)
		{
			int id = (int)subscription;
			subs[id] = true;
		}

        public void Unsubscribe(ChannelSubscriptions subscription)
        {
            int id = (int)subscription;
            subs[id] = false;
        }
    }

	public class FeedManager : IFeedManager
	{
		private DateTime _lastCycleTimestamp;
		public DateTime LastCycleTimestamp => _lastCycleTimestamp;

		private readonly IMessageFactory messageFactory;
		private readonly ILogger<FeedManager> logger;
		private readonly IConcurrencyManager concurrencyManager;

		private int cycleCount;
		public int CycleCount => cycleCount;

		private System.Timers.Timer dispatchCycleTimer;
        private System.Timers.Timer gatherCycleTimer;

		private Dictionary<string, ChannelDescriptor> channels;
		private SemaphoreSlim channelsSemaphore;

        private Report[] reports;

        public FeedManager(ILogger<FeedManager> logger, IConcurrencyManager concurrencyManager, IMessageFactory messageFactory)
		{
			this.messageFactory = messageFactory;
			this.logger = logger;
			this.concurrencyManager = concurrencyManager;

            reports = new Report[3];

			cycleCount = 0;
            dispatchCycleTimer = new System.Timers.Timer(5000);
            dispatchCycleTimer.Elapsed += (object s, ElapsedEventArgs e)=>_ = DispatchReportsAsync();
            dispatchCycleTimer.Start();

            gatherCycleTimer = new System.Timers.Timer(2500);
            gatherCycleTimer.Elapsed += async (object s, ElapsedEventArgs e) =>
            {
                reports[0] = await GenerateOrdersFullReportAsync();
                reports[1] = await GenerateUnitsFullReportAsync();
                reports[2] = await GenerateQueuesFullReportAsync();
            };

            channelsSemaphore = new SemaphoreSlim(1);
			channels = new Dictionary<string, ChannelDescriptor>();

			logger.LogInformation($"Feed Manager Started...");
		}
		public void SetInterval(int ms)
		{
            dispatchCycleTimer.Interval = ms;
		}
		public ChannelDescriptor AddChannelAsync(IClientHandler client, IConnectionHandler handler, params bool[] subcriptions)
		{
			ChannelDescriptor channel = new ChannelDescriptor(client, handler, subcriptions);
			channels.TryAdd(client.ID, channel);

            logger.LogInformation($"Channel mapped (clientID: {client.ID}");

            return channel;
		}
		public async Task SubscribeChannelOrdersAsync(string id)
		{
			await SubscribeAsync(id, ChannelSubscriptions.Orders);
		}
        public async Task SubscribeChannelUnitsAsync(string id)
        {
            await SubscribeAsync(id, ChannelSubscriptions.Units);
        }
        public async Task SubscribeChannelQueuesAsync(string id)
        {
            await SubscribeAsync(id, ChannelSubscriptions.Queues);
        }
		public async Task UnsubscribeChannelOrdersAsync(string id)
		{
            await UnsubscribeAsync(id, ChannelSubscriptions.Orders);
        }
        public async Task UnsubscribeChannelUnitsAsync(string id)
        {
            await UnsubscribeAsync(id, ChannelSubscriptions.Units);
        }
        public async Task UnsubscribeChannelQueuesAsync(string id)
        {
            await UnsubscribeAsync(id, ChannelSubscriptions.Queues);
        }
        public async Task SubscribeAsync(string id, ChannelSubscriptions subscription)
		{
			try
			{
				await channelsSemaphore.WaitAsync();
				channels[id].Subscribe(subscription);
			}
			finally
			{
				channelsSemaphore.Release();
			}
		}
        public async Task UnsubscribeAsync(string id, ChannelSubscriptions subscription)
        {
            try
            {
                await channelsSemaphore.WaitAsync();
                channels[id].Unsubscribe(subscription);
            }
            finally
            {
                channelsSemaphore.Release();
            }
        }


        //TODO: ottimizza tutti i GenerateXXXFullReportAsync() (crea GenerateXXXEditsReportAsync()-> solo modifiche rispetto ultimo full report)
        public async Task<OrdersReport> GenerateOrdersFullReportAsync()
        {
            AccessToken token = await concurrencyManager.AccessOrdersManagerAsync("internal");
			OrdersReport report;
            List<AccessToken> actives = concurrencyManager.ActiveTokens.FindAll(x=>x.Resource is ProductionOrder);
			if(actives.Count==0)
                report = new OrdersReport(token.Resource as IOrdersManager, true, null);
			else
                report = new OrdersReport(token.Resource as IOrdersManager, false, actives);


			string log = $"[{report.Timestamp.ToShortTimeString()}] Orders report generated.";
            logger.LogInformation(log);

			_= File.WriteAllTextAsync("./data/dumpOrders.txt", log + $"\n{report.ToString()}");

            return report;

        }

        public async Task<UnitsReport> GenerateUnitsFullReportAsync()
        {
            AccessToken token = await concurrencyManager.AccessUnitsManagerAsync("internal");
            UnitsReport report;
            List<AccessToken> actives = concurrencyManager.ActiveTokens.FindAll(x => x.Resource is ProductionUnit);
            if (actives.Count == 0)
                report = new UnitsReport(token.Resource as IUnitsManager, true, null);
            else
                report = new UnitsReport(token.Resource as IUnitsManager, false, actives);            

            string log = $"[{report.Timestamp.ToShortTimeString()}] Units report generated.";
            logger.LogInformation(log);

			_ = File.WriteAllTextAsync("./data/dumpUnits.txt", log + $"\n{report.ToString()}");

            return report;

        }

        public async Task<QueuesReport> GenerateQueuesFullReportAsync()
        {
            AccessToken token = await concurrencyManager.AccessQueuesManagerAsync("internal");
            QueuesReport report;
            List<AccessToken> actives = concurrencyManager.ActiveTokens.FindAll(x => x.Resource is UnitQueue);
            if (actives.Count == 0)
                report = new QueuesReport(token.Resource as IQueuesManager, true, null);
            else
                report = new QueuesReport(token.Resource as IQueuesManager, false, actives);

            string log = $"[{report.Timestamp.ToShortTimeString()}] Queues report generated.";
            logger.LogInformation(log);

            _ = File.WriteAllTextAsync("./data/dumpQueues.txt", log+$"\n{report.ToString()}");

            return report;

        }

        private async Task DispatchReportsAsync()
        {

			cycleCount++;
			Report orders, units, queues;



			//get infos from managers


            try
            {
                await channelsSemaphore.WaitAsync();

                //enqueue report for sending
                await Parallel.ForEachAsync(channels, async (pair, ctk) =>
				{
					ChannelDescriptor cdt = pair.Value;
					if (cdt.IsSubscribedOrders)
						_ = cdt.Channel.EnqueueMessageForSending(messageFactory.NewMessage(CID.DATA_DELIVERY, Flags.None, $"TEST FEED MANAGER ORDERS {cycleCount}"));

					if (cdt.IsSubscribedUnits)
						_ = cdt.Channel.EnqueueMessageForSending(messageFactory.NewMessage(CID.DATA_DELIVERY, Flags.None, $"TEST FEED MANAGER UNITS {cycleCount}"));

					if (cdt.IsSubscribedQueues)
						_ = cdt.Channel.EnqueueMessageForSending(messageFactory.NewMessage(CID.DATA_DELIVERY, Flags.None, $"TEST FEED MANAGER QUEUES{cycleCount}"));
				});
				_lastCycleTimestamp = DateTime.Now;
			}
			finally
			{
				channelsSemaphore.Release();
			}

        }
    }
}

