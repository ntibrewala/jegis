using System;
using System.Collections.Generic;
using System.Data;
using GIS.Framework.Models.EInvoice;

namespace GIS.Framework.Helpers
{
    internal static class PayloadMappers
    {
        private static decimal GetDecimal(DataRow row, string colName)
        {
            if (row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value && decimal.TryParse(row[colName].ToString(), out decimal val))
            {
                return Math.Round(val, 2, MidpointRounding.AwayFromZero);
            }
            return 0m;
        }

        private static string GetString(DataRow row, string colName)
        {
            if (row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value)
            {
                string val = row[colName].ToString().Trim();
                return string.IsNullOrEmpty(val) ? null : val;
            }
            return null;
        }

        public static EInvoicePayload MapHeader(DataRow headerRow)
        {
            string ecm = GetString(headerRow, "EcmGstin");
            var payload = new EInvoicePayload
            {
                Version = "1.1",
                TransactionDetails = new TransactionDetails
                {
                    TaxScheme = "GST",
                    SupplyType = GetString(headerRow, "SupTyp") ?? "B2B",
                    ReverseCharge = GetString(headerRow, "RegRev") ?? "N",
                    ECommerceGstin = ecm, // ecm is already null if empty
                    IgstOnIntra = GetString(headerRow, "IgstOnInt") ?? "N"
                },
                DocumentDetails = new DocumentDetails
                {
                    Type = GetString(headerRow, "Type") ?? "INV",
                    Number = GetString(headerRow, "DocNum"),
                    Date = GetString(headerRow, "Dt")
                },
                SellerDetails = new PartyDetails
                {
                    Gstin = GetString(headerRow, "Seller_GSTIN"),
                    LegalName = GetString(headerRow, "Seller_Trade_Name"),
                    TradeName = GetString(headerRow, "Seller_Trade_Name"),
                    Address1 = GetString(headerRow, "Seller_Addr1"),
                    Address2 = GetString(headerRow, "Seller_Addr2"),
                    Location = GetString(headerRow, "Seller_Loc"),
                    Pincode = GetString(headerRow, "Seller_Pin") != null ? Convert.ToInt32(GetString(headerRow, "Seller_Pin")) : 0,
                    StateCode_Address = GetString(headerRow, "Seller_State"),
                    Phone = GetString(headerRow, "Seller_Ph"),
                    Email = GetString(headerRow, "Seller_Em")
                },
                BuyerDetails = new PartyDetails
                {
                    Gstin = GetString(headerRow, "Buyer_GSTIN"),
                    LegalName = GetString(headerRow, "Buyer_LglNm"),
                    TradeName = GetString(headerRow, "Buyer_LglNm"),
                    StateCode = GetString(headerRow, "Buyer_Pos"),
                    Address1 = GetString(headerRow, "Buyer_Addr1"),
                    Address2 = GetString(headerRow, "Buyer_Addr2"),
                    Location = GetString(headerRow, "Buyer_Loc"),
                    Pincode = GetString(headerRow, "Buyer_Pin") != null ? Convert.ToInt32(GetString(headerRow, "Buyer_Pin")) : 0,
                    StateCode_Address = GetString(headerRow, "Buyer_State"),
                    Phone = GetString(headerRow, "Buyer_Ph"),
                    Email = GetString(headerRow, "Buyer_Em")
                },
                DispatchDetails = GetString(headerRow, "Disp_Dtl_Nm") != null || GetString(headerRow, "Disp_Dtl_Addr1") != null ? new PartyDetails
                {
                    LegalName = GetString(headerRow, "Disp_Dtl_Nm"),
                    TradeName = GetString(headerRow, "Disp_Dtl_Nm"),
                    Address1 = GetString(headerRow, "Disp_Dtl_Addr1"),
                    Address2 = GetString(headerRow, "Disp_Dtl_Addr2"),
                    Location = GetString(headerRow, "Disp_Dtl_Loc"),
                    Pincode = GetString(headerRow, "Disp_Dtl_Pin") != null ? Convert.ToInt32(GetString(headerRow, "Disp_Dtl_Pin")) : 0,
                    StateCode_Address = GetString(headerRow, "Disp_Dtl_Stcd")
                } : null,
                ShippingDetails = GetString(headerRow, "ShipDtls_GSTIN") != null ? new PartyDetails
                {
                    Gstin = GetString(headerRow, "ShipDtls_GSTIN"),
                    LegalName = GetString(headerRow, "ShipDtls_LglNm"),
                    TradeName = GetString(headerRow, "ShipDtls_TrdNm"),
                    Address1 = GetString(headerRow, "ShipDtls_Addr1"),
                    Address2 = GetString(headerRow, "ShipDtls_Addr2"),
                    Location = GetString(headerRow, "ShipDtls_Loc"),
                    Pincode = GetString(headerRow, "ShipDtls_Pin") != null ? Convert.ToInt32(GetString(headerRow, "ShipDtls_Pin")) : 0,
                    StateCode_Address = GetString(headerRow, "ShipDtls_Stcd")
                } : null,
                ValueDetails = new ValueDetails
                {
                    AssessableValue = GetDecimal(headerRow, "AssVal"),
                    CgstValue = GetDecimal(headerRow, "CgstVal"),
                    SgstValue = GetDecimal(headerRow, "SgstVal"),
                    IgstValue = GetDecimal(headerRow, "IgstVal"),
                    CessValue = GetDecimal(headerRow, "CesVal"),
                    StateCessValue = GetDecimal(headerRow, "StCesVal"),
                    Discount = GetDecimal(headerRow, "DiscSum"),
                    OtherCharges = GetDecimal(headerRow, "OthChrg"),
                    RoundOffAmount = GetDecimal(headerRow, "RndOffAmt"),
                    TotalInvoiceValue = GetDecimal(headerRow, "TotInvVal")
                }
            };

            return payload;
        }

        public static List<ItemDetails> MapDetails(DataTable detailsTable)
        {
            var items = new List<ItemDetails>();
            foreach (DataRow row in detailsTable.Rows)
            {
                items.Add(new ItemDetails
                {
                    SlNo = GetString(row, "LineNum"),
                    ProductDescription = GetString(row, "PrdDesc"),
                    IsService = GetString(row, "IsServc") ?? "N",
                    HsnCode = GetString(row, "HsnCd"),
                    Quantity = GetDecimal(row, "Qty"),
                    Unit = GetString(row, "Unit"),
                    UnitPrice = GetDecimal(row, "UnitPrice"),
                    TotalAmount = GetDecimal(row, "TotAmt"),
                    Discount = GetDecimal(row, "Discount"),
                    AssessableAmount = GetDecimal(row, "AssAmt"),
                    GstRate = GetDecimal(row, "GstRt"),
                    IgstAmount = GetDecimal(row, "IgstAmt"),
                    CgstAmount = GetDecimal(row, "CgstAmt"),
                    SgstAmount = GetDecimal(row, "SgstAmt"),
                    CessRate = GetDecimal(row, "CesRt"),
                    CessAmount = GetDecimal(row, "CesAmt"),
                    CessNonAdvolAmount = GetDecimal(row, "CesNonAdvlAmt"),
                    StateCessRate = GetDecimal(row, "StateCesRt"),
                    StateCessAmount = GetDecimal(row, "StateCesAmt"),
                    StateCessNonAdvolAmount = GetDecimal(row, "StateCesNonAdvlAmt"),
                    OtherCharges = GetDecimal(row, "OthChrg"),
                    TotalItemValue = GetDecimal(row, "TotItemVal")
                });
            }
            return items;
        }
    }
}
