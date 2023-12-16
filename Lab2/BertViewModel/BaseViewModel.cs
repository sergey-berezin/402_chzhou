using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BertViewModel
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    //public class MainViewModel : INotifyPropertyChanged
    //{
    //    private readonly PersistenceService _persistenceService;
    //    private AppState _appState;

    //    public MainViewModel()
    //    {
    //        _persistenceService = new PersistenceService();
    //        _appState = _persistenceService.LoadState();

    //        // Initialize other ViewModel properties using _appState
    //    }

    //    // Add methods to handle questions and update texts
    //    // Make sure to call _persistenceService.SaveState(_appState) when changes are made
    //}

}
