using GIS.Framework.Interfaces;
using GIS.Framework.Models.EInvoice;
using Newtonsoft.Json;
using System;
using System.Data;

namespace GIS.Framework.Helpers
{
    public static class PayloadGenerator
    {
        public static Tuple<string, string> GenerateEInvoicePayload(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            LoggerHelper.Log("Entered PayloadGenerator.GenerateEInvoicePayload");
            LoggerHelper.Log($"Starting Standalone E-Invoice Payload Generation for ObjType: {objType}, DocEntry: {docEntry}...");

            string headerQuery = $"CALL \"GIS_EInvoice_Get_GenInvoiceDet_Split\"('{objType}','{docEntry}','Addon','Header')";
            DataTable headerTable = dbHelper.ExecuteQuery(headerQuery);
            if (headerTable == null || headerTable.Rows.Count == 0) throw new Exception("No header data returned from GIS_EInvoice_Get_GenInvoiceDet_Split.");

            string detailQuery = $"CALL \"GIS_EInvoice_Get_GenInvoiceDet_Split\"('{objType}','{docEntry}','Addon','Detail')";
            DataTable detailTable = dbHelper.ExecuteQuery(detailQuery);
            if (detailTable == null || detailTable.Rows.Count == 0) throw new Exception("No detail data returned from GIS_EInvoice_Get_GenInvoiceDet_Split.");

            LoggerHelper.Log("Mapping raw SAP data to Standalone E-Invoice Models...");
            EInvoicePayload payload = PayloadMappers.MapHeader(headerTable.Rows[0]);
            payload.ItemList = PayloadMappers.MapDetails(detailTable);
            
            // Explicitly do NOT set EwayBillDetails, so it gets omitted due to NullValueHandling.Ignore
            payload.EwayBillDetails = null;

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string jsonPayload = JsonConvert.SerializeObject(payload, settings);

            // Extract the Target URL directly from the database response
            string targetUrl = headerTable.Rows[0].Table.Columns.Contains("URL") ? headerTable.Rows[0]["URL"].ToString().Trim() : "https://gstsandbox.charteredinfo.com/eicore/dec/v1.03/Invoice";

            LoggerHelper.Log("Exited PayloadGenerator.GenerateEInvoicePayload");
            return new Tuple<string, string>(jsonPayload, targetUrl);
        }

