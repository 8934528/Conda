using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Conda.Core.Settings
{
    public class SettingsModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- General Settings ---
        private string _language = "English (US)";
        public string Language
        {
            get => _language;
            set { if (_language != value) { _language = value; OnPropertyChanged(); } }
        }

        private string _startupBehavior = "Show Dashboard";
        public string StartupBehavior
        {
            get => _startupBehavior;
            set { if (_startupBehavior != value) { _startupBehavior = value; OnPropertyChanged(); } }
        }

        private bool _autoSave = true;
        public bool AutoSave
        {
            get => _autoSave;
            set { if (_autoSave != value) { _autoSave = value; OnPropertyChanged(); } }
        }

        private string _autoSaveInterval = "Every 30 seconds";
        public string AutoSaveInterval
        {
            get => _autoSaveInterval;
            set { if (_autoSaveInterval != value) { _autoSaveInterval = value; OnPropertyChanged(); } }
        }

        private bool _checkForUpdates = true;
        public bool CheckForUpdates
        {
            get => _checkForUpdates;
            set { if (_checkForUpdates != value) { _checkForUpdates = value; OnPropertyChanged(); } }
        }

        private bool _telemetry = false;
        public bool Telemetry
        {
            get => _telemetry;
            set { if (_telemetry != value) { _telemetry = value; OnPropertyChanged(); } }
        }

        // --- Theme & Editor Settings ---
        private string _theme = "Dark (Default)";
        public string Theme
        {
            get => _theme;
            set { if (_theme != value) { _theme = value; OnPropertyChanged(); } }
        }

        private string _accentColor = "#007acc";
        public string AccentColor
        {
            get => _accentColor;
            set { if (_accentColor != value) { _accentColor = value; OnPropertyChanged(); } }
        }

        private string _fontFamily = "Consolas";
        public string FontFamily
        {
            get => _fontFamily;
            set { if (_fontFamily != value) { _fontFamily = value; OnPropertyChanged(); } }
        }

        private double _fontSize = 14;
        public double FontSize
        {
            get => _fontSize;
            set { if (_fontSize != value) { _fontSize = value; OnPropertyChanged(); } }
        }

        private double _lineSpacing = 1.4;
        public double LineSpacing
        {
            get => _lineSpacing;
            set { if (_lineSpacing != value) { _lineSpacing = value; OnPropertyChanged(); } }
        }

        private bool _showMinimap = true;
        public bool ShowMinimap
        {
            get => _showMinimap;
            set { if (_showMinimap != value) { _showMinimap = value; OnPropertyChanged(); } }
        }

        private bool _wordWrap = false;
        public bool WordWrap
        {
            get => _wordWrap;
            set { if (_wordWrap != value) { _wordWrap = value; OnPropertyChanged(); } }
        }

        private bool _showLineNumbers = true;
        public bool ShowLineNumbers
        {
            get => _showLineNumbers;
            set { if (_showLineNumbers != value) { _showLineNumbers = value; OnPropertyChanged(); } }
        }

        // --- Python Settings ---
        private string _pythonInterpreter = "C:\\Python39\\python.exe";
        public string PythonInterpreter
        {
            get => _pythonInterpreter;
            set { if (_pythonInterpreter != value) { _pythonInterpreter = value; OnPropertyChanged(); } }
        }

        private bool _autoCreateVenv = true;
        public bool AutoCreateVenv
        {
            get => _autoCreateVenv;
            set { if (_autoCreateVenv != value) { _autoCreateVenv = value; OnPropertyChanged(); } }
        }

        private string _pipIndexUrl = "https://pypi.org/simple";
        public string PipIndexUrl
        {
            get => _pipIndexUrl;
            set { if (_pipIndexUrl != value) { _pipIndexUrl = value; OnPropertyChanged(); } }
        }

        private string _defaultPackages = "pygame, arcade, numpy";
        public string DefaultPackages
        {
            get => _defaultPackages;
            set { if (_defaultPackages != value) { _defaultPackages = value; OnPropertyChanged(); } }
        }

        // --- Editor Features ---
        private bool _autoCompletion = true;
        public bool AutoCompletion
        {
            get => _autoCompletion;
            set { if (_autoCompletion != value) { _autoCompletion = value; OnPropertyChanged(); } }
        }

        private bool _linting = true;
        public bool Linting
        {
            get => _linting;
            set { if (_linting != value) { _linting = value; OnPropertyChanged(); } }
        }
    }
}
