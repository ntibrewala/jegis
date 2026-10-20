using System;
using SAPbouiCOM;
using GIS.Framework.Helpers;

namespace GIS.AddOn.Helpers
{
    public static class UIHelper
    {
        public static void CompactUdfPanel(Form oUdfForm)
        {
            if (oUdfForm == null) return;
            try
            {
                oUdfForm.Freeze(true);
                int targetHeight = 16;
                try
                {
                    targetHeight = oUdfForm.Items.Item("U_ewayBDa").Height;
                }
                catch { }

                string[] tallFields = new[] { "U_IRN", "U_ewayBNo" };
                foreach (string fieldUid in tallFields)
                {
                    try
                    {
                        Item fldItem = oUdfForm.Items.Item(fieldUid);
                        if (fldItem.Height > targetHeight + 10)
                        {
                            int oldBottom = fldItem.Top + fldItem.Height;
                            int shrinkBy = fldItem.Height - targetHeight;
                            fldItem.Height = targetHeight;

                            // Shift all items located below this field upward by shrinkBy
                            for (int i = 0; i < oUdfForm.Items.Count; i++)
                            {
                                Item itm = oUdfForm.Items.Item(i);
                                if (itm.UniqueID != fieldUid && itm.UniqueID != "-" + fieldUid && itm.Top >= oldBottom - 4)
                                {
                                    itm.Top -= shrinkBy;
                                }
                            }
                        }
                    }
                    catch { }
                }
                oUdfForm.Freeze(false);
            }
            catch (Exception ex)
            {
                try { oUdfForm.Freeze(false); } catch { }
                LoggerHelper.Log($"Error compacting UDF panel: {ex.Message}");
            }
        }

        public static void DrawLegacyButtonCombos(Form oForm)
        {
            try
            {
                oForm.Freeze(true);

                bool isOWTR = (oForm.TypeEx == "940");

                // 1. Create btnEInv (Skip for OWTR)
                Item oItemEInv = null;
                ButtonCombo oComboEInv = null;
                if (!isOWTR)
                {
                    try
                    {
                        oItemEInv = oForm.Items.Item("btnEInv");
                        oComboEInv = (ButtonCombo)oItemEInv.Specific;
                    }
                    catch
                    {
                        oItemEInv = oForm.Items.Add("btnEInv", BoFormItemTypes.it_BUTTON_COMBO);
                        // Determine base button ID based on form type
                        string baseBtnId = "2"; // fallback
                        try { if (oForm.Items.Item("10000330") != null) baseBtnId = "10000330"; } catch { }

                        oItemEInv.Left = oForm.Items.Item(baseBtnId).Left - 110;
                        oItemEInv.Top = oForm.Items.Item(baseBtnId).Top;
                        oItemEInv.Width = oForm.Items.Item(baseBtnId).Width;
                        oItemEInv.Height = oForm.Items.Item(baseBtnId).Height;
                        oItemEInv.LinkTo = baseBtnId;

                        oComboEInv = (ButtonCombo)oItemEInv.Specific;
                        oComboEInv.ValidValues.Add("E-Invoice", "E-Invoice");
                        oComboEInv.ValidValues.Add("Generate", "Generate E-Invoice");
                        oComboEInv.ValidValues.Add("Cancel", "Cancel E-Invoice");
                        oItemEInv.DisplayDesc = true;
                    }
                    oComboEInv.Caption = "E-Invoice";
                    // Grey out (disable) E-Invoice button in JEGIS while keeping all code intact
                    oItemEInv.Enabled = false;
                    oItemEInv.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, -1, BoModeVisualBehavior.mvb_False);
                }

                // 2. Create btnEWay
                Item oItemEWay;
                ButtonCombo oComboEWay;
                try
                {
                    oItemEWay = oForm.Items.Item("btnEWay");
                    oComboEWay = (ButtonCombo)oItemEWay.Specific;
                }
                catch
                {
                    oItemEWay = oForm.Items.Add("btnEWay", BoFormItemTypes.it_BUTTON_COMBO);
                    
                    if (isOWTR)
                    {
                        // Anchor directly to base button for OWTR
                        string baseBtnId = "2"; // fallback
                        try { if (oForm.Items.Item("1250000074") != null) baseBtnId = "1250000074"; } catch { }
                        
                        oItemEWay.Left = oForm.Items.Item(baseBtnId).Left - 110;
                        oItemEWay.Top = oForm.Items.Item(baseBtnId).Top;
                        oItemEWay.Width = oForm.Items.Item(baseBtnId).Width;
                        oItemEWay.Height = oForm.Items.Item(baseBtnId).Height;
                        oItemEWay.LinkTo = baseBtnId;
                    }
                    else
                    {
                        // Anchor to btnEInv for standard documents
                        oItemEWay.Left = oItemEInv.Left - 110;
                        oItemEWay.Top = oItemEInv.Top;
                        oItemEWay.Width = oItemEInv.Width;
                        oItemEWay.Height = oItemEInv.Height;
                        oItemEWay.LinkTo = "btnEInv";
                    }

                    oComboEWay = (ButtonCombo)oItemEWay.Specific;
                    oComboEWay.ValidValues.Add("EWayBill", "E-WayBill");
                    oComboEWay.ValidValues.Add("Generate", "Generate E-WayBill");
                    oComboEWay.ValidValues.Add("Cancel", "Cancel E-WayBill");
                    oComboEWay.ValidValues.Add("Inv Transfer", "Inventory Transfer");
                    oComboEWay.ValidValues.Add("UpdateVehicle", "Update Part B");
                    oComboEWay.ValidValues.Add("ExtendValidity", "Extend Validity");
                    oComboEWay.ValidValues.Add("UpdateTrans", "Update Transporter");
                    oItemEWay.DisplayDesc = true;
                }
                oComboEWay.Caption = "E-WayBill";

                // 3. Create btnComb (Skip for OWTR)
                Item oItemComb = null;
                ButtonCombo oComboComb = null;
                if (!isOWTR)
                {
                    try
                    {
                        oItemComb = oForm.Items.Item("btnComb");
                        oComboComb = (ButtonCombo)oItemComb.Specific;
                    }
                    catch
                    {
                        oItemComb = oForm.Items.Add("btnComb", BoFormItemTypes.it_BUTTON_COMBO);
                        oItemComb.Left = oItemEWay.Left - 110;
                        oItemComb.Top = oItemEWay.Top;
                        oItemComb.Width = oItemEWay.Width;
                        oItemComb.Height = oItemEWay.Height;
                        oItemComb.LinkTo = "btnEWay";

                        oComboComb = (ButtonCombo)oItemComb.Specific;
                        oComboComb.ValidValues.Add("Combined", "Combined");
                        oComboComb.ValidValues.Add("Generate", "Generate Combined");
                        oItemComb.DisplayDesc = true;
                    }
                    oComboComb.Caption = "Combined";
                    // Grey out (disable) Combined button in JEGIS while keeping all code intact
                    oItemComb.Enabled = false;
                    oItemComb.SetAutoManagedAttribute(BoAutoManagedAttr.ama_Editable, -1, BoModeVisualBehavior.mvb_False);
                }

                oForm.Freeze(false);
            }
            catch (Exception ex)
            {
                if (oForm != null) oForm.Freeze(false);
                LoggerHelper.Log($"Error drawing UI: {ex.Message}");
            }
        }
    }
}
