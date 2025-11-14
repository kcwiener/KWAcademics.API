using Microsoft.Extensions.Options;
using KWAcademics.Api.Configuration;
using System.Text;

namespace KWAcademics.Api.Services;

public class SpeechSynthesisService
{
    private readonly AzureSpeechOptions _options;
    private readonly ILogger<SpeechSynthesisService> _logger;
    private readonly HttpClient _httpClient;

    public SpeechSynthesisService(
        IOptions<AzureSpeechOptions> options,
        ILogger<SpeechSynthesisService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        if (string.IsNullOrEmpty(_options.Key))
        {
            throw new InvalidOperationException(
                 "Azure Speech Key is not configured. Please set AzureSpeech:Key in appsettings or Key Vault.");
        }

        // Log configuration (without exposing the full key)
        _logger.LogInformation(
            "Azure Speech Service initialized (REST API) - Region: {Region}, Voice: {VoiceName}, Key: {KeyPrefix}***",
            _options.Region, 
            _options.VoiceName, 
            _options.Key.Substring(0, Math.Min(4, _options.Key.Length)));
    }

    public async Task<SynthesisResult> SynthesizeSpeechAsync(string text, int prosodyRate = 0)
    {
        try
        {
            _logger.LogInformation("Starting REST API speech synthesis for {CharCount} characters", text.Length);

            // Build SSML
            var ssml = BuildSsml(text, prosodyRate);

            // Prepare REST API request
            var url = $"https://{_options.Region}.tts.speech.microsoft.com/cognitiveservices/v1";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.Key);
            request.Headers.Add("User-Agent", "KWAcademics");
            request.Headers.Add("X-Microsoft-OutputFormat", "audio-16khz-128kbitrate-mono-mp3");
            request.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

            _logger.LogInformation("Calling Azure Speech REST API at {Url}", url);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"Speech API returned {response.StatusCode}: {errorContent}";

                _logger.LogError("Speech synthesis failed: {Error}", errorMessage);

                return new SynthesisResult
                {
                    Success = false,
                    Message = errorMessage
                };
            }

            var audioData = await response.Content.ReadAsByteArrayAsync();

            // Estimate duration (MP3 at 128kbps = 16KB/sec)
            var durationSeconds = audioData.Length / 16000.0;

            _logger.LogInformation(
                "Speech synthesis completed via REST API. Size: {Size} bytes, Estimated Duration: {Duration}s",
                audioData.Length, 
                durationSeconds);

            return new SynthesisResult
            {
                Success = true,
                AudioData = audioData,
                DurationSeconds = durationSeconds,
                Message = "Synthesis completed successfully"
            };
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error during speech synthesis: {Message}", httpEx.Message);

            return new SynthesisResult
            {
                Success = false,
                Message = $"Network error during synthesis: {httpEx.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during speech synthesis: {Message}", ex.Message);

            return new SynthesisResult
            {
                Success = false,
                Message = $"Error during synthesis: {ex.Message}"
            };
        }
    }

    private string BuildSsml(string text, int prosodyRate)
    {
        var rateString = prosodyRate >= 0 ? $"+{prosodyRate}%" : $"{prosodyRate}%";

        return
        $@"<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis'>
            <voice name='{_options.VoiceName}'>
                <prosody rate='{rateString}'>
                    {System.Security.SecurityElement.Escape(text)}
                </prosody>
            </voice>
        </speak>";
    }

    public class SynthesisResult
    {
        public bool Success { get; set; }
        public byte[]? AudioData { get; set; }
        public double DurationSeconds { get; set; }
        public string? Message { get; set; }
    }
}
