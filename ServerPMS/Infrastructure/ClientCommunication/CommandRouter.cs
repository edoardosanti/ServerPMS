using ServerPMS.Abstractions.Infrastructure.ClientCommunication;
using ServerPMS.Abstractions.Infrastructure.Concurrency;
using ServerPMS.Infrastructure.Concurrency;
using Microsoft.Extensions.Hosting;
using System.Text;
using Microsoft.Extensions.Logging;
using ServerPMS.Abstractions.Managers;

namespace ServerPMS.Infrastructure.ClientCommunication
{

    public struct BlockingOperationRequest<T> where T : Enum
    {
        public bool IsChild;
        public Guid? ParentReqID;
        public ClientHandler RequestingHandler;
        public Guid ReqID;
        public T Operation;
        public object Value;
        public TaskCompletionSource OpTask;

        public BlockingOperationRequest()
        {
            OpTask = new();
            ReqID = Guid.NewGuid();
        }
    }
    public class CommandRouter : BackgroundService, ICommandRouter
    {
        private readonly IConcurrencyManager concurrencyManager;
        private readonly IMessageFactory msgFactory;
        private readonly ILogger<CommandRouter> logger;
        private readonly IFeedManager feedManager;

        private CancellationTokenSource cts;

        private Queue<BlockingOperationRequest<OrdersBlockingOperations>> OrdersBOT;
        private Queue<BlockingOperationRequest<QueuesBlockingOperations>> QueuesBOT;
        private Queue<BlockingOperationRequest<UnitsBlockingOperations>> UnitsBOT;


        public CommandRouter(ILogger<CommandRouter> logger, IConcurrencyManager concurrencyManager, IFeedManager feedManager, IMessageFactory messageFactory)
        {
            msgFactory = messageFactory;
            this.concurrencyManager = concurrencyManager;
            this.logger = logger;
            this.feedManager = feedManager;
            OrdersBOT = new Queue<BlockingOperationRequest<OrdersBlockingOperations>>();
            QueuesBOT = new Queue<BlockingOperationRequest<QueuesBlockingOperations>>();
            UnitsBOT = new Queue<BlockingOperationRequest<UnitsBlockingOperations>>();
        }

        public async Task RouteRequestAsync(ClientHandler handler, Message msg)
        {
            string decodedPayload = Encoding.UTF8.GetString(msg.Payload);
            switch (msg.CID)
            {
                case (byte)CID.SET_DATA_ORDERS:
                    _ = EnqueueOrderBlockingOperationRequest(handler, decodedPayload);
                    break;
                case (byte)CID.SET_DATA_QUEUES:
                    _ = EnqueueQueueBlockingOperationRequest(handler, decodedPayload);
                    break;
                case (byte)CID.SET_DATA_UNITS:
                    _ = EnqueueUnitsBlockingOperationRequest(handler, decodedPayload);
                    break;
                case (byte)CID.GET_SNAPSHOT:
                    break;
                case (byte)CID.GET_DATA_SYSTEM:
                    break;
                case (byte)CID.SUBSCRIBE_FEED:
                    if (decodedPayload.Contains('O'))
                        _ = feedManager.SubscribeChannelOrdersAsync(handler.ID);
                    if (decodedPayload.Contains('U'))
                        _ = feedManager.SubscribeChannelUnitsAsync(handler.ID);
                    if (decodedPayload.Contains('Q'))
                        _ = feedManager.SubscribeChannelQueuesAsync(handler.ID);
                    break;
                case (byte)CID.UNSUBSCRIBE_FEED:
                    if (decodedPayload.Contains('O'))
                        _ = feedManager.UnsubscribeChannelOrdersAsync(handler.ID);
                    if (decodedPayload.Contains('U'))
                        _ = feedManager.UnsubscribeChannelUnitsAsync(handler.ID);
                    if (decodedPayload.Contains('Q'))
                        _ = feedManager.UnsubscribeChannelQueuesAsync(handler.ID);
                    break;

            }
        }

        public async Task RouteFeedNackAsync(ClientHandler handler, Message msg)
        {
            if (!msg.CID.Equals(CID.NACK))
                throw new InvalidOperationException("The message in not a NACK therefore can't be processed in this pipeline.");
            //todo: feed manager logic placeholder
        }

