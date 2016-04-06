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

namespace BiodiversityPlugin.Views
{
    /// <summary>
    /// Interaction logic for TissueNameSelectorView.xaml
    /// </summary>
    public partial class TissueNameSelectorView : Window
    {
        public TissueNameSelectorView(TissueNameSelectorViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            vm.CloseAction = new Action(() => this.Close());
        }
    }
}
