// PMS Project V1.0
// LSData - all rights reserved
// ProductionOrder.cs

using System.Globalization;
using ServerPMS.Infrastructure.Database;

namespace ServerPMS
{
    public class ProductionOrder:IEquatable<ProductionOrder>,IDBIdentifiable
    {
        public event EventHandler<OrderState> StateChangedHandler;
        public event EventHandler<string> PartCodeChangedHandler;
        public event EventHandler<string> PartDescriptionChangedHandler;
        public event EventHandler<int> QtyChangedHandler;
        public event EventHandler<string> CustomerOrderRefChangedHandler;
        public event EventHandler<string> MoldIDChangedHandler;
        public event EventHandler<string> MoldLocationChangedHandler;
        public event EventHandler<string> MoldNotesChangedHandler;
        public event EventHandler<string> CustomerNameChangedHandler;
        public event EventHandler<string> DeliveryFacilityChangedHandler;
        public event EventHandler<DateOnly> DeliveryDateChangedHandler;
        public event EventHandler<string> PropertyChangedHandler;


        protected void OnPartCodeChanged(string newPartCode, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            PartCodeChangedHandler?.Invoke(this, newPartCode);
        }

        protected void OnPartDesChanged(string newPartDes, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            PartDescriptionChangedHandler?.Invoke(this, newPartDes);
        }

        protected void OnQtyChanged(int newQty, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            QtyChangedHandler?.Invoke(this, newQty);
        }

        protected void OnCustomerRefChanged(string newRef, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            CustomerOrderRefChangedHandler?.Invoke(this, newRef);
        }

        protected void OnMoldIDChanged(string newMoldID, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            MoldIDChangedHandler?.Invoke(this, newMoldID);
        }

        protected void OnMoldLocationChanged(string newLocation, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            MoldLocationChangedHandler?.Invoke(this, newLocation);
        }

        protected void OnMoldNotesChanged(string newNotes, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            MoldNotesChangedHandler?.Invoke(this, newNotes);
        }

        protected void OnCustomerNameChanged(string newCustomerName, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            CustomerNameChangedHandler?.Invoke(this, newCustomerName);
        }

        protected void OnDeliveryFacilityChanged(string newFacility, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            DeliveryFacilityChangedHandler?.Invoke(this, newFacility);
        }

        protected void OnDeliveryDateChanged(DateOnly newDate, string userId)
        {
            PropertyChangedHandler?.Invoke(this, userId);
            DeliveryDateChangedHandler?.Invoke(this, newDate);
        }

        public void OnStateChanged(OrderState newState)
        {
            StateChangedHandler?.Invoke(this, newState);
        }

        public string RuntimeID { private set; get; }
        public int DBId {  set; get; }

        string code;
        public string PartCode
        {
            private set
            {
                if (!value.Equals(string.Empty))
                {
                    code = value;
                }
                else
                    throw new ArgumentException("The part code cannot be empty.");
            }

            get { return code; }
        }

        string desc;
        public string PartDescription
        {
            private set
            {
                if (!value.Equals(string.Empty))
                {
                    desc = value;
                }
                else
                    throw new ArgumentException("The part description cannot be empty.");
            }

            get { return desc; }
        }

        int qty;
        public int Qty
        {
            private set
            {
                if (value >= 0)
                {
                    qty = value;
                }
                else
                    throw new ArgumentException("The quantity cannot be under 0.");
            }

            get { return qty; }
        }

        private string customerOrderRef;
        public string CustomerOrderRef
        {
            private set
            {
                if (!value.Equals(string.Empty))
                {
                    customerOrderRef = value;
                }
                else
                    throw new ArgumentException("The customer order ID cannot be empty.");
            }

            get { return customerOrderRef; }
        }

        int unit;
        public int DefaultProductionUnit
        {
            private set
            {
                if (value >= 0)
                {
                    unit = value;
                }
                else
                    throw new ArgumentException("The default production unit property can only be set to values greater or equals to zero. Zero means there is no preferred unit.");
            }
            get { return unit; }
        }

        string mold;
        public string MoldID
        {
            private set
            {
                if (!value.Equals(string.Empty))
                {
                    mold = value;
                }
                else
                    throw new ArgumentException("The mold ID cannot be empty.");
            }

            get { return mold; }
        }

        string moldLocation;
        public string MoldLocation { 
            private set
            {
                if (!value.Equals(string.Empty))
                {
                    moldLocation = value;
                }
                else
                    throw new ArgumentException("The mold position cannot be empty.");
            }

            get { return moldLocation; }
        }
    
        public string MoldNotes { private set; get; }

        string customer;
        public string CustomerName
        {
            private set
            {
                if (!value.Equals(string.Empty))
                {
                    customer = value;
                }
                else
                    throw new ArgumentException("The customer cannot be empty.");
            }

            get { return customer; }
        }

        string deliveryFacility;
        public string DeliveryFacility
        {
            private set
            {
                if (!value.Equals(string.Empty))
                {
                    deliveryFacility = value;
                }
                else
                    throw new ArgumentException("The delivery facility cannot be empty.");
            }

            get { return deliveryFacility; }
        }

        DateOnly deliveryDate;
        public string DeliveryDate
        {
            private set
            {
                try
                {
                    string[] formats = { "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy" };
                    deliveryDate = DateOnly.ParseExact(value, formats, CultureInfo.InvariantCulture);

                }
                catch {
                    throw;
                }

            }
            get
            {
                    return deliveryDate.ToShortDateString();
            }
        }

