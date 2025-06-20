// PMS Project V1.0
// LSData - all rights reserved
// ExcelOrderParser.cs
//
//
using ClosedXML.Excel;


namespace ServerPMS
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

    public class ExcelOrderParser
    {


        IXLWorkbook workbook;
        IXLWorksheets worksheets;
        IXLWorksheet wrksheet;

        Dictionary<string, string> headersLocationMap;
        Dictionary<string, string> headersMap;


        public ExcelOrderParser(string filename, ExcelOrderParserParams parameters)
        {

            headersLocationMap = new Dictionary<string, string>();
            headersMap = parameters.headersMap;
            
            try
            {
                workbook = new XLWorkbook(filename);
                worksheets = workbook.Worksheets;
                wrksheet = worksheets.Worksheet(1);
                if (wrksheet.LastRowUsed().RowNumber() >= 2)
                    MapHeaders();
                else
                    throw new ArgumentException("The file does not contains any order. ");
            }
            catch
            {
                throw;
            }
        }

        private void MapHeaders()
        {
            IXLRow headers = wrksheet.Row(1); //1st row have to be headers row
            foreach (IXLCell c in headers.CellsUsed())
            {
                try
                {
                    headersLocationMap.Add(headersMap.First(x => x.Value == c.GetValue<string>()).Key, c.Address.ColumnLetter);
                }catch(Exception)
                {
                    throw;
                }
            }
        }

        //TODO Implement multiple worksheet support

        public List<ProductionOrder> ParseOrders()
        {
            int last = wrksheet.LastRowUsed().RowNumber();
            List<ProductionOrder> orders = new List<ProductionOrder>();

            foreach (IXLRow row in wrksheet.Rows(2, last))
            {
                DateTime delDate;
                try
                {
                    delDate = DateTime.Parse(row.Cell(headersLocationMap["DeliveryDate"]).GetString());
                }
                catch
                {
                    throw;
                }
                orders.Add(new ProductionOrder(
                    row.Cell(headersLocationMap["PartCode"]).GetString(),
                    row.Cell(headersLocationMap["PartDescription"]).GetString(),
                    row.Cell(headersLocationMap["Qty"]).GetValue<int>(),
                    row.Cell(headersLocationMap["CustomerOrderRef"]).GetString(),
                    row.Cell(headersLocationMap["DefaultProductionUnit"]).GetValue<int>(),
                    row.Cell(headersLocationMap["MoldID"]).GetString(),
                    row.Cell(headersLocationMap["MoldLocation"]).GetString(),
                    row.Cell(headersLocationMap["MoldNotes"]).GetString(),
                    row.Cell(headersLocationMap["CustomerName"]).GetString(),
                    row.Cell(headersLocationMap["DeliveryFacility"]).GetString(),
                    delDate.ToShortDateString()
                    ));
            }
            return orders;
        }
    }
}