        public static Tuple<string, string> GenerateCombinedPayload(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            LoggerHelper.Log("Entered PayloadGenerator.GenerateCombinedPayload");
            LoggerHelper.Log($"Starting Combined Payload Generation for ObjType: {objType}, DocEntry: {docEntry}...");

            string headerQuery = $"CALL \"GIS_EInvoice_Get_GenInvoiceDet_Split\"('{objType}','{docEntry}','Addon','Header')";
            DataTable headerTable = dbHelper.ExecuteQuery(headerQuery);
            if (headerTable == null || headerTable.Rows.Count == 0) throw new Exception("No header data returned from GIS_EInvoice_Get_GenInvoiceDet_Split.");

            string detailQuery = $"CALL \"GIS_EInvoice_Get_GenInvoiceDet_Split\"('{objType}','{docEntry}','Addon','Detail')";
            DataTable detailTable = dbHelper.ExecuteQuery(detailQuery);
            if (detailTable == null || detailTable.Rows.Count == 0) throw new Exception("No detail data returned from GIS_EInvoice_Get_GenInvoiceDet_Split.");

            LoggerHelper.Log("Mapping raw SAP data to E-Invoice Models...");
            EInvoicePayload payload = PayloadMappers.MapHeader(headerTable.Rows[0]);
            payload.ItemList = PayloadMappers.MapDetails(detailTable);

            // Fetch E-Way Bill Details from the dedicated E-Way Bill SP
            try
            {
                string sDocType = (objType == "13" || objType == "IN") ? "Invoice" : (objType == "14" ? "CreditMemo" : objType);
                string ewbQuery = $"CALL \"GIS_EwayBill_GetPostDet\"('{docEntry}', '', '', '', '', '', '', '', '', '', '{sDocType}', 'Header')";
                DataTable ewbTable = dbHelper.ExecuteQuery(ewbQuery);

                if (ewbTable != null && ewbTable.Rows.Count > 0)
                {
                    DataRow ewbRow = ewbTable.Rows[0];
                    if (ewbRow.Table.Columns.Contains("transporterId") || ewbRow.Table.Columns.Contains("vehicleNo"))
                    {
                        string transId = ewbRow.Table.Columns.Contains("transporterId") ? ewbRow["transporterId"].ToString().Trim() : null;
                        string transName = ewbRow.Table.Columns.Contains("transporterName") ? ewbRow["transporterName"].ToString().Trim() : null;
                        string transMode = ewbRow.Table.Columns.Contains("transMode") ? ewbRow["transMode"].ToString().Trim() : null;
                        string transDocNo = ewbRow.Table.Columns.Contains("transDocNo") ? ewbRow["transDocNo"].ToString().Trim() : null;
                        string transDocDt = ewbRow.Table.Columns.Contains("transDocDate") ? ewbRow["transDocDate"].ToString().Trim() : null;
                        string vehNo = ewbRow.Table.Columns.Contains("vehicleNo") ? ewbRow["vehicleNo"].ToString().Trim() : null;
                        string vehType = ewbRow.Table.Columns.Contains("vehicleType") ? ewbRow["vehicleType"].ToString().Trim() : null;

                        payload.EwayBillDetails = new EwbDetails
                        {
                            TransporterId = string.IsNullOrEmpty(transId) ? null : transId,
                            TransporterName = string.IsNullOrEmpty(transName) ? null : transName,
                            TransporterMode = string.IsNullOrEmpty(transMode) ? null : transMode,
                            Distance = DistanceHelper.GetDistanceWithCache(dbHelper, ewbRow),
                            TransporterDocNo = string.IsNullOrEmpty(transDocNo) ? null : transDocNo,
                            TransporterDocDate = string.IsNullOrEmpty(transDocDt) ? null : transDocDt,
                            VehicleNo = string.IsNullOrEmpty(vehNo) ? null : vehNo,
                            VehicleType = string.IsNullOrEmpty(vehType) ? "R" : vehType
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Log("Warning: Failed to fetch E-Way Bill details, omitting EwbDetails from payload. " + ex.Message);
            }

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string jsonPayload = JsonConvert.SerializeObject(payload, settings);

            // Extract the Target URL directly from the database response!
            string targetUrl = headerTable.Rows[0].Table.Columns.Contains("URL") ? headerTable.Rows[0]["URL"].ToString().Trim() : "https://gstsandbox.charteredinfo.com/eicore/dec/v1.03/Invoice";

            LoggerHelper.Log("Exited PayloadGenerator.GenerateCombinedPayload");
            return new Tuple<string, string>(jsonPayload, targetUrl);
        }

        public static Tuple<string, string> GenerateEWayBillPayload(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            LoggerHelper.Log("Entered PayloadGenerator.GenerateEWayBillPayload");
            LoggerHelper.Log($"Starting Standalone E-Way Bill Payload Generation for ObjType: {objType}, DocEntry: {docEntry}...");

            string sDocType = (objType == "13") ? "Invoice" : (objType == "14" ? "CreditMemo" : objType);
            
            string headerQuery = "";
            string detailQuery = "";
            
            if (objType == "67")
            {
                // Inventory Transfer
                headerQuery = $"CALL \"GIS_EwayBill_Transfer_Header\"('{docEntry}')";
                detailQuery = $"CALL \"GIS_EwayBill_Transfer_Detail\"('{docEntry}')";
            }
            else
            {
                // Legacy fallback for Credit Memo and everything else
                headerQuery = $"CALL \"GIS_EwayBill_GetPostDet\"('{docEntry}', '', '', '', '', '', '', '', '', '', '{sDocType}', 'Header')";
                detailQuery = $"CALL \"GIS_EwayBill_GetPostDet\"('{docEntry}', '', '', '', '', '', '', '', '', '', '{sDocType}', 'Detail')";
            }

            DataTable headerTable = dbHelper.ExecuteQuery(headerQuery);
            if (headerTable == null || headerTable.Rows.Count == 0) throw new Exception("No header data returned for E-Way Bill.");

            DataTable detailTable = dbHelper.ExecuteQuery(detailQuery);
            if (detailTable == null || detailTable.Rows.Count == 0) throw new Exception("No detail data returned for E-Way Bill.");

            LoggerHelper.Log("Mapping raw SAP data to E-Way Bill JSON...");
            
            // Dynamically map Header
            var payload = new System.Collections.Generic.Dictionary<string, object>();
            DataRow hRow = headerTable.Rows[0];
            foreach (DataColumn col in headerTable.Columns)
            {
                string colName = col.ColumnName;
                if (colName == "URL") continue; // Skip URL
                if (hRow[colName] != DBNull.Value)
                {
                    string val = hRow[colName].ToString().Trim();
                    // Basic numeric type conversions for proper JSON
                    if (colName == "transDistance" || colName.Contains("Pincode") || colName.Contains("StateCode") || colName == "transactionType" || colName == "transMode" || colName == "subSupplyType")
                    {
                        if (int.TryParse(val, out int num)) 
                        {
                            payload[colName] = num;
                        }
                        else 
                        {
                            if (colName == "transactionType") payload[colName] = 1; // Default transaction type
                            else if (colName == "transDistance") payload[colName] = DistanceHelper.GetDistanceWithCache(dbHelper, hRow); // Check cache
                            else if (string.IsNullOrEmpty(val)) { /* Omit completely from Dictionary to avoid 'Invalid Json' */ }
                            else payload[colName] = val; // Fallback
                        }
                    }
                    else
                    {
                        payload[colName] = val;
                    }
                }
            }

            // Map Details
            var itemList = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
            int itemCounter = 1;
            foreach (DataRow dRow in detailTable.Rows)
            {
                var item = new System.Collections.Generic.Dictionary<string, object>();
                foreach (DataColumn col in detailTable.Columns)
                {
                    string colName = col.ColumnName;
                    if (dRow[colName] != DBNull.Value)
                    {
                        string val = dRow[colName].ToString().Trim();
                        
                        if (colName == "itemNo")
                        {
                            item[colName] = itemCounter;
                            continue;
                        }

                        // Infer decimals
                        if (decimal.TryParse(val, out decimal decVal) && !colName.Contains("productName") && !colName.Contains("hsnCode"))
                            item[colName] = decVal;
                        else
                            item[colName] = val;
                    }
                }
                itemList.Add(item);
                itemCounter++;
            }

            payload["itemList"] = itemList;

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string jsonPayload = JsonConvert.SerializeObject(payload, settings);

            string targetUrl = "";
            
            // Fetch the GenEWay URL
            string spObjType = (objType == "13") ? "Invoice" : (objType == "14") ? "CreditMemo" : "Transfer";
            string sWhsQ = $"CALL \"TEC_GETWHSANDSTATE\"('{docEntry}','{spObjType}','WHS')";
            string whsCode = "";
            var dtWhs = dbHelper.ExecuteQuery(sWhsQ);
            if (dtWhs != null && dtWhs.Rows.Count > 0)
            {
                whsCode = dtWhs.Rows[0][0]?.ToString() ?? "";
            }

            string sStateQ = $"CALL \"TEC_GETWHSANDSTATE\"('{whsCode}','{spObjType}','State')";
            string stateCode = "";
            var dtState = dbHelper.ExecuteQuery(sStateQ);
            if (dtState != null && dtState.Rows.Count > 0)
            {
                stateCode = dtState.Rows[0][0]?.ToString() ?? "";
            }

            string urlQ = $"CALL \"TEC_EWAYLoginURL\"('GenerateEWay','{stateCode}')";
            DataTable urlDt = dbHelper.ExecuteQuery(urlQ);
            if (urlDt != null && urlDt.Rows.Count > 0)
            {
                targetUrl = urlDt.Rows[0]["URL"]?.ToString() ?? "";
            }

            if (string.IsNullOrEmpty(targetUrl)) throw new Exception("Target URL missing from database response.");

            LoggerHelper.Log("Exited PayloadGenerator.GenerateEWayBillPayload");
            return new Tuple<string, string>(jsonPayload, targetUrl);
        }

        public static Tuple<string, string> GenerateEWayBillCancelPayload(IDatabaseHelper dbHelper, string objType, string docEntry)
        {
            LoggerHelper.Log("Entered PayloadGenerator.GenerateEWayBillCancelPayload");
            LoggerHelper.Log($"Starting E-Way Bill Cancel Payload Generation for ObjType: {objType}, DocEntry: {docEntry}...");

            string tableName = (objType == "13") ? "OINV" : (objType == "14") ? "ORIN" : "OWTR";
            string query = $"SELECT \"U_ewayBNo\" FROM \"{tableName}\" WHERE \"DocEntry\"={docEntry}";
            DataTable dt = dbHelper.ExecuteQuery(query);

            if (dt == null || dt.Rows.Count == 0)
            {
                throw new Exception($"GenerateEWayBillCancelPayload: Document {docEntry} not found in {tableName}!");
            }

            string ewbNo = dt.Rows[0]["U_ewayBNo"].ToString().Trim();

            var cancelObj = new 
            { 
                action = "CANEWB", 
                ewbNo = long.Parse(ewbNo), 
                cancelRsnCode = 1, 
                cancelRmrk = "Cancelled" 
            };

            string jsonPayload = JsonConvert.SerializeObject(cancelObj);

            // Fetch the Cancel URL
            string spObjType = (objType == "13") ? "Invoice" : (objType == "14") ? "CreditMemo" : "Transfer";
            string sWhsQ = $"CALL \"TEC_GETWHSANDSTATE\"('{docEntry}','{spObjType}','WHS')";
            string whsCode = "";
            var dtWhs = dbHelper.ExecuteQuery(sWhsQ);
            if (dtWhs != null && dtWhs.Rows.Count > 0)
            {
                whsCode = dtWhs.Rows[0][0]?.ToString() ?? "";
            }

            string sStateQ = $"CALL \"TEC_GETWHSANDSTATE\"('{whsCode}','{spObjType}','State')";
            string stateCode = "";
            var dtState = dbHelper.ExecuteQuery(sStateQ);
            if (dtState != null && dtState.Rows.Count > 0)
            {
                stateCode = dtState.Rows[0][0]?.ToString() ?? "";
            }

            string urlQ = $"CALL \"TEC_EWAYLoginURL\"('CancelEWay','{stateCode}')";
            DataTable urlDt = dbHelper.ExecuteQuery(urlQ);
            string targetUrl = "";
            if (urlDt != null && urlDt.Rows.Count > 0)
            {
                targetUrl = urlDt.Rows[0]["URL"]?.ToString() ?? "";
            }
            
            if (string.IsNullOrEmpty(targetUrl)) throw new Exception("Target URL for CancelEWay missing.");

            LoggerHelper.Log("Exited PayloadGenerator.GenerateEWayBillCancelPayload");
            return new Tuple<string, string>(jsonPayload, targetUrl);
        }
    }
}