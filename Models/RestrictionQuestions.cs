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

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sierra_Romeo
{
    public class RestrictionQuestionsResponse
    {
        public string PrescriberId { get; set; }
        public string ItemCode { get; set; }
        public string RestrictionCode { get; set; }
        public RestrictionQuestionWrapper RestrictionQuestionDetails { get; set; }
        public DynamicQandADetail DynamicQuestions { get; set; }
        public StatusMessage[] StatusMessages { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }


    public class RestrictionQuestionWrapper
    {
        public RestrictionQuestionDetail[] RestrictionQuestion { get; set; }
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
        [JsonConverter(typeof(TrimmingConverter))]
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
        public int RestrictionAnswerID { get; set; }
        public int RestrictionAnswerOrderNumber { get; set; }
        [JsonConverter(typeof(TrimmingConverter))]
        public string RestrictionAnswerText { get; set; }
    }

    public class DynamicQandADetail
    {
        public Row[] Rows { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }

    public class Row
    {
        public int DisplayOrder { get; set; }
        public string ActivationTypeCode { get; set; }
        public string ActivationRefId { get; set; }
        public string ActivationMappingType { get; set; }
        public Column[] Columns { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }

    public class Column
    {
        public string QuestId { get; set; }
        public string QuestType { get; set; }
        public string QuestText { get; set; }
        public string QuestGroup { get; set; }
        public int DisplayOrder { get; set; }
        public string HtmlPostText { get; set; }
        public string HtmlHintText { get; set; }
        public string AnsDataType { get; set; }
        public int AnsMaxLength { get; set; }
        public string AnsDecFormat { get; set; }
        public AnsOption[] AnsOptions { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }

    public class AnsOption
    {
        public string OptText { get; set; }
        public string OptValue { get; set; }
        public int DisplayOrder { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; }
    }

    public class DQMSRestrictionQuestion
    {
        public int DisplayOrder { get; set; }
        public string ActivationTypeCode { get; set; }
        public string ActivationRefId { get; set; }
        public string ActivationMappingType { get; set; }
        public Column[] Columns { get; set; }
    }

    public abstract class DQMSRestrictionQuestionBase
    {
        public string QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionGroup { get; set; }
        public string Hint { get; set; }
        public abstract List<AuthorityDQAnswer> GetQuestAnswerValues();
    }

    public class DQMSRadioOption
    {
        public string Value { get; set; }
        public string QuestionText { get; set; }
        public string QuestionGroup { get; set; }
    }

    public class DQMSRadioGroup : DQMSRestrictionQuestionBase
    {
        public string Value { get; set; }

        public DQMSRadioOption[] Options { get; set; }

        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return new List<AuthorityDQAnswer> { new AuthorityDQAnswer
                {
                    Id = QuestionId,
                    Group = QuestionGroup,
                    AnswerDataType = "TEXT",
                    AnswerString = Value,
                }
            };
        }
    }

    public class DQMSCheckbox : DQMSRestrictionQuestionBase
    {
        public bool Value { get; set; }

        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return new List<AuthorityDQAnswer> { new AuthorityDQAnswer
                {
                    Id = QuestionId,
                    Group = QuestionGroup,
                    AnswerDataType= "IND",
                    AnswerString = Value? "Y": "N",
                }
            };
        }
    }

    public class DQMSIndicator : DQMSRestrictionQuestionBase
    {
        public string Value { get; set; }

        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return new List<AuthorityDQAnswer> { new AuthorityDQAnswer
                {
                    Id = QuestionId,
                    Group = QuestionGroup,
                    AnswerDataType= "IND",
                    AnswerString = Value?.ToString()
                }
            };
        }
    }

    public class DQMSText : DQMSRestrictionQuestionBase
    {
        public string Value { get; set; }

        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return new List<AuthorityDQAnswer> { new AuthorityDQAnswer
                {
                    Id = QuestionId,
                    Group = QuestionGroup,
                    AnswerDataType = "TEXT",
                    AnswerString = Value,
                }
            };
        }
    }

    public class DQMSDecimal : DQMSRestrictionQuestionBase
    {
        public decimal Value { get; set; }

        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return new List<AuthorityDQAnswer> { new AuthorityDQAnswer
                {
                    Id = QuestionId,
                    Group = QuestionGroup,
                    AnswerDataType = "DEC",
                    AnswerDecimal = Value,
                }
            };
        }
    }

    public class DQMSInteger : DQMSRestrictionQuestionBase
    {
        public int Value { get; set; }

        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return new List<AuthorityDQAnswer> { new AuthorityDQAnswer
                {
                    Id = QuestionId,
                    Group = QuestionGroup,
                    AnswerDataType = "DEC",
                    AnswerNumber = Value,
                }
            };
        }
    }

    public class DQMSDate : DQMSRestrictionQuestionBase
    {
        public DateTime Value { get; set; }

        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return new List<AuthorityDQAnswer> { new AuthorityDQAnswer
                {
                    Id = QuestionId,
                    Group = QuestionGroup,
                    AnswerDataType = "DATE",
                    AnswerDateTime = Value,
                }
            };
        }
    }

    public class DQMSMultiLine : DQMSRestrictionQuestionBase
    {
        public string Value { get; set; }

        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return new List<AuthorityDQAnswer> { new AuthorityDQAnswer
                {
                    Id = QuestionId,
                    Group = QuestionGroup,
                    AnswerDataType = "MULTLN",
                    AnswerString = Value,
                }
            };
        }
    }

    public class DQMSHeader : DQMSRestrictionQuestionBase
    {
        public override List<AuthorityDQAnswer> GetQuestAnswerValues()
        {
            return null;
        }
    }
}
