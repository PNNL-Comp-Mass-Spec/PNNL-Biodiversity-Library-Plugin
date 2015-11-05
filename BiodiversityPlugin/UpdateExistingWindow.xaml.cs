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
    /// Interaction logic for UpdateExistingWindow.xaml
    /// </summary>
    public partial class UpdateExistingWindow : Window
    {
        public UpdateExistingWindow(UpdateExistingViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
