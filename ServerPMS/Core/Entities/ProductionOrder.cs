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

        string customerOrderRef;
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

        public void OnStateChanged()
        {
            StateChangedHandler?.Invoke(this, OrderStatus);
        }

        public void ChangeState(OrderState newState)
        {
            OrderStatus = newState;
            OnStateChanged();
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

