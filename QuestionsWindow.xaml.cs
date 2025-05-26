/*
 * Sierra Romeo: Restriction questions window
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
using System.Windows;
using System.Windows.Data;

namespace Sierra_Romeo
{
    /// <summary>
    /// Interaction logic for QuestionsWindow.xaml
    /// </summary>
    public partial class QuestionsWindow : Window
    {
        public CompositeCollection RestrictionQuestions { get; set; }
        public List<AuthorityDQAnswer> DQMSRestrictionAnswers { get; set; }
        public List<AuthorityAnswer> RestrictionAnswers { get; set; }

        public QuestionsWindow(CompositeCollection RestrictionQuestions)
        {
            // Setting the property from the caller means the data is not available
            // when the components are initalised, so do it in the constructor instead
            this.RestrictionQuestions = RestrictionQuestions;
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            RestrictionAnswers = new List<AuthorityAnswer>();
            DQMSRestrictionAnswers = new List<AuthorityDQAnswer>();

            var missingQuestions = new List<string>();

            foreach (var q in RestrictionQuestions)
            {
                if (q is RestrictionQuestionDetail qamsq)
                {
                    var a = new AuthorityAnswer
                    {
                        RestrictionQuestionCode = qamsq.RestrictionQuestionCode
                    };
                    if (qamsq.RestrictionAnswerType == "LIST")
                    {
                        a.RestrictionAnswerListCode = qamsq.RestrictionAnswerList.RestrictionAnswerListCode;
                        a.RestrictionAnswerID = qamsq.AnswerOption?.RestrictionAnswerID.ToString();
                    }
                    else
                    {
                        a.RestrictionQuestionAnswer = qamsq.AnswerText;
                    }

                    if (qamsq.RestrictionQuestionMandatory && a.RestrictionAnswerID == null && (a.RestrictionQuestionAnswer == null || a.RestrictionQuestionAnswer == ""))
                    {
                        missingQuestions.Add(qamsq.RestrictionQuestionText);
                    }
                    RestrictionAnswers.Add(a);
                }
                else
                {
                    DQMSRestrictionQuestionBase dqmsq = q as DQMSRestrictionQuestionBase;
                    DQMSRestrictionAnswers.AddRange(dqmsq.GetQuestAnswerValues());
                    /// XXX it is theoretically possible to validate the form answers here, although complex
                }
            }

            if (missingQuestions.Count > 0)
            {
                MessageBox.Show($"The following questions must be answered:\n\n{String.Join("\n", missingQuestions)}", "Sierra Romeo: Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                DialogResult = true;
            }
        }
    }
}
