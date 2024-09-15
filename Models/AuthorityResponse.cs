/*
 * Sierra Romeo: AuthorityResponse model
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

    public class AuthorityResponse
    {
        public string AuthorityUniqueId { get; set; }
        public string AuthorityPrescriptionNumber { get; set; }
        public string PrescriberId { get; set; }
        public OverrideDetail[] OverrideDetail { get; set; }
        public AssessmentDetails AssessmentDetails { get; set; }
        public string AuthorityApprovalNumber { get; set; }
        public StatusMessage[] StatusMessages { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }

    public class AssessmentDetails
    {
        public string Code { get; set; }
        public string Text { get; set; }
    }

    public class OverrideDetail
    {
        public string Code { get; set; }
        public string Text { get; set; }
    }

}
