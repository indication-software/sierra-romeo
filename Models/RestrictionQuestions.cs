/*
 * Sierra Romeo: RestrictionQuestions model
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
    public class RestrictionQuestionsResponse
    {
        public string PrescriberId { get; set; }
        public string ItemCode { get; set; }
        public string RestrictionCode { get; set; }
        public RestrictionQuestionDetail[] RestrictionQuestionDetails { get; set; }
        public StatusMessage[] StatusMessages { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }


    public class RestrictionQuestionDetail
    {
        /// <summary>
        ///  This class is used for the results returned by the restriction question API.
        ///  The AnswerText/AnswerOption elements are added to allow data binding back to
        ///  the original object, rather than requiring a complicated converter.
        ///  They are then copied out into the AuthorityAnswer object.
        /// </summary>

        public int RestrictionQuestionCode { get; set; }
        public int RestrictionQuestionOrderNumber { get; set; }
        public string RestrictionQuestionText { get; set; }
        public bool RestrictionQuestionMandatory { get; set; }
        public string RestrictionAnswerType { get; set; }
        public string RestrictionAnswerFormat { get; set; }
        [JsonIgnore]
        public string AnswerText { get; set; }
        [JsonIgnore]
        public RestrictionAnswerOption AnswerOption { get; set; }
        public RestrictionAnswerList RestrictionAnswerList { get; set; }
    }

    public class RestrictionAnswerList
    {
        public string RestrictionAnswerListCode { get; set; }
        public RestrictionAnswerOption[] RestrictionAnswer { get; set; }
    }

    public class RestrictionAnswerOption
    {
        public string RestrictionAnswerId { get; set; }
        public string RestrictionAnswerOrderNumber { get; set; }
        public string RestrictionAnswerText { get; set; }
    }
}
