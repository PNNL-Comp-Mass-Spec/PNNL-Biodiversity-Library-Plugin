using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BiodiversityPlugin.Models;
using BiodiversityPlugin.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MessageBox = System.Windows.MessageBox;

namespace BiodiversityPlugin.ViewModels
{
    public class UpdateExistingViewModel : ViewModelBase
    {
        private string _blibPath;
        private List<string> _msgfPath;
        private bool _startEnable;
        private string _dbPath;
        private ObservableCollection<string> _filteredOrganisms;

        public string DbPath
        {
            get { return _dbPath; }
            set { _dbPath = value; }
        }      

        public string BlibPath
        {
            get { return _blibPath; }
            set
            {
                _blibPath = value;
                RaisePropertyChanged();
                IsStartEnabled();
            }
        }

        public List<string> MsgfPath
        {
            get { return _msgfPath; }
            set
            {
                _msgfPath = value;
                RaisePropertyChanged();
                IsStartEnabled();
            }
        }

       

        public bool StartButtonEnabled
        {
            get { return _startEnable; }
            set
            {
                _startEnable = value;
                RaisePropertyChanged();
            }
        }
        
        public RelayCommand SelectBlibCommand { get; private set; }
        public RelayCommand SelectMsgfCommand { get; private set; }
        public RelayCommand SelectButtonCommand { get; private set; }
        public RelayCommand SelectButton2Command { get; private set; }
        public RelayCommand SelectNewCommand { get; private set; }
        public RelayCommand HelpCommand { get; set; }

        public UpdateExistingViewModel(string dbpath, ObservableCollection<string> filteredOrganisms)
        {
            _dbPath = dbpath;
            _filteredOrganisms = filteredOrganisms;
            SelectBlibCommand = new RelayCommand(SelectBlib);
            SelectMsgfCommand = new RelayCommand(SelectMsgf);
            SelectButtonCommand = new RelayCommand(SelectButton);
            SelectButton2Command = new RelayCommand(SelectButton2);
            SelectNewCommand = new RelayCommand(SelectNew);
            HelpCommand = new RelayCommand(ClickHelp);
        }

        private void ClickHelp()
        {
            var help = new HelpWindow();
            help.Show();
        }

        private void IsStartEnabled()
        {
            if (!string.IsNullOrEmpty(BlibPath) && MsgfPath != null) 
            {
                StartButtonEnabled = true;
                RaisePropertyChanged();
            }
            else
            {
                StartButtonEnabled = false;
            }
        }

        private void SelectButton()
        {
            var SelectOrgWindowVm = new SelectOrgViewModel(_dbPath, _filteredOrganisms, _blibPath, _msgfPath, "replace");
            var selectOrg = new SelectDropDownOrganismWindow(SelectOrgWindowVm);
            selectOrg.ShowDialog();  
            selectOrg.Close();        
        }

        private void SelectButton2()
        {
            var SelectOrgWindowVm = new SelectOrgViewModel(_dbPath, _filteredOrganisms, _blibPath, _msgfPath, "supplement");
            var selectOrg = new SelectDropDownOrganismWindow(SelectOrgWindowVm);
            selectOrg.ShowDialog();
            selectOrg.Close();
        }

        private void SelectNew()
        {
            var SelectNewWindowVm = new TypeOrgViewModel();
            var selectNew = new TypeInOrganismWindow(SelectNewWindowVm);
            selectNew.ShowDialog();
        }

        private void SelectBlib()
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Blib files (*.blib)|*.blib"; //.blib           
            var userClickedOK = openFile.ShowDialog();
            if (userClickedOK == DialogResult.OK)
            {
                BlibPath = openFile.FileName;
            }
        }

        private void SelectMsgf()
        {
            OpenFileDialog openFolder = new OpenFileDialog();
            openFolder.Multiselect = true;
            var userClickedOK = openFolder.ShowDialog();
            if (userClickedOK == DialogResult.OK)
            {
                MsgfPath = (openFolder.FileNames).ToList();
            }
        }
    }
}
