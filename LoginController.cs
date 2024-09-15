/*
 * Sierra Romeo: Login controller
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
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;

namespace Sierra_Romeo
{

    public class LoginController : INotifyPropertyChanged
    {

        public enum LogonStates
        {
            LoggedOff,
            LoggedOn,
            Authenticating,
            Renewing
        }

        // See https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-implement-property-change-notification?view=netframeworkdesktop-4.8
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isLoggedOn = false;
        private LogonStates logonState = LogonStates.LoggedOff;

        public bool IsLoggedOn
        {
            get => isLoggedOn;
            set
            {
                isLoggedOn = value;
                OnPropertyChanged();
            }
        }

        public LogonStates LogonState
        {
            get => logonState;
            set
            {
                logonState = value;
                OnPropertyChanged();
            }
        }
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime Expiry { get; set; } = DateTime.MinValue;

        private readonly DispatcherTimer renewTimer = new DispatcherTimer();

        public LoginController()
        {
            renewTimer.Tick += new EventHandler(RenewTokenAsync);
        }

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string NewAuthRequest()
        {
            var req = new PRODA.AuthRequest();
            Debug.WriteLine($"Preparing new AuthRequest with state {req.State}");

            pending.Add(req.State, req);
            string url = PRODA.KickoffURL(req);
            Debug.WriteLine($"Opening URL: {url}");
            return url;
        }

        public class AuthTokenResponse
        {

            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public string scope { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }
        }

        public void ProcessAuthReply(string state, string code)
        {
            if (state == null || code == null)
            {
                Debug.WriteLine($"Problem with returned authorisation code: state {state}, code {code}");
                return;
            }
            Debug.WriteLine($"Got reply: state {state}, code {code}");

            if (!pending.ContainsKey(state))
            {
                Debug.WriteLine($"Got AuthRequest reply for {state}, not in pending list");
                return;
            }

            Debug.WriteLine($"Got AuthRequest reply for {state}, in pending list");

            LogonState = LogonStates.Authenticating;

            var p = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", Properties.Settings.Default.uriScheme + ":authcode" },
                    { "client_id",  Properties.Settings.Default.clientId },
                    { "code_verifier", pending[state].Verifier }
                };

            pending.Remove(state);

            RequestNewToken(PRODA.RequestEndpoint(), p);
            return;
        }

        public async void RequestNewToken(string endpoint, Dictionary<string, string> requestParams)
        {
            var c = new FormUrlEncodedContent(requestParams);
            Debug.WriteLine($"Sending token request to {endpoint}");
            var requestTime = DateTime.Now;
            string responseString = "";
            try
            {
                HttpResponseMessage response = await client.PostAsync(endpoint, c);
                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                // XXX
                MessageBox.Show(e.Message, "Sierra Romeo: Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var authToken = JsonSerializer.Deserialize<AuthTokenResponse>(responseString);

            if (authToken.ExtensionData != null && authToken.ExtensionData.ContainsKey("error"))
            {
                string err = authToken.ExtensionData["error_description"].ToString();
                Debug.WriteLine($"Got error: {err}");
                MessageBox.Show($"Error in processing authentication ticket reply: {err}", "Sierra Romeo: Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                IsLoggedOn = false;
                LogonState = LogonStates.LoggedOff;
                return;
            }

            IsLoggedOn = true;
            LogonState = LogonStates.LoggedOn;
            AccessToken = authToken.access_token;
            RefreshToken = authToken.refresh_token;
            Expiry = requestTime.AddSeconds(authToken.expires_in);
            Debug.WriteLine($"Got new access token ({AccessToken}) with expiry in {authToken.expires_in} at {Expiry}");

            renewTimer.Interval = new TimeSpan(0, 0, authToken.expires_in - 60); // new TimeSpan(0, 0, 30);
            renewTimer.Start();

            return;
        }

        public void RenewTokenAsync(object sender, EventArgs e)
        {

            renewTimer.Stop();

            if (Expiry < DateTime.Now)
            {
                Debug.WriteLine("Token expired before renewal could take place; logging off.");
                IsLoggedOn = false;
                LogonState = LogonStates.LoggedOff;
                return;
            }

            Debug.WriteLine("Renewing token...");
            LogonState = LogonStates.Renewing;

            var p = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", RefreshToken },
                    { "client_id",  Properties.Settings.Default.clientId },
                };

            RequestNewToken(PRODA.RefreshEndpoint(), p);

            return;

        }

        private readonly Dictionary<string, PRODA.AuthRequest> pending = new Dictionary<string, PRODA.AuthRequest>();
        private static readonly HttpClient client = new HttpClient();
    }


    static class PRODA
    {
        private static readonly string host = Properties.Settings.Default.prodaEndpoint;
        public static string RequestEndpoint()
        {
            return host + "/mga/sps/oauth/oauth20/token";
        }

        public static string RefreshEndpoint()
        {
            return host + "/mga/sps/oauth/oauth20/token";
        }

        public static string RevokeEndpoint()
        {
            return host + "/mga/sps/oauth/oauth20/revoke";
        }

        public static string KickoffEndpoint()
        {
            return host + "/mga/sps/oauth/oauth20/authorize";
        }

        public static string Base64UrlEncode(string s)
        {
            // This could be passed by reference but it's hardly performance-critical
            s = s.TrimEnd('=');
            s = s.Replace('+', '-');
            s = s.Replace('/', '_');
            return s;
        }

        public static string GetRandomBytes32()
        {
            byte[] b = new byte[32];
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                rg.GetBytes(b);
            }
            return Base64UrlEncode(Convert.ToBase64String(b));
        }

        public static string VerifierToChallenge(string v)
        {
            byte[] b = Encoding.ASCII.GetBytes(v);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] h = hashstring.ComputeHash(b);
            return Base64UrlEncode(Convert.ToBase64String(h));
        }

        public static string KickoffURL(AuthRequest authRequest)
        {
            string endpoint = KickoffEndpoint();
            string client_id = Properties.Settings.Default.clientId;
            string redirect_uri = WebUtility.UrlEncode(Properties.Settings.Default.uriScheme + ":authcode");
            string code_challenge = VerifierToChallenge(authRequest.Verifier);

            return $"{endpoint}?response_type=code&client_id={client_id}&scope=PBSAuthorities" +
                $"&nonce={authRequest.Nonce}&state={authRequest.State}&redirect_uri={redirect_uri}" +
                $"&code_challenge={code_challenge}&code_challenge_method=S256";
        }

        public class AuthRequest
        {
            public string State { get; }
            public string Verifier { get; }
            public string Nonce { get; }

            public AuthRequest()
            {

                Verifier = GetRandomBytes32();
                State = GetRandomBytes32();
                Nonce = GetRandomBytes32();

            }
        }

    }

}
