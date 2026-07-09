using Newtonsoft.Json;
using System.Collections.Generic;

namespace GIS.Framework.Models.EInvoice
{
    internal class EInvoicePayload
    {
        [JsonProperty("Version")]
        public string Version { get; set; } = "1.1";

        [JsonProperty("TranDtls")]
        public TransactionDetails TransactionDetails { get; set; }

        [JsonProperty("DocDtls")]
        public DocumentDetails DocumentDetails { get; set; }

        [JsonProperty("SellerDtls")]
        public PartyDetails SellerDetails { get; set; }

        [JsonProperty("BuyerDtls")]
        public PartyDetails BuyerDetails { get; set; }

        [JsonProperty("DispDtls", NullValueHandling = NullValueHandling.Ignore)]
        public DispatchDetails DispatchDetails { get; set; }

        [JsonProperty("ShipDtls", NullValueHandling = NullValueHandling.Ignore)]
        public PartyDetails ShippingDetails { get; set; }

        [JsonProperty("ItemList")]
        public List<ItemDetails> ItemList { get; set; }

        [JsonProperty("ValDtls")]
        public ValueDetails ValueDetails { get; set; }

        [JsonProperty("EwbDtls", NullValueHandling = NullValueHandling.Ignore)]
        public EwbDetails EwayBillDetails { get; set; }
    }
}
