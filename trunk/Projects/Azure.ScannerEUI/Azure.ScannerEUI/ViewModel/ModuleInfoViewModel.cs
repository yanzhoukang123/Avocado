using Azure.Avocado.EthernetCommLib;
using Azure.WPF.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Azure.ScannerEUI.ViewModel
{
    class ModuleInfoViewModel : ViewModelBase
    {
        private string _GetIVSoftwareVersionNumberValue;
        private string _GetIVOpticalModuleSerialNumberValue;
        private string _GetLaserSoftNumberValue;
        private string _GetLaserOpticalModuleNumberValue;
        private string _SelectedChannelCoeff;
        private RelayCommand _GeIVSoftwareVersionNumberCommand = null;
        private RelayCommand _GetIVOpticalModuleSerialNumberCommand = null;
        private RelayCommand _GetLaserSoftNumberCommand = null;
        private RelayCommand _GetLaserOpticalModuleNumberCommand = null;
        private ObservableCollection<string> _ChannelsOptions = new ObservableCollection<string>();
        private EthernetController _EthernetController;
        public ModuleInfoViewModel(EthernetController ethernetController)
        {
            _EthernetController = ethernetController;
        }
        public EthernetController EthernetDevice
        {
            get
            {
                return _EthernetController;
            }
        }
        public void Initialize()
        {
            if (_ChannelsOptions.Count <= 0)
            {
                _ChannelsOptions.Add("R1");
                _ChannelsOptions.Add("R2");
                _ChannelsOptions.Add("L");
                SelectedChannelCoeff = _ChannelsOptions[2];

            }

        }

        #region GeIVSoftwareVersionNumberCommand

        public ICommand GeIVSoftwareVersionNumberCommand
        {
            get
            {
                if (_GeIVSoftwareVersionNumberCommand == null)
                {
                    _GeIVSoftwareVersionNumberCommand = new RelayCommand(ExecuteGeIVSoftwareVersionNumberCommand, CanExecuteGeIVSoftwareVersionNumberCommand);
                }
                return _GeIVSoftwareVersionNumberCommand;
            }
        }
        public void ExecuteGeIVSoftwareVersionNumberCommand(object parameter)
        {
            EthernetDevice.GetIVSystemVersions();
            Thread.Sleep(200);
            if (Workspace.This.IVVM.WL1 == 0 && SelectedLaserChannel == LaserChannels.ChannelC)
            {
                GetIVSoftwareVersionNumberValue = "0";
                return;
            }
            else if (Workspace.This.IVVM.WR1 == 0 && SelectedLaserChannel == LaserChannels.ChannelA)
            {
                GetIVSoftwareVersionNumberValue = "0";
                return;
            }
            else if (Workspace.This.IVVM.WR2 == 0 && SelectedLaserChannel == LaserChannels.ChannelB)
            {
                GetIVSoftwareVersionNumberValue = "0";
                return;
            }
            GetIVSoftwareVersionNumberValue = EthernetController.IVEstimatedVersionNumberBoard[SelectedIVChannel];

        }
        public bool CanExecuteGeIVSoftwareVersionNumberCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region GetIVOpticalModuleSerialNumberCommand

        public ICommand GetIVOpticalModuleSerialNumberCommand
        {
            get
            {
                if (_GetIVOpticalModuleSerialNumberCommand == null)
                {
                    _GetIVOpticalModuleSerialNumberCommand = new RelayCommand(ExecuteGetIVOpticalModuleSerialNumberCommand, CanExecuteGetIVOpticalModuleSerialNumberCommand);
                }
                return _GetIVOpticalModuleSerialNumberCommand;
            }
        }
        public void ExecuteGetIVOpticalModuleSerialNumberCommand(object parameter)
        {

            EthernetDevice.GetIVOpticalModuleSerialNumber();
            Thread.Sleep(200);
            if (Workspace.This.IVVM.WL1 == 0 && SelectedLaserChannel == LaserChannels.ChannelC)
            {
                GetIVOpticalModuleSerialNumberValue = "0";
                return;
            }
            else if (Workspace.This.IVVM.WR1 == 0 && SelectedLaserChannel == LaserChannels.ChannelA)
            {
                GetIVOpticalModuleSerialNumberValue = "0";
                return;
            }
            else if (Workspace.This.IVVM.WR2 == 0 && SelectedLaserChannel == LaserChannels.ChannelB)
            {
                GetIVOpticalModuleSerialNumberValue = "0";
                return;
            }
            GetIVOpticalModuleSerialNumberValue = EthernetController.IVOpticalModuleSerialNumber[SelectedIVChannel];
        }
        public bool CanExecuteGetIVOpticalModuleSerialNumberCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region GetLaserSoftNumberCommand

        public ICommand GetLaserSoftNumberCommand
        {
            get
            {
                if (_GetLaserSoftNumberCommand == null)
                {
                    _GetLaserSoftNumberCommand = new RelayCommand(ExecuteGetLaserSoftNumberCommand, CanExecuteGetLaserSoftNumberCommand);
                }
                return _GetLaserSoftNumberCommand;
            }
        }
        public void ExecuteGetLaserSoftNumberCommand(object parameter)
        {
            EthernetDevice.GetLaserSystemVersions();
            Thread.Sleep(200);
            if (Workspace.This.IVVM.WL1 == 0 && SelectedLaserChannel == LaserChannels.ChannelC)
            {
                GetLaserSoftNumberValue = "0";
                return;
            }
            else if (Workspace.This.IVVM.WR1 == 0 && SelectedLaserChannel == LaserChannels.ChannelA)
            {
                GetLaserSoftNumberValue = "0";
                return;
            }
            else if (Workspace.This.IVVM.WR2 == 0 && SelectedLaserChannel == LaserChannels.ChannelB)
            {
                GetLaserSoftNumberValue = "0";
                return;
            }
            GetLaserSoftNumberValue = EthernetController.LaserBoardFirmwareVersionNumber[SelectedLaserChannel];

        }
        public bool CanExecuteGetLaserSoftNumberCommand(object parameter)
        {
            return true;
        }

        #endregion

        #region GetLaserOpticalModuleNumberCommand

        public ICommand GetLaserOpticalModuleNumberCommand
        {
            get
            {
                if (_GetLaserOpticalModuleNumberCommand == null)
                {
                    _GetLaserOpticalModuleNumberCommand = new RelayCommand(ExecuteGetLaserOpticalModuleNumberCommand, CanExecuteGetLaserOpticalModuleNumberCommand);
                }
                return _GetLaserOpticalModuleNumberCommand;
            }
        }
        public void ExecuteGetLaserOpticalModuleNumberCommand(object parameter)
        {
            EthernetDevice.GetLaserOpticalModuleSerialNumber();
            Thread.Sleep(200);
            if (Workspace.This.IVVM.WL1 == 0 && SelectedLaserChannel == LaserChannels.ChannelC)
            {
                GetLaserOpticalModuleNumberValue = "0";
                return;
            }
            else if (Workspace.This.IVVM.WR1 == 0 && SelectedLaserChannel == LaserChannels.ChannelA)
            {
                GetLaserOpticalModuleNumberValue = "0";
                return;
            }
            else if (Workspace.This.IVVM.WR2 == 0 && SelectedLaserChannel == LaserChannels.ChannelB)
            {
                GetLaserOpticalModuleNumberValue = "0";
                return;
            }
            GetLaserOpticalModuleNumberValue = EthernetController.LaserOpticalModuleSerialNumber[SelectedLaserChannel];

        }
        public bool CanExecuteGetLaserOpticalModuleNumberCommand(object parameter)
        {
            return true;
        }

        #endregion
        public ObservableCollection<string> ChannelsOptions
        {
            get
            {
                return _ChannelsOptions;
            }
        }
        public string SelectedChannelCoeff
        {
            get
            {
                return _SelectedChannelCoeff;
            }
            set
            {
                if (_SelectedChannelCoeff != value)
                {
                    if (value == "R1")
                    {
                        SelectedLaserChannel = LaserChannels.ChannelA;
                        SelectedIVChannel = IVChannels.ChannelA;
                    }
                    else if (value == "R2")
                    {
                        SelectedLaserChannel = LaserChannels.ChannelB;
                        SelectedIVChannel = IVChannels.ChannelB;
                    }
                    else
                    {
                        SelectedLaserChannel = LaserChannels.ChannelC;
                        SelectedIVChannel = IVChannels.ChannelC;
                    }
                    _SelectedChannelCoeff = value;
                    RaisePropertyChanged("SelectedChannelCoeff");
                }
            }
        }

        private LaserChannels SelectedLaserChannel=LaserChannels.ChannelC;

        private IVChannels SelectedIVChannel = IVChannels.ChannelC;

        public string GetIVSoftwareVersionNumberValue
        {
            get { return _GetIVSoftwareVersionNumberValue; }
            set
            {

                if (_GetIVSoftwareVersionNumberValue != value)
                {
                    _GetIVSoftwareVersionNumberValue = value;
                    RaisePropertyChanged("GetIVSoftwareVersionNumberValue");
                }
            }
        }
        public string GetIVOpticalModuleSerialNumberValue
        {
            get { return _GetIVOpticalModuleSerialNumberValue; }
            set
            {

                if (_GetIVOpticalModuleSerialNumberValue != value)
                {
                    _GetIVOpticalModuleSerialNumberValue = value;
                    RaisePropertyChanged("GetIVOpticalModuleSerialNumberValue");
                }
            }
        }

        public string GetLaserSoftNumberValue
        {
            get { return _GetLaserSoftNumberValue; }
            set
            {

                if (_GetLaserSoftNumberValue != value)
                {
                    _GetLaserSoftNumberValue = value;
                    RaisePropertyChanged("GetLaserSoftNumberValue");
                }
            }
        }

        public string GetLaserOpticalModuleNumberValue
        {
            get { return _GetLaserOpticalModuleNumberValue; }
            set
            {

                if (_GetLaserOpticalModuleNumberValue != value)
                {
                    _GetLaserOpticalModuleNumberValue = value;
                    RaisePropertyChanged("GetLaserOpticalModuleNumberValue");
                }
            }
        }
    }
}
