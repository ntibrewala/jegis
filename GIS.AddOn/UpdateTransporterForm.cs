using GIS.Framework;
using GIS.Framework.Helpers;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;
using System;

namespace GIS.AddOn
{
    class UpdateTransporterForm
    {
        public static void LoadForm(SAPbouiCOM.Application oApplication, SAPbobsCOM.Company oCompany, string docEntry, string docType, string ewayNo, string parentFormUID)
        {
            try
            {
                SAPbouiCOM.FormCreationParams fcp = (SAPbouiCOM.FormCreationParams)oApplication.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams);
                fcp.UniqueID = "frmUpdTrn" + DateTime.Now.ToString("mmss");
                fcp.FormType = "GIS_OUPT";
                fcp.BorderStyle = SAPbouiCOM.BoFormBorderStyle.fbs_Fixed;
                SAPbouiCOM.Form oForm = oApplication.Forms.AddEx(fcp);

                oForm.Title = "Update Transporter";
                oForm.Width = 400;
                oForm.Height = 150;

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

                SAPbouiCOM.Item itmEWay = oForm.Items.Add("txtEWay", SAPbouiCOM.BoFormItemTypes.it_EDIT);
                itmEWay.Left = -100; itmEWay.Top = -100; itmEWay.Width = 10; itmEWay.Height = 10;
                itmEWay.Visible = false;
                ((SAPbouiCOM.EditText)itmEWay.Specific).Value = ewayNo;

                AddStatic(oForm, "lblEWay", 10, 10, 110, 14, "E-Way Bill No");
                AddStatic(oForm, "lblEwb", 125, 10, 200, 14, ewayNo);

                // Transporter Dropdown (BP Master)
                AddStatic(oForm, "lblTrnId", 10, 30, 110, 14, "Transporter (BP)");
                SAPbouiCOM.Item itmTrnId = oForm.Items.Add("cmbTrnId", SAPbouiCOM.BoFormItemTypes.it_COMBO_BOX);
                itmTrnId.Left = 125; itmTrnId.Top = 30; itmTrnId.Width = 190; itmTrnId.Height = 14;
                SAPbouiCOM.ComboBox cmbTrnId = (SAPbouiCOM.ComboBox)itmTrnId.Specific;

                // Populate Dropdown from OCRD
                var dbHelper = new DatabaseHelper(oCompany);
                string bpQuery = "SELECT \"CardCode\", \"CardName\" FROM OCRD WHERE \"CardType\" = 'S'";
                System.Data.DataTable dtBPs = dbHelper.ExecuteQuery(bpQuery);
                if (dtBPs != null)
                {
                    foreach (System.Data.DataRow row in dtBPs.Rows)
                    {
                        string cCode = row["CardCode"].ToString().Trim();
                        string cName = row["CardName"].ToString().Trim();
                        if (cName.Length > 250) cName = cName.Substring(0, 250);
                        try { cmbTrnId.ValidValues.Add(cCode, cName); } catch { }
                    }
                }

                // Buttons
                AddButton(oForm, "btnSubmit", 120, 70, 80, 20, "Submit");
                AddButton(oForm, "btnCancel", 210, 70, 80, 20, "Cancel");

                oForm.Visible = true;
            }
            catch (Exception ex)
            {
                oApplication.StatusBar.SetText(ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
        }

        private static void AddStatic(SAPbouiCOM.Form f, string uid, int l, int t, int w, int h, string caption)
        {
            SAPbouiCOM.Item i = f.Items.Add(uid, SAPbouiCOM.BoFormItemTypes.it_STATIC);
            i.Left = l; i.Top = t; i.Width = w; i.Height = h;
            ((SAPbouiCOM.StaticText)i.Specific).Caption = caption;
        }

        private static void AddButton(SAPbouiCOM.Form f, string uid, int l, int t, int w, int h, string caption)
        {
            SAPbouiCOM.Item i = f.Items.Add(uid, SAPbouiCOM.BoFormItemTypes.it_BUTTON);
            i.Left = l; i.Top = t; i.Width = w; i.Height = h;
            ((SAPbouiCOM.Button)i.Specific).Caption = caption;
        }

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

        private static void SubmitData(SAPbouiCOM.Application oApplication, SAPbobsCOM.Company oCompany, SAPbouiCOM.Form oForm)
        {
            try
            {
                oForm.ActiveItem = "btnCancel";

                oApplication.StatusBar.SetText("Processing Update Transporter...", SAPbouiCOM.BoMessageTime.bmt_Long, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);

                string ewayNo = ((SAPbouiCOM.EditText)oForm.Items.Item("txtEWay").Specific).Value;
                SAPbouiCOM.ComboBox cmbTrnId = (SAPbouiCOM.ComboBox)oForm.Items.Item("cmbTrnId").Specific;
                
                if (cmbTrnId.Selected == null)
                {
                    oApplication.StatusBar.SetText("Please select a Transporter.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }
                
                string trnCardCode = cmbTrnId.Selected.Value;
                string sourceDocEntry = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocE").Specific).Value;
                string sourceDocType = ((SAPbouiCOM.EditText)oForm.Items.Item("txtDocT").Specific).Value;
                string parentFormUID = ((SAPbouiCOM.EditText)oForm.Items.Item("txtPar").Specific).Value;

                var dbHelper = new DatabaseHelper(oCompany);

                // 1. Get the GSTIN for the selected Business Partner
                string gstQ = "SELECT COALESCE((SELECT \"TaxId0\" FROM CRD7 WHERE \"CardCode\" = '" + trnCardCode + "' AND \"Address\" = '' LIMIT 1), \"LicTradNum\") AS \"GSTIN\" FROM OCRD WHERE \"CardCode\" = '" + trnCardCode + "'";
                System.Data.DataTable dtGst = dbHelper.ExecuteQuery(gstQ);
                string trnGstin = dtGst != null && dtGst.Rows.Count > 0 ? dtGst.Rows[0]["GSTIN"].ToString().Trim() : "";

                if (string.IsNullOrEmpty(trnGstin))
                {
                    oApplication.StatusBar.SetText("Selected Transporter does not have a valid GSTIN setup in BP Master.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
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

                System.Data.DataTable dtLoc = dbHelper.ExecuteQuery(sLocQuery);
                if (dtLoc == null || dtLoc.Rows.Count == 0)
                {
                    oApplication.StatusBar.SetText("Could not determine Warehouse GSTIN/State Code from document.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                string locGSTIN = dtLoc.Rows[0]["GSTRegnNo"].ToString().Trim();
                string locState = dtLoc.Rows[0]["GSTCode"].ToString().Trim();
                string objType = (sourceDocType == "Invoice") ? "13" : "14";

                var tokenResp = AuthHelper.GetCharteredAuthTokenSync(dbHelper, objType, sourceDocEntry);
                string sAuthToken = tokenResp.Data?.AuthToken;
                if (string.IsNullOrEmpty(sAuthToken)) return;

                var creds = CredentialManager.GetCredentialsForDocument(dbHelper, objType, sourceDocEntry);

                // Build Request
                dynamic trnReq = new System.Dynamic.ExpandoObject();
                trnReq.ewbNo = Convert.ToInt64(ewayNo);
                trnReq.transporterId = trnGstin;
                trnReq.transId = trnGstin;
                
                string sJSON = Newtonsoft.Json.JsonConvert.SerializeObject(trnReq);

                string sUrlQ = "CALL \"TEC_EWAYLoginURL\"('UpdateTransporter','" + locState + "')";
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

                string XML_Final = ApiRunner.PostInvoice(sJSON, sAuthToken, creds.GSTIN, creds.UserName, creds.AspPassword, creds.AspId, UpdURL);

                string strQueryLog = oCompany.DbServerType == BoDataServerTypes.dst_HANADB
                    ? "INSERT INTO TEC_EW_LOG VALUES('" + sourceDocEntry + "','UpdateTrn','" + sJSON.Replace("'", "''") + "','" + XML_Final.Replace("'", "''") + "',CURRENT_TIMESTAMP,'" + oCompany.UserSignature + "')"
                    : "INSERT INTO TEC_EW_LOG VALUES('" + sourceDocEntry + "','UpdateTrn','" + sJSON.Replace("'", "''") + "','" + XML_Final.Replace("'", "''") + "',GETDATE(),'" + oCompany.UserSignature + "')";
                dbHelper.ExecuteNonQuery(strQueryLog);

                if (XML_Final.Contains("vehUpdDate") || XML_Final.Contains("\"status\":\"1\"") || XML_Final.ToLower().Contains("success"))
                {
                    oApplication.StatusBar.SetText("Transporter Updated Successfully!", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
                    try
                    {
                        string tableName = "OINV";
                        if (sourceDocType == "Transfer" || sourceDocType == "67") tableName = "OWTR";
                        else if (sourceDocType == "CreditMemo" || sourceDocType == "14") tableName = "ORIN";
                        
                        try
                        {
                            SAPbouiCOM.Form parentForm = oApplication.Forms.Item(parentFormUID);
                            parentForm.DataSources.DBDataSources.Item(tableName).SetValue("U_VendCode", 0, trnCardCode);
                            if (parentForm.Mode == SAPbouiCOM.BoFormMode.fm_OK_MODE)
                                parentForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE;
                            
                            parentForm.Items.Item("1").Click(SAPbouiCOM.BoCellClickType.ct_Regular);
                        }
                        catch (Exception)
                        {
                            string updateQ = $"UPDATE \"{tableName}\" SET \"U_VendCode\" = '{trnCardCode}' WHERE \"DocEntry\" = {sourceDocEntry}";
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
                    oApplication.StatusBar.SetText("Error updating transporter: " + XML_Final, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                }
            }
            catch (Exception ex)
            {
                oApplication.StatusBar.SetText("Error: " + ex.Message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
            }
        }
    }
}
