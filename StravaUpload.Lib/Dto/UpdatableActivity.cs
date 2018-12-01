using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Strava.Activities;

namespace StravaUpload.Lib.Dto
{
    public class UpdatableActivity
    {
        [JsonProperty(PropertyName = "commute")]
        public bool Commute { get; set; }

        [JsonProperty(PropertyName = "trainer")]
        public bool Trainer { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActivityType Type { get; set; }

        [JsonProperty(PropertyName = "private")]
        public bool Private { get; set; }
    }
}