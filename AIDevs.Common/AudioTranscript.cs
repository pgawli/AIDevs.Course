using OpenAI.Audio;

namespace AIDevs.Common;

public class AudioTranscript
{
  private readonly AudioClient audioClient;

  public AudioTranscript(string openAiToken, string model = "whisper-1")
  {
    audioClient = new AudioClient(model: model, openAiToken);
  }
  
  public async Task<string> Convert(string audioFilePath)
  {
    await using var stream = File.OpenRead(audioFilePath);
    var result = await audioClient.TranscribeAudioAsync(stream, Path.GetFileName(audioFilePath));
    if (result != null)
    {
      return result.Value.Text;
    }
    return string.Empty;
  }
}