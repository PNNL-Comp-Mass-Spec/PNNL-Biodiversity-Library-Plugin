using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using BiodiversityPlugin.ViewModels;

namespace BiodiversityPlugin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void Hyperlink_MailTo(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            var address = "mailto:" + hyperlink.NavigateUri.ToString();
            try
            {
                System.Diagnostics.Process.Start(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show("That e-mail address is invalid.", "E-mail error");
            }
        }
    }
}
