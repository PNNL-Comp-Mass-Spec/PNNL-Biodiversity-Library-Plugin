﻿using System;
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
        private string showMsgfPaths;
        private bool _startEnable;
        private string _dbPath;
        private ObservableCollection<OrganismWithFlag> _filteredOrganisms;

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

        public string ShowMsgfPaths
        {
            get { return showMsgfPaths; }
            set
            {
                showMsgfPaths = value; 
                RaisePropertyChanged();
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
        public RelayCommand ReplaceButtonCommand { get; private set; }
        public RelayCommand SupplementButtonCommand { get; private set; }
        public RelayCommand InsertNewCommand { get; private set; }
        public RelayCommand HelpCommand { get; set; }

        public UpdateExistingViewModel(string dbpath, ObservableCollection<OrganismWithFlag> filteredOrganisms)
        {
            _dbPath = dbpath;
            _filteredOrganisms = filteredOrganisms;
            SelectBlibCommand = new RelayCommand(SelectBlib);
            SelectMsgfCommand = new RelayCommand(SelectMsgf);
            ReplaceButtonCommand = new RelayCommand(ReplaceButton);
            SupplementButtonCommand = new RelayCommand(SupplementButton);
            InsertNewCommand = new RelayCommand(InsertNew);
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

        private void ReplaceButton()
        {
            var SelectOrgWindowVm = new SelectOrgViewModel(_dbPath, _filteredOrganisms, _blibPath, _msgfPath, "replace");
            var selectOrg = new SelectDropDownOrganismWindow(SelectOrgWindowVm);
            selectOrg.ShowDialog();   
            Close();      
        }

        private void SupplementButton()
        {
            var SelectOrgWindowVm = new SelectOrgViewModel(_dbPath, _filteredOrganisms, _blibPath, _msgfPath, "supplement");
            var selectOrg = new SelectDropDownOrganismWindow(SelectOrgWindowVm);
            selectOrg.ShowDialog();
            Close();
        }

        private void InsertNew()
        {
            var SelectNewWindowVm = new TypeOrgViewModel(_dbPath, _blibPath, _msgfPath);
            var selectNew = new TypeInOrganismWindow(SelectNewWindowVm);
            selectNew.ShowDialog();
            Close();
        }

        public Action CloseAction { get; set; }

        private void Close()
        {
            this.CloseAction();
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
                var separator = ", ";
                var array = openFolder.FileNames;
                ShowMsgfPaths = string.Join(separator, array);
                MsgfPath = (openFolder.FileNames).ToList();
            }
        }
    }
}
