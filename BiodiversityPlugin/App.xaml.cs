using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BiodiversityPlugin.IO;
using BiodiversityPlugin.ViewModels;
using KeggDataLibrary.DataManagement;
using SkylineTool;

namespace BiodiversityPlugin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            SkylineToolClient _toolClient = null;
            if (e.Args.Length > 0)
            {
                _toolClient = new SkylineToolClient(e.Args[0], "BioDiversity Library");
                _toolClient.DocumentChanged += OnDocumentChanged;
                _toolClient.SelectionChanged += OnSelectionChanged;

            }

            //Built to run in skyline
            try
            {
		        const string dbPath = "Tools\\BiodiversityPlugin\\DataFiles\\DBs\\PBL.db";

                if (!File.Exists(dbPath))
                {
                    var ex = new DirectoryNotFoundException();
                    throw ex;
                }
                TextExporter.createInstance(dbPath);
                var vm = new MainViewModel(new DatabaseDataLoader(dbPath), new DatabaseDataLoader(dbPath), dbPath,
                    _toolClient);
                var mainWindow = new MainWindow { DataContext = vm };
                mainWindow.Show();
            }
            //Last ditch to run in debug or stand-alone mode
            catch (DirectoryNotFoundException ex)
            {
				const string dbPath = "DataFiles\\DBs\\PBL.db";
                //TextExporter.createInstance(dbPath);
                var vm = new MainViewModel(new DatabaseDataLoader(dbPath), new DatabaseDataLoader(dbPath), dbPath,
                    _toolClient);
                var mainWindow = new MainWindow { DataContext = vm };
                mainWindow.Show();
            }
            catch (Exception a)
            {
                MessageBox.Show(a.Message);
                throw;
            }

        }

        private void OnSelectionChanged(object sender, EventArgs args)
        {
            //throw new NotImplementedException();
        }

        private void OnDocumentChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
