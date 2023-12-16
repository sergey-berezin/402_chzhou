using BERTTokenizers;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BertModelLibrary
{
    public class BertModel
    {
        private InferenceSession session;
        static Semaphore sessionSemaphore = new Semaphore(1, 1);
        static bool isDownloaded = false;
        static string modelWebSource = "";

        public BertModel(string webSource)
        {
            modelWebSource = webSource;
        }

        public async Task Create()
        {
            try
            {
                String path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                var modelPath = Path.Combine(path, "bert-large-uncased-whole-word-masking-finetuned-squad.onnx");

                string downaloadedModelPath = modelPath;

                if (!File.Exists(modelPath))
                {
                    downaloadedModelPath = await DownloadModel(modelPath, modelWebSource);
                }
                else
                {
                    isDownloaded = true;
                }
                this.session = new InferenceSession(downaloadedModelPath);
            }
            catch
            {
                throw;
            }


        }
        public static async Task<string> DownloadModel(string modelPath, string modelWebSource)
        {
            try
            {

                var httpClient = new HttpClient();
                int retriesRemain = 5;
                while (retriesRemain > 0 && !isDownloaded)
                {
                    try
                    {
                        using var stream = await httpClient.GetStreamAsync(modelWebSource);
                        using var fileStream = new FileStream(modelPath, FileMode.CreateNew);
                        await stream.CopyToAsync(fileStream);
                        isDownloaded = true;
                    }
                    catch (Exception)
                    {
                        retriesRemain--;
                        await Task.Delay(1000);
                    }

                }
                if (!File.Exists(modelPath))
                {
                    throw new Exception($"Failed to download {modelWebSource}, retries expired.");
                }
                return modelPath;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<String> AnswerQuestionAsync(string text, string question, CancellationToken token)
        {
            try
            {
                var allComputations = await Task.Factory.StartNew<string>(_ =>
                {
                    try
                    {
                        if (!isDownloaded)
                            throw new Exception("Model is not downloaded!");
                        if (token.IsCancellationRequested)
                            token.ThrowIfCancellationRequested();

                        var sentence = $"{{\"question\": {question}, \"context\": \"@CTX\"}}".Replace("@CTX", text);

                        // Create Tokenizer and tokenize the sentence.
                        var tokenizer = new BertUncasedLargeTokenizer();

                        // Get the sentence tokens.
                        var tokens = tokenizer.Tokenize(sentence);

                        // Encode the sentence and pass in the count of the tokens in the sentence.
                        var encoded = tokenizer.Encode(tokens.Count(), sentence);

                        // Break out encoding to InputIds, AttentionMask and TypeIds from list of (input_id, attention_mask, type_id).
                        var bertInput = new BertInput()
                        {
                            InputIds = encoded.Select(t => t.InputIds).ToArray(),
                            AttentionMask = encoded.Select(t => t.AttentionMask).ToArray(),
                            TypeIds = encoded.Select(t => t.TokenTypeIds).ToArray(),
                        };

                        // Create input tensor.
                        var input_ids = ConvertToTensor(bertInput.InputIds, bertInput.InputIds.Length);
                        var attention_mask = ConvertToTensor(bertInput.AttentionMask, bertInput.InputIds.Length);
                        var token_type_ids = ConvertToTensor(bertInput.TypeIds, bertInput.InputIds.Length);


                        // Create input data for session.
                        var input = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input_ids", input_ids),
                                                    NamedOnnxValue.CreateFromTensor("input_mask", attention_mask),
                                                    NamedOnnxValue.CreateFromTensor("segment_ids", token_type_ids) };

                        // Run session and send the input data in to get inference output.

                        sessionSemaphore.WaitOne();
                        var output = session.Run(input);
                        sessionSemaphore.Release();

                        if (token.IsCancellationRequested)
                            token.ThrowIfCancellationRequested();


                        // Call ToList on the output.
                        // Get the First and Last item in the list.
                        // Get the Value of the item and cast as IEnumerable<float> to get a list result.
                        List<float> startLogits = (output.ToList().First().Value as IEnumerable<float>).ToList();
                        List<float> endLogits = (output.ToList().Last().Value as IEnumerable<float>).ToList();

                        // Get the Index of the Max value from the output lists.
                        var startIndex = startLogits.ToList().IndexOf(startLogits.Max());
                        var endIndex = endLogits.ToList().IndexOf(endLogits.Max());

                        // From the list of the original tokens in the sentence
                        // Get the tokens between the startIndex and endIndex and convert to the vocabulary from the ID of the token.
                        var predictedTokens = tokens
                                    .Skip(startIndex)
                                    .Take(endIndex + 1 - startIndex)
                                    .Select(o => tokenizer.IdToToken((int)o.VocabularyIndex))
                                    .ToList();

                        // Print the result.
                        Task.Delay(5000).Wait();

                        if (token.IsCancellationRequested)
                            token.ThrowIfCancellationRequested();

                        Task.Delay(5000).Wait();

                        if (token.IsCancellationRequested)
                            token.ThrowIfCancellationRequested();

                        return String.Join(" ", predictedTokens);
                    }
                    catch (Exception ex)
                    {
                        //throw;
                        return ex.Message;
                    }


                }, token, TaskCreationOptions.LongRunning);


                return allComputations;
            }
            catch (Exception)
            {
                sessionSemaphore.Release();
                throw;
            }


        }

        public static Tensor<long> ConvertToTensor(long[] inputArray, int inputDimension)
        {
            // Create a tensor with the shape the model is expecting. Here we are sending in 1 batch with the inputDimension as the amount of tokens.
            Tensor<long> input = new DenseTensor<long>(new[] { 1, inputDimension });

            // Loop through the inputArray (InputIds, AttentionMask and TypeIds)
            for (var i = 0; i < inputArray.Length; i++)
            {
                // Add each to the input Tenor result.
                // Set index and array value of each input Tensor.
                input[0, i] = inputArray[i];
            }
            return input;
        }
    }

    public class BertInput
    {
        public long[] InputIds { get; set; }
        public long[] AttentionMask { get; set; }
        public long[] TypeIds { get; set; }
    }
}

