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
    public class CommandRouter :BackgroundService, ICommandRouter
    {
        private readonly IConcurrencyManager concurrencyManager;
        private readonly IMessageFactory msgFactory;
        private readonly ILogger<CommandRouter> logger;

        private CancellationTokenSource cts;

        private Queue<BlockingOperationRequest<OrdersBlockingOperations>> OrdersBOT;
        private Queue<BlockingOperationRequest<QueuesBlockingOperations>> QueuesBOT;
        private Queue<BlockingOperationRequest<UnitsBlockingOperations>> UnitsBOT;


        public CommandRouter(ILogger<CommandRouter> logger, IConcurrencyManager concurrencyManager, IMessageFactory messageFactory)
        {
            msgFactory = messageFactory;
            this.concurrencyManager = concurrencyManager;
            this.logger = logger;
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
                    _ = EnqueueUnitsBlockingOperationRequest(handler,decodedPayload);
                    break;

                    //aggiungere gestione op non bloccanti
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

        private Task EnqueueUnitsBlockingOperationRequest(ClientHandler handler, string request)
        {
            //sample command: us@{unit id}#{new status}
            //update status on unit {unit id} to {new status}

            UnitsBlockingOperations op;

            switch (request)
            {
                case string r when r.ElementAt(2) == '@':
                    try
                    {
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
                            Value = new object[] { unitId, status },
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
            //sample command: mu@{queue id}#{order id}
            //move up order with id == id1 

            QueuesBlockingOperations op;

            switch (request)
            {
                case string r when r.ElementAt(2) == '@':

                    try
                    {
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
                            Value = new string[] { queueId, orderId },
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
            //sample command: u@{id}#cn!Nupi Industrie Italiane SpA
            //update customer name to Nupi..

            OrdersBlockingOperations op;

            switch (request)
            {
                case string r when r.StartsWith("u@"):
                    r = r.Replace("u@", "");
                    try
                    {

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
                            Value = new string[] { id, args },
                            RequestingHandler = handler
                            
                        };

                        OrdersBOT.Enqueue(bOp);
                        return bOp.OpTask.Task;
                    }
                    catch
                    {
                        throw new InvalidOperationException("No arguments provided.");
                    }

                case string r when r.StartsWith("a!"):

                    //sample command i!order1|order2|order3

                    r = r.Replace("i!", "");
                    string[] dumps = r.Split("|");
                    if (dumps.Length == 0)
                        throw new InvalidOperationException("No orders found.");

                    BlockingOperationRequest<OrdersBlockingOperations> bOp2 = new BlockingOperationRequest<OrdersBlockingOperations>()
                    {
                        Operation = OrdersBlockingOperations.Import,
                        Value = dumps
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
                    string queueID = (request.Value as string[])?[0]; //parse queue id 
                    string orderID = (request.Value as string[])?[1]; //parse order id

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
                    string unitID = (request.Value as string[])?[0]; //parse unit id 
                    string param = (request.Value as string[])?[1]; //parse param 

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
                                        unit.Stop();
                                        break;
                                    case "1":
                                        unit.Start();
                                        break;
                                    case "2":
                                        unit.ChangeOver();
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

                if(OrdersBOT.Count > 0)
                {
                    //checks if the resource is used from someone else and proceed only if avaible

                    BlockingOperationRequest<OrdersBlockingOperations> request = OrdersBOT.Peek();
                    AccessToken at;
                    string[] dumps = request.Value as string[];  

                    switch (request.Operation)
                    {
                        case OrdersBlockingOperations.Import:
                            at = await concurrencyManager.AccessOrdersManagerAsync(request.RequestingHandler);
                            request = OrdersBOT.Dequeue();

                            IOrdersManager manager = at.Resource as IOrdersManager;

                            List<ProductionOrder> imports = new List<ProductionOrder>();
                            foreach (string dump in dumps)
                            {
                                imports.Add(ProductionOrder.FromDump(dump));
                            }
                            manager.Import(imports);

                            break;

                        default:
                            //throw new InvalidDataException(); -> non rilevata 
                            at = await concurrencyManager.AccessResourceAsync(request.RequestingHandler, dumps[0]);  //FIXME: probabile eccezione non gestita
                            if (at.AccessMode.Equals(AccessMode.ReadWrite))
                            {
                                ProductionOrder order = at.Resource as ProductionOrder; //get ptr to resource
                                switch (request.Operation)
                                {
                                    case OrdersBlockingOperations.Import:
                                        break;
                                    default:
                                        string param = string.Empty;
                                        if (dumps.Length == 2) //mezzo inutile (probabile residuo di v1)
                                            param = dumps[1];

                                        switch (request.Operation)
                                        {
                                            case OrdersBlockingOperations.UpdatePartCode:
                                                order.UpdatePartCode(param);
                                                break;
                                            case OrdersBlockingOperations.UpdateDescription:
                                                order.UpdatePartDescription(param);
                                                break;
                                            case OrdersBlockingOperations.UpdateQty:
                                                order.UpdateQty(int.Parse(param));
                                                break;
                                            case OrdersBlockingOperations.UpdateMoldID:
                                                order.UpdateMoldID(param);
                                                break;
                                            case OrdersBlockingOperations.UpdateMoldPosition:
                                                order.UpdateMoldLocation(param);
                                                break;
                                            case OrdersBlockingOperations.UpdateDeliveryFacility:
                                                order.UpdateDeliveryFacility(param);
                                                break;
                                            case OrdersBlockingOperations.UpdateDeliveryDate:
                                                order.UpdateDeliveryDate(param);
                                                break;
                                            case OrdersBlockingOperations.UpdateNotes:
                                                order.UpdateMoldNotes(param);
                                                break;
                                            case OrdersBlockingOperations.UpdateCustomerName:
                                                order.UpdateCustomerName(param);
                                                break;
                                        }
                                        break;
                                }
                            }

                            break;
                    }
                    concurrencyManager.ReleaseResource(at);
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
                async () =>
                {
                    try
                    {
                        await OrdersBlockingLoopAsync();
                    }
                    catch { throw; }
                },
                cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            Task UnitsLoop = Task.Factory.StartNew(UnitsBlockingLoopAsync,
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
    }
}