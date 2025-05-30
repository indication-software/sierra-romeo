using System;
using System.Diagnostics;
using System.Web;
using System.Windows;

namespace Sierra_Romeo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        // Single instance work taken from https://github.com/microsoft/WPF-Samples/blob/master/Application%20Management/SingleInstanceDetection/
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
}
