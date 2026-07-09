using GIS.Framework;
using GIS.Framework.Helpers;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;
using System;

namespace GIS.AddOn
{
    class ExtendValidityForm
    {
        public static void LoadForm(SAPbouiCOM.Application oApplication, SAPbobsCOM.Company oCompany, string docEntry, string docType, string ewayNo, string parentFormUID)
        {
            try
            {

                SAPbouiCOM.FormCreationParams fcp = (SAPbouiCOM.FormCreationParams)oApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams);
                fcp.UniqueID = "frmExtVld" + DateTime.Now.ToString("mmss");
                fcp.FormType = "GIS_OEXT";
                fcp.BorderStyle = SAPbouiCOM.BoFormBorderStyle.fbs_Fixed;
                SAPbouiCOM.Form oForm = oApplication.Forms.AddEx(fcp);

                oForm.Title = "Extend E-Way Bill Validity";
                oForm.Width = 420;
                oForm.Height = 450;

                // Hidden context fields
                SAPbouiCOM.Item itmDocE = oForm.Items.Add("txtDocE", SAPbouiCOM.BoFormItemTypes.it_EDIT);
                itmDocE.Left = -100; itmDocE.Top = -100; itmDocE.Width = 10; itmDocE.Height = 10;
                itmDocE.Visible = false;
                ((SAPbouiCOM.EditText)itmDocE.Specific).Value = docEntry;

                SAPbouiCOM.Item itmDocT = oForm.Items.Add("txtDocT", SAPbouiCOM.BoFormItemTypes.it_EDIT);
                itmDocT.Left = -100; itmDocT.Top = -100; itmDocT.Width = 10; itmDocT.Height = 10;
                itmDocT.Visible = false;
                ((SAPbouiCOM.EditText)itmDocT.Specific).Value = docType;

                SAPbouiCOM.Item itmParent = oForm.Items.Add("txtPar", SAPbouiCOM.BoFormItemTypes.it_EDIT);
                itmParent.Left = -100; itmParent.Top = -100; itmParent.Width = 10; itmParent.Height = 10;
                itmParent.Visible = false;
                ((SAPbouiCOM.EditText)itmParent.Specific).Value = parentFormUID;

                SAPbouiCOM.Item itmEwb = oForm.Items.Add("txtEwb", SAPbouiCOM.BoFormItemTypes.it_EDIT);
                itmEwb.Left = -100; itmEwb.Top = -100; itmEwb.Width = 10; itmEwb.Height = 10;
                itmEwb.Visible = false;
                ((SAPbouiCOM.EditText)itmEwb.Specific).Value = ewayNo;

                // EWB Number (Read Only Display)
                AddStatic(oForm, "lblEwb", 10, 10, 110, 14, "E-Way Bill No");
                AddStatic(oForm, "lblEwbR", 125, 10, 190, 14, ewayNo);

                // Current Place
                AddStatic(oForm, "lblPlace", 10, 30, 110, 14, "Current Place");
                AddEdit(oForm, "txtPlace", 125, 30, 190, 14);

                // Current Pincode
                AddStatic(oForm, "lblPin", 10, 50, 110, 14, "Current Pincode");
                AddEdit(oForm, "txtPin", 125, 50, 190, 14);

                // Current State
                AddStatic(oForm, "lblState", 10, 70, 110, 14, "Current State");
                SAPbouiCOM.Item itmState = oForm.Items.Add("cmbState", SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX);
                itmState.Left = 125; itmState.Top = 70; itmState.Width = 190; itmState.Height = 14;
                SAPbouiCOM.ComboBox cmbState = (SAPbouiCOM.ComboBox)itmState.Specific;
                cmbState.ValidValues.Add("01", "Jammu & Kashmir"); cmbState.ValidValues.Add("02", "Himachal Pradesh");
                cmbState.ValidValues.Add("03", "Punjab"); cmbState.ValidValues.Add("04", "Chandigarh");
                cmbState.ValidValues.Add("05", "Uttarakhand"); cmbState.ValidValues.Add("06", "Haryana");
                cmbState.ValidValues.Add("07", "Delhi"); cmbState.ValidValues.Add("08", "Rajasthan");
                cmbState.ValidValues.Add("09", "Uttar Pradesh"); cmbState.ValidValues.Add("10", "Bihar");
                cmbState.ValidValues.Add("11", "Sikkim"); cmbState.ValidValues.Add("12", "Arunachal Pradesh");
                cmbState.ValidValues.Add("13", "Nagaland"); cmbState.ValidValues.Add("14", "Manipur");
                cmbState.ValidValues.Add("15", "Mizoram"); cmbState.ValidValues.Add("16", "Tripura");
                cmbState.ValidValues.Add("17", "Meghalaya"); cmbState.ValidValues.Add("18", "Assam");
                cmbState.ValidValues.Add("19", "West Bengal"); cmbState.ValidValues.Add("20", "Jharkhand");
                cmbState.ValidValues.Add("21", "Odisha"); cmbState.ValidValues.Add("22", "Chhattisgarh");
                cmbState.ValidValues.Add("23", "Madhya Pradesh"); cmbState.ValidValues.Add("24", "Gujarat");
                cmbState.ValidValues.Add("25", "Daman & Diu"); cmbState.ValidValues.Add("26", "Dadra & Nagar Haveli");
                cmbState.ValidValues.Add("27", "Maharashtra"); cmbState.ValidValues.Add("28", "Andhra Pradesh (Old)");
                cmbState.ValidValues.Add("29", "Karnataka"); cmbState.ValidValues.Add("30", "Goa");
                cmbState.ValidValues.Add("31", "Lakshadweep"); cmbState.ValidValues.Add("32", "Kerala");
                cmbState.ValidValues.Add("33", "Tamil Nadu"); cmbState.ValidValues.Add("34", "Puducherry");
                cmbState.ValidValues.Add("35", "Andaman & Nicobar Islands"); cmbState.ValidValues.Add("36", "Telangana");
                cmbState.ValidValues.Add("37", "Andhra Pradesh (New)"); cmbState.ValidValues.Add("38", "Ladakh");
                cmbState.Select("24", SAPbouiCOM.BoSearchKey.psk_ByValue);

                // Remaining Distance
                AddStatic(oForm, "lblDist", 10, 90, 110, 14, "Remaining Distance");
                AddEdit(oForm, "txtDist", 125, 90, 190, 14);

                // Reason Dropdown
                AddStatic(oForm, "lblRsn", 10, 110, 110, 14, "Extension Reason");
                SAPbouiCOM.Item itmRsn = oForm.Items.Add("cmbRsn", SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX);
                itmRsn.Left = 125; itmRsn.Top = 110; itmRsn.Width = 190; itmRsn.Height = 14;
                SAPbouiCOM.ComboBox cmbRsn = (SAPbouiCOM.ComboBox)itmRsn.Specific;
                cmbRsn.ValidValues.Add("1", "Natural Calamity");
                cmbRsn.ValidValues.Add("2", "Law and Order");
                cmbRsn.ValidValues.Add("3", "Transshipment");
                cmbRsn.ValidValues.Add("4", "Accident");
                cmbRsn.ValidValues.Add("5", "Other");
                cmbRsn.Select("3", SAPbouiCOM.BoSearchKey.psk_ByValue);

                // Remarks
                AddStatic(oForm, "lblRem", 10, 130, 110, 14, "Remarks");
                AddEdit(oForm, "txtRem", 125, 130, 190, 14);

                // Consignment Status
                AddStatic(oForm, "lblCons", 10, 150, 110, 14, "Consignment Status");
                SAPbouiCOM.Item itmCons = oForm.Items.Add("cmbCons", SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX);
                itmCons.Left = 125; itmCons.Top = 150; itmCons.Width = 190; itmCons.Height = 14;
                SAPbouiCOM.ComboBox cmbCons = (SAPbouiCOM.ComboBox)itmCons.Specific;
                cmbCons.ValidValues.Add("M", "In Movement");
                cmbCons.ValidValues.Add("T", "In Transit");
                cmbCons.Select("M", SAPbouiCOM.BoSearchKey.psk_ByValue);

                // Transit Type
                AddStatic(oForm, "lblTran", 10, 170, 110, 14, "Transit Type");
                SAPbouiCOM.Item itmTran = oForm.Items.Add("cmbTran", SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX);
                itmTran.Left = 125; itmTran.Top = 170; itmTran.Width = 190; itmTran.Height = 14;
                SAPbouiCOM.ComboBox cmbTran = (SAPbouiCOM.ComboBox)itmTran.Specific;
                cmbTran.ValidValues.Add("-", "None");
                cmbTran.ValidValues.Add("R", "Road");
                cmbTran.ValidValues.Add("W", "Warehouse");
                cmbTran.ValidValues.Add("O", "Others");
                cmbTran.Select("-", SAPbouiCOM.BoSearchKey.psk_ByValue);

                // Address Lines (For Transit)
                AddStatic(oForm, "lblAdd1", 10, 190, 110, 14, "Address Line 1");
                AddEdit(oForm, "txtAdd1", 125, 190, 190, 14);

                AddStatic(oForm, "lblAdd2", 10, 210, 110, 14, "Address Line 2");
                AddEdit(oForm, "txtAdd2", 125, 210, 190, 14);

                AddStatic(oForm, "lblAdd3", 10, 230, 110, 14, "Address Line 3");
                AddEdit(oForm, "txtAdd3", 125, 230, 190, 14);

                // Trans Mode
                AddStatic(oForm, "lblMode", 10, 250, 110, 14, "Transport Mode");
                SAPbouiCOM.Item itmMode = oForm.Items.Add("cmbMode", SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX);
                itmMode.Left = 125; itmMode.Top = 250; itmMode.Width = 190; itmMode.Height = 14;
                SAPbouiCOM.ComboBox cmbMode = (SAPbouiCOM.ComboBox)itmMode.Specific;
                cmbMode.ValidValues.Add("1", "Road");
                cmbMode.ValidValues.Add("2", "Rail");
                cmbMode.ValidValues.Add("3", "Air");
                cmbMode.ValidValues.Add("4", "Ship");
                cmbMode.ValidValues.Add("5", "InTransit");
                cmbMode.Select("1", SAPbouiCOM.BoSearchKey.psk_ByValue);

                // Vehicle No
                AddStatic(oForm, "lblVeh", 10, 270, 110, 14, "Vehicle No");
                AddEdit(oForm, "txtVeh", 125, 270, 190, 14);

                // Trans Doc No
                AddStatic(oForm, "lblDocNo", 10, 290, 110, 14, "Trans Doc No");
                AddEdit(oForm, "txtDocNo", 125, 290, 190, 14);

                // Trans Doc Date
                AddStatic(oForm, "lblDocDt", 10, 310, 110, 14, "Trans Doc Date");
                AddEdit(oForm, "txtDocDt", 125, 310, 190, 14);
                ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocDt").Specific).Value = DateTime.Now.ToString("dd/MM/yyyy");

                // Submit Button
                SAPbouiCOM.Item itmBtn = oForm.Items.Add("btnSub", SAPbouiCOM.BoFormItemTypes.it_BUTTON);
                itmBtn.Left = 125; itmBtn.Top = 340; itmBtn.Width = 80; itmBtn.Height = 20;
                ((SAPbouiCOM.Button)itmBtn.Specific).Caption = "Extend";

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                oApplication.StatusBar.SetText("Extend Form Error: " + ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
        }

        public static void ItemEvent(ref SAPbouiCOM.Application oApplication, ref SAPbobsCOM.Company oCompany, SAPbouiCOM.Form oForm, ref SAPbouiCOM.ItemEvent pVal, ref bool BubbleEvent)
        {
            BubbleEvent = true;
            try
            {
                if (pVal.BeforeAction == false)
                {
                    if (pVal.EventType == SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED)
                    {
                        if (pVal.ItemUID == "btnSub")
                        {
                            oApplication.StatusBar.SetText("Extend button clicked - calling SubmitData...", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                            SubmitData(oApplication, oCompany, oForm);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                oApplication.StatusBar.SetText("ExtendValidity Event Error: " + ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
        }

        private static void AddStatic(SAPbouiCOM.Form form, string uid, int l, int t, int w, int h, string cap)
        {
            SAPbouiCOM.Item itm = form.Items.Add(uid, SAPbouiCOM.BoFormItemTypes.it_STATIC);
            itm.Left = l; itm.Top = t; itm.Width = w; itm.Height = h;
            ((SAPbouiCOM.StaticText)itm.Specific).Caption = cap;
        }

        private static void AddEdit(SAPbouiCOM.Form form, string uid, int l, int t, int w, int h)
        {
            SAPbouiCOM.Item itm = form.Items.Add(uid, SAPbouiCOM.BoFormItemTypes.it_EDIT);
            itm.Left = l; itm.Top = t; itm.Width = w; itm.Height = h;
        }

        private static void SubmitData(SAPbouiCOM.Application oApplication, SAPbobsCOM.Company oCompany, SAPbouiCOM.Form oForm)
        {
            try
            {
                oApplication.StatusBar.SetText("Processing Extension...", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);

                string ewayNo = ((SAPbouiCOM.EditText)oForm.Items.Item("txtEwb").Specific).Value;
                string vehNo = ((SAPbouiCOM.EditText)oForm.Items.Item("txtVeh").Specific).Value;
                string place = ((SAPbouiCOM.EditText)oForm.Items.Item("txtPlace").Specific).Value;
                string pin = ((SAPbouiCOM.EditText)oForm.Items.Item("txtPin").Specific).Value;
                string payloadState = ((SAPbouiCOM.ComboBox)oForm.Items.Item("cmbState").Specific).Selected.Value;
                string distance = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDist").Specific).Value;
                string reasonCode = ((SAPbouiCOM.ComboBox)oForm.Items.Item("cmbRsn").Specific).Selected.Value;
                string remarks = ((SAPbouiCOM.EditText)oForm.Items.Item("txtRem").Specific).Value;
                string consStat = ((SAPbouiCOM.ComboBox)oForm.Items.Item("cmbCons").Specific).Selected.Value;
                string tranType = ((SAPbouiCOM.ComboBox)oForm.Items.Item("cmbTran").Specific).Selected.Value;
                if (tranType == "-") tranType = "";
                string transMode = ((SAPbouiCOM.ComboBox)oForm.Items.Item("cmbMode").Specific).Selected.Value;
                string docNo = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocNo").Specific).Value;
                string docDt = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocDt").Specific).Value;
                string add1 = ((SAPbouiCOM.EditText)oForm.Items.Item("txtAdd1").Specific).Value;
                string add2 = ((SAPbouiCOM.EditText)oForm.Items.Item("txtAdd2").Specific).Value;
                string add3 = ((SAPbouiCOM.EditText)oForm.Items.Item("txtAdd3").Specific).Value;

                string sourceDocEntry = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocE").Specific).Value;
                string sourceDocType = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocT").Specific).Value;
                string parentFormUID = ((SAPbouiCOM.EditText)oForm.Items.Item("txtPar").Specific).Value;

                // NIC API Validations for Transit Type
                if (transMode == "5")
                {
                    consStat = "T";
                    if (string.IsNullOrWhiteSpace(tranType))
                    {
                        oApplication.StatusBar.SetText("Transit Type (R/W/O) is mandatory when mode is InTransit.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                        return;
                    }
                }
                else
                {
                    consStat = "M";
                    tranType = "";
                    add1 = ""; add2 = ""; add3 = "";
                }

                if (string.IsNullOrEmpty(place) || string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(distance) || string.IsNullOrEmpty(remarks))
                {
                    oApplication.StatusBar.SetText("Place, Pincode, Distance and Remarks are mandatory.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                if (pin.Length != 6 || !int.TryParse(pin, out _))
                {
                    oApplication.StatusBar.SetText("Current Pincode must be a 6 digit number.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                if (transMode == "1" && string.IsNullOrWhiteSpace(vehNo))
                {
                    oApplication.StatusBar.SetText("Vehicle Number is mandatory for Road mode.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                if ((transMode == "2" || transMode == "3" || transMode == "4") && string.IsNullOrWhiteSpace(docNo))
                {
                    oApplication.StatusBar.SetText("Transport Document Number is mandatory for Rail/Air/Ship.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                // Dynamically fetch the Warehouse GSTIN and State Code based on the source document
                string sLocQuery = "";
                if (sourceDocType == "13" || sourceDocType == "Invoice")
                {
                    sLocQuery = "SELECT D.\"GSTRegnNo\", A4.\"GSTCode\" FROM INV1 A2 INNER JOIN OLCT D ON D.\"Code\"=A2.\"LocCode\" INNER JOIN OCST A4 ON A4.\"Code\"=D.\"State\" AND A4.\"Country\"=D.\"Country\" WHERE A2.\"DocEntry\" = " + sourceDocEntry + " LIMIT 1";
                }
                else if (sourceDocType == "67" || sourceDocType == "Transfer")
                {
                    sLocQuery = "SELECT D.\"GSTRegnNo\", A4.\"GSTCode\" FROM WTR1 A2 INNER JOIN OLCT D ON D.\"Code\"=A2.\"LocCode\" INNER JOIN OCST A4 ON A4.\"Code\"=D.\"State\" AND A4.\"Country\"=D.\"Country\" WHERE A2.\"DocEntry\" = " + sourceDocEntry + " LIMIT 1";
                }
                else
                {
                    // Fallback if CreditMemo or other
                    sLocQuery = "SELECT D.\"GSTRegnNo\", A4.\"GSTCode\" FROM RIN1 A2 INNER JOIN OLCT D ON D.\"Code\"=A2.\"LocCode\" INNER JOIN OCST A4 ON A4.\"Code\"=D.\"State\" AND A4.\"Country\"=D.\"Country\" WHERE A2.\"DocEntry\" = " + sourceDocEntry + " LIMIT 1";
                }

                var dbHelper = new DatabaseHelper(oCompany);
                System.Data.DataTable dtLoc = dbHelper.ExecuteQuery(sLocQuery);
                if (dtLoc == null || dtLoc.Rows.Count == 0)
                {
                    oApplication.StatusBar.SetText("Could not determine Warehouse GSTIN/State Code from document.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                string locGSTIN = dtLoc.Rows[0]["GSTRegnNo"].ToString().Trim();
                string locState = dtLoc.Rows[0]["GSTCode"].ToString().Trim();

                // Map DocType to SAP ObjType
                string objType = (sourceDocType == "Invoice") ? "13" : "14";

                // Single universal token — exact same as Combined
                var tokenResp = AuthHelper.GetCharteredAuthTokenSync(dbHelper, objType, sourceDocEntry);
                string sAuthToken = tokenResp.Data?.AuthToken;
                if (string.IsNullOrEmpty(sAuthToken))
                {
                    oApplication.StatusBar.SetText("Auth token not generated!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                // Read credentials that GetUniversalToken populated
                var creds = CredentialManager.GetCredentialsForDocument(dbHelper, objType, sourceDocEntry);
                string GSTNo = creds.GSTIN;
                string AspId = creds.AspId;
                string AspPwd = creds.AspPassword;
                string UserName = creds.UserName;

                // Prepare EXTENDVALIDITY Payload
                dynamic extReq = new System.Dynamic.ExpandoObject();
                extReq.ewbNo = Convert.ToInt64(ewayNo);
                extReq.vehicleNo = vehNo;
                extReq.fromPlace = place;
                extReq.fromState = Convert.ToInt32(payloadState);
                extReq.remainingDistance = Convert.ToInt32(distance);
                extReq.transDocNo = docNo;
                extReq.transDocDate = docDt;
                extReq.transMode = transMode;
                extReq.extnRsnCode = Convert.ToInt32(reasonCode);
                extReq.extnRemarks = remarks;
                extReq.fromPincode = Convert.ToInt32(pin);
                extReq.consignmentStatus = consStat;
                extReq.transitType = tranType;
                extReq.addressLine1 = add1;
                extReq.addressLine2 = add2;
                extReq.addressLine3 = add3;

                string sJSON = Newtonsoft.Json.JsonConvert.SerializeObject(extReq);

                // Get update URL and call NIC portal
                string sUrlQ = "CALL \"TEC_EWAYLoginURL\"('ExtendValidity','" + locState + "')";
                System.Data.DataTable oADT1 = dbHelper.ExecuteQuery(sUrlQ);
                string UpdURL = "";
                if (oADT1 != null && oADT1.Rows.Count > 0)
                {
                    UpdURL = oADT1.Rows[0]["URL"].ToString().Trim();
                }

                if (string.IsNullOrEmpty(UpdURL))
                {
                    oApplication.StatusBar.SetText("Target URL missing from database response.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                string XML_Final = ApiRunner.PostInvoice(sJSON, sAuthToken, GSTNo, UserName, AspPwd, AspId, UpdURL);

                // Log the request to existing TEC_EW_LOG table
                string logQ = oCompany.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB
                    ? "INSERT INTO TEC_EW_LOG VALUES('" + sourceDocEntry + "','ExtendValidity','" + sJSON.Replace("'", "''") + "','" + XML_Final.Replace("'", "''") + "',CURRENT_TIMESTAMP,'" + oCompany.UserSignature + "')"
                    : "INSERT INTO TEC_EW_LOG VALUES('" + sourceDocEntry + "','ExtendValidity','" + sJSON.Replace("'", "''") + "','" + XML_Final.Replace("'", "''") + "',GETDATE(),'" + oCompany.UserSignature + "')";
                dbHelper.ExecuteNonQuery(logQ);

                // Handle response
                if (XML_Final.Contains("ewayBillNo") && XML_Final.Contains("validUpto"))
                {
                    oApplication.StatusBar.SetText("Validity Extended Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                    try
                    {
                        dynamic successObj = Newtonsoft.Json.JsonConvert.DeserializeObject(XML_Final);
                        string newValidUpto = successObj.validUpto != null ? successObj.validUpto.ToString() : "";
                        
                        string tableName = "OINV";
                        if (sourceDocType == "Transfer" || sourceDocType == "67") tableName = "OWTR";
                        else if (sourceDocType == "CreditMemo" || sourceDocType == "14") tableName = "ORIN";
                        
                        try
                        {
                            SAPbouiCOM.Form parentForm = oApplication.Forms.Item(parentFormUID);
                            parentForm.DataSources.DBDataSources.Item(tableName).SetValue("U_VehicleNo", 0, vehNo);
                            parentForm.DataSources.DBDataSources.Item(tableName).SetValue("U_EwayVal", 0, newValidUpto);
                            if (parentForm.Mode == SAPbouiCOM.BoFormMode.fm_OK_MODE)
                                parentForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE;
                            
                            parentForm.Items.Item("1").Click(SAPbouiCOM.BoCellClickType.ct_Regular);
                        }
                        catch (Exception)
                        {
                            string updateQ = $"UPDATE \"{tableName}\" SET \"U_VehicleNo\" = '{vehNo}', \"U_EwayVal\" = '{newValidUpto}' WHERE \"DocEntry\" = {sourceDocEntry}";
                            dbHelper.ExecuteNonQuery(updateQ);
                        }
                    }
                    catch (Exception ex)
                    {
                        oApplication.StatusBar.SetText("Failed to update SAP Document: " + ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                    }
                    oForm.Close();
                }
                else
                {
                    string errMsg = "Unknown Error";
                    if (XML_Final.Trim().StartsWith("{"))
                    {
                        try
                        {
                            dynamic respObj = Newtonsoft.Json.JsonConvert.DeserializeObject(XML_Final);
                            if (respObj != null && respObj["error"] != null)
                            {
                                if (respObj["error"]["message"] != null)
                                    errMsg = respObj["error"]["message"].ToString();
                                else
                                    errMsg = respObj["error"].ToString();
                            }
                            else if (respObj != null && respObj["message"] != null)
                            {
                                errMsg = respObj["message"].ToString();
                            }
                            else if (respObj != null && respObj["ErrorDetails"] != null)
                            {
                                errMsg = respObj["ErrorDetails"][0]["ErrorMessage"]?.ToString() ?? "Unknown API Error";
                            }
                        }
                        catch { errMsg = XML_Final; }
                    }
                    else
                    {
                        errMsg = XML_Final;
                    }

                    // Distance Match Check Logic
                    // "Distance between these two pincodes is 1230 KMs"
                    if (errMsg.Contains("Distance between these two pincodes is"))
                    {
                        try
                        {
                            string[] words = errMsg.Split(' ');
                            for (int i = 0; i < words.Length; i++)
                            {
                                if (words[i].ToLower() == "is" && i + 1 < words.Length)
                                {
                                    int suggestedDist = 0;
                                    if (int.TryParse(words[i + 1], out suggestedDist))
                                    {
                                        ((SAPbouiCOM.EditText)oForm.Items.Item("txtDist").Specific).Value = suggestedDist.ToString();
                                        errMsg = "Distance mismatch! The correct distance has been filled in. Please review and click Extend again.";
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    // Cap the error message length to avoid SAP status bar overflow
                    if (errMsg.Length > 200) errMsg = errMsg.Substring(0, 200) + "...";
                    oApplication.StatusBar.SetText("Extend Error: " + errMsg, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                }
            }
            catch (Exception ex)
            {
                oApplication.StatusBar.SetText("Submit Error: " + ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
        }
    }
}
