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

using System.Collections.Generic;
using System.Windows;

namespace Sierra_Romeo
{
    /// <summary>
    /// Interaction logic for QuestionsWindow.xaml
    /// </summary>
    public partial class QuestionsWindow : Window
    {
        public RestrictionQuestionDetail[] RestrictionQuestions { get; set; }
        public List<AuthorityAnswer> RestrictionAnswers { get; set; }
        public QuestionsWindow(RestrictionQuestionDetail[] RestrictionQuestions)
        {
            // Setting the property from the caller means the data is not available
            // when the components are initalised, so do it in the constructor instead
            this.RestrictionQuestions = RestrictionQuestions;
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            RestrictionAnswers = new List<AuthorityAnswer>();
            foreach (var q in RestrictionQuestions)
            {
                var a = new AuthorityAnswer
                {
                    Code = q.RestrictionQuestionCode
                };
                if (q.RestrictionAnswerType == "LIST")
                {
                    a.AnswerListCode = q.RestrictionAnswerList.RestrictionAnswerListCode;
                    a.AnswerId = int.Parse(q.AnswerOption.RestrictionAnswerId);
                }
                else
                {
                    a.AnswerText = q.AnswerText;
                }

                RestrictionAnswers.Add(a);
            }
            DialogResult = true;
        }
    }
}
