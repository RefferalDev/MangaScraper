﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using log4net;

namespace Blacker.MangaScraper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(App));

        public App() : base()
        {
            // Initialize log4net
            log4net.Config.XmlConfigurator.Configure();

            _log.InfoFormat("Starting up MangaScraper. Assembly version: {0}. Targeted framework: {1} {2}", 
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version,
                AssemblyInfo.TargetFramework,
                AssemblyInfo.TargetFrameworkVersion);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // define application exception handler
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);

            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            Version appVersion = a.GetName().Version;
            string appVersionString = appVersion.ToString();

            if (Blacker.MangaScraper.Properties.Settings.Default.ApplicationVersion != appVersion.ToString())
            {
                Blacker.MangaScraper.Properties.Settings.Default.Upgrade();
                Blacker.MangaScraper.Properties.Settings.Default.ApplicationVersion = appVersionString;
            }

        }

        void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //process exception
#if !DEBUG
            MessageBox.Show("Application encountered following unrecoverable error and will now shut down:\n\n\"" + e.Exception.Message + "\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            _log.Error("Unexpected error.", e.Exception);

            e.Handled = true;
            
            // kill application
            this.MainWindow.Close();
#endif
        }
    }
}