        public async Task RouteSystemAsync(ClientHandler handler, Message cmd)
        {
            switch (cmd.CID)
            {
                case (byte)CID.LOGIN:
                    Message idDeliver = msgFactory.NewMessage(CID.DATA_DELIVERY, new Guid(cmd.ID), Flags.None, handler.ID);
                    await handler.EnqueueSystemMessage(idDeliver);
                    break;
                //reserved for expansion
                case (byte)CID.SET_DATA_SYSTEM:
                    break;
                case (byte)CID.GET_DATA_SYSTEM:
                    break;

            }
        }

        #region Blocking Operations

        private Task EnqueueUnitsBlockingOperationRequest(ClientHandler handler, string request)
        {
            //sample command: {user}^us@{unit id}#{new status}
            //update status on unit {unit id} to {new status}

            UnitsBlockingOperations op;

            switch (request)
            {
                case string r when r.Split("^")[1].ElementAt(2) == '@':
                    try
                    {
                        string user = r.Split("^")[0];
                        r = r.Split("^")[1];

                        int atIndex = 2;
                        int hashIndex = r.IndexOf("#");

                        string cmd = r.Substring(0, atIndex);
                        string unitId = r.Substring(atIndex + 1, hashIndex - 1);
                        UnitState status = (UnitState)int.Parse(r.Substring(hashIndex + 1));

                        op = cmd switch
                        {
                            "us" => UnitsBlockingOperations.UpdateState,
                            "un" => UnitsBlockingOperations.UpdateNotes,
                            _ => throw new InvalidOperationException("Unknown op id.")
                        };

                        BlockingOperationRequest<UnitsBlockingOperations> bOp = new BlockingOperationRequest<UnitsBlockingOperations>()
                        {
                            Operation = op,
                            Value = new object[] { user, unitId, status },
                            RequestingHandler = handler
                        };


                        UnitsBOT.Enqueue(bOp);
                        return bOp.OpTask.Task;
                    }
                    catch
                    {
                        throw new InvalidOperationException("No arguments provided.");
                    }
            }
            return null;
        }

        private Task EnqueueQueueBlockingOperationRequest(ClientHandler handler, string request)
        {
            //sample command: {user}^mu@{queue id}#{order id}
            //move up order with id == id1 

            QueuesBlockingOperations op;

            switch (request)
            {
                case string r when r.Split("^")[1].ElementAt(2) == '@':
                    try
                    {
                        string user = r.Split("^")[0];
                        r = r.Split("^")[1];

                        int atIndex = 2;
                        int hashIndex = r.IndexOf("#");

                        string cmd = r.Substring(0, atIndex);
                        string queueId = r.Substring(atIndex + 1, hashIndex - 1);
                        string orderId = r.Substring(hashIndex + 1);

                        op = cmd switch
                        {
                            "mu" => QueuesBlockingOperations.MoveUp,
                            "md" => QueuesBlockingOperations.MoveDown,
                            "dl" => QueuesBlockingOperations.DequeueLast,
                            "rm" => QueuesBlockingOperations.Remove,
                            "en" => QueuesBlockingOperations.Enqueue,
                            _ => throw new InvalidOperationException("Unknown op id.")
                        };

                        BlockingOperationRequest<QueuesBlockingOperations> bOp = new BlockingOperationRequest<QueuesBlockingOperations>()
                        {
                            Operation = op,
                            Value = new string[] { user, queueId, orderId },
                            RequestingHandler = handler
                        };

                        QueuesBOT.Enqueue(bOp);
                        return bOp.OpTask.Task;
                    }
                    catch
                    {
                        throw new InvalidOperationException("No arguments provided.");
                    }
                default:
                    throw new InvalidOperationException("Invalid operation ID");
            }
        }

