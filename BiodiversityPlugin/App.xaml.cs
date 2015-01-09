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

            var writer = new StreamWriter("C:\\Temp\\log.txt", true);
            try
            {
                writer.WriteLine("Fresh boot");
                writer.Close();
                var tsvOrgPath = "Tools/BioDiversity/DataFiles/Organisms.txt";
                var dbPath = "Tools/BioDiversity/DataFiles/DBs/PBL.db";
                var proteinsPath = "Tools/BioDiversity/DataFiles/Proteins.txt";
                //var vm = new MainViewModel(new HardCodedData(), csvPath, csvPathwaysPath);
                //var vm = new MainViewModel(new CsvDataLoader(), tsvOrgPath, tsvPathwaysPath);
                writer = new StreamWriter("C:\\Temp\\log.txt", true);
                writer.WriteLine("In try");
                writer.Close();
                var vm = new MainViewModel(new CsvDataLoader(tsvOrgPath, ""), new DatabaseDataLoader(dbPath), dbPath,
                    proteinsPath);
                var mainWindow = new MainWindow {DataContext = vm};
                mainWindow.Show();
            }
            catch (DirectoryNotFoundException f)
            {
                var tsvOrgPath = "DataFiles/Organisms.txt";
                var dbPath = "DataFiles/DBs/PBL.db";
                var proteinsPath = "DataFiles/Proteins.txt";
                //var vm = new MainViewModel(new HardCodedData(), csvPath, csvPathwaysPath);
                //var vm = new MainViewModel(new CsvDataLoader(), tsvOrgPath, tsvPathwaysPath);
                //var vm = new MainViewModel(new CsvDataLoader(tsvOrgPath, ""), new DatabaseDataLoader(dbPath), dbPath,
                //    proteinsPath);
                writer = new StreamWriter("C:\\Temp\\log.txt", true);
                writer.WriteLine("In catch 1");
                writer.Close();
                var vm = new MainViewModel(new DatabaseDataLoader(dbPath), new DatabaseDataLoader(dbPath), dbPath,
                    proteinsPath);
                var mainWindow = new MainWindow {DataContext = vm};
                mainWindow.Show();
            }
            catch (FileNotFoundException)
            {
                var tsvOrgPath = "DataFiles/Organisms.txt";
                var dbPath = "DataFiles/PBL.db";
                var proteinsPath = "DataFiles/Proteins.txt";
                //var vm = new MainViewModel(new HardCodedData(), csvPath, csvPathwaysPath);
                //var vm = new MainViewModel(new CsvDataLoader(), tsvOrgPath, tsvPathwaysPath);
                //var vm = new MainViewModel(new CsvDataLoader(tsvOrgPath, ""), new DatabaseDataLoader(dbPath), dbPath,
                //    proteinsPath);
                writer = new StreamWriter("C:\\Temp\\log.txt", true);
                writer.WriteLine("In catch2");
                writer.Close();
                var vm = new MainViewModel(new DatabaseDataLoader(dbPath), new DatabaseDataLoader(dbPath), dbPath,
                    proteinsPath);
                var mainWindow = new MainWindow { DataContext = vm };
                mainWindow.Show();
            }
            catch (Exception a)
            {
                writer = new StreamWriter("C:\\Temp\\log.txt", true);
                writer.WriteLine("In final catch");
                MessageBox.Show(a.Message);
                throw;
            }
        }
    }
}
