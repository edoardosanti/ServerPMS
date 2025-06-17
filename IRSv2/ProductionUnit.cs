// PMS Project V1.0
// LSData - all rights reserved
// ProductionUnit.cs
//
//

namespace IRSv2
{
    public class ProductionUnit
    {
        static int code;
        static ProductionUnit()
        {
            code = 0;
        }

        ReorderableQueue<ProductionOrder> Queue;

        State status;
        public State UnitStatus
        {
            private set
            {
                try
                {
                    status = value;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            get { return status; }

        }

        UnitType type;
        public UnitType UnitType
        {
            private set
            {
                try
                {
                    type = value;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            get { return type; }

        }
        public int ID { private set; get; }
        public string UnitNotes { private set; get; }
        public ReorderableQueue<ProductionOrder> ProdOrdersQueue => Queue;

        public ProductionUnit(UnitType type, string notes, params ProductionOrder[] orders)
        {
            ID = ++code;
            UnitNotes = notes;
            UnitStatus = State.Standby;
            UnitType = type;

            Queue = new ReorderableQueue<ProductionOrder>(orders);
            CurrentProduction = null;
            NextInQueue = Queue.Next;

        }

        public ProductionOrder CurrentProduction;
        public ProductionOrder NextInQueue;

        public void Start()
        {
            CurrentProduction = Queue.GetNextAndDequeue();
            UnitStatus = State.Running;
        }

        public void Stop()
        {
            CurrentProduction = null;
            UnitStatus = State.Standby;
        }

        public void ChangeOver()
        {
            CurrentProduction = null;
            UnitStatus = State.ChangeOver;

        }

        public string ToInfo()
        {
            return string.Format("ID: {0} Notes: {1} Unit Type: {2}\nStatus: {3} \n-------\nCurrent Production:\n{4}\n-------\nQueue:\n{5}", ID, UnitNotes, UnitType, UnitStatus, (CurrentProduction==null)?"None":CurrentProduction.ToShortInfo(),Queue.StrDump());
        }
    }
}

