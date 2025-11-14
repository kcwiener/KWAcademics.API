using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KWAcademics.Api.Services;

namespace KWAcademics.Api.Controllers;

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
                Title = "Kyle says Speech synthesis failed",
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
