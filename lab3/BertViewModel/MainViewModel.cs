using BertModelLibrary;
using DBModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace BertViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private string modelWebSource = "https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx";

        private BertModel bertModel;

        TextTabContext database = new TextTabContext();

        public ObservableCollection<TabItemViewModel> TabItems { get; set; } = new ObservableCollection<TabItemViewModel>();
        public int SelectedTab { get; set; }

        private readonly IErrorSender errorSender;
        private readonly IFileDialog fileDialog;
        public ICommand NewTabCommand { get; private set; }
        public ICommand RemoveTabCommand { get; private set; }
        public ICommand ClearAllCommand { get; private set; }

        public MainViewModel(IErrorSender errorSender, IFileDialog fileDialog)
        {
            this.fileDialog = fileDialog;
            this.errorSender = errorSender;
            NewTabCommand = new RelayCommand(o => { NewTabCommandHandler(); });
            RemoveTabCommand = new RelayCommand(o => { RemoveTabCommandHandler(o); });
            ClearAllCommand = new RelayCommand(o => { ClearAllCommandHandler(); });
        }

        public async void GetBertModel()
        {
            try
            {
                bertModel = new BertModel(modelWebSource);
                var createTask = bertModel.Create();
                await createTask;
            }
            catch (Exception ex)
            {
                errorSender.SendError("Ошибка:" + ex.Message);
            }

        }

        public void LoadfromDB()
        {
            database.Database.EnsureCreated();
            foreach (var tabDb in database.TextTabs.ToList())
            {
                String tabName = string.Format("Tab {0}", tabDb.Id);
                TabItems.Add(new TabItemViewModel(tabName, bertModel, errorSender, fileDialog, database, tabDb.Id, tabDb.Text, tabDb.LatestQuestion, tabDb.LatestAnswer));
            }
        }

        private void NewTabCommandHandler()
        {
            try
            {
                TextTab newTab = new TextTab { Text = "", LatestQuestion = "", LatestAnswer = "...." };
                database.TextTabs.Add(newTab);
                database.SaveChanges();
                String tabName = string.Format("Tab {0}", newTab.Id);

                TabItems.Add(new TabItemViewModel(tabName, bertModel, errorSender, fileDialog, database, newTab.Id));
                SelectedTab = TabItems.Count - 1;
                RaisePropertyChanged(nameof(TabItems));
                RaisePropertyChanged(nameof(SelectedTab));
            }
            catch (Exception ex)
            {
                errorSender.SendError("Ошибка:" + ex.Message);
            }
        }

        private void RemoveTabCommandHandler(object sender)
        {
            try
            {
                TabItemViewModel tabItem = sender as TabItemViewModel;
                var tabDb = database.TextTabs.ToList().Where(t => t.Id == tabItem.DbTabId).First();
                database.TextTabs.Remove(tabDb);
                database.SaveChanges();

                int index = TabItems.IndexOf(tabItem);
                if (SelectedTab == index)
                    SelectedTab = SelectedTab - 1;
                TabItems.RemoveAt(index);
                RaisePropertyChanged(nameof(TabItems));
                RaisePropertyChanged(nameof(SelectedTab));
            }
            catch (Exception ex)
            {
                errorSender.SendError("Ошибка:" + ex.Message);
            }
        }

        private void ClearAllCommandHandler()
        {
            try
            {
                clearDatabase();
                TabItems.Clear();
                RaisePropertyChanged(nameof(TabItems));
            }
            catch (Exception ex)
            {
                errorSender.SendError("Ошибка:" + ex.Message);
            }
        }

        private void clearDatabase()
        {

            database.ChangeTracker.Clear();
            database.Database.EnsureDeleted();
            database.Database.EnsureCreated();
        }

    }
}
