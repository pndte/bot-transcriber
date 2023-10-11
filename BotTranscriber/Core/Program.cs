using Telegram.Bot;
using Telegram.Bot.Types;
using Vosk;
using File = System.IO.File;

Console.WriteLine("Hello, World!");


var botClient = new TelegramBotClient($"{File.ReadAllText("C:\\Users\\Albert\\RiderProjects\\BotTranscriber\\BotTranscriber\\Core\\bot.config")}"); //TODO: заменить
botClient.StartReceiving(UpdateHandler, ErrorHandler);

async void UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    await Task.Run(() =>
    {
        
    });
}



async void ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
}