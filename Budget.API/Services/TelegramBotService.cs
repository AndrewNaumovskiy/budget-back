using Budget.API.Models.RequestModels;
using System.Globalization;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Budget.API.Services;

public enum BotState
{
    MainMenu,

    BalanceMenu,

    AddEntry,
    ViewEntries,

    EnterExpenses,
    EnterExpensesCategory,
    EnterExpensesSubCategory,
    EnterExpensesAccount,
    EnterExpensesDesc,

    EnterIncome,

    ViewExpenses
}

public class TelegramBotService : IAsyncDisposable
{
    const string BotToken = "1181390616:AAHgxLLxhIz4W8_DL-EdQFcI1jbWJYCJEmU";
    const int MyUserId = 290597767;
    private readonly TelegramBotClient _bot;

    private readonly ExpenseService _expensesService;
    private readonly BalanceService _balanceService;

    private BotState _state = BotState.MainMenu;

    private double _amount;
    private int _accountId;
    private int _subCategoryId;
    private string _description;

    private string _accountName;
    private string _subCategoryName;

    public TelegramBotService(
        ExpenseService expensesService,
        BalanceService balanceService)
    {
        _expensesService = expensesService;
        _balanceService = balanceService;

        return;

        _bot = new TelegramBotClient(BotToken);

        Task.Factory.StartNew(RunBot);
    }

