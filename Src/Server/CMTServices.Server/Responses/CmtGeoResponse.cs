﻿using Newtonsoft.Json;

namespace CMTServices.Responses
{
    public class CmtGeoResponse
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("vehicles")]
        public CmtGeoContent[] Entities { get; set; }
    }
}
