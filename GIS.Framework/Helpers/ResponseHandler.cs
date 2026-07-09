using GIS.Framework.Interfaces;
using System;

namespace GIS.Framework.Helpers
{
    public static class ResponseHandler
    {
        public static void RouteResponse(string actionType, IDatabaseHelper dbHelper, string objType, string docEntry, string rawJsonRequest, string rawJsonResponse)
        {
            LoggerHelper.Log("Entered ResponseHandler.RouteResponse");
            LoggerHelper.Log($"Routing Response for Action Type: {actionType}");
            
            IResponseHandler handler;

            switch (actionType.ToLower())
            {
                case "combined":
                    handler = new CombinedResponseHandler();
                    break;
                // Add more cases here for 'einvoiceonly', 'ewaybill', 'cancel' etc later
                default:
                    throw new Exception($"No Response Handler found for Action Type: {actionType}");
            }

            // Delegate to the specific sub-handler
            handler.Handle(dbHelper, objType, docEntry, rawJsonRequest, rawJsonResponse);
            LoggerHelper.Log("Exited ResponseHandler.RouteResponse");
        }
    }
}
