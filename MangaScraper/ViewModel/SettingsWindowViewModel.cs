﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Blacker.MangaScraper.Commands;
using System.ComponentModel;
using Blacker.MangaScraper.Common;
using log4net;

namespace Blacker.MangaScraper.ViewModel
{
    class SettingsWindowViewModel : BaseViewModel, IDataErrorInfo, IBrowseCommand, ISaveCommand, IClearCommand
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingsWindowViewModel));

        private readonly ICommand _saveSettingsCommand;
        private readonly ICommand _browseCommand;
        private readonly ICommand _clearCommand;

        public SettingsWindowViewModel()
        {
            _saveSettingsCommand = new SaveCommand(this);
            _browseCommand = new BrowseCommand(this);
            _clearCommand = new ClearCommand(this, false, true, "Do you really want to clear recent folder history?");

            MaxParallelDownloads = Properties.Settings.Default.MaxParallelDownloads;
            ReaderPath = Properties.Settings.Default.ReaderPath;
            EnablePreload = Properties.Settings.Default.EnablePreload;
            MaxRecentFolders = Properties.Settings.Default.RecentFolders.MaxItems;

            Scrapers = ScraperLoader.Instance.AllScrapers
                .Select(s =>
                    new ScraperInfo(s, !Properties.Settings.Default.DisabledScrapers.Contains(s.ScraperGuid))).ToList();

            try
            {
                var reader = new System.IO.StreamReader(System.Windows.Application.GetResourceStream(new System.Uri("/readme.txt", UriKind.Relative)).Stream, Encoding.UTF8);
                AboutText = reader.ReadToEnd();
            }
            catch (System.IO.IOException ex)
            {
                _log.Error("Unable to load about information from resource.", ex);
                AboutText = "";
            }

        }

        public ICommand SaveSettingsCommand { get { return _saveSettingsCommand; } }

        public ICommand BrowseCommand { get { return _browseCommand; } }

        public ICommand ClearCommand { get { return _clearCommand; } }

        public string AboutText { get; set; }

        public ushort MaxParallelDownloads { get; set; }

        public string ReaderPath { get; set; }

        public bool EnablePreload { get; set; }

        public uint MaxRecentFolders { get; set; }

        public IEnumerable<ScraperInfo> Scrapers { get; private set; }

        #region Commands

        public void SaveClicked(object parameter)
        {
            if (string.IsNullOrEmpty(Error))
            {
                Properties.Settings.Default.MaxParallelDownloads = MaxParallelDownloads;
                Properties.Settings.Default.ReaderPath = ReaderPath;
                Properties.Settings.Default.EnablePreload = EnablePreload;

                Properties.Settings.Default.RecentFolders.MaxItems = MaxRecentFolders;

                Properties.Settings.Default.DisabledScrapers.Clear();
                foreach (var scraper in Scrapers)
                {
                    if (!scraper.Enabled)
                        Properties.Settings.Default.DisabledScrapers.Add(scraper.ScraperGuid);
                }

                Properties.Settings.Default.Save();

                // todo: somehow signalize that settings were saved
            }
        }

        public void BrowseClicked(object parameter)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    DefaultExt = ".exe",
                    Filter = "Executables (.exe,.cmd,.bat)|*.exe;*.cmd;*.bat"
                };

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                ReaderPath = dlg.FileName;
                InvokePropertyChanged("ReaderPath");
            }
        }

        public void ClearClicked(object parameter)
        {
            Properties.Settings.Default.RecentFolders.Clear();
        }

        #endregion // Commands

        #region IDataErrorInfo implementation

        public string Error
        {
            get { return this[string.Empty]; }
        }

        public string this[string columnName]
        {
            get
            {
                columnName = columnName ?? string.Empty;
                if (columnName == string.Empty || columnName == "MaxParallelDownloads")
                {
                    if (MaxParallelDownloads == 0 || MaxParallelDownloads > 50)
                    {
                        return "Maximum number of parallel downloads must be number from 1 to 50.";
                    }
                }
                if (columnName == string.Empty || columnName == "MaxRecentFolders")
                {
                    if (MaxRecentFolders > 50)
                    {
                        return "Maximum number of recent output folders must be number lower or equal to 50.";
                    }
                }
                if (columnName == string.Empty || columnName == "ReaderPath")
                {
                    if (!string.IsNullOrEmpty(ReaderPath) && !System.IO.File.Exists(ReaderPath))
                    {
                        return "File not found on the specified path.";
                    }
                }

                return string.Empty;
            }
        }

        #endregion // IDataErrorInfo implementation

        #region Scraper wrapper class

        public class ScraperInfo
        {
            private readonly IScraper _scraper;

            public ScraperInfo(IScraper scraper, bool enabled)
            {
                if (scraper == null)
                    throw new ArgumentNullException("scraper");

                _scraper = scraper;
                Enabled = enabled;
            }

            public string Name { get { return _scraper.Name; } }

            public Guid ScraperGuid { get { return _scraper.ScraperGuid; } }

            public bool Enabled { get; set; }
        }

        #endregion
    }
}
