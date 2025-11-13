using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TextToSpeechConverter.Api.Services;

namespace TextToSpeechConverter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpeechController : ControllerBase
{
    private readonly SpeechSynthesisService _speechService;
    private readonly ILogger<SpeechController> _logger;
    private const int MaxWordCount = 200;

    public SpeechController(
        SpeechSynthesisService speechService,
        ILogger<SpeechController> logger)
    {
        _speechService = speechService;
        _logger = logger;
    }

    [HttpPost("synthesize")]
    [Authorize(Policy = "RequireTtsConvertScope")]
    [ProducesResponseType(typeof(SynthesizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Synthesize([FromBody] SynthesizeRequest request)
    {
        // Log authentication details
        _logger.LogInformation("=== Authentication Details ===");
        _logger.LogInformation("User authenticated: {IsAuthenticated}", User.Identity?.IsAuthenticated);
        _logger.LogInformation("User name: {UserName}", User.Identity?.Name);
        _logger.LogInformation("Authentication type: {AuthType}", User.Identity?.AuthenticationType);
        
        // Log SCOPES specifically
        var scopeClaims = User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/identity/claims/scope" || c.Type == "scp").ToList();
        if (scopeClaims.Any())
        {
            _logger.LogInformation("=== SCOPES IN TOKEN ===");
            foreach (var scopeClaim in scopeClaims)
            {
                _logger.LogWarning("SCOPE CLAIM TYPE: {Type}", scopeClaim.Type);
                _logger.LogWarning("SCOPE CLAIM VALUE: {Value}", scopeClaim.Value);
                
                // Parse individual scopes if space-separated
                var individualScopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var scope in individualScopes)
                {
                    _logger.LogWarning("  - Individual Scope: {Scope}", scope);
                }
            }
        }
        else
        {
            _logger.LogError("? NO SCOPE CLAIMS FOUND IN TOKEN!");
        }

        // Log all claims for debugging
        _logger.LogInformation("=== ALL CLAIMS IN TOKEN ===");
        foreach (var claim in User.Claims)
        {
            _logger.LogInformation("Claim Type: {Type} | Value: {Value}", claim.Type, claim.Value);
        }
        _logger.LogInformation("=== End Authentication Details ===");

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid input",
                Detail = "Text cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Validate word count
        int wordCount = request.Text.Split(new[] { ' ', '\n', '\r', '\t' },
          StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

        if (wordCount > MaxWordCount)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Text too long",
                Detail = $"Maximum word count is {MaxWordCount}, but got {wordCount} words.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        _logger.LogInformation("Synthesizing speech for {WordCount} words", wordCount);

        var result = await _speechService.SynthesizeSpeechAsync(request.Text, request.ProsodyRate);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Speech synthesis failed",
                Detail = result.Message,
                Status = StatusCodes.Status500InternalServerError
            });
        }

        // Calculate WPM
        double wpm = result.DurationSeconds > 0 ? wordCount / (result.DurationSeconds / 60.0) : 0;

        var response = new SynthesizeResponse
        {
            Success = true,
            Message = "Conversion completed successfully",
            AudioDataBase64 = Convert.ToBase64String(result.AudioData!),
            DurationSeconds = result.DurationSeconds,
            Wpm = wpm,
            WordCount = wordCount
        };

        return Ok(response);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

public record SynthesizeRequest
{
    public string Text { get; init; } = string.Empty;
    public int ProsodyRate { get; init; } = 0;
}

public record SynthesizeResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? AudioDataBase64 { get; init; }
    public double DurationSeconds { get; init; }
    public double Wpm { get; init; }
    public int WordCount { get; init; }
}
