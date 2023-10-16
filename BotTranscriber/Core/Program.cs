using System.Reflection;
using BotTranscriber.Voice;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Vosk;
using File = System.IO.File;

List<string> openedVoices = new List<string>();
string[] commands = new[] { "/get_text@ok_transcriber_bot", "/get_text" };
Model smallModel = new Model("vosk-model-small-ru-0.22");
// Model bigModel = new Model("C:\\Users\\Albert\\Desktop\\vosk-model-ru-0.42");
VoskRecognizer rec = new VoskRecognizer(smallModel, 48000.0f);
var token = File.ReadAllText("bot.config");
Console.WriteLine("\n" + token);
var botClient = new TelegramBotClient(token); //TODO: заменить
botClient.StartReceiving(UpdateHandler, ErrorHandler);
Console.WriteLine("Bot started!");
Console.ReadLine();

async void UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    await Task.Run(async () =>
    {
        var message = update.Message;

        if (update.Message?.Text != null)
        {
            if (!commands.Contains(message.Text))
            {
                return;
            }
            if (message.ReplyToMessage?.Voice != null)
            {
                await TranscribeAudio(message.ReplyToMessage);
                return;
            }

            if (message.ReplyToMessage?.VideoNote != null)
            {
                await TranscribeVideo(message.ReplyToMessage);
                return;
            }
        }

        if (message?.Voice == null)
        {
            if (message?.VideoNote != null)
            {
                await TranscribeVideo(message);
            }
            return;
        }

        await TranscribeAudio(message);
    });
}

async Task TranscribeVideo(Message videoNoteMessage)
{
    var telegramVideo = await botClient.GetFileAsync(videoNoteMessage.VideoNote.FileId);
    var fileName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + telegramVideo.FileId;
    var mp4Path = fileName + ".mp4";
    var wavPath = fileName + ".wav";
    if (openedVoices.Contains(wavPath)) return;
    openedVoices.Add(wavPath);

    await using (var saveVideoNote = new FileStream(mp4Path, FileMode.Create))
    {
        await botClient.DownloadFileAsync(telegramVideo.FilePath, saveVideoNote);
    }
    
    await using(var reader = new MediaFoundationReader(mp4Path))
    {
        WaveFileWriter.CreateWaveFile(wavPath, reader);
    }

    var voice = new VoiceMessage(wavPath);
    var text = voice.Text(rec);
    
    try
    {
        await botClient.SendTextMessageAsync(videoNoteMessage.Chat.Id, text, replyToMessageId: videoNoteMessage.MessageId);
    }
    catch (ApiRequestException e)
    {
        Console.WriteLine(e.Message);
    }

    openedVoices.Remove(wavPath);
    File.Delete(mp4Path);
    File.Delete(wavPath);
}

async Task TranscribeAudio(Message voiceMessage)
{
    var telegramVoice = await botClient.GetFileAsync(voiceMessage.Voice.FileId);
    var fileName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + telegramVoice.FileId;
    var oggPath = fileName + ".ogg";
    var wavPath = fileName + ".wav";
    if (openedVoices.Contains(wavPath)) return;
    openedVoices.Add(wavPath);

    await using (var saveVoiceStream = new FileStream(oggPath, FileMode.Create))
    {
        await botClient.DownloadFileAsync(telegramVoice.FilePath, saveVoiceStream);
    }

    var wavBytes = ConvertOggToWav(File.ReadAllBytes(oggPath));
    File.WriteAllBytes(wavPath, wavBytes);

    var voice = new VoiceMessage(wavPath);
    string text = voice.Text(rec);
    try
    {
        await botClient.SendTextMessageAsync(voiceMessage.Chat.Id, text, replyToMessageId: voiceMessage.MessageId);
    }
    catch (ApiRequestException e)
    {
        Console.WriteLine(e.Message);
    }

    openedVoices.Remove(wavPath);
    File.Delete(oggPath);
    File.Delete(wavPath);

    byte[] ConvertOggToWav(byte[] audioBytes)
    {
        var decoder = new OpusDecoder(48000, 1);
        MemoryStream audioInput = new MemoryStream(audioBytes);
        List<byte> output = new List<byte>();
        var opus = new OpusOggReadStream(decoder, audioInput);

        while (opus.HasNextPacket)
        {
            short[] packet = opus.DecodeNextPacket();
            if (packet != null)
            {
                for (int i = 0; i < packet.Length; i++)
                {
                    var bytes = BitConverter.GetBytes(packet[i]);
                    output.AddRange(bytes);
                }
            }
        }

        return output.ToArray();
    }
}

async void ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
}