using dotnetproject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Azure.Storage.Blobs;
using System.IO;
using Azure.AI.OpenAI;
using Azure;
//using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Azure.AI.TextAnalytics; // Add this line
using System.Linq;
using System;








namespace dotnetproject.Controllers
{
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new SpeechToTextModel();
            return View(model);
        }


        private string DetectLanguage(string text)
        {
            string key = "094f202e5f7c40a5a6902df0dfa77068";
            Uri endpoint = new Uri("https://textanalyticsfortest.cognitiveservices.azure.com/");
            AzureKeyCredential credentials = new AzureKeyCredential(key);
            TextAnalyticsClient textAnalyticsClient = new TextAnalyticsClient(endpoint, credentials);

            try
            {
                DetectedLanguage detectedLanguage = textAnalyticsClient.DetectLanguage(text);

                // Check if there is at least one detected language
                if (textAnalyticsClient.DetectLanguage(text) != null)
                {
                    // Extract the language code
                    string languageCode = detectedLanguage.Iso6391Name;
                    return languageCode;
                }
                else
                {
                    // Handle the case where language detection failed
                    Console.WriteLine("Language detection failed.");
                    return null;
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error during language detection: {ex.Message}");
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConvertSpeechToText()
        {
            try
            {
                var speechConfig = SpeechConfig.FromSubscription("8eed5c65ae94466babb63658d40fedb4", "westeurope");

                speechConfig.SpeechRecognitionLanguage = "en-US";

                using (var audioStream = Request.Body)
                using (var audioConfig = AudioConfig.FromStreamInput(AudioInputStream.CreatePushStream()))
                using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
                {
                    Console.WriteLine("SpeechRecognizer created successfully.");

                    // Start continuous recognition
                    var stopRecognition = new TaskCompletionSource<int>();
                    recognizer.Recognizing += (s, e) =>
                    {
                        // Process intermediate recognition results in real-time
                        Console.WriteLine($"Interim result: {e.Result.Text}");
                        // You can send this intermediate result to the client using SignalR
                    };

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Console.WriteLine($"Final result: {e.Result.Text}");
                            // You can send this final result to the client using SignalR
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine("No speech could be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        }

                        // Signal the task completion to stop the recognition
                        stopRecognition.TrySetResult(0);
                    };

                    // Start recognition
                    await recognizer.StartContinuousRecognitionAsync();

                    // Continue processing until the task is signaled to stop
                    // (e.g., after receiving a stop request from the client)
                    await stopRecognition.Task;

                    // Stop recognition
                    await recognizer.StopContinuousRecognitionAsync();
                    string detectedLanguage = "en"; // Replace with the actual detected language
                    var speechToTextModel = new SpeechToTextModel { Language = detectedLanguage };

                    // Call SendText method with the detected language
                    var sendTextResponse = await SendText(speechToTextModel);

                    return sendTextResponse;

                    //return Json(new { Status = "Recognition stopped successfully" });


                }
            }
            catch (Exception ex)
            {
                return Json(new { Error = $"Error during speech recognition: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SendText([FromBody] SpeechToTextModel model)
        {
            try
            {
                // Get the text from the model
                string userText = model.Text;

                // Your OpenAI settings
                string proxyUrl1 = "https://aoai.hacktogether.net";
                string key = "42e4f3a0-9ecc-47de-aca0-3702df81b334";

                // The full url is appended by /v1/api
                Uri proxyUrl = new Uri(proxyUrl1 + "/v1/api");

                // The full key is appended by "/YOUR-GITHUB-ALIAS"
                AzureKeyCredential token = new AzureKeyCredential(key + "/artemcodit");

                // Instantiate the client with the "full" values for the url and key/token
                OpenAIClient openAIClient = new OpenAIClient(proxyUrl, token);

                ChatCompletionsOptions completionOptions = new ChatCompletionsOptions
                {
                    MaxTokens = 150,
                    Temperature = 0.5f,
                    NucleusSamplingFactor = 0.95f,
                    DeploymentName = "gpt-35-turbo"
                };

                // Add system and user messages
                completionOptions.Messages.Add(new ChatMessage(ChatRole.User, userText));

                // Get response from Azure OpenAI
                var response = await openAIClient.GetChatCompletionsAsync(completionOptions);

                // Access the generated completion content if there is a 'Choices' property
                if (response.Value != null && response.Value.Choices.Count > 0)
                {
                    string completionContent = response.Value.Choices[0].Message.Content;

                    // You can do further processing with the generated completionContent

                    //model.Language = language;

                    // Return the response to the client
                    return Json(new { Answer = completionContent });
                }
                else
                {
                    return Json(new { Error = "No valid response received from Azure OpenAI." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Error = $"Error processing text: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SynthesizeSpeech([FromBody] SpeechToTextModel model)
        {
            try
            {
                Console.WriteLine($"Received language in SynthesizeSpeech: {model.Language}");


                // Assuming model.Text contains the text to be synthesized
                string subscriptionKey = "8eed5c65ae94466babb63658d40fedb4";
                string subscriptionRegion = "westeurope";


                // Detect language
                string language = DetectLanguage(model.Text);
                Console.WriteLine($"Detected language in SynthesizeSpeech: {language}");


                // Set the appropriate voice based on the detected language
                string voiceName = (language == "en") ? "en-US-JennyNeural" : "de-DE-ConradNeural";
                Console.WriteLine($"Selected voice name: {voiceName}");


                var config = SpeechConfig.FromSubscription(subscriptionKey, subscriptionRegion);
                //config.SpeechSynthesisVoiceName = "en-US-JennyNeural";

                config.SpeechSynthesisVoiceName = voiceName;



                using (var synthesizer = new SpeechSynthesizer(config))
                {
                    using (var result = await synthesizer.SpeakTextAsync(model.Text))
                    {
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            Console.WriteLine($"Speech synthesized for text [{model.Text}]");
                            return Json(new { Status = "Speech synthesis completed", Language = language });
                        }
                        else if (result.Reason == ResultReason.Canceled)
                        {
                            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                            if (cancellation.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                            }

                            return Json(new { Error = "Speech synthesis canceled or encountered an error" });
                        }
                    }
                }

                // Add a default return statement if needed
                return Json(new { Error = "Unexpected error during speech synthesis" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during speech synthesis: {ex.Message}");
                return Json(new { Error = $"Error during speech synthesis: {ex.Message}" });

            }
        }




        //    public class PronunciationResult
        //    {
        //        public string WordText { get; set; }
        //        public string ErrorType { get; set; }
        //        public double AccuracyScore { get; set; }

        //        public PronunciationResult(string wordText, string errorType, double accuracyScore)
        //        {
        //            WordText = wordText;
        //            ErrorType = errorType;
        //            AccuracyScore = accuracyScore;
        //        }
        //    }
        //    private double CalculateSum(IEnumerable<double> values)
        //    {
        //        double sum = 0;
        //        int count = 0;

        //        foreach (var value in values)
        //        {
        //            sum += value;
        //            count++;
        //        }

        //        return count > 0 ? sum / count : 0;
        //    }

        //    [HttpPost]
        //    public async Task<IActionResult> AssessPronunciation([FromBody] SpeechToTextModel model)
        //    {
        //        try
        //        {
        //            // Assuming model.Text contains the text for pronunciation assessment
        //            string subscriptionKey = "8eed5c65ae94466babb63658d40fedb4";
        //            string subscriptionRegion = "westeurope";

        //            var config = SpeechConfig.FromSubscription(subscriptionKey, subscriptionRegion);
        //            config.SpeechRecognitionLanguage = "en-US";

        //            using (var recognizer = new SpeechRecognizer(config))
        //            {
        //                //var referenceText = "Today was a beautiful day. We had a great time taking a long walk outside in the morning. The countryside was in full bloom, yet the air was crisp and cold. Towards the end of the day, clouds came in, forecasting much needed rain.";
        //                var referenceText = model.Text;
        //                // Create pronunciation assessment config
        //                string jsonConfig = "{\"GradingSystem\":\"HundredMark\",\"Granularity\":\"Phoneme\",\"EnableMiscue\":true, \"EnableProsodyAssessment\":true}";
        //                var pronConfig = PronunciationAssessmentConfig.FromJson(jsonConfig);
        //                pronConfig.ReferenceText = referenceText;

        //                // Apply the pronunciation assessment config
        //                pronConfig.ApplyTo(recognizer);

        //                var recognizedWords = new List<string>();
        //                var pronResults = new List<PronunciationResult>();
        //                var fluencyScores = new List<double>();
        //                var prosodyScores = new List<double>();
        //                var durations = new List<int>();
        //                var done = false;

        //                recognizer.SessionStopped += (s, e) => done = true;
        //                recognizer.Canceled += (s, e) => done = true;

        //                recognizer.Recognized += (s, e) =>
        //                {
        //                    var pronResult = PronunciationAssessmentResult.FromResult(e.Result);

        //                    // Check if pronResult is not null before accessing its properties
        //                    if (pronResult != null)
        //                    {
        //                        Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
        //                        Console.WriteLine($"    Accuracy score: {pronResult.AccuracyScore}, pronunciation score: {pronResult.PronunciationScore}, completeness score: {pronResult.CompletenessScore}, fluency score: {pronResult.FluencyScore}, prosody score: {pronResult.ProsodyScore}");

        //                        fluencyScores.Add(pronResult.FluencyScore);
        //                        prosodyScores.Add(pronResult.ProsodyScore);

        //                        foreach (var word in pronResult.Words)
        //                        {
        //                            var newWord = new PronunciationResult(word.Word, word.ErrorType, word.AccuracyScore);
        //                            pronResults.Add(newWord);
        //                        }

        //                        foreach (var result in e.Result.Best())
        //                        {
        //                            durations.Add(result.Words.Sum(item => item.Duration));
        //                            recognizedWords.AddRange(result.Words.Select(item => item.Word).ToList());
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // Handle the case where pronResult is null (optional)
        //                        Console.WriteLine("Pronunciation result is null.");
        //                    }
        //                };

        //                // Starts continuous recognition.
        //                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

        //        while (!done)
        //        {
        //            // Allow the program to run and process results continuously.
        //            await Task.Delay(1000); // Adjust the delay as needed.
        //        }

        //        // Waits for completion.
        //        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

        //        // Your logic for comparing recognized and reference words and calculating scores goes here
        //        string[] referenceWords = referenceText.ToLower().Split(' ');
        //        for (int j = 0; j < referenceWords.Length; j++)
        //        {
        //            referenceWords[j] = Regex.Replace(referenceWords[j], "^[\\p{P}\\s]+|[\\p{P}\\s]+$", "");
        //        }

        //        var differ = new Differ();
        //        var inlineBuilder = new InlineDiffBuilder(differ);
        //        var diffModel = inlineBuilder.BuildDiffModel(string.Join("\n", referenceWords), string.Join("\n", recognizedWords));

        //        int currentIdx = 0;
        //        var finalWords = new List<PronunciationResult>();

        //        foreach (var delta in diffModel.Lines)
        //        {
        //            if (delta.Type == ChangeType.Unchanged)
        //            {
        //                finalWords.Add(pronResults[currentIdx]);
        //                currentIdx += 1;
        //            }

        //            if (delta.Type == ChangeType.Deleted || delta.Type == ChangeType.Modified)
        //            {
        //                var word = new PronunciationResult(delta.Text, "Omission", 0); // You may need to adjust the values
        //                finalWords.Add(word);
        //            }

        //            if (delta.Type == ChangeType.Inserted || delta.Type == ChangeType.Modified)
        //            {
        //                var word = pronResults[currentIdx];
        //                if (word.ErrorType == "None")
        //                {
        //                    word.ErrorType = "Insertion";
        //                    finalWords.Add(word);
        //                }
        //                currentIdx += 1;
        //            }
        //        }

        //        // Calculate scores based on finalWords
        //        var accuracyScore = finalWords.Sum(item => item.AccuracyScore) / finalWords.Count();
        //        var prosodyScore = CalculateSum(prosodyScores);
        //        var fluencyScore = CalculateSum(fluencyScores.Zip(durations, (x, y) => x * y)) / durations.Sum();
        //        var completenessScore = (double)pronResults.Count(item => item.ErrorType == "None") / referenceWords.Length * 100;
        //        completenessScore = completenessScore <= 100 ? completenessScore : 100;

        //        var pronScore = accuracyScore * 0.4 + prosodyScore * 0.2 + fluencyScore * 0.2 + completenessScore * 0.2;

        //        Console.WriteLine("Paragraph pronunciation score: {0}, accuracy score: {1}, completeness score: {2}, fluency score: {3}, prosody score: {4}", pronScore, accuracyScore, completenessScore, fluencyScore, prosodyScore);

        //        return Json(new
        //        {
        //            PronunciationScore = pronScore,
        //            AccuracyScore = accuracyScore,
        //            CompletenessScore = completenessScore,
        //            FluencyScore = fluencyScore,
        //            ProsodyScore = prosodyScore,
        //            PronunciationResults = finalWords,
        //            Details = "Detailed pronunciation assessment results...",
        //        });
        //    }
        //}
        //        catch (Exception ex)
        //        {
        //            return Json(new { Error = $"Error during pronunciation assessment: {ex.Message}" });
        //        }
        //    }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
