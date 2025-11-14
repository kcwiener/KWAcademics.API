namespace KWAcademics.Api.Configuration;

/// <summary>
/// Configuration options for Azure Speech Service
/// </summary>
public class AzureSpeechOptions
{
    public const string SectionName = "AzureSpeech";

    /// <summary>
    /// Azure Speech Service subscription key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Azure Speech Service region (e.g., "eastus", "westus")
    /// </summary>
    public string Region { get; set; } = "eastus";

    /// <summary>
    /// Voice name for speech synthesis (e.g., "en-US-AriaNeural")
    /// </summary>
    public string VoiceName { get; set; } = "en-US-AriaNeural";
}
