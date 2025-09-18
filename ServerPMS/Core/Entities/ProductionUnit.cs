// PMS Project V1.0
// LSData - all rights reserved
// ProductionUnit.cs
//
//

using System.Collections;
using ServerPMS.Infrastructure.Database;
using ServerPMS.Infrastructure.Generic;
namespace ServerPMS
{
    public class ProductionUnit:IDBIdentifiable
    {

        public int DBId { set; get; }
        public event EventHandler UnitStartHandler;
        public event EventHandler UnitStopHandler;
        public event EventHandler UnitChangeOverHandler;
        public event EventHandler<string> UnitNotesUpdateHandler;


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

        protected void OnNotesUpdate(string notes)
        {
            UnitNotesUpdateHandler?.Invoke(this, notes);
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
        public string Notes { set; get; }
        public string Name { private set; get; }

        //binded queue logic
        private UnitQueue? queue;
        public string? QueueID
        {
            get
            {
                if (queue != null)
                    return queue.Id;
                else
                    return null;
            }
        }

        public ProductionUnit(int dbId, string name, UnitType type, string notes, UnitQueue? queue = null) //la produzione corrente viene SEMPRE rimossa dalla coda all'avvio della suddeta
        {
            DBId = dbId;
            Notes = notes;
            UnitStatus = UnitState.Standby;
            UnitType = type;
            Name = name;
            this.queue = queue;

        }

        public void UpdateNotes(string notes)
        {

            Notes = notes;
            
        }

        public void BindQueue(UnitQueue queue)
        {
            this.queue = queue;
        }

        //todo: check production lifetime cycle from unit control panel

        public void Start()
        {
            UnitStatus = UnitState.Running;
            OnUnitStart(EventArgs.Empty);
        }

        public void Stop()
        {
            UnitStatus = UnitState.Standby;
            OnUnitStop(EventArgs.Empty);
        }

        public void ChangeOver()
        {
            UnitStatus = UnitState.ChangeOver;
            OnUnitChangeOver(EventArgs.Empty);

        }

        public string ToInfo()
        {
            return string.Format("DBID: {0} Notes: {1} Unit Type: {2}\nStatus: {3} \nQueue:\n{4}", DBId, Notes, UnitType, UnitStatus,QueueID);
        }
    }
}

