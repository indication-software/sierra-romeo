/*
 * Sierra Romeo: ExternalRequest model, for text files prepared by other programs
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
    public class ExternalRequest
    {
        public int FormatVersion { get; set; }
        public string MedicareNumber { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        [JsonPropertyName("authorityPrescriptionNumber")]
        public string ScriptNumber { get; set; }
        // public string PBSCode { get; set; }
        // public string DrugId { get; set; }
        public string SearchTerm { get; set; }
        public string Dose { get; set; }
        public int? DoseFrequency { get; set; }
        public int? Quantity { get; set; }
        public int? Repeats { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }
}
