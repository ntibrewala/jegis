using System;
using GIS.Framework;
using GIS.Framework.Helpers;
using GIS.AddOn.Helpers;
using Newtonsoft.Json.Linq;

namespace GIS.AddOn
{
    internal class UIEventHandler
    {
        // Reference to the ConnectionManager to get the Application object
        private ConnectionManager _connectionManager;

        public UIEventHandler(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;

            SetFilters();

            // Subscribe to SAP UI events
            _connectionManager.SboApplication.ItemEvent += new SAPbouiCOM._IApplicationEvents_ItemEventEventHandler(SboApplication_ItemEvent);
            LoggerHelper.Log("UIEventHandler initialized and listening for SAP events.");
        }

        private void SetFilters()
        {
            SAPbouiCOM.EventFilters oFilters = new SAPbouiCOM.EventFilters();
            SAPbouiCOM.EventFilter oFilter;

            // 1. Filter for FORM_LOAD
            oFilter = oFilters.Add(SAPbouiCOM.BoEventTypes.et_FORM_LOAD);
            oFilter.AddEx("133");
            oFilter.AddEx("179");
            oFilter.AddEx("940");

            // 2. Filter for COMBO_SELECT
            oFilter = oFilters.Add(SAPbouiCOM.BoEventTypes.et_COMBO_SELECT);
            oFilter.AddEx("133");
            oFilter.AddEx("179");
            oFilter.AddEx("940");

            // 3. Filter for ITEM_PRESSED (Custom Forms)
            oFilter = oFilters.Add(SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED);
            oFilter.AddEx("GIS_OUPV");
            oFilter.AddEx("GIS_OEXT");
            oFilter.AddEx("GIS_OUPT");
            
            // 4. Filter for FORM_UNLOAD (Custom Forms)
            oFilter = oFilters.Add(SAPbouiCOM.BoEventTypes.et_FORM_UNLOAD);
            oFilter.AddEx("GIS_OUPV");
            oFilter.AddEx("GIS_OEXT");
            oFilter.AddEx("GIS_OUPT");

            _connectionManager.SboApplication.SetFilter(oFilters);
            LoggerHelper.Log("Event filters applied successfully.");
        }

        private bool ConfirmAction(string buttonCaption)
        {
            string msg = $"Are you sure you want to run the {buttonCaption} generation?";
            int userResponse = _connectionManager.SboApplication.MessageBox(msg, 1, "Yes", "No", "");
            return userResponse == 1;
        }

