/*
 * Sierra Romeo: StatusMessage model
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

namespace Sierra_Romeo
{
    public class StatusMessage
    {
        public string ReasonCode { get; set; }
        public string ReasonText { get; set; }
        public string ReasonType { get; set; }
        public string OverrideIndicator { get; set; }
    }
}
