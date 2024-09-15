/*
 * Sierra Romeo: Main menu window
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Sierra_Romeo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly LoginController loginController;
        public MainWindow(LoginController loginController)
        {
            InitializeComponent();
            this.loginController = loginController;
            DataContext = loginController;
        }

        private void Auth_Click(object sender, RoutedEventArgs e)
        {
            string url = loginController.NewAuthRequest();
            Process.Start(url);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }

        // Taken from https://social.technet.microsoft.com/wiki/contents/articles/30568.wpf-implementing-global-hot-keys.aspx

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Use the PBS authority phone line number as the hotkey ID
        private const int HOTKEY_ID = 1800888333;

        //Modifiers:
        private const uint MOD_NONE = 0x0000; //(none)
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        // Key codes from https://docs.microsoft.com/en-gb/windows/win32/inputdev/virtual-key-codes
        // S
        private const uint VK_S = 0x53;

        private IntPtr _windowHandle;
        private HwndSource _source;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_S); // CTRL + ALT + S
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:

                            IImporter activeImporter = null;

                            // Is there an active importer?
                            foreach (var importer in Importers.List)
                            {
                                if (importer.ConfigName == Properties.Settings.Default.Importer)
                                {
                                    activeImporter = (IImporter)Activator.CreateInstance(importer.className);
                                }
                            }
                            var authWindow = new AuthorityWindow(loginController, activeImporter)
                            {
                                Owner = this,
                            };
                            authWindow.Show();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }

        private void Config_Click(object sender, RoutedEventArgs e)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            var confWindow = new ConfigWindow
            {
                Owner = this
            };
            confWindow.ShowDialog();
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_S); // CTRL + ALT + S
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            new AuthorityWindow(loginController, null)
            {
                Owner = this,
            };
        }
    }
}
