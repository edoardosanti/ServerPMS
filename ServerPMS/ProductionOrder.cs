// PMS Project V1.0
// LSData - all rights reserved
// ProductionOrder.cs

namespace ServerPMS
{
    public class ProductionOrder
    {
        static int c;
        static ProductionOrder()
        {
            c = 0;
        }
        public int ID { private set; get; }

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
                    deliveryDate = DateOnly.Parse(value);
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

        public ProductionOrder(string partCode, string partDescription, int qty, string customerOrderRef, int defaultProdUnit, string moldID, string moldLocation, string moldNotes, string customerName,string deliveryFacility, string deliveryDate)
        {
            try
            {
                ID = ++c;
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
            }
            catch
            {
                throw;
            }

        }

        public override string ToString()
        {
            return string.Format("{0}${1}${2}${3}${4}${5}${6}${7}${8}${9}${10}", PartCode, PartDescription, Qty, CustomerOrderRef, DefaultProductionUnit, MoldID, MoldLocation, MoldNotes, CustomerName, DeliveryFacility, DeliveryDate);
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

