using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TinyConfig;

namespace TinyConfigSample
{
    public partial class MainWindow : Window
    {
        private const string RegistrySubKey = @"SOFTWARE\TinyConfigSample";
        private string _basePath;

        private string _iniFilePath;
        private string _jsonFilePath;
        private string _xmlFilePath;

        public MainWindow()
        {
            InitializeComponent();
            _basePath = AppDomain.CurrentDomain.BaseDirectory;

            _iniFilePath = Path.Combine(_basePath, "settings.ini");
            _jsonFilePath = Path.Combine(_basePath, "settings.json");
            _xmlFilePath = Path.Combine(_basePath, "settings.xml");

            iniPath.Text = _iniFilePath;
            jsonPath.Text = _jsonFilePath;
            xmlPath.Text = _xmlFilePath;
        }

        private void UpdatePathHeader(TextBlock header, string path)
        {
            header.Text = path;
            header.ToolTip = path;
        }

        // === INI ===

        private ITinyConfig GetIni() =>
            TinyConfig.TinyConfig.FromFile(_iniFilePath);

        private void IniGet_Click(object sender, RoutedEventArgs e)
        {
            DoGet(GetIni(), iniSection, iniKey, iniValue, iniStatus);
            RefreshFilePreview(iniPreview, _iniFilePath);
        }

        private void IniSet_Click(object sender, RoutedEventArgs e)
        {
            DoSet(GetIni(), iniSection, iniKey, iniValue, iniStatus);
            RefreshFilePreview(iniPreview, _iniFilePath);
        }

        private void IniExists_Click(object sender, RoutedEventArgs e)
        {
            DoExists(GetIni(), iniSection, iniKey, iniStatus);
        }

        private void IniPath_Changed(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            _iniFilePath = iniPath.Text.Trim();
            UpdatePathHeader(iniPathText, _iniFilePath);
        }

        private void IniBrowse_Click(object sender, RoutedEventArgs e)
        {
            string path = BrowseFile("INI Files|*.ini|All Files|*.*", _iniFilePath);
            if (path != null) iniPath.Text = path;
        }

        // === JSON ===

        private ITinyConfig GetJson() =>
            TinyConfig.TinyConfig.FromJson(_jsonFilePath);

        private void JsonGet_Click(object sender, RoutedEventArgs e)
        {
            DoGet(GetJson(), jsonSection, jsonKey, jsonValue, jsonStatus);
            RefreshFilePreview(jsonPreview, _jsonFilePath);
        }

        private void JsonSet_Click(object sender, RoutedEventArgs e)
        {
            DoSet(GetJson(), jsonSection, jsonKey, jsonValue, jsonStatus);
            RefreshFilePreview(jsonPreview, _jsonFilePath);
        }

        private void JsonExists_Click(object sender, RoutedEventArgs e)
        {
            DoExists(GetJson(), jsonSection, jsonKey, jsonStatus);
        }

        private void JsonPath_Changed(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            _jsonFilePath = jsonPath.Text.Trim();
            UpdatePathHeader(jsonPathText, _jsonFilePath);
        }

        private void JsonBrowse_Click(object sender, RoutedEventArgs e)
        {
            string path = BrowseFile("JSON Files|*.json|All Files|*.*", _jsonFilePath);
            if (path != null) jsonPath.Text = path;
        }

        // === XML ===

        private ITinyConfig GetXml() =>
            TinyConfig.TinyConfig.FromXml(_xmlFilePath);

        private void XmlGet_Click(object sender, RoutedEventArgs e)
        {
            DoGet(GetXml(), xmlSection, xmlKey, xmlValue, xmlStatus);
            RefreshFilePreview(xmlPreview, _xmlFilePath);
        }

        private void XmlSet_Click(object sender, RoutedEventArgs e)
        {
            DoSet(GetXml(), xmlSection, xmlKey, xmlValue, xmlStatus);
            RefreshFilePreview(xmlPreview, _xmlFilePath);
        }

        private void XmlExists_Click(object sender, RoutedEventArgs e)
        {
            DoExists(GetXml(), xmlSection, xmlKey, xmlStatus);
        }

        private void XmlPath_Changed(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            _xmlFilePath = xmlPath.Text.Trim();
            UpdatePathHeader(xmlPathText, _xmlFilePath);
        }

        private void XmlBrowse_Click(object sender, RoutedEventArgs e)
        {
            string path = BrowseFile("XML Files|*.xml|All Files|*.*", _xmlFilePath);
            if (path != null) xmlPath.Text = path;
        }

        // === Registry ===

        private ITinyConfig GetReg() =>
            TinyConfig.TinyConfig.FromRegistry(RegistrySubKey);

        private void RegGet_Click(object sender, RoutedEventArgs e)
        {
            DoGet(GetReg(), regSection, regKey, regValue, regStatus);
        }

        private void RegSet_Click(object sender, RoutedEventArgs e)
        {
            DoSet(GetReg(), regSection, regKey, regValue, regStatus);
        }

        private void RegExists_Click(object sender, RoutedEventArgs e)
        {
            DoExists(GetReg(), regSection, regKey, regStatus);
        }

        // === Common ===

        private void DoGet(ITinyConfig config, TextBox sectionBox, TextBox keyBox, TextBox valueBox, TextBlock status)
        {
            string section = sectionBox.Text.Trim();
            string key = keyBox.Text.Trim();

            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key))
            {
                status.Text = "Input required.";
                return;
            }

            try
            {
                if (!config.Exists(section, key))
                {
                    valueBox.Text = "";
                    status.Text = "Not found.";
                }
                else
                {
                    valueBox.Text = config.Get(section, key);
                    status.Text = "OK";
                }
            }
            catch (Exception ex)
            {
                status.Text = "Error: " + ex.Message;
            }
        }

        private void DoSet(ITinyConfig config, TextBox sectionBox, TextBox keyBox, TextBox valueBox, TextBlock status)
        {
            string section = sectionBox.Text.Trim();
            string key = keyBox.Text.Trim();

            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key))
            {
                status.Text = "Input required.";
                return;
            }

            try
            {
                config.Set(section, key, valueBox.Text);
                status.Text = "Saved.";
            }
            catch (Exception ex)
            {
                status.Text = "Error: " + ex.Message;
            }
        }

        private void DoExists(ITinyConfig config, TextBox sectionBox, TextBox keyBox, TextBlock status)
        {
            string section = sectionBox.Text.Trim();
            string key = keyBox.Text.Trim();

            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key))
            {
                status.Text = "Input required.";
                return;
            }

            try
            {
                bool exists = config.Exists(section, key);
                status.Text = exists ? "Exists: Yes" : "Exists: No";
            }
            catch (Exception ex)
            {
                status.Text = "Error: " + ex.Message;
            }
        }

        private void RefreshFilePreview(TextBox preview, string path)
        {
            preview.Text = File.Exists(path) ? File.ReadAllText(path) : "";
        }

        private void Tab_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            RefreshFilePreview(iniPreview, _iniFilePath);
            RefreshFilePreview(jsonPreview, _jsonFilePath);
            RefreshFilePreview(xmlPreview, _xmlFilePath);
        }

        private string BrowseFile(string filter, string currentPath)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = Path.GetFileName(currentPath),
                InitialDirectory = Path.GetDirectoryName(currentPath) ?? _basePath,
                OverwritePrompt = false
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }
}
