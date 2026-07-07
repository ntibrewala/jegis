using GIS.Framework.Interfaces;
using System;
using System.Data;

namespace GIS.Framework.Helpers
{
    /// <summary>
    /// Handles logic for calculating and caching local transportation distances.
    /// </summary>
    public static class DistanceHelper
    {
        public static int GetDistanceWithCache(IDatabaseHelper dbHelper, DataRow row)
        {
            LoggerHelper.Log("Entered DistanceHelper.GetDistanceWithCache");
            int transDist = 0;
            if (row.Table.Columns.Contains("transDistance") && row["transDistance"] != DBNull.Value && !string.IsNullOrEmpty(row["transDistance"].ToString()))
            {
                transDist = Convert.ToInt32(row["transDistance"]);
            }

            string fromPin = row.Table.Columns.Contains("fromPincode") ? row["fromPincode"].ToString().Trim() : "";
            string toPin = row.Table.Columns.Contains("toPincode") ? row["toPincode"].ToString().Trim() : "";

            if (transDist == 0 && !string.IsNullOrEmpty(fromPin) && fromPin == toPin)
            {
                string fromAddr = (row.Table.Columns.Contains("fromAddr1") ? row["fromAddr1"].ToString() : "") + " " + (row.Table.Columns.Contains("fromPlace") ? row["fromPlace"].ToString() : "");
                string toAddr = (row.Table.Columns.Contains("toAddr1") ? row["toAddr1"].ToString() : "") + " " + (row.Table.Columns.Contains("toPlace") ? row["toPlace"].ToString() : "");

                fromAddr = fromAddr.Trim().Replace("'", "").Replace("\"", "");
                toAddr = toAddr.Trim().Replace("'", "").Replace("\"", "");
                
                // Truncate to reasonable lengths for a cache key
                if (fromAddr.Length > 50) fromAddr = fromAddr.Substring(0, 50);
                if (toAddr.Length > 50) toAddr = toAddr.Substring(0, 50);
                
                string routeHash = (fromAddr + "_" + toAddr).Replace(" ", "").ToUpper();
                if (routeHash.Length > 100) routeHash = routeHash.Substring(0, 100);

                var dtCache = dbHelper.ExecuteQuery($@"SELECT ""Distance"" FROM ""GIS_DIST_CACHE"" WHERE ""Code"" = '{routeHash}'");
                if (dtCache != null && dtCache.Rows.Count > 0)
                {
                    transDist = Convert.ToInt32(dtCache.Rows[0][0]);
                }
                else
                {
                    LoggerHelper.Log("Exited DistanceHelper.GetDistanceWithCache (with Exception)");
                    throw new Exception($"MISSING_DISTANCE|{routeHash}|{fromAddr}|{toAddr}");
                }
            }

            LoggerHelper.Log("Exited DistanceHelper.GetDistanceWithCache");
            return transDist;
        }
    }
}
