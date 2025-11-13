using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;
using TextToSpeechConverter.Api.Configuration;

namespace TextToSpeechConverter.Api.Services;

public class SpeechSynthesisService
{
    private readonly AzureSpeechOptions _options;
    private readonly ILogger<SpeechSynthesisService> _logger;

    public SpeechSynthesisService(
        IOptions<AzureSpeechOptions> options,
        ILogger<SpeechSynthesisService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.Key))
        {
            throw new InvalidOperationException(
                    "Azure Speech Key is not configured. Please set AzureSpeech:Key in appsettings or Key Vault.");
        }
    }

    public async Task<SynthesisResult> SynthesizeSpeechAsync(string text, int prosodyRate = 0)
    {
        try
        {
            var config = SpeechConfig.FromSubscription(_options.Key, _options.Region);
            config.SpeechSynthesisVoiceName = _options.VoiceName;

            // Build SSML with prosody rate
            var ssml = BuildSsml(text, prosodyRate);

            using var synthesizer = new SpeechSynthesizer(config, null);

            _logger.LogInformation("Starting speech synthesis for {CharCount} characters", text.Length);

            var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                var audioData = result.AudioData;
                var duration = result.AudioDuration;

                _logger.LogInformation("Speech synthesis completed. Duration: {Duration}ms, Size: {Size} bytes",
             duration.TotalMilliseconds, audioData.Length);

                return new SynthesisResult
                {
                    Success = true,
                    AudioData = audioData,
                    DurationSeconds = duration.TotalSeconds,
                    Message = "Synthesis completed successfully"
                };
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                var errorMessage = $"Speech synthesis canceled: {cancellation.Reason}. {cancellation.ErrorDetails}";

                _logger.LogError(errorMessage);

                return new SynthesisResult
                {
                    Success = false,
                    Message = errorMessage
                };
            }
            else
            {
                return new SynthesisResult
                {
                    Success = false,
                    Message = $"Speech synthesis failed with reason: {result.Reason}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during speech synthesis");
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

        return $@"<speak version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2001/10/synthesis'>
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