        private Task EnqueueOrderBlockingOperationRequest(ClientHandler handler, string request)
        {
            //sample command: {user}^u@{id}#cn!Nupi Industrie Italiane SpA
            //update customer name to Nupi..

            OrdersBlockingOperations op;


            switch (request)
            {
                case string r when r.Split("^")[1].StartsWith("u@"):
                    try
                    {

                        string[] tmp = r.Split("^");
                        string user = tmp[0];
                        r = tmp[1].Replace("u@", "");

                        string id, cmd, args;
                        int hashIndex = r.IndexOf('#');
                        int bangIndex = r.IndexOf('!', hashIndex + 1);

                        id = r.Substring(0, hashIndex);
                        cmd = r.Substring(hashIndex + 1, bangIndex - hashIndex - 1);
                        args = r.Substring(bangIndex + 1);

                        op = cmd switch
                        {
                            "cn" => OrdersBlockingOperations.UpdateCustomerName,
                            "pc" => OrdersBlockingOperations.UpdatePartCode,
                            "qt" => OrdersBlockingOperations.UpdateQty,
                            "ds" => OrdersBlockingOperations.UpdateDescription,
                            "mi" => OrdersBlockingOperations.UpdateMoldID,
                            "mp" => OrdersBlockingOperations.UpdateMoldPosition,
                            "dd" => OrdersBlockingOperations.UpdateDeliveryDate,
                            "df" => OrdersBlockingOperations.UpdateDeliveryFacility,
                            "nt" => OrdersBlockingOperations.UpdateNotes,
                            _ => throw new InvalidOperationException("Update target not found.")
                        };

                        if (op.Equals(OrdersBlockingOperations.UpdateQty))
                        {
                            int.TryParse(args, out int val);
                            args = val.ToString();
                        }

                        BlockingOperationRequest<OrdersBlockingOperations> bOp = new BlockingOperationRequest<OrdersBlockingOperations>()
                        {
                            Operation = op,
                            Value = new string[] { user, id, args },
                            RequestingHandler = handler

                        };

                        OrdersBOT.Enqueue(bOp);
                        return bOp.OpTask.Task;
                    }
                    catch
                    {
                        throw new InvalidOperationException("No arguments provided.");
                    }

                case string r when r.Split("^")[1].StartsWith("i!"):

                    //sample command {user}^i!order1|order2|order3

                    string[] tmp2 = r.Split("^");
                    string user2 = tmp2[0];
                    r = tmp2[1].Replace("i!", "");


                    List<string> dumps = new List<string>();
                    dumps.Add(user2);
                    dumps.Concat(r.Split("|"));
                   
                    if (dumps.Count == 1)
                        throw new InvalidOperationException("No orders found.");

                    BlockingOperationRequest<OrdersBlockingOperations> bOp2 = new BlockingOperationRequest<OrdersBlockingOperations>()
                    {
                        Operation = OrdersBlockingOperations.Import,
                        Value = dumps.ToArray()
                    };

                    OrdersBOT.Enqueue(bOp2);
                    return bOp2.OpTask.Task;

                default:
                    throw new InvalidOperationException("Invalid operation ID");
            }
        }

        private async Task QueuesBlockingLoopAsync()  //Access resource and make changes 
        {
            while (!cts.IsCancellationRequested) {

                await Task.Yield();

                if (QueuesBOT.Count > 0)
                {
                    //checks if the resource is used from someone else and proceed only if avaible

                    BlockingOperationRequest<QueuesBlockingOperations> request = QueuesBOT.Peek();
                    string userID = (request.Value as string[])?[0]; //parse user id
                    string queueID = (request.Value as string[])?[1]; //parse queue id 
                    string orderID = (request.Value as string[])?[2]; //parse order id

                    AccessToken at = await concurrencyManager.AccessIEMAsync(request.RequestingHandler);

                    if (at.AccessMode.Equals(AccessMode.ReadWrite))
                    {

                        request = QueuesBOT.Dequeue();
                        IIntegratedEventsManager iem = at.Resource as IIntegratedEventsManager;
                        switch (request.Operation)
                        {
                            case QueuesBlockingOperations.MoveUp:
                                iem.MoveUpInQueue(queueID, orderID, 1);
                                break;
                            case QueuesBlockingOperations.MoveDown:
                                iem.MoveDownInQueue(queueID, orderID, 1);
                                break;
                            case QueuesBlockingOperations.Remove:
                                iem.RemoveFromQueueNotEOF(queueID, orderID);
                                break;
                            case QueuesBlockingOperations.DequeueLast:
                                iem.DequeueAndComplete(queueID);
                                break;
                            case QueuesBlockingOperations.Enqueue:
                                iem.Enqueue(queueID, orderID);
                                break;
                        }

                        concurrencyManager.ReleaseResource(at);
                    }
                }
                else
                    await Task.Delay(1000);
            }
        }

