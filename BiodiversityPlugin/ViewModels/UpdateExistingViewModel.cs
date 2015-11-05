using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace BiodiversityPlugin.ViewModels
{
    public class UpdateExistingViewModel : ViewModelBase
    {
        private string _blibPath;
        private string _msgfPath;
        private string _orgName;
        private bool _startEnable;
        private string _dbPath;
        private ObservableCollection<string> _filteredOrganisms;

        public string DbPath
        {
            get { return _dbPath; }
            set { _dbPath = value; }
        }

        public ObservableCollection<string> FilteredOrganisms
        {
            get { return _filteredOrganisms; }
            set { _filteredOrganisms = value; }
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

        public string MsgfPath
        {
            get { return _msgfPath; }
            set
            {
                _msgfPath = value;
                RaisePropertyChanged();
                IsStartEnabled();
            }
        }

        public string OrgName
        {
            get { return _orgName; }
            set
            {
                _orgName = value;
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
        public RelayCommand UpdateExistingClassCommand { get; private set; }
        public RelayCommand SelectBlibCommand { get; private set; }
        public RelayCommand SelectMsgfCommand { get; private set; }
        

        public UpdateExistingViewModel(string dbpath, ObservableCollection<string> organisms) //TODO pass in the database path from the main view model
        {
            _dbPath = dbpath;
            _filteredOrganisms = organisms;
            UpdateExistingClassCommand = new RelayCommand(UpdateExistingClass);
            SelectBlibCommand = new RelayCommand(SelectBlib);
            SelectMsgfCommand = new RelayCommand(SelectMsgf);
            
        }
        
        

        private void IsStartEnabled()
        {
            if (!string.IsNullOrEmpty(BlibPath) && !string.IsNullOrEmpty(MsgfPath) && !string.IsNullOrEmpty(OrgName))
            {
                StartButtonEnabled = true;
                RaisePropertyChanged();
            }
            else
            {
                StartButtonEnabled = false;
            }
        }

        private void UpdateExistingClass()
        {
            UpdateExistingOrganism.UpdateExisting(_orgName, _blibPath, _msgfPath, _dbPath);
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
            FolderBrowserDialog openFolder = new FolderBrowserDialog();
            var userClickedOK = openFolder.ShowDialog();
            if (userClickedOK == DialogResult.OK)
            {
                MsgfPath = openFolder.SelectedPath;
            }
        }
    }
}
