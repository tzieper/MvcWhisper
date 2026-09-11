using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using MvcWhisper.Services;

namespace MvcWhisper.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudioController : ControllerBase
{
    private readonly TranscriptionService _transcriptionService;

    public AudioController(TranscriptionService transcriptionService)
    {
        _transcriptionService = transcriptionService;
    }

    [HttpPost("transcribe")]
    public async Task<IActionResult> Transcribe(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Keine Audio-Datei empfangen.");

        using var stream = file.OpenReadStream();
        
        // Transkription starten
        var resultText = await _transcriptionService.TranscribeGermanAudioAsync(stream);

        // Gibt den reinen Text ohne JSON-Hülle oder Metadaten zurück
        return Content(resultText, "text/plain");
    }

//[HttpPost("api/audio/live-prompt")]
[HttpPost("live-prompt")] // Ergibt: api/audio/live-prompt
public async Task<IActionResult> ProcessLivePrompt(IFormFile audioFile)
{
    if (audioFile == null || audioFile.Length == 0)
        return BadRequest("Keine Audiodaten empfangen.");

    using var stream = audioFile.OpenReadStream();
    
    // 1. Audio transkribieren
    var promptText = await _transcriptionService.TranscribeBrowserAudioAsync(stream);

    // 2. Hier können Sie den Prompt direkt an ein LLM (wie Ollama) weitergeben!
    // var aiResponse = await _ollamaService.GenerateAsync(promptText);

    return Ok(new { Prompt = promptText });
}

}