        private async Task UnitsBlockingLoopAsync()  //Access resource and make changes 
        {
            while (!cts.IsCancellationRequested) {

                await Task.Yield();

                if (UnitsBOT.Count > 0)
                {
                    //checks if the resource is used from someone else and proceed only if avaible

                    BlockingOperationRequest<UnitsBlockingOperations> request = UnitsBOT.Peek();
                    string userID = (request.Value as string[])?[0];  //parse user id
                    string unitID = (request.Value as string[])?[2]; //parse unit id 
                    string param = (request.Value as string[])?[3]; //parse param 

                    AccessToken at = await concurrencyManager.AccessResourceAsync(request.RequestingHandler, unitID);

                    if (at.AccessMode.Equals(AccessMode.ReadWrite))
                    {

                        request = UnitsBOT.Dequeue();
                        ProductionUnit unit = at.Resource as ProductionUnit;
                        switch (request.Operation)
                        {
                            case UnitsBlockingOperations.UpdateState:
                                switch (param)
                                {
                                    case "0":
                                        unit.Stop(userID);
                                        break;
                                    case "1":
                                        unit.Start(userID);
                                        break;
                                    case "2":
                                        unit.ChangeOver(userID);
                                        break;
                                }
                                break;
                            case UnitsBlockingOperations.UpdateNotes:
                                unit.UpdateNotes(param);
                                break;
                        }

                        concurrencyManager.ReleaseResource(at);
                    }
                    else
                        await Task.Delay(1000);
                }
            }
        }

        private async Task OrdersBlockingLoopAsync()  //Access resource and make changes 
        {
            while (!cts.IsCancellationRequested)
            {
                await Task.Yield();

                if (OrdersBOT.Count > 0)
                {
                    //checks if the resource is used from someone else and proceed only if avaible

                    BlockingOperationRequest<OrdersBlockingOperations> request = OrdersBOT.Peek();
                    AccessToken at;
                    string[] dumps = request.Value as string[];
                   
                    switch (request.Operation)
                    {
                        case OrdersBlockingOperations.Import:
                            string[] importsID = dumps.Skip(0).ToArray();
                            at = await concurrencyManager.AccessOrdersManagerAsync(request.RequestingHandler);
                            IOrdersManager manager = at.Resource as IOrdersManager;
                            request = OrdersBOT.Dequeue();

                            List<ProductionOrder> imports = new List<ProductionOrder>();
                            foreach (string id in importsID)
                            {
                                imports.Add(ProductionOrder.FromDump(id));
                            }
                            manager.Import(imports);

                            break;

                        default:
                            //throw new InvalidDataException(); -> non rilevata
                            string user = dumps[0];
                            at = await concurrencyManager.AccessResourceAsync(request.RequestingHandler, dumps[1]);  
                            if (at.AccessMode.Equals(AccessMode.ReadWrite))
                            {
                                request = OrdersBOT.Dequeue();
                                ProductionOrder order = at.Resource as ProductionOrder; //get ptr to resource
                                switch (request.Operation)
                                {
                                    case OrdersBlockingOperations.Import:
                                        break;
                                    default:
                                        string param = string.Empty;
                                        if (dumps.Length == 3) //mezzo inutile (probabile residuo di v1)
                                            param = dumps[2];

                                        switch (request.Operation)
                                        {
                                            case OrdersBlockingOperations.UpdatePartCode:
                                                order.UpdatePartCode(param, user);
                                                break;
                                            case OrdersBlockingOperations.UpdateDescription:
                                                order.UpdatePartDescription(param,user);
                                                break;
                                            case OrdersBlockingOperations.UpdateQty:
                                                order.UpdateQty(int.Parse(param),user);
                                                break;
                                            case OrdersBlockingOperations.UpdateMoldID:
                                                order.UpdateMoldID(param,user);
                                                break;
                                            case OrdersBlockingOperations.UpdateMoldPosition:
                                                order.UpdateMoldLocation(param,user);
                                                break;
                                            case OrdersBlockingOperations.UpdateDeliveryFacility:
                                                order.UpdateDeliveryFacility(param,user);
                                                break;
                                            case OrdersBlockingOperations.UpdateDeliveryDate:
                                                order.UpdateDeliveryDate(param,user);
                                                break;
                                            case OrdersBlockingOperations.UpdateNotes:
                                                order.UpdateMoldNotes(param,user);
                                                break;
                                            case OrdersBlockingOperations.UpdateCustomerName:
                                                order.UpdateCustomerName(param,user);
                                                break;
                                        }
                                        break;
                                }
                            }

                            break;
                    }
                    concurrencyManager.ReleaseResource(at); //checkpoint
                }
                else
                    await Task.Delay(1000);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            logger.LogInformation("Starting command router service.");

            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            //start long running tasks
            Task OrdersLoop = Task.Factory.StartNew(
                OrdersBlockingLoopAsync,
                cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Task UnitsLoop = Task.Factory.StartNew(
                UnitsBlockingLoopAsync,
                cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Task QueuesLoop = Task.Factory.StartNew(QueuesBlockingLoopAsync,
                cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            logger.LogInformation("Command router service started.");

            await Task.WhenAll(OrdersLoop, UnitsLoop, QueuesLoop);
        }

        #endregion
    }
}