using Newtonsoft.Json;

namespace GIS.Framework.Models.EInvoice
{
    internal class DispatchDetails
    {
        [JsonProperty("Nm")]
        public string Name { get; set; }

        [JsonProperty("Addr1")]
        public string Address1 { get; set; }

        [JsonProperty("Addr2")]
        public string Address2 { get; set; }

        [JsonProperty("Loc")]
        public string Location { get; set; }

        [JsonProperty("Pin")]
        public int Pincode { get; set; }

        [JsonProperty("Stcd")]
        public string StateCode { get; set; }
    }
}
