using dotnetproject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using Azure.AI.OpenAI;
using Azure;
//using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Azure.AI.TextAnalytics; // Add this line









namespace dotnetproject.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            var model = new SpeechToTextModel();
            return View(model);
        }

        private static SpeechSynthesizer _currentSynthesizer;


        private string DetectLanguage(string text)
        {
            string key = "<YOUR-KEY>";
            Uri endpoint = new Uri("<YOUR-ENDPOINT-URI>");
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
        public async Task<IActionResult> SendText([FromBody] SpeechToTextModel model)
        {
            try
            {
                    // Get the text from the model
                    string userText = model.Text;

                    // Your OpenAI settings
                    string proxyUrl1 = "<YOUR-URL>";
                    string key = "<YOUR-KEY>";

                    // The full url is appended by /v1/api
                    Uri proxyUrl = new Uri(proxyUrl1 + "/v1/api");

                    // The full key is appended by "/YOUR-GITHUB-ALIAS"
                    AzureKeyCredential token = new AzureKeyCredential(key + "/key");

                    // Instantiate the client with the "full" values for the url and key/token
                    OpenAIClient openAIClient = new OpenAIClient(proxyUrl, token);

                    ChatCompletionsOptions completionOptions = new ChatCompletionsOptions
                    {
                        MaxTokens = 150,
                        Temperature = 0.7f,
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
                string subscriptionKey = "<YOUR-KEY>";
                string subscriptionRegion = "<YOUR-REGION>";


                // Detect language
                string language = DetectLanguage(model.Text);
                Console.WriteLine($"Detected language in SynthesizeSpeech: {language}");


                // Set the appropriate voice based on the detected language
                string voiceName = (language == "en") ? "en-US-JennyNeural" : "de-DE-ConradNeural";
                Console.WriteLine($"Selected voice name: {voiceName}");


                var config = SpeechConfig.FromSubscription(subscriptionKey, subscriptionRegion);
                //config.SpeechSynthesisVoiceName = "en-US-JennyNeural";

                config.SpeechSynthesisVoiceName = voiceName;

                if (_currentSynthesizer != null)
                {
                    await _currentSynthesizer.StopSpeakingAsync();
                    _currentSynthesizer.Dispose();
                }

                _currentSynthesizer = new SpeechSynthesizer(config);

                using (var result = await _currentSynthesizer.SpeakTextAsync(model.Text))
                {
                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
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
