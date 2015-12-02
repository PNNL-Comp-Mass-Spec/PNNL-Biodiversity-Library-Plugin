using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BiodiversityPlugin.ViewModels;

namespace BiodiversityPlugin
{
    /// <summary>
    /// Interaction logic for SelectDropDownOrganismWindow.xaml
    /// </summary>
    public partial class SelectDropDownOrganismWindow : Window
    {
        public SelectDropDownOrganismWindow(SelectOrgViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            vm.CloseAction = new Action(() => this.Close());
        }
    }
}
