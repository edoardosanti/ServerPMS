// PMS Project V1.0
// LSData - all rights reserved
// ProductionUnit.cs
//
//

using System.Collections;
using System.CodeDom;
using ServerPMS.Infrastructure.Database;
using ServerPMS.Infrastructure.Generic;
namespace ServerPMS
{
    public class ProductionUnit : IDBIdentifiable
    {

        public int DBId { private set; get; }
        public string RuntimeID { private set; get; }
        public event EventHandler<string> UnitStartHandler;
        public event EventHandler<string> UnitStopHandler;
        public event EventHandler<string> UnitChangeOverHandler;
        public event EventHandler<string> UnitNotesUpdateHandler;
        public event EventHandler<string> UnitNameChangedHandler;


        protected void OnUnitStop(string userId)
        {
            if (UnitStopHandler != null)
            {
                UnitStopHandler(this, userId);
            }
        }

        protected void OnUnitStart(string userId)
        {
            if (UnitStartHandler != null)
            {
                UnitStartHandler(this, userId);
            }
        }

        protected void OnNotesUpdate(string notes)
        {
            UnitNotesUpdateHandler?.Invoke(this, notes);
        }
        
        protected void OnUnitChangeOver(string userId)
        {
            if (UnitChangeOverHandler != null)
            {
                UnitChangeOverHandler(this, userId);
            }
        }

        protected void OnUnitNameChanged(string name)
        {
            UnitNameChangedHandler?.Invoke(this, name);
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

        public ProductionUnit(int dbId, string runtimeID, string name, UnitType type, string notes, UnitQueue? queue = null) //la produzione corrente viene SEMPRE rimossa dalla coda all'avvio della suddeta
        {
            DBId = dbId;
            RuntimeID = runtimeID;
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

        public void Start(string userID)
        {
            UnitStatus = UnitState.Running;
            OnUnitStart(userID);
        }

        public void Stop(string userID)
        {
            UnitStatus = UnitState.Standby;
            OnUnitStop(userID);
        }

        public void ChangeOver(string userID)
        {
            UnitStatus = UnitState.ChangeOver;
            OnUnitChangeOver(userID);

        }

        public string ToInfo()
        {
            return string.Format("DBID: {0} Notes: {1} Unit Type: {2}\nStatus: {3} \nQueue:\n{4}", DBId, Notes, UnitType, UnitStatus,QueueID);
        }

        public override string ToString()
        {
            return $"{DBId}${RuntimeID}${Name}${Notes}${QueueID}${(int)UnitType}${(int)UnitStatus}";
        }
    }
}

