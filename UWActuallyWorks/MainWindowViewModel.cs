﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.PubSubEvents;
using Presentation.WPF;
using Presentation.WPF.Events;

namespace UWActuallyWorks
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _currentStatus;

        public MainWindowViewModel(EventAggregator aggregator) : base(aggregator)
        {
            Aggregator.GetEvent<CurrentStatusMessageChangedEvent>().Subscribe(UpdateStatusBar);
            UpdateStatusBar("Welcome");
        }

        public string CurrentStatus
        {
            get
            {
                return _currentStatus;
            }
            private set
            {
                _currentStatus = value;
                OnPropertyChanged();
            }
        }

        private void UpdateStatusBar(string status)
        {
            CurrentStatus = status;
        }
    }
}
