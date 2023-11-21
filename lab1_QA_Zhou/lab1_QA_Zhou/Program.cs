using classlibrary1;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lab1_QA_Zhou
{
    class Program
    {
        static Semaphore consoleSemaphore = new Semaphore(1, 1);
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    string path = @args[0];
                    //string path = "..\\..\\..\\..\\hobbit.txt";
                    //string path = "..\\..\\..\\..\\hole.txt";
                    //string path = "..\\..\\..\\..\\gandalf.txt";
                    string text = GetTextFromFile(path);
                    string modelWebSource = "https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx1";
                    Console.WriteLine(text);

                    CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
                    CancellationToken token = cancelTokenSource.Token;

                    var createTask = BertModel.Create(modelWebSource);
                    while (true && !createTask.IsCompleted)
                    {
                        lock (BertModel.progressBar)
                        {
                            while (BertModel.progressBar.Count > 0)
                                Console.Out.WriteLine(BertModel.progressBar.Dequeue());
                        }
                    }
                    var bertModel = await createTask;

                    string? question = "start";
                    consoleSemaphore.WaitOne();
                    while ((question = Console.ReadLine()) != "")
                    {
                        consoleSemaphore.Release();
                        if (question == "cancel") { cancelTokenSource.Cancel(); }
                        var answer = ProcessQuestionAsync(bertModel, text, question, token);
                        consoleSemaphore.WaitOne();
                    }
                }
                else
                {
                    throw new ArgumentException("No file path in command line arguments!");
                }
            }
            catch (Exception ex)
            {
                consoleSemaphore.WaitOne();
                Console.WriteLine(ex.Message);
                consoleSemaphore.Release();
            }
        }

        static async Task<string> ProcessQuestionAsync(BertModel bertModel, string text, string question, CancellationToken token)
        {
            try
            {
                return await bertModel.AnswerQuestionAsync(text, question, token);
            }
            catch (Exception ex)
            {
                return $"Error processing question '{question}': {ex.Message}";
            }
        }

        static string GetTextFromFile(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception loading from file: {ex.Message}");
                return "";
            }
        }
    }
}