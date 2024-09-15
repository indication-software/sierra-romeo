/*
 * Sierra Romeo: PBSItem model
 * Copyright 2024 David Adam <mail@davidadam.com.au>
 * 
 * Sierra Romeo is free software: you can redistribute it and/or modify it under the terms of the
 * GNU General Public License as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 * 
 * Sierra Romeo is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See
 * the GNU General Public License for more details.
 */

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sierra_Romeo
{
    public class PBSItem
    {
        [JsonPropertyName("pbs_code")]
        public string ItemCode { get; set; }
        public string Brands { get; set; }
        [JsonPropertyName("amt_id")]
        public string AMTCode { get; set; }
        public string Drug { get; set; }
        public string Program { get; set; }
        [JsonPropertyName("treatment_code")]
        public string Restriction { get; set; }
        [JsonPropertyName("restriction_text")]
        public string RestrictionText { get; set; }
        [JsonPropertyName("max_quantity")]
        public int Quantity { get; set; }
        [JsonPropertyName("max_repeats")]
        public int Repeats { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }

    public class PBSItemSearchResults
    {
        public List<PBSItem> Results { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }

}
