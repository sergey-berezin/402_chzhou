using BertModelLibrary;
using DBModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace BertViewModel
{
    public class TabItemViewModel : BaseViewModel, IDataErrorInfo
    {
        public string TabName { get; set; }
        public bool CancelEnabled { get; set; } = false;

        public String TextFromFile { get; set; } = "";
        public String Question { get; set; } = "";
        public String Answer { get; set; } = "....";
        private readonly IErrorSender errorSender;
        private readonly IFileDialog fileDialog;

        private BertModel bertModel;
        private CancellationTokenSource tokenSource;

        private TextTabContext Database;
        public int DbTabId;

        public ICommand LoadTextFileCommand { get; private set; }
        public ICommand GetAnswerCommand { get; private set; }
        public ICommand CancelAnswerCommand { get; private set; }
        public TabItemViewModel(string tabName, BertModel bertModel, IErrorSender errorSender, IFileDialog fileDialog, TextTabContext database, int DbTabId, string text = "", string question = "", string answer = "....")
        {
            this.TabName = tabName;
            this.fileDialog = fileDialog;
            this.errorSender = errorSender;
            this.bertModel = bertModel;
            this.TextFromFile = text;
            this.Question = question;
            this.Answer = answer;
            this.Database = database;
            this.DbTabId = DbTabId;
            this.tokenSource = new CancellationTokenSource();
            LoadTextFileCommand = new RelayCommand(_ => { LoadTextFileCommandHandler(); });
            CancelAnswerCommand = new RelayCommand(_ =>
            {
                CancelEnabled = false;
                RaisePropertyChanged(nameof(CancelEnabled));
                tokenSource.Cancel();
                tokenSource = new CancellationTokenSource();
            });
            GetAnswerCommand = new AsyncRelayCommand(async _ =>
            {
                CancelEnabled = true;
                RaisePropertyChanged(nameof(CancelEnabled));
                CancellationToken token = tokenSource.Token;
                await ProcessQuestionAsync(bertModel, TextFromFile, Question, token);
                CancelEnabled = false;
                RaisePropertyChanged(nameof(CancelEnabled));
            },
            _ =>
            {
                return string.IsNullOrEmpty(this["TextFromFile"]) && string.IsNullOrEmpty(this["Question"]);
            });
        }

        private void LoadTextFileCommandHandler()
        {
            try
            {
                string filename = fileDialog.OpenFileDialog();
                if (!string.IsNullOrEmpty(filename))
                {
                    LoadFile(filename);
                    RaisePropertyChanged(nameof(TextFromFile));
                }
            }
            catch (Exception ex)
            {
                errorSender.SendError("Ошибка:" + ex.Message);
            }
        }

        private void LoadFile(string filename)
        {
            try
            {
                TextFromFile = GetTextFromFile(path: filename);
            }
            catch (Exception)
            {
                throw;
            }
        }

        static string GetTextFromFile(string path)
        {
            StreamReader? reader = null;
            try
            {
                reader = new StreamReader(path);
                string text = reader.ReadToEnd();
                return text;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
            }
        }

        private async Task ProcessQuestionAsync(BertModel bertModel, string text, string question, CancellationToken token)
        {
            try
            {
                var textTab = Database.TextTabs.Where(tab => tab.Id == DbTabId).First();
                var textEntity = textTab.TextEntity;
                if (textEntity == null || textEntity.Text != text)
                {
                    var foundTextEntities = Database.TextEntities.Where(t => t.Text == text);
                    if (foundTextEntities.Any())
                    {
                        textEntity = foundTextEntities.First();
                        textTab.TextEntity = textEntity;
                        textTab.Text = text;
                        textTab.LatestQuestion = question;
                        Database.TextTabs.Update(textTab);
                        Database.SaveChanges();
                    }
                    else
                    {
                        textEntity = new TextEntity { Text = text };
                        textTab.TextEntity = textEntity;
                        textTab.Text = text;
                        textTab.LatestQuestion = question;
                        Database.TextEntities.Add(textEntity);
                        Database.SaveChanges();
                    }
                }
                var questionFromDb = Database.QuestionsAndAnswers.Where(q => q.TextEntityId == textEntity.Id && q.Question == question);
                if (questionFromDb.Any())
                {
                    textTab.LatestAnswer = Answer;
                    Answer = questionFromDb.First().Answer;
                    Database.SaveChanges();
                }
                else
                {
                    var answer = await bertModel.AnswerQuestionAsync(text, question, token);
                    Answer = answer;
                    if (!token.IsCancellationRequested)
                    {
                        textTab.LatestAnswer = Answer;
                        Database.QuestionsAndAnswers.Add(new QuestionAndAnswer { Question = question, Answer = Answer, TextEntity = textEntity });
                        Database.SaveChanges();
                    }
                }
                RaisePropertyChanged(nameof(Answer));
            }
            catch (Exception ex)
            {
                errorSender.SendError("Ошибка:" + ex.Message);
            }
        }

        public string this[string columnName]
        {
            get
            {
                string error = string.Empty;
                switch (columnName)
                {
                    case "TextFromFile":
                        if (TextFromFile.Length == 0)
                        {
                            error = "Введите входной текст!";
                        }
                        break;
                    case "Question":
                        if (Question.Length == 0)
                        {
                            error = "Введите вопрос!";
                        }
                        break;
                }
                return error;
            }
        }
        public string Error { get; }

    }
}
