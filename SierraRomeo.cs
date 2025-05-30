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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sierra_Romeo
{

    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private App _app;

        public SingleInstanceManager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            // First time app is launched
            _app = new App();
            _app.InitializeComponent();
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

    public class TrimmingConverter : JsonConverter<String>
    {
        /// <summary>
        /// For reasons that are unclear, some JSON elements are returned from the PBS questions interface
        /// with a long suffix of whitespace. This converter allows these to be trimmed.
        /// </summary>
        /// Idea from https://stackoverflow.com/questions/19271470/deserialize-json-with-auto-trimming-strings
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override string Read(ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return reader.GetString().Trim();
        }

        public override void Write(Utf8JsonWriter writer,
            string value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}