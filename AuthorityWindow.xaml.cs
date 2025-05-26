/*
 * Sierra Romeo: Authority request creation window
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
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Sierra_Romeo
{
    /// <summary>
    /// Interaction logic for AuthorityWindow.xaml
    /// </summary>
    public partial class AuthorityWindow : Window
    {
        internal LoginController loginController;
        internal AuthorityRequestController requestController;
        private readonly AuthorityRequest currentRequest;
        public AMTDrug CurrentDrug { get; set; } = new AMTDrug();
        private string lastDrugSearch = "";
        private CancellationTokenSource questionsCancellationToken = null;
        private List<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();
        private bool resultCopied = false;

        public AuthorityWindow(LoginController loginController, IImporter importer)
        {
            InitializeComponent();
            this.loginController = loginController;
            requestController = new AuthorityRequestController(loginController);

            currentRequest = new AuthorityRequest();

            if (importer != null)
            {
                ExternalRequest imported = importer.Import();
                if (imported != null)
                {
                    // The import failed; perhaps the file path was unreadable,
                    // or the JSON was malformed.

                    currentRequest.ScriptNumber = imported.ScriptNumber;
                    currentRequest.PatientDetails.PatientFirstName = imported.FirstName;
                    currentRequest.PatientDetails.PatientSurname = imported.Surname;
                    currentRequest.PatientDetails.MedicareNumber = imported.MedicareNumber;
                    currentRequest.ItemDetails.Quantity = imported.Quantity ?? 0;
                    currentRequest.ItemDetails.NumberOfRepeats = imported.Repeats ?? 0;
                    currentRequest.ItemDetails.Dose = imported.Dose;
                    currentRequest.ItemDetails.DoseFrequency = imported.DoseFrequency;

                    if (imported.SearchTerm != null && imported.SearchTerm != "")
                    {
                        lastDrugSearch = imported.SearchTerm;
                    }
                }

            }

            if (currentRequest.ItemDetails.DoseFrequency == null)
            {
                currentRequest.ItemDetails.DoseFrequency = 1;
            }

            DataContext = currentRequest;

            Show();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (approvalNumberText.Text != "" && resultCopied == false)
            {
                var ret = MessageBox.Show("Copy authority approval number to clipboard?", "Sierra Romeo: Copy authority number?",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (ret == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (ret == MessageBoxResult.Yes)
                {
                    Copy_Click(sender, e);
                    Close();
                }
                else // MessageBoxResult.No
                {
                    Close();
                }

            }
            else
            {
                Close();
            }
        }

        private void DrugSearch_Click(object sender, RoutedEventArgs e)
        {
            var SearchWindow = new SearchDrug
            {
                Owner = this,
                PrescriberNumber = currentRequest.PrescriberID
            };
            if (lastDrugSearch != "")
            {
                SearchWindow.queryInput.Text = lastDrugSearch;
            }
            bool? ret = SearchWindow.ShowDialog();
            if (ret.HasValue && ret.Value)
            {
                selectDrug.DataContext = CurrentDrug = SearchWindow.CurrentDrug;
                ItemSearch_Click(sender, null);
            }
            lastDrugSearch = SearchWindow.queryInput.Text;
        }


        private async void ItemSearch_Click(object sender, RoutedEventArgs e)
        {
            // Cancel any pending searches for restriction questions, otherwise the
            // restriction dialog can appear over the search dialog
            questionsCancellationToken?.Cancel();

            var SearchWindow = new SearchItem
            {
                Owner = this,
                PrescriberNumber = currentRequest.PrescriberID
            };
            if (CurrentDrug != null)
            {
                SearchWindow.queryInput.Text = CurrentDrug.AMTCode;
            }

            bool? searchRet = SearchWindow.ShowDialog();

            if (searchRet.HasValue && searchRet.Value)
            {
                currentRequest.ItemDetails.Item = SearchWindow.CurrentItem;
                currentRequest.RestrictionQuestionDetails.RestrictionCode = SearchWindow.CurrentItem.Restriction;

                // Rather than implement INotifyPropertyChanged on AMTDrug,
                // just build a new one and make it the datacontext for the label
                CurrentDrug = new AMTDrug
                {
                    Drug = SearchWindow.CurrentItem.Drug,
                    Brands = SearchWindow.CurrentItem.Brands,
                    AMTCode = SearchWindow.CurrentItem.AMTCode
                };
                selectDrug.DataContext = CurrentDrug;

                currentRequest.RestrictionQuestionDetails.RestrictionQuestion = null;

                using (questionsCancellationToken = new CancellationTokenSource())
                {
                    try
                    {
                        Cursor = Cursors.Wait;

                        var restrictionQuestions = await requestController.GetRestrictionQuestions(currentRequest, questionsCancellationToken.Token);

                        if (restrictionQuestions != null)
                        {
                            var QuestionsWindow = new QuestionsWindow(restrictionQuestions)
                            {
                                Owner = this,
                            };
                            bool? questionsRet = QuestionsWindow.ShowDialog();

                            if (questionsRet.HasValue && questionsRet.Value)
                            {
                                if (QuestionsWindow.DQMSRestrictionAnswers.Count > 0)
                                {
                                    currentRequest.DynamicQuestAnswerValue = QuestionsWindow.DQMSRestrictionAnswers.ToArray();
                                }
                                else
                                {
                                    currentRequest.DynamicQuestAnswerValue = null;
                                }
                                if (QuestionsWindow.RestrictionAnswers.Count > 0)
                                {
                                    currentRequest.RestrictionQuestionDetails.RestrictionQuestion = QuestionsWindow.RestrictionAnswers.ToArray();
                                }
                                else
                                {
                                    currentRequest.RestrictionQuestionDetails.RestrictionQuestion = null;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }
                questionsCancellationToken = null;
            }
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {

            // Validate the form entries unless the shift key is held
            // This is useful in testing but is left in the release builds
            // in case there is some problem with validation.
            bool validate = true;
            if (Keyboard.IsKeyDown(Key.LeftShift) ||
                Keyboard.IsKeyDown(Key.RightShift))
            {
                validate = false;
            }

            if (validate)
            {
                // There's no easy way of running all validators for a form
                // This might get out of date with XAML but it's a start
                TextBox[] controls = { prescriberNumberInput,
                    medicareCardInput,
                    scriptNumberInput,
                    doseInput, quantityInput, repeatsInput};
                foreach (var control in controls)
                {
                    BindingExpression be = control.GetBindingExpression(TextBox.TextProperty);
                    be.UpdateSource();
                }

                // Start with the WPF validation errors, then add things that
                // can't be expressed in validation
                // I can't believe it's not a map operation
                List<string> validationMessages = ValidationErrors.ConvertAll(c =>
                    { return c.ErrorContent.ToString(); });

                // These fields are initialised to empty strings
                if (currentRequest.PatientDetails.PatientFirstName == "" &&
                    currentRequest.PatientDetails.PatientSurname == "")
                {
                    validationMessages.Add("At least one of first name or surname must be provided");
                }

                // Code is a calculated property that returns the empty string if the item is null
                if (currentRequest.ItemDetails.ItemCode == "")
                {
                    validationMessages.Add("A drug/item must be selected");
                }

                if (validationMessages.Count > 0)
                {
                    string msg = "The request cannot be submitted until the following problems are corrected:\n\n";
                    msg += string.Join("\n", validationMessages);
                    msg = msg.TrimEnd();
                    MessageBox.Show(msg, "Sierra Romeo: Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning); ;
                    return;
                }
            }


            if (!loginController.IsLoggedOn)
            {
                string url = loginController.NewAuthRequest();
                Process.Start(url);
                return;
            }

            Cursor = Cursors.Wait;

            // Rather than supporting cancellation, only allow the request to be submitted once
            // (unless a null result is returned, indicating it's unlikely to have been received
            // and processed).
            // A rejected request leaves the editable property as true, and these should
            // be able to be resubmitted as well.

            // The submit button is normally bound to the declaration checkbox
            // Save this binding to restore it later if needed
            Binding binding = submitButton.GetBindingExpression(IsEnabledProperty).ParentBinding;
            submitButton.IsEnabled = false;
            AuthorityResponse response = await requestController.SubmitRequest(currentRequest);

            if (response != null)
            {
                // Any response - show the result panel
                resultPanel.Visibility = Visibility.Visible;
                // An action has been taken, so there's no cancelling now
                cancelButton.Content = "Close";
            }

            if (response == null || currentRequest.Editable == true)
            {
                // If there's no response, or it can be resubmitted, restore the old binding
                submitButton.SetBinding(IsEnabledProperty, binding);
            }

            Cursor = Cursors.Arrow;
            resultPanel.DataContext = response;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            resultCopied = true;
            Clipboard.SetText(approvalNumberText.Text);
        }

        private async void Override_Click(object sender, RoutedEventArgs e)
        {

            if (!loginController.IsLoggedOn)
            {
                string url = loginController.NewAuthRequest();
                Process.Start(url);
                return;
            }

            Cursor = Cursors.Wait;
            // Rather than supporting cancellation, only allow the request to be submitted once
            // (unless a null result is returned, indicating it's unlikely to have been received
            // and processed).
            // A rejected request leaves the editable property as true, and these should
            // be able to be resubmitted as well.

            // The submit button is normally bound to the declaration checkbox
            // Save this binding to restore it later if needed
            Binding binding = submitButton.GetBindingExpression(IsEnabledProperty).ParentBinding;
            submitButton.IsEnabled = false;
            // The override button has no binding normally
            overrideButton.IsEnabled = false;

            AuthorityResponse response = await requestController.UpdateRequest(currentRequest, (OverrideDetail)overrideSelect.SelectedItem);

            if (response == null)
            {
                // If there's no response, restore the old binding & reenable the override submission
                submitButton.SetBinding(IsEnabledProperty, binding);
                overrideButton.IsEnabled = true;
            }

            Cursor = Cursors.Arrow;
            resultPanel.DataContext = response;
        }

        private void Window_Error(object sender, ValidationErrorEventArgs e)
        {
            // https://stackoverflow.com/a/4274961
            // https://stackoverflow.com/a/1616211
            if (e.Action == ValidationErrorEventAction.Added)
            {
                ValidationErrors.Add(e.Error);
            }
            else
            {
                ValidationErrors.Remove(e.Error);
            }
        }

        private void StackPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Reset the interval/interval units on show or hide of the "every:" stackpanel
            currentRequest.ItemDetails.DoseInterval = null;
            currentRequest.ItemDetails.DoseIntervalUnit = null;
            doseIntervalUnit.SelectedItem = null;
        }
    }
}
