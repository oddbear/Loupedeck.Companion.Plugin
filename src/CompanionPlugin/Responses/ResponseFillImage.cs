using Newtonsoft.Json;

namespace Loupedeck.CompanionPlugin.Responses
{
    class ResponseFillImage
    {
        [JsonProperty("keyIndex")]
        public int KeyIndex { get; set; }

        [JsonProperty("page")]
        public int? Page { get; set; }

        [JsonProperty("bank")]
        public int? Bank { get; set; }

        [JsonProperty("data")]
        public BufferData Data { get; set; }

        public class BufferData
        {
            public string Type { get; set; }
            public byte[] Data { get; set; }
        }
    }
}