    private async Task RunBot()
    {
        using var cts = new CancellationTokenSource();

        ReceiverOptions options = new ReceiverOptions()
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message, UpdateType.CallbackQuery
            },
            // not process messages while stopped
            DropPendingUpdates = true
        };

        _bot.StartReceiving(updateHandler: HandleMessages, errorHandler: HandleError, options, cancellationToken: cts.Token);

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
        if (update.Message.Chat.Id != MyUserId)
            return;

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

                        #region MainMenu
                        case TelegramKeyboards.Balance:
                            await OpenBalanceMenu(cancellationToken, true);
                            break;

                        case TelegramKeyboards.AddEntry:
                            await OpenAddEntryMenu(cancellationToken);
                            break;

                        case TelegramKeyboards.ViewEntrie:
                            await OpenViewEntriesMenu(cancellationToken);
                            break;
                        #endregion

                        #region BalanceMenu
                        case TelegramKeyboards.ShowInUsd:
                            await OpenBalanceMenu(cancellationToken, false);
                            break;

                        case TelegramKeyboards.NetWorth:
                            await _bot.SendMessage(MyUserId, "Net worth", cancellationToken: cancellationToken);
                            break;

                        case TelegramKeyboards.EnterExpenses:
                            await HandleEnterExpenses(cancellationToken);
                            break;
                        #endregion

                        #region ViewEntriesMenu
                            case TelegramKeyboards.ViewExpenses:
                            await OpenViewExpensesMenu(cancellationToken);
                            break;
                        #endregion

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

    public async Task OpenBalanceMenu(CancellationToken cancellationToken, bool inUah = true)
    {
        _state = BotState.BalanceMenu;
        var balance = await _balanceService.GetBalance(inUah);
        var balanceMessage = $"🌟 *My Balances* 🌟\n\n💳 *UkrSib*: {balance.UkrSib}\n\n🏧 *Privat*: {balance.Privat}\n\n💵 *Cash*: {balance.Cash}\n\n📈 *Total*: *{balance.Total}*";
        await _bot.SendMessage(MyUserId, balanceMessage, parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.BalanceMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task OpenAddEntryMenu(CancellationToken cancellationToken)
    {
        _state = BotState.AddEntry;
        await _bot.SendMessage(MyUserId, "Add Entry", replyMarkup: TelegramKeyboards.AddEntryMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task OpenViewEntriesMenu(CancellationToken cancellationToken)
    {
        _state = BotState.ViewEntries;
        await _bot.SendMessage(MyUserId, "View Entries", replyMarkup: TelegramKeyboards.ViewEntriesMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task HandleBack(CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case BotState.MainMenu:
            case BotState.BalanceMenu:
            case BotState.AddEntry:
            case BotState.ViewEntries:
            case BotState.ViewExpenses:
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

            #region AddExpenses
            case BotState.EnterExpenses: // entered amount of money
                await HandleEnterExpensesAmount(update, cancellationToken);
                break;
            case BotState.EnterExpensesCategory: // entered category
                await HandleEnterExpensesCategory(update, cancellationToken);
                break;
            case BotState.EnterExpensesSubCategory: // entered sub category
                await HandleEnterExpensesSubCategory(update, cancellationToken);
                break;
            case BotState.EnterExpensesAccount: // entered account
                await HandleEnterExpensesAccount(update, cancellationToken);
                break;
            case BotState.EnterExpensesDesc: // entered description
                await HandleEnterExpensesDesc(update, cancellationToken);
                break;
                #endregion
        }
    }
    #region AddExpenses
    public async Task HandleEnterExpenses(CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpenses;
        const string message = "💵 Please enter the amount of your expense:";
        await _bot.SendMessage(MyUserId, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesAmount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesCategory;

        if (!double.TryParse(update.Message.Text.Replace('.',','), out _amount))
        {
            await OpenMainMenu(cancellationToken);
            return;
        }
        
        var categories = _expensesService.GetCategories();

        const string message = "📂 Now, choose a category for this expense:";
        await _bot.SendMessage(MyUserId, message, replyMarkup: TelegramKeyboards.ExpensesCategoryKeyboard(categories), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesCategory(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesSubCategory;

        var category = update.Message.Text;
        var subCategories = _expensesService.GetSubCategories(category);

        const string message = "📂 Now, choose a subcategory for this expense:";
        await _bot.SendMessage(MyUserId, message, replyMarkup: TelegramKeyboards.ExpensesCategoryKeyboard(subCategories), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesSubCategory(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesAccount;

        _subCategoryName = update.Message.Text;
        _subCategoryId = _expensesService.GetCategoryIdByName(_subCategoryName);

        var accounts = _expensesService.GetAccounts();

        const string message = "🏦 Which account should this expense be deducted from?";
        await _bot.SendMessage(MyUserId, "Enter account", replyMarkup: TelegramKeyboards.ExpensesAccountKeyboard(accounts), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesAccount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesDesc;

        _accountName = update.Message.Text;
        _accountId = _expensesService.GetAccountIdByName(_accountName);

        const string message = "📝 Optional: Add a short description for this expense.";
        await _bot.SendMessage(MyUserId, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesDesc(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.BalanceMenu;

        _description = update.Message.Text;

        //await _expensesService.AddExpense(new AddExpensesRequestModel()
        //{
        //    Amount = _amount,
        //    AccountId = _accountId,
        //    CategoryId = _subCategoryId,
        //    Date = DateTime.UtcNow,
        //    Description = _description
        //});

        var currentBalance = _balanceService.GetCurrentBalance(_accountId);

        await ConfirmExpensesPage(cancellationToken);
    }

    private async Task ConfirmExpensesPage(CancellationToken cancellationToken)
    {
        _state = BotState.MainMenu;
        var message = $"💸 *Expense added* 💸\n\n💵 *Amount*: `{_amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("uk-UA"))}`\n\n📂 *Category*: `{_subCategoryName}`\n\n🏦 *Account*: `{_accountName}`\n\n📝 *Description*: `{_description}`";
        await _bot.SendMessage(MyUserId, message, parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.MainMenuKeyboard(), cancellationToken: cancellationToken);
    }
    #endregion

    private async Task OpenViewExpensesMenu(CancellationToken cancellationToken)
    {
        CultureInfo culture = new CultureInfo("uk-UA");

        _state = BotState.ViewExpenses;
        var expenses = await _expensesService.GetExpenses(5);
        
        var message = "📝 *Recent Expenses* 📝\n\n";
        await _bot.SendMessage(MyUserId, message, parseMode: ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        
        foreach (var expense in expenses)
        {
            string temp = $"💸 *Amount*: `{expense.Amount.ToString("C", culture)}`\n\n📂 *Category*: `{expense.CategoryName}`\n\n🏦 *Account*: `{expense.AccountName}`\n\n📝 *Description*: `{expense.Description}`\n\n💰 *Balance*: `{expense.Balance.ToString("C", culture)}`";
            await _bot.SendMessage(MyUserId, temp, parseMode: ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        await _bot.SendMessage(MyUserId, "LOAD MORE?!", parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.ViewExpensesMenuKeyboard(), cancellationToken: cancellationToken);
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
    #region MainMenu
    public const string Balance = "💰 Balance";
    public const string AddEntry = "➕ Add Entry";
    public const string ViewEntrie = "📋 View Entries";
    public const string Shortcuts = "⚙️ Shortcuts";
    #endregion

    #region Balance Menu
    public const string ShowInUsd = "🔄 Show in USD";
    public const string NetWorth = "Net worth";
    public const string Back = "🔙 Back";
    #endregion

    #region AddEntry Menu
    public const string EnterExpenses = "➖ Add Expense";
    public const string EnterIncome = "➕ Add Income";
    // Back
    #endregion

    #region ViewEntries Menu
    public const string ViewExpenses = "📝 Recent Expenses";
    public const string ViewIncome = "💵 Recent Income";
    // Back
    #endregion

    public static ReplyKeyboardMarkup MainMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(
            [
                [
                    new KeyboardButton(Balance),
                ],
                [
                    new KeyboardButton(AddEntry),
                    new KeyboardButton(ViewEntrie),
                ],
                [
                    new KeyboardButton(Shortcuts),
                ],
            ]
        );
    }

    public static ReplyKeyboardMarkup BalanceMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(
            [
                [
                    new KeyboardButton(NetWorth),
                    new KeyboardButton(ShowInUsd),
                ],
                [
                    new KeyboardButton(Back),
                ]
            ]
        );
    }
    
    public static ReplyKeyboardMarkup AddEntryMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(
            [
                [
                    new KeyboardButton(EnterExpenses),
                    new KeyboardButton(EnterIncome),
                ],
                [
                    new KeyboardButton(Back),
                ],
            ]
        );
    }
    
    public static ReplyKeyboardMarkup ViewEntriesMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(
            [
                [
                    new KeyboardButton(ViewExpenses),
                    new KeyboardButton(ViewIncome),
                ],
                [
                    new KeyboardButton(Back),
                ],
            ]
        );
    }

    public static ReplyKeyboardMarkup ViewExpensesMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(
            [
                [
                    new KeyboardButton("Load more"),
                ],
                [
                    new KeyboardButton(Back),
                ],
            ]
        );
    }

    #region Expenses
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
    #endregion
}