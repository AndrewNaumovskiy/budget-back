using Budget.API.Models.RequestModels;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Budget.API.Services;

public enum BotState
{
    MainMenu,

    BalanceMenu,

    EnterExpenses,
    EnterExpensesCategory,
    EnterExpensesAccount,
    EnterExpensesDesc,

    EnterIncome
}

public class TelegramBotService : IAsyncDisposable
{
    const string BotToken = "1181390616:AAHgxLLxhIz4W8_DL-EdQFcI1jbWJYCJEmU";
    const int MyUserId = 290597767;
    private readonly TelegramBotClient _bot;

    private readonly ExpensesService _expensesService;

    private BotState _state = BotState.MainMenu;

    private double _amount;
    private int _accountId;
    private int _categoryId;
    private string _description;

    public TelegramBotService(ExpensesService expensesService)
    {
        _expensesService = expensesService;

        _bot = new TelegramBotClient(BotToken);
        
        Task.Factory.StartNew(RunBot);
    }

    private async Task RunBot()
    {
        using var cts = new CancellationTokenSource();
        
        _bot.StartReceiving(updateHandler: HandleMessages, errorHandler: HandleError, cancellationToken: cts.Token);

        await _bot.SetMyCommands(
            new[]
            {
                new BotCommand { Command = "/main", Description = "Go to main menu" },
                new BotCommand { Command = "/meow", Description = "Meowing" }
            },
            cancellationToken: cts.Token);

        await Task.Delay(-1);
    }

    async Task HandleMessages(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        switch(update.Type)
        {
            case UpdateType.Message:
                {
                    switch(update.Message.Text)
                    {
                        case "/main":
                            await OpenMainMenu(cancellationToken);
                            break;

                        case "/meow":
                            await TestInline(update, cancellationToken);
                            break;

                        case TelegramKeyboards.Balance:
                            await OpenBalanceMenu(cancellationToken);
                            break;

                        case TelegramKeyboards.EnterExpenses:
                            await HandleEnterExpenses(cancellationToken);
                            break;

                        case TelegramKeyboards.Back:
                            await HandleBack(cancellationToken);
                            break;

                        default:
                            await HandleText(update, cancellationToken);
                            break;
                    }
                    break;
                }
            case UpdateType.CallbackQuery:
                string codeOfButton = update.CallbackQuery.Data;
                if(codeOfButton == "btn1")
                {
                    await _bot.SendMessage(chatId: update.CallbackQuery.Message.Chat.Id, "ti sho knipku 1 nazhav?", cancellationToken: cancellationToken);
                }
                else if(codeOfButton == "btn2")
                {
                    InlineKeyboardMarkup inlineKeyBoard = new InlineKeyboardMarkup(
                        new[]
                        {
                            // first row
                            new[]
                            {
                                // first button in row
                                InlineKeyboardButton.WithCallbackData(text: "Button3", callbackData: "btn3"),
                                // second button in row
                                InlineKeyboardButton.WithCallbackData(text: "Button4", callbackData: "btn4"),
                            },
                        });

                    await _bot.EditMessageText(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId, "button2", replyMarkup: inlineKeyBoard);
                }
                break;
        }
    }

    async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
    {
        // TODO: add logger
        await Console.Error.WriteLineAsync(exception.Message);
    }

    public async Task OpenMainMenu(CancellationToken cancellationToken)
    {
        _state = BotState.MainMenu;
        await _bot.SendMessage(MyUserId, "Main Menu", replyMarkup: TelegramKeyboards.MainMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task OpenBalanceMenu(CancellationToken cancellationToken)
    {
        _state = BotState.BalanceMenu;
        const int Balance = 1000;
        await _bot.SendMessage(MyUserId, $"Your balance is {Balance}", replyMarkup: TelegramKeyboards.BalanceMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task HandleEnterExpenses(CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpenses;
        await _bot.SendMessage(MyUserId, $"Enter amount", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    public async Task HandleBack(CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case BotState.BalanceMenu:
                await OpenMainMenu(cancellationToken);
                break;
        }
    }

    public async Task HandleText(Update update, CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case BotState.MainMenu:
                return;
            case BotState.EnterExpenses: // entered amount of money
                await HandleEnterExpensesAmount(update, cancellationToken);
                break;
            case BotState.EnterExpensesCategory: // entered category
                await HandleEnterExpensesCategory(update, cancellationToken);
                break;
            case BotState.EnterExpensesAccount: // entered account
                await HandleEnterExpensesAccount(update, cancellationToken);
                break;
            case BotState.EnterExpensesDesc: // entered description
                await HandleEnterExpensesDesc(update, cancellationToken);
                break;
        }
    }

    private async Task HandleEnterExpensesAmount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesCategory;

        _amount = double.Parse(update.Message.Text.Replace('.', ','));

        var categories = await _expensesService.GetCategories();
        await _bot.SendMessage(MyUserId, "Enter category", replyMarkup: TelegramKeyboards.ExpensesCategoryKeyboard(categories), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesCategory(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesAccount;

        // TODO: find better approach
        _categoryId = (await _expensesService.GetCategories()).IndexOf(update.Message.Text);

        var accounts = await _expensesService.GetAccounts();
        await _bot.SendMessage(MyUserId, "Enter account", replyMarkup: TelegramKeyboards.ExpensesAccountKeyboard(accounts), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesAccount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesDesc;

        _accountId = (await _expensesService.GetAccounts()).IndexOf(update.Message.Text);

        await _bot.SendMessage(MyUserId, "Enter description", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesDesc(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.BalanceMenu;

        _description = update.Message.Text;

        await _expensesService.AddExpense(new AddExpensesRequestModel()
        {
            Amount = _amount,
            AccountId = _accountId,
            CategoryId = _categoryId,
            Date = DateTime.UtcNow,
            Description = _description
        });

        await OpenMainMenu(cancellationToken);
    }


    public async Task TestInline(Update update, CancellationToken cancellationToken)
    {
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(
            // keyboard
            new[]
            {
                // first row
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Button1", callbackData: "btn1"),
                    InlineKeyboardButton.WithCallbackData(text: "Button2", callbackData: "btn2"),
                },
                // second row
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Button3", callbackData: "meow")
                }
            }
        );

        var message = await _bot.SendMessage(MyUserId, text: "testing", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _bot.Close();
    }
}

public static class TelegramKeyboards
{
    public const string Balance = "💰 Balance";
    public const string EnterExpenses = "Enter Expenses";
    public const string Back = "Back";

    public static ReplyKeyboardMarkup MainMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(
            [
                [
                    new KeyboardButton(Balance),
                ],
                [
                    new KeyboardButton(EnterExpenses),
                    new KeyboardButton("Enter Income"),
                ]
            ]
        );
    }

    public static ReplyKeyboardMarkup BalanceMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(
            [
                [
                    new KeyboardButton("Back"),
                ],
            ]
        );
    }

    public static ReplyKeyboardMarkup ExpensesCategoryKeyboard(List<string> expensesCategories)
    {
        var result = new ReplyKeyboardMarkup();

        var rowCount = Math.Ceiling(expensesCategories.Count / 3.0);

        for(int i = 0; i< rowCount; i++)
        {
            result.AddNewRow(
                expensesCategories.Skip(i * 3).Take(3).Select(x => new KeyboardButton(x)).ToArray()
            );
        }

        return result;
    }

    public static ReplyKeyboardMarkup ExpensesAccountKeyboard(List<string> expensesAccount)
    {
        var result = new ReplyKeyboardMarkup();

        result.AddButtons(
            expensesAccount.Select(x => new KeyboardButton(x)).ToArray()
        );

        return result;
    }
}