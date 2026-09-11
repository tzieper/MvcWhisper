using System.IO;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using FFMpegCore;
using FFMpegCore.Pipes;
using System.Diagnostics;

namespace MvcWhisper.Services;

public class TranscriptionService
{
    private readonly string _modelPath = "wwwroot/models/ggml-base.bin";

    public async Task<string> TranscribeGermanAudioAsync(Stream audioStream)
    {
        // 1. Whisper-Factory mit dem deutschen/multilingualen Modell laden
        using var factory = WhisperFactory.FromPath(_modelPath);

        // 2. Prozessor konfigurieren (Sprache explizit auf Deutsch setzen)
        using var processor = factory.CreateBuilder()
            .WithLanguage("de") // Zwingt das Modell auf Deutsch
            .Build();

        var textBuilder = new StringBuilder();

        // 3. Audio-Stream verarbeiten (Muss 16kHz, Mono, 16-bit WAV sein)
        await foreach (var result in processor.ProcessAsync(audioStream))
        {
            // Fügt die erkannten Textfragmente nahtlos aneinander
            textBuilder.Append(result.Text);
        }

        return textBuilder.ToString().Trim();
    }

public async Task<string> TranscribeWithNativeFFmpegAsync(Stream inputWebmStream)
{
    using var memoryStream = new MemoryStream();

    // Startet direkt den installierten ffmpeg-Befehl des Betriebssystems
    var startInfo = new ProcessStartInfo
    {
        FileName = "ffmpeg",
        Arguments = "-i pipe:0 -acodec pcm_s16le -ar 16000 -ac 1 -f wav pipe:1",
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true, // Verhindert Blockaden bei Fehlern
        CreateNoWindow = true
    };

    using (var process = Process.Start(startInfo))
    {
        if (process != null)
        {
            // Schreibt das Browser-Audio in den FFmpeg-Input
            var inputTask = Task.Run(async () =>
            {
                await inputWebmStream.CopyToAsync(process.StandardInput.BaseStream);
                process.StandardInput.BaseStream.Close(); // Schließen signalisiert "Ende der Datei"
            });

            // Liest das konvertierte WAV aus dem FFmpeg-Output
            var outputTask = process.StandardOutput.BaseStream.CopyToAsync(memoryStream);

            await Task.WhenAll(inputTask, outputTask);
            await process.WaitForExitAsync();
        }
    }

    memoryStream.Position = 0;

    // Übergabe an Whisper
    using var factory = WhisperFactory.FromPath(_modelPath);
    using var processor = factory.CreateBuilder().WithLanguage("de").Build();

    var textBuilder = new System.Text.StringBuilder();
    await foreach (var result in processor.ProcessAsync(memoryStream))
    {
        textBuilder.Append(result.Text);
    }

    return textBuilder.ToString().Trim();
}
public async Task<string> TranscribeBrowserAudioAsync(Stream inputWebmStream)
    {
        using var memoryStream = new MemoryStream();
        
        // Korrekte Syntax für FFMpegCore Pipes in aktuellen Versionen
        await FFMpegArguments
            .FromPipeInput(new StreamPipeSource(inputWebmStream))
            .OutputToPipe(new StreamPipeSink(memoryStream), options => options
                .WithAudioCodec("pcm_s16le")
                .WithAudioSamplingRate(16000)
                .WithCustomArgument("-ac 1")
                .ForceFormat("wav"))
            .ProcessAsynchronously();

        memoryStream.Position = 0;

        using var factory = WhisperFactory.FromPath(_modelPath);
        using var processor = factory.CreateBuilder().WithLanguage("de").Build();

        var textBuilder = new System.Text.StringBuilder();
        await foreach (var result in processor.ProcessAsync(memoryStream))
        {
            textBuilder.Append(result.Text);
        }

        return textBuilder.ToString().Trim();
    }

}
