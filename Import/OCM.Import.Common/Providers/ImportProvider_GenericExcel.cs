#if OPENXML

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
        private ExcelTemplateFormat ImportFormat = ExcelTemplateFormat.FormatV1;

        public enum ExcelTemplateFormat
        {
            FormatV1 = 1,
            FormatV2 = 2,
        }

        public ImportProvider_GenericExcel()
        {
            ProviderName = "Excel";
            OutputNamePrefix = "Excel";

            IsAutoRefreshed = false;
            IsProductionReady = true;
            IsStringData = false;

            SourceEncoding = Encoding.GetEncoding("UTF-8");
            ImportFormat = ExcelTemplateFormat.FormatV2;
        }

        public List<API.Common.Model.ChargePoint> Process(API.Common.Model.CoreReferenceData coreRefData)
        {
            List<ChargePoint> outputList = new List<ChargePoint>();

            string importMappingJSON = "{ID:0,LocationTitle:1, AddressLine1:2,	AddressLine2:3,	Town:4,	StateOrProvince:5,	Postcode:6,	Country:7,	Latitude:8,	Longitude:9,Addr_ContactTelephone1:10,	Addr_ContactTelephone2:11,	Addr_ContactEmail:12,	Addr_AccessComments:13,	Addr_GeneralComments:14,	Addr_RelatedURL:15,	UsageType:16,	NumberOfPoints:17,	GeneralComments:18,	DateLastConfirmed:19,	StatusType:20,	DateLastStatusUpdate:21,	UsageCost:22,	Connection1_Type:23,	Connection1_Amps:24,	Connection1_Volts:25,	Connection1_Level:26,	Connection1_Quantity:27,	Connection2_Type:28,	Connection2_Amps:29,	Connection2_Volts:30,	Connection2_Level:31,	Connection2_Quantity:32,	Connection3_Type:33,	Connection3_Amps:34,	Connection3_Volts:35,	Connection3_Level:36,	Connection3_Quantity:37}";

            if (ImportFormat == ExcelTemplateFormat.FormatV2)
            {
                importMappingJSON = "{ID:0,LocationTitle:1, AddressLine1:2,	AddressLine2:3,	Town:4,	StateOrProvince:5,	Postcode:6,	Country:7,	Latitude:8,	Longitude:9,Addr_ContactTelephone1:10,	Addr_ContactTelephone2:11,	Addr_ContactEmail:12,	Addr_AccessComments:13,	Addr_GeneralComments:14,	Addr_RelatedURL:15,	UsageType:16,	NumberOfPoints:17,	GeneralComments:18,	DateLastConfirmed:19,	StatusType:20,	DateLastStatusUpdate:21,	UsageCost:22, Operator:23, 	Connection1_Type:24,	Connection1_kW:25, Connection1_Amps:26,	Connection1_Volts:27,	Connection1_Level:28,	Connection1_Quantity:29,	Connection2_Type:30,	Connection2_kW:31, Connection2_Amps:32,	Connection2_Volts:33,	Connection2_Level:34,	Connection2_Quantity:35,	Connection3_Type:36,  Connection3_kW:37,	Connection3_Amps:38,	Connection3_Volts:39,	Connection3_Level:40,	Connection3_Quantity:41,Connection4_Type:42,  Connection4_kW:43,	Connection4_Amps:44,	Connection4_Volts:45,	Connection4_Level:46,	Connection4_Quantity:47,Connection5_Type:48,  Connection5_kW:49,	Connection5_Amps:50,	Connection5_Volts:51,	Connection5_Level:52,	Connection5_Quantity:53,Connection6_Type:54,  Connection6_kW:55,	Connection6_Amps:56,	Connection6_Volts:57,	Connection6_Level:58,	Connection6_Quantity:58, POI_Types:59}";
            }
            //get import column mappings from JSON format config item into a Dictionary
            IDictionary<string, JToken> tmpMap = JObject.Parse(importMappingJSON);
            Dictionary<string, int> lookup = tmpMap.ToDictionary(pair => pair.Key, pair => (int)pair.Value);

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

                    var docProperties = document.PackageProperties;

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

                            var col = new string[headerCells.Count];
                            for (int i = 0; i < col.Length; i++)
                            {
                                string headerCellref = Regex.Replace(headerCells[i].CellReference.Value, @"[\d-]", string.Empty);
                                //if cell has value, populate array position
                                var cell = cellList.FirstOrDefault(c => c.CellReference.Value == (headerCellref + (sourceRowIndex + 1)));
                                if (cell != null)
                                {
                                    col[i] = (cell.DataType != null && cell.DataType.HasValue && cell.DataType == CellValues.SharedString ? sharedStrings.ChildElements[int.Parse(cell.CellValue.InnerText)].InnerText : (cell.CellValue != null ? cell.CellValue.InnerText : null));
                                }
                                else
                                {
                                    //empty cell
                                }
                            }

                            if (col.Any(t => t != null))
                            {
                                //load import row values from value in worksheet row
                                import.DataProvidersReference = col[lookup["ID"]];
                                import.DataQualityLevel = 3;

                                if (this.DefaultDataProvider != null) import.DataProvider = DefaultDataProvider;

                                /*try
                                {
                                    importRow.DateLastConfirmed = DateTime.FromOADate(
                                        Double.Parse(
                                            textArray[importMappings["DateLastConfirmed"]]
                                        )
                                        ); //excel date is a double value (from OLE Automation) in days since 1900/1/1
                                }
                                */

                                import.DataProviderID = (int)StandardDataProviders.OpenChargeMapContrib;
                                import.AddressInfo = new AddressInfo();
                                import.AddressInfo.Title = col[lookup["LocationTitle"]];
                                import.AddressInfo.AddressLine1 = col[lookup["AddressLine1"]];
                                import.AddressInfo.AddressLine2 = col[lookup["AddressLine2"]];
                                import.AddressInfo.Town = col[lookup["Town"]];
                                import.AddressInfo.StateOrProvince = col[lookup["StateOrProvince"]];
                                import.AddressInfo.Postcode = col[lookup["Postcode"]];

                                if (string.IsNullOrEmpty(import.AddressInfo.AddressLine1) && string.IsNullOrEmpty(import.AddressInfo.Postcode))
                                {
                                    import.AddressCleaningRequired = true;
                                }

                                var country = col[lookup["Country"]];
                                import.AddressInfo.CountryID = coreRefData.Countries.FirstOrDefault(c => c.Title.Equals(country, StringComparison.InvariantCultureIgnoreCase))?.ID;

                                import.AddressInfo.ContactTelephone1 = col[lookup["Addr_ContactTelephone1"]];
                                import.AddressInfo.ContactTelephone2 = col[lookup["Addr_ContactTelephone2"]];
                                import.AddressInfo.ContactEmail = col[lookup["Addr_ContactEmail"]];
                                import.AddressInfo.RelatedURL = col[lookup["Addr_RelatedURL"]];

                                import.GeneralComments = col[lookup["GeneralComments"]];

                                if (!String.IsNullOrEmpty(import.AddressInfo.RelatedURL) && !import.AddressInfo.RelatedURL.StartsWith("http"))
                                {
                                    import.AddressInfo.RelatedURL = "http://" + import.AddressInfo.RelatedURL;
                                }

                                string countryName = col[lookup["Country"]];
                                import.AddressInfo.Country = coreRefData.Countries.FirstOrDefault(c => c.Title == countryName);

                                //latitude
                                try
                                {
                                    import.AddressInfo.Latitude = double.Parse(col[lookup["Latitude"]]);
                                }
                                catch (Exception exp)
                                {
                                    //failed to parse value
                                    throw exp;
                                }

                                //longitude
                                try
                                {
                                    import.AddressInfo.Longitude = double.Parse(col[lookup["Longitude"]]);
                                }
                                catch (Exception exp)
                                {
                                    //failed to parse value
                                    throw exp;
                                }

                                //Usage type
                                var usageCol = col[lookup["UsageType"]];
                                if (!String.IsNullOrWhiteSpace(usageCol))
                                {
                                    var usageType = coreRefData.UsageTypes.FirstOrDefault(u => u.Title == usageCol);
                                    if (usageType != null)
                                    {
                                        import.UsageTypeID = usageType.ID;
                                    }
                                    else
                                    {
                                        Log("Unknown usage type: " + usageCol);
                                    }
                                }

                                //status type
                                var statusCol = col[lookup["StatusType"]];
                                if (!String.IsNullOrWhiteSpace(statusCol))
                                {
                                    var statusType = coreRefData.StatusTypes.FirstOrDefault(u => u.Title == statusCol);
                                    if (statusType != null)
                                    {
                                        import.StatusTypeID = statusType.ID;
                                    }
                                    else
                                    {
                                        Log("Unknown status type: " + statusCol);
                                    }
                                }

                                //number of points
                                if (!String.IsNullOrWhiteSpace(col[lookup["NumberOfPoints"]]))
                                {
                                    import.NumberOfPoints = int.Parse(col[lookup["NumberOfPoints"]]);
                                }

                                // Operator
                                var operatorCol = col[lookup["Operator"]];
                                if (!String.IsNullOrWhiteSpace(operatorCol))
                                {
                                    var operatorInfo = coreRefData.Operators.FirstOrDefault(u => u.Title.StartsWith(operatorCol, StringComparison.InvariantCultureIgnoreCase));
                                    if (operatorInfo != null)
                                    {
                                        import.OperatorID = operatorInfo.ID;
                                    }
                                    else
                                    {
                                        Log("Unknown Operator: " + operatorCol);
                                    }
                                }

                                //TODO: parse POI_Types (Parking Lot)
                                //poi type metadata:
                                var poiTypeCol = col[lookup["POI_Types"]];
                                if (!String.IsNullOrWhiteSpace(poiTypeCol))
                                {
                                    import.MetadataValues = new List<MetadataValue>();

                                    var vals = poiTypeCol.Split(',');
                                    foreach (var p in vals)
                                    {
                                        if (p == "Parking Lot")
                                        {
                                            import.MetadataValues.Add(new MetadataValue { MetadataFieldID = (int)StandardMetadataFields.POIType, MetadataFieldOptionID = (int)StandardMetadataFieldOptions.Parking });
                                        }
                                        else
                                        {
                                            Log("Unknown POI type:" + p);
                                        }
                                    }
                                }

                                if (import.Connections == null) import.Connections = new List<ConnectionInfo>();

                                //connection info 1
                                int maxConnections = 6;
                                for (int i = 1; i <= maxConnections; i++)
                                {
                                    var conn = new ConnectionInfo();
                                    var connectionTypeCol = col[lookup["Connection" + i + "_Type"]];
                                    if (!String.IsNullOrWhiteSpace(connectionTypeCol))
                                    {
                                        connectionTypeCol = connectionTypeCol.Trim();
                                        if (connectionTypeCol == "Mennekes")
                                        {
                                            conn.ConnectionTypeID = (int)StandardConnectionTypes.MennekesType2;
                                            conn.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                        }
                                        else if (connectionTypeCol == "CCS2")
                                        {
                                            conn.ConnectionTypeID = (int)StandardConnectionTypes.CCSComboType2;
                                            conn.CurrentTypeID = (int)StandardCurrentTypes.DC;
                                        }
                                        else if (connectionTypeCol == "CEE 7/4 - Schuko - Type F")
                                        {
                                            conn.ConnectionTypeID = (int)StandardConnectionTypes.Schuko;
                                            conn.CurrentTypeID = (int)StandardCurrentTypes.SinglePhaseAC;
                                        }
                                        else
                                        {
                                            var connectiontype = coreRefData.ConnectionTypes.FirstOrDefault(c => c.Title == col[lookup["Connection1_Type"]]);
                                            if (connectiontype != null)
                                            {
                                                conn.ConnectionTypeID = connectiontype.ID;
                                            }
                                            else
                                            {
                                                Log("Failed to match connection type:" + col[lookup["Connection" + i + "_Type"]]);
                                            }
                                        }
                                    }

                                    if (!String.IsNullOrWhiteSpace(col[lookup["Connection" + i + "_kW"]]))
                                    {
                                        conn.PowerKW = double.Parse(col[lookup["Connection" + i + "_kW"]]);
                                    }

                                    if (!String.IsNullOrWhiteSpace(col[lookup["Connection" + i + "_Amps"]]))
                                    {
                                        conn.Amps = int.Parse(col[lookup["Connection" + i + "_Amps"]]);
                                    }

                                    if (!String.IsNullOrWhiteSpace(col[lookup["Connection" + i + "_Volts"]]))
                                    {
                                        conn.Voltage = int.Parse(col[lookup["Connection" + i + "_Volts"]]);
                                    }

                                    if (!String.IsNullOrWhiteSpace(col[lookup["Connection" + i + "_Level"]]))
                                    {
                                        conn.LevelID = int.Parse(col[lookup["Connection" + i + "_Level"]]);
                                    }

                                    if (!IsConnectionInfoBlank(conn))
                                    {
                                        import.Connections.Add(conn);
                                    }
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
                            if (import != null) outputList.Add(import);
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

#endif