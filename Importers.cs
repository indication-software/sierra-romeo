/*
 * Sierra Romeo: Import interfaces
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
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using UIAutomationClient;

namespace Sierra_Romeo
{
    /// <summary>
    /// This file contains a series of importers - classes that know how to import the information
    /// needed to start building the request from a variety of sources (eg text files, other software).
    /// 
    /// To add a new source:
    /// 1. Create a new class that implements IImport, and which returns an ExternalRequest
    ///    object from the Import method
    /// 2. Add it to the Importers.List array (which makes it usable from the UI)
    /// 
    /// ExternalRequests do not have to be filled - any or all fields can be null
    /// </summary>
    public static class Importers
    {
        public class Importer
        {
            public string ConfigName { get; set; } // The string used in the configuration file
            public string InterfaceName { get; set; } // The string displayed in the UI
            public Type className; // The class of the importer
        }

        public static Importer[] List =
        {
            // new Importer{ConfigName = "", InterfaceName = "", className = null}
            // json-only importer is not listed in the UI or in the config; it is only
            // and called in response to other actions
            new Importer{ConfigName = "textfile",
                InterfaceName = "Text file",
                className = typeof(ImportTextFile)},
            new Importer{ConfigName = "medtech01",
                InterfaceName = "MedTech Evolution",
                className = typeof(ImportMedtechEvolution)}
        };
    }

    public interface IImporter
    {
        ExternalRequest Import();
    }

    public class ImportJson : IImporter
    {
        private readonly string contents;
        private static readonly JsonSerializerOptions serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ImportJson(string contents)
        {
            this.contents = contents;
        }

        public ExternalRequest Import()
        {
            try
            {
                ExternalRequest request = JsonSerializer.Deserialize<ExternalRequest>(contents, serializeOptions);

                switch (request.FormatVersion)
                {
                    case 1:
                        {
                            return request;
                        }
                    default:
                        {
                            return null;
                        }
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    public class ImportTextFile : IImporter
    {

        private readonly string path;
        public ImportTextFile()
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Sierra Romeo",
                    "NewRequest.txt");
        }
        public ImportTextFile(string path)
        {
            this.path = path;
        }
        public ExternalRequest Import()
        {
            string contents;
            // Try and read the file, there's a lot of different exceptions here but
            // catch them all
            try
            {
                contents = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tried to open {path} but failed with exception: {ex.Message}");
                return null;
            }
            var importer = new ImportJson(contents);
            return importer.Import();
        }
    }

    public class ImportMedtechEvolution : IImporter
    {

        internal PropertyCondition type_tab = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tab);
        internal PropertyCondition type_pane = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane);
        internal PropertyCondition type_window = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);

        internal PropertyCondition PropertyConditionName(string name)
        {
            return new PropertyCondition(AutomationElement.NameProperty, name);
        }

        internal void ListChildren(AutomationElement element)
        {
            string message = $"Children of element {element.Current.Name} (type {element.Current.LocalizedControlType} and class name {element.Current.ClassName}):\n\n";

            foreach (AutomationElement foo in element.FindAll(System.Windows.Automation.TreeScope.Children, System.Windows.Automation.Condition.TrueCondition))
            {
                message += $"'{foo.Current.Name}' of type {foo.Current.LocalizedControlType} with class name {foo.Current.ClassName}\n";
            }

            MessageBox.Show(message);
        }

        public ExternalRequest Import()
        {
            // The first iteration of this function used the .NET automation APIs,
            // but they tended to crash even with children-only searches. The modern
            // interface isn't as nice, but is a lot more reliable.

            // Create a new UIA client
            IUIAutomation _uia = new CUIAutomation();
            // Get the root element
            IUIAutomationElement rootElement = _uia.GetRootElement();

            // Okay, so. MedTech has a main window that is invisible, and then a secondary
            // window which actually contains the UI. These are not parent and child. Probably
            // because Delphi. So, search for a window with the class name
            // "TfmAt32Main" (the main window is "TApplication").

            var mainwindow = rootElement.FindFirst(UIAutomationClient.TreeScope.TreeScope_Children,
                _uia.CreatePropertyCondition(UIA_PropertyIds.UIA_ClassNamePropertyId, "TfmAt32Main"));

            if (mainwindow == null)
            {
                Debug.WriteLine($"Tried to find the UI window, no luck");
                return null;
            }

            // XXX: this is quite slow; would be better to step through the two children

            var pr_window = mainwindow.FindFirst(UIAutomationClient.TreeScope.TreeScope_Descendants,
                _uia.CreatePropertyCondition(UIA_PropertyIds.UIA_ClassNamePropertyId, "TfmViewPat"));

            if (pr_window == null)
            {
                Debug.WriteLine($"Tried to find the patient register, no luck");
                MessageBox.Show("Couldn't find Patient Register window (F3) - is it open?",
                    "Sierra Romeo - Medtech Evolution Importer", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return null;
            }

            // XXX: this is quite slow as well but probably more robust

            var name_pane = pr_window.FindFirst(UIAutomationClient.TreeScope.TreeScope_Descendants,
                _uia.CreateAndCondition(_uia.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_PaneControlTypeId),
                _uia.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Name")));

            if (name_pane == null)
            {
                Debug.WriteLine($"Tried to find the name tab, no luck");
                MessageBox.Show("Couldn't find the patient name details tab - is it chosen in the Patient Register (F3)?",
                    "Sierra Romeo - Medtech Evolution Importer", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return null;
            }

            var nameaddr_pane = name_pane.FindFirst(UIAutomationClient.TreeScope.TreeScope_Children,
                _uia.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Name And Address"));

            if (nameaddr_pane == null)
            {
                Debug.WriteLine($"Tried to find the Name And Address pane, no luck");
                return null;
            }

            var nameaddr_pane_children = nameaddr_pane.FindAll(UIAutomationClient.TreeScope.TreeScope_Children,
                _uia.CreateTrueCondition());

            if (nameaddr_pane_children == null)
            {
                Debug.WriteLine($"Tried to get the children of the Name And Address pane, no luck");
                return null;
            }

            // Last name is the last child but there's no other way of identifying it

            var last_name_edit = nameaddr_pane_children.GetElement(nameaddr_pane_children.Length - 1).GetCurrentPattern(UIA_PatternIds.UIA_ValuePatternId) as IUIAutomationValuePattern;

            if (last_name_edit == null)
            {
                Debug.WriteLine($"Tried to get the Last Name edit, no luck");
                return null;
            }

            // First name is the second last child but there's no other way of identifying it

            var first_name_edit = nameaddr_pane_children.GetElement(nameaddr_pane_children.Length - 2).GetCurrentPattern(UIA_PatternIds.UIA_ValuePatternId) as IUIAutomationValuePattern;

            if (first_name_edit == null)
            {
                Debug.WriteLine($"Tried to get the First Name edit, no luck");
                return null;
            }

            var cards_pane = name_pane.FindFirst(UIAutomationClient.TreeScope.TreeScope_Children,
                _uia.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, "Cards"));

            if (cards_pane == null)
            {
                Debug.WriteLine($"Tried to find the Cards pane, no luck");
                return null;
            }

            var cards_pane_children = cards_pane.FindAll(UIAutomationClient.TreeScope.TreeScope_Children,
                _uia.CreateTrueCondition());

            if (cards_pane_children == null)
            {
                Debug.WriteLine($"Tried to get the children of the Cards pane, no luck");
                return null;
            }

            // Medicare card number is the last child but there's no other way of identifying it

            var medicare_card_edit = cards_pane_children.GetElement(cards_pane_children.Length - 1).GetCurrentPattern(UIA_PatternIds.UIA_ValuePatternId) as IUIAutomationValuePattern;

            if (medicare_card_edit == null)
            {
                Debug.WriteLine($"Tried to get the Medicare card edit, no luck");
                return null;
            }

            // IRN is the last child but there's no other way of identifying it

            var irn_edit = cards_pane_children.GetElement(cards_pane_children.Length - 2).GetCurrentPattern(UIA_PatternIds.UIA_ValuePatternId) as IUIAutomationValuePattern;

            if (irn_edit == null)
            {
                Debug.WriteLine($"Tried to get the IRN edit, no luck");
                return null;
            }

            ExternalRequest r = new ExternalRequest()
            {
                Surname = last_name_edit.CurrentValue,
                FirstName = first_name_edit.CurrentValue,
                MedicareNumber = medicare_card_edit.CurrentValue.Replace(" ", "") + irn_edit.CurrentValue,
            };

            return r;
        }
    }
}
