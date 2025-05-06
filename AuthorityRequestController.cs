/*
 * Sierra Romeo: Authority Request controller
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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sierra_Romeo
{
    public class AuthorityRequestController
    {
        internal LoginController loginController;
        internal static HttpTracerHandler tracer = new HttpTracerHandler(new TraceLogger())
        {
            Verbosity = HttpMessageParts.All
        };
        private static readonly HttpClient client = new HttpClient(tracer);
        private static readonly Uri submitEndpoint = new Uri(System.Configuration.ConfigurationManager.AppSettings["pbsEndpoint"] + "/assess/submit");
        private static readonly Uri updateEndpoint = new Uri(System.Configuration.ConfigurationManager.AppSettings["pbsEndpoint"] + "/assess/submit");
        private static readonly Uri questionsEndpointBase = new Uri(System.Configuration.ConfigurationManager.AppSettings["pbsEndpoint"] + "/question/");
        private static readonly JsonSerializerOptions serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        internal static string unexpectedResponseMessage = @"The Services Australia server returned an unexpected response:

Code: {0}
Message: {1}

You can report this message to info@sierraromeo.com.au.";

        public AuthorityRequestController(LoginController loginController)
        {
            this.loginController = loginController;
            // Close connections after 60 seconds, see https://contrivedexample.com/2017/07/01/using-httpclient-as-it-was-intended-because-youre-not/ etc
            ServicePointManager.FindServicePoint(new Uri(System.Configuration.ConfigurationManager.AppSettings["pbsEndpoint"])).ConnectionLeaseTimeout = 60 * 1000;
        }

        public async Task<AuthorityResponse> SubmitRequest(AuthorityRequest authRequest)
        {
            string jsonString = JsonSerializer.Serialize(authRequest, serializeOptions);
            string responseString;
            HttpResponseMessage response;

            // XXX
            try
            {
                response = await client.SendAsync(PrepareRequest(authRequest.PrescriberID, submitEndpoint, HttpMethod.Post, jsonString));
                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                // XXX
                MessageBox.Show(e.Message, "Sierra Romeo: Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            try
            {
                response.EnsureSuccessStatusCode();
                var result = JsonSerializer.Deserialize<AuthorityResponse>(responseString, serializeOptions);

                if (result.AuthorityUniqueID != null)
                {
                    authRequest.AuthorityUniqueID = result.AuthorityUniqueID;
                }

                if (result.AssessmentDetails != null)
                {
                    return result;
                }
                else if (result.StatusMessages != null)
                {
                    // Sometimes results are returned with no AssessmentDetails sections but
                    // with rejection messages in StatusMessages (eg no first or last name provided.)
                    // Just fake up a new AssessmentDetails as a rejection.
                    result.AssessmentDetails = new AssessmentDetails
                    {
                        Code = "3",
                        Text = "Rejected"
                    };
                    return result;
                }
                else
                {
                    // Errors at this level should come in the format described in the document
                    // "PBS Authority Common Details" (section 3.1).
                    string msg = string.Format(unexpectedResponseMessage, result.ExtensionData["code"].ToString(), result.ExtensionData["message"].ToString());
                    MessageBox.Show(msg, "Sierra Romeo: Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (HttpRequestException e)
            {
                /// XXX
                MessageBox.Show(e.Message + responseString, "Sierra Romeo: Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public async Task<AuthorityResponse> UpdateRequest(AuthorityRequest authRequest, OverrideDetail overrideDetail)
        {
            authRequest.OverrideCode = overrideDetail.Code;
            string jsonString = JsonSerializer.Serialize(authRequest, serializeOptions);

            HttpResponseMessage response = await client.SendAsync(PrepareRequest(authRequest.PrescriberID, updateEndpoint, HttpMethod.Put, jsonString));
            string responseString = await response.Content.ReadAsStringAsync();

            try
            {
                response.EnsureSuccessStatusCode();
                var result = JsonSerializer.Deserialize<AuthorityResponse>(responseString, serializeOptions);

                if (result.AssessmentDetails != null)
                {
                    // If this is a finalised result, mark the request as finalised (this is reflected in the UI)
                    switch (result.AssessmentDetails.Code)
                    // Fun note, this is a string, not an integer, which means type checking
                    // didn't save me from having used the wrong property name above initially
                    {
                        case "1": // Approved
                        case "2": // Approved with changes
                        case "4": // Pending
                        case "5": // Previously rejected/now approved
                            authRequest.Editable = false;
                            break;
                        case "3": // Rejected - can be resent with changes
                        case "6": // Authority not required - may need changing
                                  // Editable is true by default
                            break;
                    }
                    return result;
                }
                else
                {
                    // XXX
                    string msg = string.Format(unexpectedResponseMessage, result.ExtensionData["code"].ToString(), result.ExtensionData["message"].ToString());
                    MessageBox.Show(msg, "Sierra Romeo: Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (HttpRequestException e)
            {
                /// XXX
                MessageBox.Show(e.Message + responseString, "Sierra Romeo: Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public async Task<RestrictionQuestionDetail[]> GetRestrictionQuestions(AuthorityRequest authRequest, CancellationToken cancellationToken)
        {
            string responseString;
            Uri url = new Uri(questionsEndpointBase, $"{authRequest.PrescriberID}/{authRequest.ItemDetails.ItemCode}/{authRequest.RestrictionQuestionDetails.RestrictionCode}");

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequestMessage.Headers.Host = url.Host;
            AddHeaders(httpRequestMessage, authRequest.PrescriberID);

            // XXX
            try
            {
                HttpResponseMessage response = await client.SendAsync(httpRequestMessage, cancellationToken);
                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                // XXX
                MessageBox.Show("An error occurred while requesting restriction questions: " + e.Message + " Your authority request may not be successful. Try selecting the item again.", "Sierra Romeo: Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            RestrictionQuestionsResponse responseObj;

            try
            {
                responseObj = JsonSerializer.Deserialize<RestrictionQuestionsResponse>(responseString, serializeOptions);
            }
            catch (JsonException e)
            {
                // XXX
                MessageBox.Show("An error occurred while decoding restriction questions: " + e.Message + " Your authority request may not be successful. Try selecting the item again.", "Sierra Romeo: Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return responseObj.RestrictionQuestionDetails.RestrictionQuestion;

        }

        public HttpRequestMessage PrepareRequest(string PrescriberId, Uri url, HttpMethod method, string json)
        {
            StringContent json_content = new StringContent(json, Encoding.UTF8, "application/json");
            MultipartFormDataContent content = new MultipartFormDataContent
            {
                // Embedded quotes are important here. The Services Australia endpoint requires them. This was
                // required by RFC 2616, but are not by RFC 6266. Perhaps there is an ancient HTTP library,
                // perhaps there are regular expressions for parsing HTTP headers.
                { json_content, "authoritydetails", "\"AssessAuthorityRequest.json\"" }
            };

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, url);
            httpRequestMessage.Headers.Host = url.Host;
            AddHeaders(httpRequestMessage, PrescriberId);

            httpRequestMessage.Content = content;

            return httpRequestMessage;
        }

        private void AddHeaders(HttpRequestMessage httpRequestMessage, string PrescriberId)
        {
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue("SierraRomeo", "1.0.0"));

            Guid guid = Guid.NewGuid();
            httpRequestMessage.Headers.Add("dhs-messageId", guid.ToString());
            httpRequestMessage.Headers.Add("dhs-correlationId", guid.ToString());
            httpRequestMessage.Headers.Add("dhs-auditIdType", "PRESCRIBER");
            httpRequestMessage.Headers.Add("dhs-auditId", PrescriberId);
            httpRequestMessage.Headers.Add("dhs-subjectId", PrescriberId);
            httpRequestMessage.Headers.Add("dhs-subjectIdType", "PRESCRIBER");
            httpRequestMessage.Headers.Add("dhs-productId", System.Configuration.ConfigurationManager.AppSettings["clientName"]);
            httpRequestMessage.Headers.Add("Authorization", loginController.AccessToken);
        }
    }
}
