// PMS Project V1.0
// LSData - all rights reserved
// ProductionUnit.cs
//
//

using System.Collections;

namespace ServerPMS
{
    public class ProductionUnit:IDBIdentifiable
    {

        public int DBId { set; get; }
        public event EventHandler UnitStartHandler;
        public event EventHandler UnitStopHandler;
        public event EventHandler UnitChangeOverHandler;


        protected void OnUnitStop(EventArgs args)
        {
            if (UnitStopHandler != null)
            {
                UnitStopHandler(this,args);
            }
        }

        protected void OnUnitStart(EventArgs args)
        {
            if (UnitStartHandler != null)
            {
                UnitStartHandler(this, args);
            }
        }

        protected void OnUnitChangeOver(EventArgs args)
        {
            if (UnitChangeOverHandler != null)
            {
                UnitChangeOverHandler(this, args);
            }
        }


        UnitState status;
        public UnitState UnitStatus
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
        public string UnitNotes { private set; get; }

        public ProductionUnit(int dbId,UnitType type, string notes, params int[] orders)
        {
            DBId = dbId;
            UnitNotes = notes;
            UnitStatus = UnitState.Standby;
            UnitType = type;

           
            CurrentProductionID = -1;
            NextInQueue = -1; //just to not get error

        }

        public int CurrentProductionID;
        public int NextInQueue;

        //public void Enqueue(int id)
        //{
        //    Queue.Enqueue(id);
        //}

        //public bool Dequeue(int id)
        //{
        //    return Queue.Dequeue(id);
        //}

        //public void MoveDown(Predicate<int> predicate, int jmps)
        //{
        //    if (jmps < 0)
        //        throw new ArgumentOutOfRangeException("Jumps in queue can be only values from zero.");
        //    for (int i = 0; i < jmps; i++)
        //    {
        //        Queue.MoveDown(predicate);
        //    }
        //}

        //public void MoveUp(Predicate<int> predicate, int jmps)
        //{
        //    if (jmps < 0)
        //        throw new ArgumentOutOfRangeException("Jumps in queue can be only values from zero.");
        //    for (int i = 0; i < jmps; i++)
        //    {
        //        Queue.MoveUp(predicate);
        //    }
        //}

        public void Start()
        {
            //CurrentProductionID = Queue.GetNextAndDequeue();
            UnitStatus = UnitState.Running;
            OnUnitStart(EventArgs.Empty);
        }

        public void Stop()
        {
            CurrentProductionID = -1;
            UnitStatus = UnitState.Standby;
            OnUnitStop(EventArgs.Empty);
        }

        public void ChangeOver()
        {
            CurrentProductionID = -1;
            UnitStatus = UnitState.ChangeOver;
            OnUnitChangeOver(EventArgs.Empty);

        }

        public string ToInfo()
        {
            return string.Format("DBID: {0} Notes: {1} Unit Type: {2}\nStatus: {3} \n-------\nCurrent Production:\n{4}\n-------\nQueue:\n{5}", DBId, UnitNotes, UnitType, UnitStatus, (CurrentProductionID == -1) ? "None" : CurrentProductionID.ToString(),"");
        }
    }
}

