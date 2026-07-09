using GIS.Framework;
using GIS.Framework.Helpers;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;
using System;

namespace GIS.AddOn
{
    class UpdateVehicleForm
    {
        public static void LoadForm(SAPbouiCOM.Application oApplication, SAPbobsCOM.Company oCompany, string docEntry, string docType, string ewayNo, string parentFormUID)
        {
            try
            {
                SAPbouiCOM.FormCreationParams fcp = (SAPbouiCOM.FormCreationParams)oApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams);
                fcp.UniqueID = "frmUpdVeh" + DateTime.Now.ToString("mmss");
                fcp.FormType = "GIS_OUPV";
                fcp.BorderStyle = SAPbouiCOM.BoFormBorderStyle.fbs_Fixed;
                SAPbouiCOM.Form oForm = oApplication.Forms.AddEx(fcp);

                oForm.Title = "Update Vehicle Number (Part-B)";
                oForm.Width = 400;
                oForm.Height = 280;

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

                // E-Way Bill No (display label + hidden edit to store value)
                AddStatic(oForm, "lblEWay", 10, 10, 110, 14, "E-Way Bill No");
                AddStatic(oForm, "lblEwb", 125, 10, 200, 14, ewayNo);
                // Hidden edit holds the value for SubmitData to read
                SAPbouiCOM.Item itmEWay = oForm.Items.Add("txtEWay", SAPbouiCOM.BoFormItemTypes.it_EDIT);
                itmEWay.Left = -100; itmEWay.Top = -100; itmEWay.Width = 10; itmEWay.Height = 10;
                itmEWay.Visible = false;
                ((SAPbouiCOM.EditText)itmEWay.Specific).Value = ewayNo;

                // Trans Mode
                AddStatic(oForm, "lblTransM", 10, 30, 110, 14, "Trans Mode");
                SAPbouiCOM.Item itmTransM = oForm.Items.Add("cmbTransM", SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX);
                itmTransM.Left = 125; itmTransM.Top = 30; itmTransM.Width = 190; itmTransM.Height = 14;
                SAPbouiCOM.ComboBox cmbTransM = (SAPbouiCOM.ComboBox)itmTransM.Specific;
                cmbTransM.ValidValues.Add("1", "Road");
                cmbTransM.ValidValues.Add("2", "Rail");
                cmbTransM.ValidValues.Add("3", "Air");
                cmbTransM.ValidValues.Add("4", "Ship");
                cmbTransM.Select("1", SAPbouiCOM.BoSearchKey.psk_ByValue);

                // Vehicle No
                AddStatic(oForm, "lblVehNo", 10, 50, 110, 14, "Vehicle No");
                AddEdit(oForm, "txtVehNo", 125, 50, 190, 14);

                // Reason
                AddStatic(oForm, "lblReason", 10, 70, 110, 14, "Reason");
                SAPbouiCOM.Item itmReason = oForm.Items.Add("cmbReason", SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX);
                itmReason.Left = 125; itmReason.Top = 70; itmReason.Width = 190; itmReason.Height = 14;
                SAPbouiCOM.ComboBox cmbReason = (SAPbouiCOM.ComboBox)itmReason.Specific;
                cmbReason.ValidValues.Add("1", "Due To Break Down");
                cmbReason.ValidValues.Add("2", "Due To Transhipment");
                cmbReason.ValidValues.Add("3", "Others");
                cmbReason.ValidValues.Add("4", "First Time");
                cmbReason.Select("2", SAPbouiCOM.BoSearchKey.psk_ByValue);

                // Place of Change
                AddStatic(oForm, "lblPlace", 10, 90, 110, 14, "Place of Change");
                AddEdit(oForm, "txtPlace", 125, 90, 190, 14);



                // Trans Doc No
                AddStatic(oForm, "lblDocNo", 10, 130, 110, 14, "Trans Doc No");
                AddEdit(oForm, "txtDocNo", 125, 130, 190, 14);

                // Trans Doc Date
                AddStatic(oForm, "lblDocDt", 10, 150, 110, 14, "Trans Doc Date");
                AddEdit(oForm, "txtDocDt", 125, 150, 190, 14);

                // Buttons
                AddButton(oForm, "btnSubmit", 120, 180, 80, 20, "Submit");
                AddButton(oForm, "btnCancel", 210, 180, 80, 20, "Cancel");

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                oApplication.StatusBar.SetText(ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static void AddStatic(SAPbouiCOM.Form f, string uid, int l, int t, int w, int h, string caption)
        {
            SAPbouiCOM.Item i = f.Items.Add(uid, SAPbouiCOM.BoFormItemTypes.it_STATIC);
            i.Left = l; i.Top = t; i.Width = w; i.Height = h;
            ((SAPbouiCOM.StaticText)i.Specific).Caption = caption;
        }

        private static void AddEdit(SAPbouiCOM.Form f, string uid, int l, int t, int w, int h)
        {
            SAPbouiCOM.Item i = f.Items.Add(uid, SAPbouiCOM.BoFormItemTypes.it_EDIT);
            i.Left = l; i.Top = t; i.Width = w; i.Height = h;
        }

        private static void AddButton(SAPbouiCOM.Form f, string uid, int l, int t, int w, int h, string caption)
        {
            SAPbouiCOM.Item i = f.Items.Add(uid, SAPbouiCOM.BoFormItemTypes.it_BUTTON);
            i.Left = l; i.Top = t; i.Width = w; i.Height = h;
            ((SAPbouiCOM.Button)i.Specific).Caption = caption;
        }

        // ── Item Event ───────────────────────────────────────────────────────
        public static void ItemEvent(ref SAPbouiCOM.Application oApplication, ref SAPbobsCOM.Company oCompany, SAPbouiCOM.Form oForm, ref SAPbouiCOM.ItemEvent pVal, ref bool BubbleEvent)
        {
            BubbleEvent = true;
            if (pVal.BeforeAction == false)
            {
                if (pVal.EventType == SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED)
                {
                    if (pVal.ItemUID == "btnCancel")
                    {
                        oForm.Close();
                    }
                    else if (pVal.ItemUID == "btnSubmit")
                    {
                        SubmitData(oApplication, oCompany, oForm);
                    }
                }
            }
        }

        // ── Submit ───────────────────────────────────────────────────────────
        private static void SubmitData(SAPbouiCOM.Application oApplication, SAPbobsCOM.Company oCompany, SAPbouiCOM.Form oForm)
        {
            try
            {
                // Move focus to a neutral combo to commit whatever field is active
                oForm.ActiveItem = "cmbTransM";

                oApplication.StatusBar.SetText("Processing Update Part-B...", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);

                string ewayNo = ((SAPbouiCOM.EditText)oForm.Items.Item("txtEWay").Specific).Value;
                string vehNo = ((SAPbouiCOM.EditText)oForm.Items.Item("txtVehNo").Specific).Value;
                string transM = ((SAPbouiCOM.ComboBox)oForm.Items.Item("cmbTransM").Specific).Selected.Value;
                string reason = ((SAPbouiCOM.ComboBox)oForm.Items.Item("cmbReason").Specific).Selected.Value;
                string reasonDesc = ((SAPbouiCOM.ComboBox)oForm.Items.Item("cmbReason").Specific).Selected.Description;
                string place = ((SAPbouiCOM.EditText)oForm.Items.Item("txtPlace").Specific).Value;
                string docNo = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocNo").Specific).Value;
                string docDt = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocDt").Specific).Value;
                string sourceDocEntry = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocE").Specific).Value;
                string sourceDocType = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocT").Specific).Value;
                string parentFormUID = ((SAPbouiCOM.EditText)oForm.Items.Item("txtPar").Specific).Value;

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

                if (string.IsNullOrEmpty(vehNo))
                {
                    oApplication.StatusBar.SetText("Vehicle Number is required.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

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


                // Build request payload
                dynamic vehReq = new System.Dynamic.ExpandoObject();
                vehReq.ewbNo = Convert.ToInt64(ewayNo);
                vehReq.vehicleNo = vehNo;
                vehReq.fromPlace = place;
                vehReq.fromState = Convert.ToInt32(locState);
                vehReq.reasonCode = reason;
                vehReq.reasonRem = reasonDesc;
                vehReq.transDocNo = docNo;
                vehReq.transDocDate = docDt;
                vehReq.transMode = transM;
                vehReq.vehicleType = "R";

                string sJSON = Newtonsoft.Json.JsonConvert.SerializeObject(vehReq);

                // Get update URL and call NIC portal
                string sUrlQ = "CALL \"TEC_EWAYLoginURL\"('UpdateVehicle','" + locState + "')";
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

                // Log to existing TEC_EW_LOG table
                string strQueryLog = oCompany.DbServerType == BoDataServerTypes.dst_HANADB
                    ? "INSERT INTO TEC_EW_LOG VALUES('" + sourceDocEntry + "','UpdateVeh','" + sJSON.Replace("'", "''") + "','" + XML_Final.Replace("'", "''") + "',CURRENT_TIMESTAMP,'" + oCompany.UserSignature + "')"
                    : "INSERT INTO TEC_EW_LOG VALUES('" + sourceDocEntry + "','UpdateVeh','" + sJSON.Replace("'", "''") + "','" + XML_Final.Replace("'", "''") + "',GETDATE(),'" + oCompany.UserSignature + "')";
                dbHelper.ExecuteNonQuery(strQueryLog);

                // Handle response
                if (XML_Final.Contains("vehUpdDate") && !XML_Final.Contains("\"error\""))
                {
                    oApplication.StatusBar.SetText("Part-B Updated Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                    try
                    {
                        string tableName = "OINV";
                        if (sourceDocType == "Transfer" || sourceDocType == "67") tableName = "OWTR";
                        else if (sourceDocType == "CreditMemo" || sourceDocType == "14") tableName = "ORIN";
                        
                        // We update the open form directly instead of doing a backend DB update to avoid "Modified by another user" error
                        try
                        {
                            SAPbouiCOM.Form parentForm = oApplication.Forms.Item(parentFormUID);
                            parentForm.DataSources.DBDataSources.Item(tableName).SetValue("U_VehicleNo", 0, vehNo);
                            if (parentForm.Mode == SAPbouiCOM.BoFormMode.fm_OK_MODE)
                                parentForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE;
                            
                            parentForm.Items.Item("1").Click(SAPbouiCOM.BoCellClickType.ct_Regular);
                        }
                        catch (Exception)
                        {
                            // Fallback to SQL update if the form was somehow closed
                            string updateQ = $"UPDATE \"{tableName}\" SET \"U_VehicleNo\" = '{vehNo}' WHERE \"DocEntry\" = {sourceDocEntry}";
                            dbHelper.ExecuteNonQuery(updateQ);
                        }
                    }
                    catch (Exception ex)
                    {
                        oApplication.StatusBar.SetText("Failed to update SAP Document: " + ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                    }
                    oForm.ActiveItem = "cmbTransM";
                    try
                    {
                        oForm.Close();
                    }
                    catch (Exception ex)
                    {
                        oApplication.StatusBar.SetText("Close Error: " + ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    }
                }
                else
                {
                    string sErrorMessage = XML_Final;
                    if (XML_Final.Trim().StartsWith("{"))
                    {
                        try
                        {
                            dynamic respObj = Newtonsoft.Json.JsonConvert.DeserializeObject(XML_Final);
                            if (respObj != null && respObj["error"] != null)
                            {
                                if (respObj["error"]["message"] != null)
                                    sErrorMessage = respObj["error"]["message"].ToString();
                                else
                                    sErrorMessage = respObj["error"].ToString();
                            }
                            else if (respObj != null && respObj["message"] != null)
                            {
                                sErrorMessage = respObj["message"].ToString();
                            }
                        }
                        catch { }
                    }

                    // Cap the error message length to avoid SAP status bar overflow
                    if (sErrorMessage.Length > 200) sErrorMessage = sErrorMessage.Substring(0, 200) + "...";
                    oApplication.StatusBar.SetText("Error: " + sErrorMessage, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                }
            }
            catch (Exception ex)
            {
                oApplication.StatusBar.SetText("Error: " + ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
        }
    }
}
