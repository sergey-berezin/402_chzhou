using BertViewModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace lab_2zhou
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel mainViewModel;
        public MainWindow()
        {
            InitializeComponent();
            mainViewModel = new MainViewModel(new MessageBoxErrorSender(), new SaveAndLoadFileDialog());
            mainViewModel.GetBertModel();
            mainViewModel.LoadfromDB();
            DataContext = mainViewModel;
        }

        public class MessageBoxErrorSender : IErrorSender
        {
            public void SendError(string message) => MessageBox.Show(message);
        }
        public class SaveAndLoadFileDialog : IFileDialog
        {
            public string OpenFileDialog()
            {
                try
                {
                    string resultFileName = "";
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "Text|*.txt|All|*.*";
                    if (openFileDialog.ShowDialog() == true)
                        resultFileName = openFileDialog.FileName;
                    return resultFileName;
                }
                catch (Exception ex)
                {
                    throw new Exception("Не удалось открыть файл: " + ex.Message);
                }
            }
        }

        private void AddTabBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
