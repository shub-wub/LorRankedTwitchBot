using Newtonsoft.Json;
using System.Collections.Generic;

namespace TwitchBot
{
    public class Players
    {
        [JsonProperty("Players")]
        public List<Player> data { get; set; }
    }

    public class Player
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("rank")]
        public string Rank { get; set; }
        [JsonProperty("lp")]
        public string Lp { get; set; }
    }
}