        private void SboApplication_ItemEvent(string FormUID, ref SAPbouiCOM.ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            try
            {
                // ROUTE EVENTS FOR CUSTOM ADD-ON FORMS
                if (pVal.FormTypeEx == "GIS_OUPV" || pVal.FormTypeEx == "GIS_OEXT" || pVal.FormTypeEx == "GIS_OUPT")
                {
                    if (pVal.EventType == SAPbouiCOM.BoEventTypes.et_FORM_UNLOAD) return; // Form is closing, ignore

                    var app = _connectionManager.SboApplication;
                    var comp = _connectionManager.Company;
                    var frm = app.Forms.Item(FormUID);

                    if (pVal.FormTypeEx == "GIS_OUPV")
                        UpdateVehicleForm.ItemEvent(ref app, ref comp, frm, ref pVal, ref BubbleEvent);
                    else if (pVal.FormTypeEx == "GIS_OEXT")
                        ExtendValidityForm.ItemEvent(ref app, ref comp, frm, ref pVal, ref BubbleEvent);
                    else if (pVal.FormTypeEx == "GIS_OUPT")
                        UpdateTransporterForm.ItemEvent(ref app, ref comp, frm, ref pVal, ref BubbleEvent);

                    return;
                }

                // DRAW UI ON FORM LOAD FOR INVOICE (133), CREDIT MEMO (179), and INVENTORY TRANSFER (940)
                if ((pVal.FormTypeEx == "133" || pVal.FormTypeEx == "179" || pVal.FormTypeEx == "940") && pVal.EventType == SAPbouiCOM.BoEventTypes.et_FORM_LOAD && !pVal.BeforeAction)
                {
                    var oForm = _connectionManager.SboApplication.Forms.Item(FormUID);
                    UIHelper.DrawLegacyButtonCombos(oForm);
                    return;
                }

                // LISTEN FOR COMBOBOX SELECTION ON THE MAIN FORM (133, 179, 940) FOR OUR CUSTOM BUTTON COMBOS
                if ((pVal.FormTypeEx == "133" || pVal.FormTypeEx == "179" || pVal.FormTypeEx == "940") && pVal.EventType == SAPbouiCOM.BoEventTypes.et_COMBO_SELECT && !pVal.BeforeAction)
                {
                    if (pVal.ItemUID == "btnComb" || pVal.ItemUID == "btnEWay" || pVal.ItemUID == "btnEInv")
                    {
                        var oForm = _connectionManager.SboApplication.Forms.Item(FormUID);
                        var combo = (SAPbouiCOM.ButtonCombo)oForm.Items.Item(pVal.ItemUID).Specific;
                        
                        string selectedValue = "";
                        try { selectedValue = combo.Selected.Value.Trim(); } catch { return; }

                        if (string.IsNullOrEmpty(selectedValue) || selectedValue.ToLower() == "combined" || selectedValue.ToLower() == "ewaybill" || selectedValue.ToLower() == "e-invoice")
                        {
                            return; // Ignored default placeholder values
                        }

                        // Determine the Action Type based on the combobox and selected value
                        string actionName = "";
                        if (pVal.ItemUID == "btnComb" && selectedValue == "Generate") actionName = "Generate Combined";
                        
                        if (pVal.ItemUID == "btnEWay" && selectedValue == "Generate") actionName = "Generate E-Way Bill";
                        if (pVal.ItemUID == "btnEWay" && selectedValue == "Cancel") actionName = "Cancel E-Way Bill";
                        if (pVal.ItemUID == "btnEWay" && selectedValue == "Inv Transfer") actionName = "Inventory Transfer";
                        if (pVal.ItemUID == "btnEWay" && selectedValue == "UpdateVehicle") actionName = "Update Part B";
                        if (pVal.ItemUID == "btnEWay" && selectedValue == "ExtendValidity") actionName = "Extend Validity";
                        if (pVal.ItemUID == "btnEWay" && selectedValue == "UpdateTrans") actionName = "Update Transporter";
                        
                        if (pVal.ItemUID == "btnEInv" && selectedValue == "Generate") actionName = "Generate E-Invoice";
                        if (pVal.ItemUID == "btnEInv" && selectedValue == "Cancel") actionName = "Cancel E-Invoice";

                        if (string.IsNullOrEmpty(actionName)) return;

                        // Reusable Confirmation Helper
                        if (!ConfirmAction(actionName))
                        {
                            // Reset the combobox back to default (0 index) if user clicks No
                            combo.Select(0, SAPbouiCOM.BoSearchKey.psk_Index);
                            return;
                        }

                        // Retrieve the internal DocEntry
                        string objType = (pVal.FormTypeEx == "133") ? "13" : (pVal.FormTypeEx == "179") ? "14" : "67";
                        string tableName = (objType == "13") ? "OINV" : (objType == "14") ? "ORIN" : "OWTR";
                        string docEntry = oForm.DataSources.DBDataSources.Item(tableName).GetValue("DocEntry", 0).Trim();

                        if (string.IsNullOrEmpty(docEntry))
                        {
                            _connectionManager.SboApplication.MessageBox("Please Add the document first!");
                            combo.Select(0, SAPbouiCOM.BoSearchKey.psk_Index);
                            return;
                        }

                        if (actionName == "Generate Combined")
                        {
                            LoggerHelper.Log($"GIS ButtonCombo Selected! Initializing Combined Generation for DocEntry: {docEntry}...");
                            var dbHelper = new DatabaseHelper(_connectionManager.Company);
                            _connectionManager.SboApplication.StatusBar.SetText("Generating E-Invoice, please wait...", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            
                            var result = CombinedEngine.ProcessCombined(dbHelper, objType, docEntry);
                            string jsonPayload = result.Item1;
                            string apiRes = result.Item2;

                            // Parse the API Response to extract exact errors
                            try
                            {
                                JObject jObj = JObject.Parse(apiRes);
                                string status = jObj["Status"]?.ToString() ?? "";
                                
                                if (status == "0" || status.ToLower() == "false")
                                {
                                    // Total Failure
                                    string errorMsg = "Combined Failed.";
                                    if (jObj["ErrorDetails"] != null && jObj["ErrorDetails"].HasValues)
                                    {
                                        errorMsg = jObj["ErrorDetails"][0]["ErrorMessage"]?.ToString() ?? errorMsg;
                                    }
                                    _connectionManager.SboApplication.MessageBox($"API Error: {errorMsg}");
                                    _connectionManager.SboApplication.StatusBar.SetText($"API Error: {errorMsg}", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                                }
                                else if (status == "1" || status.ToLower() == "true")
                                {
                                    // Success for IRN, check if E-Way Bill threw a warning
                                    bool hasEwbError = false;
                                    string ewbErrorMsg = "";

                                    if (jObj["InfoDtls"] != null && jObj["InfoDtls"].HasValues)
                                    {
                                        string infCd = jObj["InfoDtls"][0]["InfCd"]?.ToString() ?? "";
                                        if (infCd == "DUPIRN")
                                        {
                                            _connectionManager.SboApplication.StatusBar.SetText("Duplicate IRN Detected! Successfully recovered previous IRN details.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                                            return;
                                        }
                                        else if (infCd == "EWBERR")
                                        {
                                            hasEwbError = true;
                                            var descArray = jObj["InfoDtls"][0]["Desc"] as JArray;
                                            if (descArray != null && descArray.Count > 0)
                                            {
                                                ewbErrorMsg = descArray[0]["ErrorMessage"]?.ToString() ?? "E-Way Bill Generation Failed.";
                                            }
                                            else
                                            {
                                                ewbErrorMsg = jObj["InfoDtls"][0]["Desc"]?.ToString() ?? "E-Way Bill Generation Failed.";
                                            }
                                        }
                                    }

                                    if (hasEwbError)
                                    {
                                        _connectionManager.SboApplication.MessageBox($"E-Invoice generated successfully, BUT E-Way Bill failed:\n\n{ewbErrorMsg}");
                                    }
                                    else
                                    {
                                        _connectionManager.SboApplication.StatusBar.SetText("Combined Generated Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                                    }
                                }
                            }
                            catch (Exception parseEx)
                            {
                                LoggerHelper.Log($"Failed to parse API Response in UI: {parseEx.Message}");
                                _connectionManager.SboApplication.StatusBar.SetText("Combined Generation Processed, but response could not be read.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            }
                            finally
                            {
                                GIS.Framework.Helpers.ResponseHandler.RouteResponse("combined", dbHelper, objType, docEntry, jsonPayload, apiRes);
                            }
                        }
                        else if (actionName == "Generate E-Invoice")
                        {
                            LoggerHelper.Log($"GIS ButtonCombo Selected! Initializing Standalone E-Invoice Generation for DocEntry: {docEntry}...");
                            var dbHelper = new DatabaseHelper(_connectionManager.Company);
                            _connectionManager.SboApplication.StatusBar.SetText("Generating Standalone E-Invoice, please wait...", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            
                            var result = EInvoiceEngine.ProcessEInvoice(dbHelper, objType, docEntry);
                            string jsonPayload = result.Item1;
                            string apiRes = result.Item2;

                            // Parse the API Response to extract exact errors
                            try
                            {
                                JObject jObj = JObject.Parse(apiRes);
                                string status = jObj["Status"]?.ToString() ?? "";
                                
                                if (status == "0" || status.ToLower() == "false")
                                {
                                    // Total Failure
                                    string errorMsg = "Standalone E-Invoice Failed.";
                                    if (jObj["ErrorDetails"] != null && jObj["ErrorDetails"].HasValues)
                                    {
                                        errorMsg = jObj["ErrorDetails"][0]["ErrorMessage"]?.ToString() ?? errorMsg;
                                    }
                                    _connectionManager.SboApplication.MessageBox($"API Error: {errorMsg}");
                                    _connectionManager.SboApplication.StatusBar.SetText($"API Error: {errorMsg}", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                                }
                                else if (status == "1" || status.ToLower() == "true")
                                {
                                    _connectionManager.SboApplication.StatusBar.SetText("Standalone E-Invoice Generated Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                                }
                            }
                            catch (Exception parseEx)
                            {
                                LoggerHelper.Log($"Failed to parse API Response in UI: {parseEx.Message}");
                                _connectionManager.SboApplication.StatusBar.SetText("Standalone E-Invoice Processed, but response could not be read.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            }
                            finally
                            {
                                GIS.Framework.Helpers.IResponseHandler responseHandler = new GIS.Framework.Helpers.CombinedResponseHandler();
                                responseHandler.Handle(dbHelper, objType, docEntry, jsonPayload, apiRes);
                            }
                        }
                        else if (actionName == "Cancel E-Invoice")
                        {
                            LoggerHelper.Log($"GIS ButtonCombo Selected! Initializing Cancel E-Invoice for DocEntry: {docEntry}...");
                            var dbHelper = new DatabaseHelper(_connectionManager.Company);
                            _connectionManager.SboApplication.StatusBar.SetText("Cancelling E-Invoice, please wait...", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);

                            var result = CancelEngine.ProcessCancel(dbHelper, objType, docEntry);
                            string jsonPayload = result.Item1;
                            string apiRes = result.Item2;

                            try
                            {
                                JObject jObj = JObject.Parse(apiRes);
                                string status = jObj["Status"]?.ToString() ?? "";

                                if (status == "0" || status.ToLower() == "false")
                                {
                                    string errorMsg = "Cancellation Failed.";
                                    if (jObj["ErrorDetails"] != null && jObj["ErrorDetails"].HasValues)
                                    {
                                        errorMsg = jObj["ErrorDetails"][0]["ErrorMessage"]?.ToString() ?? errorMsg;
                                    }
                                    _connectionManager.SboApplication.MessageBox($"API Error: {errorMsg}");
                                    _connectionManager.SboApplication.StatusBar.SetText($"API Error: {errorMsg}", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                                }
                                else
                                {
                                    _connectionManager.SboApplication.StatusBar.SetText("E-Invoice Cancelled Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                                }
                            }
                            catch (Exception parseEx)
                            {
                                LoggerHelper.Log($"Failed to parse Cancel Response in UI: {parseEx.Message}");
                                _connectionManager.SboApplication.StatusBar.SetText("Cancellation Processed, but response could not be read.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            }
                            finally
                            {
                                GIS.Framework.Helpers.CancelResponseHandler.RouteCancelResponse(dbHelper, objType, docEntry, jsonPayload, apiRes);
                            }
                        }
                        else if (actionName == "Generate E-Way Bill")
                        {
                            LoggerHelper.Log($"GIS ButtonCombo Selected! Initializing Standalone E-Way Bill for DocEntry: {docEntry}...");
                            var dbHelper = new DatabaseHelper(_connectionManager.Company);
                            _connectionManager.SboApplication.StatusBar.SetText("Generating E-Way Bill, please wait...", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            
                            var result = EWayBillEngine.ProcessEWayBill(dbHelper, objType, docEntry);
                            string jsonPayload = result.Item1;
                            string apiRes = result.Item2;

                            try
                            {
                                JObject jObj = JObject.Parse(apiRes);
                                
                                if (jObj["error"] != null || jObj["status_cd"]?.ToString() == "0")
                                {
                                    string errorMsg = jObj["error"]?["message"]?.ToString() ?? "Unknown API Error";
                                    _connectionManager.SboApplication.StatusBar.SetText($"API Error: {errorMsg}", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                                }
                                else if (jObj["ewayBillNo"] != null)
                                {
                                    _connectionManager.SboApplication.StatusBar.SetText("E-Way Bill Generated Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                                }
                                else
                                {
                                    _connectionManager.SboApplication.StatusBar.SetText($"Unexpected Response: {apiRes}", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                                }
                            }
                            catch (Exception parseEx)
                            {
                                LoggerHelper.Log($"Failed to parse API Response: {parseEx.Message}");
                                _connectionManager.SboApplication.StatusBar.SetText("E-Way Bill Processed, but response could not be read.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            }
                            finally
                            {
                                GIS.Framework.Helpers.IResponseHandler responseHandler = new GIS.Framework.Helpers.EWayBillResponseHandler();
                                responseHandler.Handle(dbHelper, objType, docEntry, jsonPayload, apiRes);
                            }
                        }
                        else if (actionName == "Cancel E-Way Bill")
                        {
                            LoggerHelper.Log($"GIS ButtonCombo Selected! Initializing Cancel E-Way Bill for DocEntry: {docEntry}...");
                            var dbHelper = new DatabaseHelper(_connectionManager.Company);
                            _connectionManager.SboApplication.StatusBar.SetText("Cancelling E-Way Bill, please wait...", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);

                            var result = EWayBillCancelEngine.ProcessCancel(dbHelper, objType, docEntry);
                            string jsonPayload = result.Item1;
                            string apiRes = result.Item2;

                            try
                            {
                                JObject jObj = JObject.Parse(apiRes);
                                
                                if (jObj["error"] != null || jObj["status_cd"]?.ToString() == "0")
                                {
                                    string errorMsg = jObj["error"]?["message"]?.ToString() ?? "Unknown API Error";
                                    _connectionManager.SboApplication.StatusBar.SetText($"API Error: {errorMsg}", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                                }
                                else if (jObj["ewayBillNo"] != null)
                                {
                                    _connectionManager.SboApplication.StatusBar.SetText("E-Way Bill Cancelled Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                                }
                                else
                                {
                                    _connectionManager.SboApplication.StatusBar.SetText($"Unexpected Response: {apiRes}", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                                }
                            }
                            catch (Exception parseEx)
                            {
                                LoggerHelper.Log($"Failed to parse Cancel Response: {parseEx.Message}");
                                _connectionManager.SboApplication.StatusBar.SetText("Cancellation Processed, but response could not be read.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            }
                            finally
                            {
                                GIS.Framework.Helpers.IResponseHandler responseHandler = new GIS.Framework.Helpers.EWayBillCancelResponseHandler();
                                responseHandler.Handle(dbHelper, objType, docEntry, jsonPayload, apiRes);
                            }
                        }
                        else if (actionName == "Inventory Transfer")
                        {
                            LoggerHelper.Log($"GIS ButtonCombo Selected! Initializing Inventory Transfer E-Way Bill for DocEntry: {docEntry}...");
                            var dbHelper = new DatabaseHelper(_connectionManager.Company);
                            _connectionManager.SboApplication.StatusBar.SetText("Generating Inventory Transfer E-Way Bill, please wait...", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            
                            // It uses the same engine but the PayloadGenerator will map it correctly based on objType 67
                            var result = EWayBillEngine.ProcessEWayBill(dbHelper, objType, docEntry);
                            string jsonPayload = result.Item1;
                            string apiRes = result.Item2;

                            // We will process the response exactly like Generate E-Way Bill
                            try
                            {
                                JObject jObj = JObject.Parse(apiRes);
                                string status = jObj["Status"]?.ToString() ?? "";
                                
                                if (status == "0" || status.ToLower() == "false")
                                {
                                    string errorMsg = "E-Way Bill Failed.";
                                    if (jObj["ErrorDetails"] != null && jObj["ErrorDetails"].HasValues)
                                        errorMsg = jObj["ErrorDetails"][0]["ErrorMessage"]?.ToString() ?? errorMsg;
                                    _connectionManager.SboApplication.MessageBox($"API Error: {errorMsg}");
                                    _connectionManager.SboApplication.StatusBar.SetText($"API Error: {errorMsg}", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                                }
                                else
                                {
                                    _connectionManager.SboApplication.StatusBar.SetText("Inventory Transfer E-Way Bill Generated Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                                }
                            }
                            catch (Exception parseEx)
                            {
                                LoggerHelper.Log($"Failed to parse API Response: {parseEx.Message}");
                                _connectionManager.SboApplication.StatusBar.SetText("E-Way Bill Processed, but response could not be read.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            }
                            finally
                            {
                                GIS.Framework.Helpers.IResponseHandler responseHandler = new GIS.Framework.Helpers.EWayBillResponseHandler();
                                responseHandler.Handle(dbHelper, objType, docEntry, jsonPayload, apiRes);
                            }
                        }
                        else if (actionName == "Update Part B")
                        {
                            string ewayNo = oForm.DataSources.DBDataSources.Item(tableName).GetValue("U_ewayBNo", 0).Trim();
                            if (string.IsNullOrEmpty(ewayNo) || ewayNo.IndexOf("Cancelled", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _connectionManager.SboApplication.StatusBar.SetText("Update Part B cannot run because the E-Way Bill is empty, null, or cancelled.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                            }
                            else
                            {
                                UpdateVehicleForm.LoadForm(_connectionManager.SboApplication, _connectionManager.Company, docEntry, objType, ewayNo, oForm.UniqueID);
                            }
                        }
                        else if (actionName == "Extend Validity")
                        {
                            string ewayNo = oForm.DataSources.DBDataSources.Item(tableName).GetValue("U_ewayBNo", 0).Trim();
                            if (string.IsNullOrEmpty(ewayNo) || ewayNo.IndexOf("Cancelled", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _connectionManager.SboApplication.StatusBar.SetText("Extend Validity cannot run because the E-Way Bill is empty, null, or cancelled.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                            }
                            else
                            {
                                ExtendValidityForm.LoadForm(_connectionManager.SboApplication, _connectionManager.Company, docEntry, objType, ewayNo, oForm.UniqueID);
                            }
                        }
                        else if (actionName == "Update Transporter")
                        {
                            string ewayNo = oForm.DataSources.DBDataSources.Item(tableName).GetValue("U_ewayBNo", 0).Trim();
                            if (string.IsNullOrEmpty(ewayNo) || ewayNo.IndexOf("Cancelled", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _connectionManager.SboApplication.StatusBar.SetText("Update Transporter cannot run because the E-Way Bill is empty, null, or cancelled.", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                            }
                            else
                            {
                                UpdateTransporterForm.LoadForm(_connectionManager.SboApplication, _connectionManager.Company, docEntry, objType, ewayNo, oForm.UniqueID);
                            }
                        }
                        else
                        {
                            _connectionManager.SboApplication.MessageBox($"The action '{actionName}' is not implemented yet!");
                        }

                        // Reset the combobox back to default after execution
                        combo.Select(0, SAPbouiCOM.BoSearchKey.psk_Index);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("MISSING_DISTANCE|"))
                {
                    var parts = ex.Message.Split('|');
                    if (parts.Length == 4)
                    {
                        string routeHash = parts[1];
                        string fromAddr = parts[2];
                        string toAddr = parts[3];
                        
                        string promptMsg = $"Local Route Detected (Same Pincodes).\nPlease enter the exact distance in Kilometers between:\n\nFROM: {fromAddr}\nTO: {toAddr}";
                        string inputDist = ShowInputDialog(promptMsg, "Distance Required");
                        
                        if (!string.IsNullOrEmpty(inputDist) && int.TryParse(inputDist, out int dist))
                        {
                            try
                            {
                                var dbH = new DatabaseHelper(_connectionManager.Company);
                                dbH.ExecuteQuery($@"INSERT INTO ""GIS_DIST_CACHE"" (""Code"", ""FromAddr"", ""ToAddr"", ""Distance"") VALUES ('{routeHash}', '{fromAddr.Replace("'", "''")}', '{toAddr.Replace("'", "''")}', {dist})");
                                _connectionManager?.SboApplication?.MessageBox("Distance saved to cache! Please click Generate again.");
                            }
                            catch (Exception saveEx)
                            {
                                _connectionManager?.SboApplication?.MessageBox($"Failed to save distance: {saveEx.Message}");
                            }
                        }
                        else
                        {
                            _connectionManager?.SboApplication?.StatusBar?.SetText("Distance entry cancelled or invalid.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                        }
                    }
                }
                else
                {
                    LoggerHelper.Log($"Error in UI Event: {ex.Message}");
                    _connectionManager?.SboApplication?.StatusBar?.SetText($"Error: {ex.Message}", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                }
            }
        }

        private string ShowInputDialog(string text, string caption)
        {
            string result = "";
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                System.Windows.Forms.Form prompt = new System.Windows.Forms.Form()
                {
                    Width = 450,
                    Height = 280,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    Text = caption,
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                    TopMost = true, // Forces window to be above SAP
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                System.Windows.Forms.Label textLabel = new System.Windows.Forms.Label() { Left = 20, Top = 20, Width = 400, Height = 120, Text = text, Font = new System.Drawing.Font("Arial", 10) };
                System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox() { Left = 20, Top = 150, Width = 400, Font = new System.Drawing.Font("Arial", 12), Text = "15" };
                System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button() { Text = "OK", Left = 320, Width = 100, Top = 190, DialogResult = System.Windows.Forms.DialogResult.OK };
                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(textLabel);
                prompt.AcceptButton = confirmation;

                if (prompt.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    result = textBox.Text;
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join(); // Wait for the user to close the form

            return result;
        }
    }
}
