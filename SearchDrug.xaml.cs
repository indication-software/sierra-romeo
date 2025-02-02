/*
 * Sierra Romeo: Drug search window
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

using HttpTracer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sierra_Romeo
{
    /// <summary>
    /// Interaction logic for SearchDrug.xaml
    /// </summary>
    public partial class SearchDrug : Window
    {
        internal static HttpTracerHandler tracer = new HttpTracerHandler
        {
            Verbosity = HttpMessageParts.All
        };
        private static readonly HttpClient client = new HttpClient(tracer);
        private static readonly Uri endpoint = new Uri(Properties.Settings.Default.pbsserveEndpoint + "/v1/drug");
        public string PrescriberNumber;

        private CancellationTokenSource cancellationToken = null;

        public AMTDrug CurrentDrug;

        public SearchDrug()
        {
            InitializeComponent();
            // Close connections after 120 seconds, see https://contrivedexample.com/2017/07/01/using-httpclient-as-it-was-intended-because-youre-not/ etc
            ServicePointManager.FindServicePoint(endpoint).ConnectionLeaseTimeout = 120 * 1000;
        }

        private async Task<List<AMTDrug>> GetSearchResultsAsync(string query, CancellationToken cancellationToken)
        {
            Uri uri = new Uri(endpoint + "?q=" + query);

            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("SierraRomeo", "1.0.0"));
            req.Headers.Host = uri.Host;
            req.Headers.Add("Authorisation", PrescriberNumber);

            HttpResponseMessage response = await client.SendAsync(req, cancellationToken);
            string responseString = await response.Content.ReadAsStringAsync();

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                //XXX
                MessageBox.Show(e.Message + responseString, "Sierra Romeo: Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var results = JsonSerializer.Deserialize<AMTDrugSearchResults>(responseString, serializeOptions);

            // This is a bit of an abuse of the cancellation token system, as cancellation is generally
            // supposed to be a suggestion rather than an order, but for internal use I think it's fine
            cancellationToken.ThrowIfCancellationRequested();
            return results.Results;
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            cancellationToken?.Cancel();
            using (cancellationToken = new CancellationTokenSource())
            {
                var token = cancellationToken.Token;
                resultsList.ItemsSource = new List<PBSItem>();
                try
                {
                    List<AMTDrug> res = await GetSearchResultsAsync(queryInput.Text, token);
                    if (!token.IsCancellationRequested && res != null)
                    {
                        resultsList.ItemsSource = res;
                        if (resultsList.Items.Count == 1)
                        {
                            resultsList.SelectedItem = resultsList.Items[0];
                        }
                        // When autosearch is added, this will need to be conditional on
                        // whether the search was clicked
                        resultsList.Focus();
                    }
                }
                catch (OperationCanceledException)
                {

                }
            }
            cancellationToken = null;
        }

        private void SearchItem_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OK_Click(sender, null);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (resultsList.SelectedItems.Count == 1)
            {
                CurrentDrug = (AMTDrug)resultsList.SelectedItem;
                DialogResult = true;
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (queryInput.Text != "")
            {
                Search_Click(sender, null);
            }
            else
            {
                queryInput.Focus();
            }
        }
    }
}
