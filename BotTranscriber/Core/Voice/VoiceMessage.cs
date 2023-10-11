using Vosk;

namespace BotTranscriber.Voice;

public class VoiceMessage
{
    private readonly string _path;

    public VoiceMessage(string path)
    {
        _path = path;
    }
    
    string Text(VoskRecognizer recognizer)
    {
        //var vosk = new VoskRecognizer(new Model("C:\\Users\\Albert\\Desktop\\vosk-model-small-ru-0.22"), 48000);
        //vosk.SetMaxAlternatives(0);
        //vosk.SetWords(true);
        using (Stream source = File.OpenRead(_path))
        {
            byte[] buffer = new byte[16384];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                Console.WriteLine(recognizer.AcceptWaveform(buffer, bytesRead) ? recognizer.Result() : recognizer.PartialResult());
            }
        }

        Console.WriteLine($"Результат: {recognizer.FinalResult()}");
        return recognizer.FinalResult();
    }
}