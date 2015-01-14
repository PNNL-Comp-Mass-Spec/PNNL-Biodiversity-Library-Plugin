﻿using System;
using System.IO;
using System.Windows;
using BiodiversityPlugin.DataManagement;
using BiodiversityPlugin.ViewModels;

namespace BiodiversityPlugin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
			//Built to run in skyline
	        try
	        {
		        const string tsvOrgPath = "Tools\\BiodiversityPlugin\\DataFiles\\Organisms.txt";
		        const string dbPath = "Tools\\BiodiversityPlugin\\DataFiles\\DBs\\PBL.db";
		        const string proteinsPath = "Tools\\BiodiversityPlugin\\DataFiles\\Proteins.txt";

		        var vm = new MainViewModel(new CsvDataLoader(tsvOrgPath, ""), new DatabaseDataLoader(dbPath), dbPath,
			        proteinsPath);
		        var mainWindow = new MainWindow {DataContext = vm};
		        mainWindow.Show();
	        }
			//Last ditch to run in debug or stand-alone mode
	        catch (DirectoryNotFoundException x)
	        {
				const string tsvOrgPath = "DataFiles\\Organisms.txt";
				const string dbPath = "DataFiles\\DBs\\PBL.db";
				const string proteinsPath = "DataFiles\\Proteins.txt";

				var vm = new MainViewModel(new CsvDataLoader(tsvOrgPath, ""), new DatabaseDataLoader(dbPath), dbPath,
					proteinsPath);
				var mainWindow = new MainWindow { DataContext = vm };
				mainWindow.Show();
	        }
			catch (Exception a)
			{
				MessageBox.Show(a.Message);
				throw;
			}
        }
    }
}