        public OrderState OrderStatus { private set; get; }

        DateOnly dateAdded;
        public string DateAdded
        {
            private set
            {
                try
                {
                    string[] formats = { "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy" };
                    dateAdded = DateOnly.ParseExact(value, formats, CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw;
                }
            }
            get { return dateAdded.ToShortDateString(); }
        }

        public void UpdatePartCode(string newPartCode, string userId)
        {
            PartCode = newPartCode;
            OnPartCodeChanged(newPartCode,userId);
        }

        public void UpdatePartDescription(string newDescription, string userId)
        {
            PartDescription = newDescription;
            OnPartDesChanged(newDescription,userId);
        }

        public void UpdateQty(int newQty, string userId)
        {
            Qty = newQty;
            OnQtyChanged(newQty,userId);
        }

        public void UpdateCustomerOrderRef(string newRef, string userId)
        {
            CustomerOrderRef = newRef;
            OnCustomerRefChanged(newRef,userId);
        }

        public void UpdateDefaultProductionUnit(int newUnit, string userId)
        {
            DefaultProductionUnit = newUnit;
            // No event defined for DefaultProductionUnit
        }

        public void UpdateMoldID(string newMoldID, string userId)
        {
            MoldID = newMoldID;
            OnMoldIDChanged(newMoldID,userId);
        }

        public void UpdateMoldLocation(string newLocation, string userId)
        {
            MoldLocation = newLocation;
            OnMoldLocationChanged(newLocation,userId);
        }

        public void UpdateMoldNotes(string newNotes, string userId)
        { 

            MoldNotes = newNotes;
            OnMoldNotesChanged(newNotes,userId);
        }

        public void UpdateCustomerName(string newCustomerName, string userId)
        {
            CustomerName = newCustomerName;
            OnCustomerNameChanged(newCustomerName,userId);
        }

        public void UpdateDeliveryFacility(string newFacility, string userId)
        {
            DeliveryFacility = newFacility;
            OnDeliveryFacilityChanged(newFacility,userId);
        }

        public void UpdateDeliveryDate(string newDate, string userId)
        {
            DeliveryDate = newDate;
            OnDeliveryDateChanged(deliveryDate,userId);
        }

        public void UpdateOrderStatus(OrderState newStatus)
        {
            OrderStatus = newStatus;
            OnStateChanged(newStatus);
        }

        public ProductionOrder(string partCode, string partDescription, int qty, string customerOrderRef, int defaultProdUnit, string moldID, string moldLocation, string moldNotes, string customerName,string deliveryFacility, string deliveryDate, int DBId=-1, OrderState state=OrderState.Imported)
        {
            try
            {
                this.DBId = DBId;
                RuntimeID = Guid.NewGuid().ToString();
                PartCode = partCode;
                PartDescription = partDescription;
                Qty = qty;
                CustomerOrderRef = customerOrderRef;
                DefaultProductionUnit = defaultProdUnit;
                MoldID = moldID;
                MoldLocation = moldLocation;
                MoldNotes = moldNotes;
                CustomerName = customerName;
                DeliveryFacility = deliveryFacility;
                DeliveryDate = deliveryDate;
                OrderStatus = state;
            }
            catch
            {
                throw;
            }

        }

        public override string ToString()
        {
            return string.Format("{0}${1}${2}${3}${4}${5}${6}${7}${8}${9}${10}${11}${12}", DBId, PartCode, PartDescription, Qty, CustomerOrderRef, DefaultProductionUnit, MoldID, MoldLocation, MoldNotes, CustomerName, DeliveryFacility, DeliveryDate,OrderStatus);
        }

        public static ProductionOrder FromDump(string dump)
        {
            ProductionOrder order;
            if (dump != string.Empty)
            {
                string[] tmp = dump.Split("$");
                order = new ProductionOrder(
                    tmp[1],
                    tmp[2],
                    int.Parse(tmp[3]),
                    tmp[4],
                    int.Parse(tmp[5]),
                    tmp[6],
                    tmp[7],
                    tmp[8],
                    tmp[9],
                    tmp[10],
                    tmp[11],
                    int.Parse(tmp[0]),
                    (OrderState)Enum.Parse(typeof(OrderState), tmp[12])
                    );
                return order;
            }
            else
                throw new InvalidOperationException("Dump cant be empty");
            
        }

        public bool Equals(ProductionOrder? order)
        {
            if (DBId == order?.DBId || RuntimeID == order?.RuntimeID)
                return true;
            return false;
        }

        public string ToInfo()
        {
            return string.Format("PartCode: {0} PartDescription: {1} Qty: {2} CustomerOrderRef: {3} DefaultProductionUnit: {4}\nMoldID: {5} MoldLocation: {6} MoldNotes: {7} CustomerName: {8}\nDeliveryFacility: {9} DeliveryDate: {10}\n\n", PartCode, PartDescription, Qty, CustomerOrderRef, DefaultProductionUnit, MoldID, MoldLocation, MoldNotes, CustomerName, DeliveryFacility, DeliveryDate);
        }

        public string ToShortInfo()
        {
            return string.Format("PartCode: {0} Qty: {1} MoldID: {2} MoldLocation: {3} MoldNotes: {4}", PartCode, Qty, MoldID, MoldLocation,MoldNotes);
        }
    }
}

