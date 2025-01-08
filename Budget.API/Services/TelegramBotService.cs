using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Budget.API.Services;

public class TelegramBotService : IAsyncDisposable
{
    const string BotToken = "1181390616:AAHgxLLxhIz4W8_DL-EdQFcI1jbWJYCJEmU";
    private readonly TelegramBotClient _bot;

    public TelegramBotService()
    {
        _bot = new TelegramBotClient(BotToken);
        using var cts = new CancellationTokenSource();

        _bot.StartReceiving(updateHandler: HandleMessages, errorHandler: HandleError, cancellationToken: cts.Token);
    }

    async Task HandleMessages(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        switch(update.Type)
        {
            case UpdateType.Message:
                await _bot.SendMessage(update.Message.Chat.Id, "meow", cancellationToken: cancellationToken);
                break;
        }
    }

    async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
    {
        // TODO: add logger
        await Console.Error.WriteLineAsync(exception.Message);
    }

    public async ValueTask DisposeAsync()
    {
        
    }
}
