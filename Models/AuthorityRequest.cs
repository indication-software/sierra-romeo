/*
 * Sierra Romeo: AuthorityRequest model
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
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Controls;

namespace Sierra_Romeo
{

    public class AuthorityItem : INotifyPropertyChanged

    {
        private PBSItem item;

        [JsonIgnore]
        public PBSItem Item
        {
            get => item; set
            {
                item = value;
                OnPropertyChanged();
            }
        }
        public string ItemCode
        {
            get
            {
                if (Item != null)
                {
                    return Item.ItemCode;
                }
                else
                {
                    return "";
                }
            }
        }
        public int Quantity { get; set; }
        public int NumberOfRepeats { get; set; }
        public string Dose { get; set; }
        public int? DoseFrequency { get; set; }
        public int? DoseInterval { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string DoseIntervalUnit { get; set; } = "";

        // See https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-implement-property-change-notification?view=netframeworkdesktop-4.8
        public event PropertyChangedEventHandler PropertyChanged;
        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class AuthorityPatient
    {
        public string MedicareNumber { get; set; } = "";
        public string PatientFirstName { get; set; } = "";
        public string PatientSurname { get; set; } = "";

    }

    public class AuthorityDetails
    {
        public string RestrictionCode { get; set; }

        public AuthorityAnswer[] RestrictionQuestion { get; set; }
    }

    public class AuthorityAnswer
    {
        /// <summary>
        ///  Answers in the QAMS format
        /// </summary>
        public int RestrictionQuestionCode { get; set; }
        public string RestrictionQuestionAnswer { get; set; }
        public string RestrictionAnswerListCode { get; set; }
        public string RestrictionAnswerID { get; set; }
    }

    public class AuthorityDQAnswer
    {
        /// <summary>
        /// Answers in the DQ&A format
        /// </summary>
        /// These properties do not match the JSON property names because the latter are bad
        [JsonPropertyName("questId")]
        public string Id { get; set; } = "";
        [JsonPropertyName("questGroup")]
        public string Group { get; set; } = "";
        [JsonPropertyName("ansDataType")]
        public string AnswerDataType { get; set; } = "";
        [JsonPropertyName("ansString")]
        public string AnswerString { get; set; }
        [JsonPropertyName("ansNumber")]
        public double? AnswerNumber { get; set; }
        [JsonIgnore]
        public DateTime AnswerDateTime { get; set; }
        [JsonPropertyName("ansDate")]
        public string AnswerDate
        {
            get
            {
                if (AnswerDateTime == DateTime.MinValue)
                {
                    return null;
                }
                else
                {
                    return AnswerDateTime.ToString("yyyy-MM-dd"); ;
                }
            }
        }
        [JsonPropertyName("ansDecFormat")]
        public decimal? AnswerDecimal { get; set; }
    }

    public class AuthorityRequest : INotifyPropertyChanged
    {
        /// <summary>
        /// This class holds the details of the current authority request
        /// and is used as the scaffolding for the JSON request
        /// </summary>
        public string PrescriberID { get; set; } = Properties.Settings.Default.PrescriberNumber;

        [JsonPropertyName("authorityPrescriptionNumber")]
        public string ScriptNumber { get; set; } = "";

        public AuthorityPatient PatientDetails { get; set; } = new AuthorityPatient();

        public AuthorityItem ItemDetails { get; set; } = new AuthorityItem();

        public string CertifyIndicator { get; set; } = "Y";

        public AuthorityDetails RestrictionQuestionDetails { get; set; } = new AuthorityDetails();

        public AuthorityDQAnswer[] DynamicQuestAnswerValue { get; set; }

        // Only used for updates
        public string OverrideCode { get; set; }

        // Only used for updates
        public string AuthorityUniqueID { get; set; }

        [JsonIgnore]
        private bool editable = true;

        [JsonIgnore]
        public bool Editable
        {
            get => editable; set
            {
                editable = value;
                OnPropertyChanged();
            }
        }

        // See https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-implement-property-change-notification?view=netframeworkdesktop-4.8
        public event PropertyChangedEventHandler PropertyChanged;
        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MustExistValidation : ValidationRule
    {

        public string PropertyName { get; set; }
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {

            if (PropertyName == null)
            {
                return ValidationResult.ValidResult;
            }

            if (value == null || value.ToString().Length == 0)
            {
                return new ValidationResult(false, $"{PropertyName} must be provided");
            }

            return ValidationResult.ValidResult;
        }

    }

    public class NumericValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            // https://stackoverflow.com/a/7461095
            foreach (char c in value.ToString())
            {
                if (c < '0' || c > '9')
                    return new ValidationResult(false, "Entry must be all digits");
            }
            return ValidationResult.ValidResult;
        }
    }

    public class AuthorityNumberValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            // Algorithm from https://www.pbs.gov.au/news/2020/09/files/Authority-Prescription-Number-Format-Fact-Sheet.docx
            // First seven digits added mod 9 should equal eighth digit
            char[] charAuthNum = value.ToString().PadLeft(8, '0').ToCharArray();
            int[] intIdentifer = Array.ConvertAll(charAuthNum, c => (int)char.GetNumericValue(c));

            int calculatedChecksum = 0;
            for (int i = 0; i < 7; i++) // [7] is the check digit
            {
                calculatedChecksum += intIdentifer[i];
            }

            if (calculatedChecksum % 9 != intIdentifer[7])
            {
                return new ValidationResult(false, "Authority prescription number is not valid");
            }

            return ValidationResult.ValidResult;
        }
    }

    public class MedicareValidation : ValidationRule
    {

        private readonly int[] weights = { 1, 3, 7, 9, 1, 3, 7, 9 };

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string cardNum = value.ToString();

            if (cardNum.Length == 0)
            {
                return new ValidationResult(false, "Medicare card number is required");
            }
            if (cardNum.Length != 11)
            {
                return new ValidationResult(false, "Medicare card number is incorrect length");
            }

            char[] charIdentifier = cardNum.Substring(0, 8).ToCharArray();
            // https://stackoverflow.com/a/21587297
            int[] intIdentifer = Array.ConvertAll(charIdentifier, c => (int)char.GetNumericValue(c));

            int checksum = (int)char.GetNumericValue(cardNum[8]);

            // This would be better as a map operation but requires taking LINQ as a dependency
            int calculatedChecksum = 0;
            for (int i = 0; i < intIdentifer.Length; i++)
            {
                calculatedChecksum += intIdentifer[i] * weights[i];
            }

            if (calculatedChecksum % 10 != checksum)
            {
                return new ValidationResult(false, "Medicare card number is not valid");
            }

            return ValidationResult.ValidResult;
        }
    }
}
