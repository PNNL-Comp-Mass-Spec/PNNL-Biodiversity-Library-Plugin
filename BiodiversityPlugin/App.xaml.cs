using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
            try
            {
                var tsvOrgPath = "Tools/BioDiversity/DataFiles/Organisms.txt";
                var dbPath = "Tools/BioDiversity/DataFiles/DBs/PBL.db";
                //var vm = new MainViewModel(new HardCodedData(), csvPath, csvPathwaysPath);
                //var vm = new MainViewModel(new CsvDataLoader(), tsvOrgPath, tsvPathwaysPath);
                var vm = new MainViewModel(new CsvDataLoader(tsvOrgPath, ""), new DatabaseDataLoader(dbPath), dbPath);
                var mainWindow = new MainWindow {DataContext = vm};
                mainWindow.Show();
            }
            catch (DirectoryNotFoundException f)
            {
                var tsvOrgPath = "DataFiles/Organisms.txt";
                var dbPath = "DataFiles/DBs/PBL.db";
                //var vm = new MainViewModel(new HardCodedData(), csvPath, csvPathwaysPath);
                //var vm = new MainViewModel(new CsvDataLoader(), tsvOrgPath, tsvPathwaysPath);
                var vm = new MainViewModel(new CsvDataLoader(tsvOrgPath, ""), new DatabaseDataLoader(dbPath), dbPath);
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
