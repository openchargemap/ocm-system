using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;
using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OCM.Import.Providers
{

    public class ImportProvider_GenericExcel : BaseImportProvider, IImportProvider
    {
        public enum ExcelTemplateFormat
        {
            FormatV1 = 1
        }

        public ImportProvider_GenericExcel()
        {
            ProviderName = "Excel";
            OutputNamePrefix = "Excel";

            IsAutoRefreshed = false;
            IsProductionReady = true;

            SourceEncoding = Encoding.GetEncoding("UTF-8");
        }

        public List<API.Common.Model.ChargePoint> Process(API.Common.Model.CoreReferenceData coreRefData)
        {

            List<ChargePoint> outputList = new List<ChargePoint>();

            string importMappingJSON = "{ID:0,LocationTitle:1, AddressLine1:2,	AddressLine2:3,	Town:4,	StateOrProvince:5,	Postcode:6,	Country:7,	Latitude:8,	Longitude:9,Addr_ContactTelephone1:10,	Addr_ContactTelephone2:11,	Addr_ContactEmail:12,	Addr_AccessComments:13,	Addr_GeneralComments:14,	Addr_RelatedURL:15,	UsageType:16,	NumberOfPoints:17,	GeneralComments:18,	DateLastConfirmed:19,	StatusType:20,	DateLastStatusUpdate:21,	UsageCost:22,	Connection1_Type:23,	Connection1_Amps:24,	Connection1_Volts:25,	Connection1_Level:26,	Connection1_Quantity:27,	Connection2_Type:28,	Connection2_Amps:29,	Connection2_Volts:30,	Connection2_Level:31,	Connection2_Quantity:32,	Connection3_Type:33,	Connection3_Amps:34,	Connection3_Volts:35,	Connection3_Level:36,	Connection3_Quantity:37}";

            //get import column mappings from JSON format config item into a Dictionary
            IDictionary<string, JToken> tmpMap = JObject.Parse(importMappingJSON);
            Dictionary<string, int> importMappings = tmpMap.ToDictionary(pair => pair.Key, pair => (int)pair.Value);

            try
            {
                //Open the Excel workbook.
                //example: http://msdn.microsoft.com/library/dd920313.aspx
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(this.InputPath, false))
                {
                    //References to the workbook and Shared String Table.
                    var workBook = document.WorkbookPart.Workbook;
                    var workSheets = workBook.Descendants<Sheet>();
                    var sharedStrings = document.WorkbookPart.SharedStringTablePart.SharedStringTable;

                    //fetch first sheet in workbook
                    var importSheetID = workSheets.First().Id;
                    var importSheet = (WorksheetPart)document.WorkbookPart.GetPartById(importSheetID);

                    Row headerRow = importSheet.RootElement.Descendants<Row>().First(r => r.RowIndex == 1);
                    List<Cell> headerCells = headerRow.Descendants<Cell>().ToList();

                    //LINQ query to skip first row with column names.
                    IEnumerable<Row> dataRows =
                        from row in importSheet.RootElement.Descendants<Row>()
                        where row.RowIndex > 1
                        select row;

                    int sourceRowIndex = 0;
                    foreach (Row row in dataRows)
                    {
                        sourceRowIndex++;
                        ChargePoint import = new ChargePoint();
                        try
                        {

                            List<Cell> cellList = row.Elements<Cell>().ToList();

                            var textArray = new string[headerCells.Count];
                            for (int i = 0; i < textArray.Length; i++)
                            {
                                string headerCellref = Regex.Replace(headerCells[i].CellReference.Value, @"[\d-]", string.Empty);
                                //if cell has value, populate array position
                                var cell = cellList.FirstOrDefault(c => c.CellReference.Value == (headerCellref + (sourceRowIndex + 1)));
                                if (cell != null)
                                {
                                    textArray[i] = (cell.DataType != null && cell.DataType.HasValue && cell.DataType == CellValues.SharedString ? sharedStrings.ChildElements[int.Parse(cell.CellValue.InnerText)].InnerText : (cell.CellValue != null ? cell.CellValue.InnerText : null));
                                }
                                else
                                {
                                    //empty cell
                                }
                            }

                            if (textArray.Any(t=>t!=null))
                            {
                                //load import row values from value in worksheet row
                                import.DataProvidersReference = textArray[importMappings["ID"]];
                                import.DataQualityLevel = 3; 

                                if (this.DefaultDataProvider != null) import.DataProvider = DefaultDataProvider;
                                
                                /*try
                                {
                                    importRow.InvoiceDate = DateTime.FromOADate(
                                        Double.Parse(
                                            textArray[importMappings["DateLastConfirmed"]]
                                        )
                                        ); //excel date is a double value (from OLE Automation) in days since 1900/1/1
                                }
                                */

                                import.AddressInfo = new AddressInfo();
                                import.AddressInfo.Title = textArray[importMappings["LocationTitle"]];
                                import.AddressInfo.AddressLine1 = textArray[importMappings["AddressLine1"]];
                                import.AddressInfo.AddressLine2 = textArray[importMappings["AddressLine2"]];
                                import.AddressInfo.Town = textArray[importMappings["Town"]];
                                import.AddressInfo.StateOrProvince = textArray[importMappings["StateOrProvince"]];
                                import.AddressInfo.Postcode = textArray[importMappings["Postcode"]];

                                string countryName = textArray[importMappings["Country"]];
                                import.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.Title == countryName);

                                //latitude
                                try
                                {
                                    import.AddressInfo.Latitude = double.Parse(textArray[importMappings["Latitude"]]);
                                }
                                catch (Exception exp)
                                {
                                    //failed to parse value
                                    throw exp;
                                }

                                //longitude
                                try
                                {
                                    import.AddressInfo.Longitude = double.Parse(textArray[importMappings["Longitude"]]);
                                }
                                catch (Exception exp)
                                {
                                    //failed to parse value
                                    throw exp;
                                }


                                if (import.Connections == null) import.Connections = new List<ConnectionInfo>();

                                //connection info 1
                               
                                if (textArray[importMappings["Connection1_Type"]] != null)
                                {
                                    var conn = new ConnectionInfo();
                                    conn.ConnectionType = coreRefData.ConnectionTypes.FirstOrDefault(c => c.Title == textArray[importMappings["Connection1_Type"]]);
                                    import.Connections.Add(conn);
                                }
                                if (textArray[importMappings["Connection2_Type"]] != null)
                                {
                                    var conn = new ConnectionInfo();
                                    conn.ConnectionType = coreRefData.ConnectionTypes.FirstOrDefault(c => c.Title == textArray[importMappings["Connection2_Type"]]);
                                    import.Connections.Add(conn);
                                }

                            }
                            else
                            {
                               //blank row
                                import = null;
                            }


                          


                        }
                        catch (Exception exp)
                        {
                            //exception parsing current row
                            System.Diagnostics.Debug.WriteLine("Excel Import: " + exp.Message);

                        }
                        finally
                        {
                           if (import!=null) outputList.Add(import);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                //could not read from file
                throw (exp);
            }

            return outputList;

        }

        private static Cell GetCellInRow(Row row, uint cellIndex, uint rowIndex)
        {
            List<Cell> allCells = row.Elements<Cell>().ToList();

            return allCells.First(c => c.CellMetaIndex == cellIndex);

            /*return row.Elements<Cell>().Where(c => string.Compare
                   (c.CellReference.Value, columnName + rowIndex, true) == 0).First();*/
        }


        // Given a worksheet and a row index, return the row.
        private static Row GetRow(Worksheet worksheet, uint rowIndex)
        {
            return worksheet.GetFirstChild<SheetData>().Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
        }
    }
}
