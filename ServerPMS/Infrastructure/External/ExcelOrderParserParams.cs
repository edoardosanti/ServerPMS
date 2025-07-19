using System;
namespace ServerPMS.Infrastructure.External
{
    public class ExcelOrderParserParams
    {
        public Dictionary<string, string> headersMap;

        public ExcelOrderParserParams(string partCodeHeader, string partDescriptionHeader, string qtyHeader, string customerOrderRefHeader, string defaultProdUnitHeader, string moldIDHeader, string moldLocationHeader, string moldNotesHeader, string customerNameHeader, string deliveryFacilityHeader, string deliveryDateHeader)
        {
            headersMap = new Dictionary<string, string>();

            headersMap["PartCode"] = partCodeHeader;
            headersMap["PartDescription"] = partDescriptionHeader;
            headersMap["Qty"] = qtyHeader;
            headersMap["CustomerOrderRef"] = customerOrderRefHeader;
            headersMap["DefaultProductionUnit"] = defaultProdUnitHeader;
            headersMap["MoldID"] = moldIDHeader;
            headersMap["MoldLocation"] = moldLocationHeader;
            headersMap["MoldNotes"] = moldNotesHeader;
            headersMap["CustomerName"] = customerNameHeader;
            headersMap["DeliveryFacility"] = deliveryFacilityHeader;
            headersMap["DeliveryDate"] = deliveryDateHeader;

        }
    }
}

