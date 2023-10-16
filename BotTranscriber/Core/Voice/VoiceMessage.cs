using Newtonsoft.Json.Linq;
using Vosk;

namespace BotTranscriber.Voice;

public class VoiceMessage
{
    private readonly string _path;

    public VoiceMessage(string path)
    {
        _path = path;
    }
    
    public string Text(VoskRecognizer rec)
    {
        rec.SetMaxAlternatives(0);
        rec.SetWords(true);
        using(Stream source = File.OpenRead(_path)) {
            byte[] buffer = new byte[16000];
            int bytesRead;
            while((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                rec.AcceptWaveform(buffer, bytesRead);
            }
        }

        JObject text = JObject.Parse(rec.FinalResult());
        var result = text.Last.ToString();

        return result.Substring(8);
    }
}
