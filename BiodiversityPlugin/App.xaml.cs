using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
            var tsvOrgPath = "../../Organisms.txt";
            var tsvPathwaysPath = "../../Pathways.txt";
            //var vm = new MainViewModel(new HardCodedData(), csvPath, csvPathwaysPath);
            //var vm = new MainViewModel(new CsvDataLoader(), tsvOrgPath, tsvPathwaysPath);
            var vm = new MainViewModel(new DatabaseDataLoader(), tsvOrgPath, tsvPathwaysPath);
            var mainWindow = new MainWindow {DataContext = vm};
            mainWindow.Show();
        }
    }
}
