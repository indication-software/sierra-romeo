/*
 * Sierra Romeo: entry points
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

using HttpTracer.Logger;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Web;
using System.Windows;

namespace Sierra_Romeo
{
    // Single instance work taken from https://github.com/microsoft/WPF-Samples/blob/master/Application%20Management/SingleInstanceDetection/
    public class SierraRomeoApp : Application
    {

        private MainWindow mw;
        public LoginController loginController;

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            URIHandler.AddURIHandler("x-sierra-romeo", System.Reflection.Assembly.GetExecutingAssembly().Location);

            loginController = new LoginController();

            // Create and show the application's main window
            mw = new MainWindow(loginController);

            if (e.Args.Length != 0)
            {
                ParseArgs(e.Args);
            }
            mw.Show();
        }

        public void Activate(string[] eventArgs)
        {
            // Reactivate application's main window
            MainWindow.Activate();
            if (eventArgs.Length != 0)
            {
                ParseArgs(eventArgs);
            }
        }

        private void ParseArgs(string[] args)
        {
            // Two different argument types are supported:
            // 1. An x-sierra-romeo: URI - this is untrusted input as it may come from
            //    outside the desktop security boundary
            // 2. A file path (eg from direct invocation or drag and drop) - this
            //    is considered more trustworthy as the program is not registered as a file:// handler
            //    and any execution with a file argument must come from the same user
            // More than one file path can be given, but there should be no mixing and matching

            Debug.WriteLine($"Called with arguments: {string.Join(" ", args)}");
            Uri uri;
            try
            {
                uri = new Uri(args[0]);
            }
            catch (UriFormatException)
            {
                Debug.WriteLine("Could not parse first argument as URI");
                uri = null;
            }

            if (uri != null)
            {
                if (uri.Scheme == "x-sierra-romeo")
                {
                    switch (uri.LocalPath)
                    {
                        case "authcode":
                            var queryString = HttpUtility.ParseQueryString(uri.Query);
                            string state = queryString.Get("state");
                            string code = queryString.Get("code");
                            loginController.ProcessAuthReply(state, code);
                            return;

                        default:
                            Debug.WriteLine($"Got unknown URI path ${uri.PathAndQuery}");
                            return;
                    }
                }
                else if (uri.Scheme == "file")
                {
                    // Continue to file paths below
                }
                else
                {
                    Debug.WriteLine($"Got unknown URI scheme {uri.Scheme}");
                    return;
                }
            }

            // Doesn't look like a URI, so these might be file paths
            foreach (string filename in args)
            {
                var importer = new ImportTextFile(filename);
                new AuthorityWindow(loginController, importer)
                {
                    Owner = mw,
                };
            }
        }
    }
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private SierraRomeoApp _app;

        public SingleInstanceManager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            // First time app is launched
            _app = new SierraRomeoApp();
            ResourceDictionary appResources = new ResourceDictionary
            {
                Source = new Uri("ApplicationResources.xaml", UriKind.Relative)
            };
            Application.Current.Resources.MergedDictionaries.Add(appResources);
            _app.Run();
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Subsequent launches
            base.OnStartupNextInstance(eventArgs);
            // eventArgs is a ReadOnlyCollection, but StartupEventArgs.Args is a string[]
            string[] args = new string[eventArgs.CommandLine.Count];
            eventArgs.CommandLine.CopyTo(args, 0);
            _app.Activate(args);
        }

    }
    class EntryPoint
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Debug.AutoFlush = true;
            var manager = new SingleInstanceManager();
            manager.Run(args);
        }
    }

    static class URIHandler
    {
        public static void AddURIHandler(string protocol, string exePath)
        {
            RegistryKey hkcu = Registry.CurrentUser;
            var urikey = hkcu.CreateSubKey($"Software\\Classes\\{protocol}");
            urikey.SetValue("", "URL:Sierra Romeo Protocol");
            urikey.SetValue("URL Protocol", "");
            var defaulticon = urikey.CreateSubKey("Default Icon");
            defaulticon.SetValue("", exePath + ",1");
            var cmd = urikey.CreateSubKey("shell\\open\\command");
            cmd.SetValue("", $"\"{exePath}\" \"%1\"");
        }
    }

    public class TraceLogger : ILogger
    {
        /// <summary>
        /// Logs the Trace Message using the Trace class
        /// </summary>
        /// <param name="message"><see cref="HttpTracer"/> Trace message</param>
        public void Log(string message) => Trace.WriteLine(message);
    }
}