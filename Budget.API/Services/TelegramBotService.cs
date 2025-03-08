using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;
using System.Diagnostics;
using Budget.API.Models.RequestModels;

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
    EnterIncomeCategory,
    EnterIncomeAccount,
    EnterIncomeDesc,

    EnterSaving,
    EnterSavingCategory,
    EnterSavingAccount,
    EnterSavingDesc,

    EnterTransfer,
    EnterTransferAccountFrom,
    EnterTransferAccountTo,
    EnterTransferDesc,

    ViewExpenses,

    OpenStatistics,
    Summary,
    ExpenseChart,
}

public class TelegramBotService : IAsyncDisposable
{
    const string BotToken = "1181390616:AAHgxLLxhIz4W8_DL-EdQFcI1jbWJYCJEmU";

    private readonly TelegramBotClient _bot;

    private readonly SavingService _savingService;
    private readonly IncomeService _incomeService;
    private readonly BalanceService _balanceService;
    private readonly ExpenseService _expensesService;
    private readonly TransferService _transferService;
    private readonly TransactionsService _transactionsService;
    private readonly DatabaseSelectorService _databaseSelectorService;

    private BotState _state = BotState.MainMenu;

    private double _amount;
    private int _accountId;
    private int _subCategoryId;
    private string _description;

    private int _accountFromId;
    private int _accountToId;
    private string _accountFromName;
    private string _accountToName;

    private string _accountName;
    private string _subCategoryName;

    private int _summaryYear;
    private int _summaryMonth;

    public TelegramBotService(
        SavingService savingService,
        IncomeService incomeService,
        BalanceService balanceService,
        ExpenseService expensesService,
        TransferService transferService,
        TransactionsService transactionsService,
        DatabaseSelectorService databaseSelectorService)
    {
        _savingService = savingService;
        _incomeService = incomeService;
        _balanceService = balanceService;
        _expensesService = expensesService;
        _transferService = transferService;
        _transactionsService = transactionsService;
        _databaseSelectorService = databaseSelectorService;

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
        if (!_databaseSelectorService.CheckUser(update.Message.Chat.Id))
        {
            Debug.WriteLine($"User {update.Message.Chat.Id} is not allowed");
            await _bot.SendMessage(update.Message.Chat.Id, "You are not allowed to use this bot", cancellationToken: cancellationToken);
            return;
        }

        switch(update.Type)
        {
            case UpdateType.Message:
                {
                    switch(update.Message.Text)
                    {
                        case "/main":
                            await OpenMainMenu(update.Message.Chat.Id, cancellationToken);
                            break;

                        case "/meow":
                            await TestInline(update, cancellationToken);
                            break;

                        #region MainMenu
                        case TelegramKeyboards.Balance:
                            await OpenBalanceMenu(update.Message.Chat.Id, cancellationToken, true);
                            break;

                        case TelegramKeyboards.AddEntry:
                            await OpenAddEntryMenu(update.Message.Chat.Id, cancellationToken);
                            break;

                        case TelegramKeyboards.ViewEntries:
                            await OpenViewEntriesMenu(update.Message.Chat.Id, cancellationToken);
                            break;

                        case TelegramKeyboards.Statistics:
                            await OpenStatisticsMenu(update.Message.Chat.Id, cancellationToken);
                            break;
                        #endregion

                        #region BalanceMenu
                        case TelegramKeyboards.ShowInUsd:
                            await OpenBalanceMenu(update.Message.Chat.Id, cancellationToken, false);
                            break;

                        // TODO: implement net worth
                        case TelegramKeyboards.NetWorth:
                            await _bot.SendMessage(update.Message.Chat.Id, "Net worth", cancellationToken: cancellationToken);
                            break;
                        #endregion

                        #region AddEntry
                        case TelegramKeyboards.EnterExpenses:
                            await HandleEnterExpenses(update.Message.Chat.Id, cancellationToken);
                            break;
                        case TelegramKeyboards.EnterIncome:
                            await HandleEnterIncome(update.Message.Chat.Id, cancellationToken);
                            break;
                        case TelegramKeyboards.EnterTransfer:
                            await HandleEnterTransfer(update.Message.Chat.Id, cancellationToken);
                            break;
                        case TelegramKeyboards.EnterSaving:
                            await HandleEnterSaving(update.Message.Chat.Id, cancellationToken);
                            break;
                        #endregion

                        #region ViewEntriesMenu
                        case TelegramKeyboards.ViewExpenses:
                            await OpenViewExpensesMenu(update.Message.Chat.Id, cancellationToken);
                            break;
                        #endregion

                        #region Statistics
                        case TelegramKeyboards.Summary:
                            await OpenSummaryMenu(update.Message.Chat.Id, cancellationToken);
                            break;
                        case TelegramKeyboards.ExpenseChart:
                            await OpenIncomeExpenseChartMenu(update.Message.Chat.Id, cancellationToken);
                            break;
                        #endregion

                        case TelegramKeyboards.Back:
                            await HandleBack(update.Message.Chat.Id, cancellationToken);
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

    public async Task OpenMainMenu(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.MainMenu;
        await _bot.SendMessage(userId, "Main Menu", replyMarkup: TelegramKeyboards.MainMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task OpenBalanceMenu(long userId, CancellationToken cancellationToken, bool inUah = true)
    {
        _state = BotState.BalanceMenu;

        var (_, dbOptions) = _databaseSelectorService.GetUserDatabase(userId);
        var balance = await _balanceService.GetBalance(dbOptions, inUah);

        var balanceMessage = $"🌟 *My Balances* 🌟\n\n{string.Join("\n\n", balance)}";
        await _bot.SendMessage(userId, balanceMessage, parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.BalanceMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task OpenAddEntryMenu(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.AddEntry;
        await _bot.SendMessage(userId, "Add Entry", replyMarkup: TelegramKeyboards.AddEntryMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task OpenViewEntriesMenu(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.ViewEntries;
        await _bot.SendMessage(userId, "View Entries", replyMarkup: TelegramKeyboards.ViewEntriesMenuKeyboard(), cancellationToken: cancellationToken);
    }

    public async Task HandleBack(long userId, CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case BotState.MainMenu:
            case BotState.BalanceMenu:
            case BotState.AddEntry:
            case BotState.ViewEntries:
            case BotState.ViewExpenses:
            case BotState.OpenStatistics:
                await OpenMainMenu(userId, cancellationToken);
                break;

            case BotState.Summary:
            case BotState.ExpenseChart:
                await OpenStatisticsMenu(userId, cancellationToken);
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

            #region AddIncome
            case BotState.EnterIncome: // entered amount of money
                await HandleEnterIncomeAmount(update, cancellationToken);
                break;
            case BotState.EnterIncomeCategory: // entered category
                await HandleEnterIncomeCategory(update, cancellationToken);
                break;
            case BotState.EnterIncomeAccount: // entered account
                await HandleEnterIncomeAccount(update, cancellationToken);
                break;
            case BotState.EnterIncomeDesc: // entered description
                await HandleEnterIncomeDesc(update, cancellationToken);
                break;
            #endregion

            #region AddTransfer
            case BotState.EnterTransfer: // entered amount of money
                await HandleEnterTransferAmount(update, cancellationToken);
                break;
            case BotState.EnterTransferAccountFrom: // entered category
                await HandleEnterTransferAccountFrom(update, cancellationToken);
                break;
            case BotState.EnterTransferAccountTo: // entered account
                await HandleEnterTransferAccountTo(update, cancellationToken);
                break;
            case BotState.EnterTransferDesc: // entered description
                await HandleEnterTransferDesc(update, cancellationToken);
                break;
            #endregion

            #region AddSaving
            case BotState.EnterSaving: // entered amount of money
                await HandleEnterSavingAmount(update, cancellationToken);
                break;
            case BotState.EnterSavingCategory: // entered category
                await HandleEnterSavingCategory(update, cancellationToken);
                break;
            case BotState.EnterSavingAccount: // entered account
                await HandleEnterSavingAccount(update, cancellationToken);
                break;
            case BotState.EnterSavingDesc: // entered description
                await HandleEnterSavingDesc(update, cancellationToken);
                break;
                #endregion

            case BotState.Summary:
                await HandleSummaryMonth(update, cancellationToken);
                break;
            case BotState.ExpenseChart:
                await HandleExpenseChartMonth(update, cancellationToken);
                break;
        }
    }
    #region AddExpenses
    public async Task HandleEnterExpenses(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpenses;
        const string message = "💵 Please enter the amount of your expense:";
        await _bot.SendMessage(userId, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesAmount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesCategory;

        // TODO: fix can be overwritten by another user
        if (!double.TryParse(update.Message.Text.Replace(",",""), CultureInfo.InvariantCulture, out _amount))
        {
            await OpenMainMenu(update.Message.Chat.Id, cancellationToken);
            return;
        }

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        var categories = await _expensesService.GetCategories(username, dbOptions);

        var categoriesStr = categories.Select(x => x.Name).ToList();

        const string message = "📂 Now, choose a category for this expense:";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: TelegramKeyboards.ExpensesCategoryKeyboard(categoriesStr), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesCategory(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesSubCategory;

        var category = update.Message!.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        var subCategories = await _expensesService.GetSubCategories(username, dbOptions, category);

        const string message = "📂 Now, choose a subcategory for this expense:";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: TelegramKeyboards.ExpensesCategoryKeyboard(subCategories), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesSubCategory(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesAccount;

        _subCategoryName = update.Message.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        _subCategoryId = await _expensesService.GetCategoryIdByName(username, dbOptions, _subCategoryName);

        var accounts = await _balanceService.GetAccounts(username, dbOptions);
        var accountsStr = accounts.Select(x => x.Name).ToList();

        const string message = "🏦 Which account should this expense be deducted from?";
        await _bot.SendMessage(update.Message.Chat.Id, "Enter account", replyMarkup: TelegramKeyboards.ExpensesAccountKeyboard(accountsStr), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesAccount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterExpensesDesc;

        _accountName = update.Message.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        _accountId = await _balanceService.GetAccountIdByName(username, dbOptions, _accountName);

        const string message = "📝 Optional: Add a short description for this expense.";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterExpensesDesc(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.BalanceMenu;

        _description = update.Message.Text;

        var req = new AddExpensesRequestModel()
        {
            Amount = _amount,
            AccountId = _accountId,
            CategoryId = _subCategoryId,
            Date = DateTime.UtcNow,
            Description = _description
        };

        var (_, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        await _expensesService.AddExpense(req, dbOptions);

        var currentBalance = _balanceService.GetCurrentBalance(_accountId, dbOptions);

        await ConfirmExpensesPage(update.Message.Chat.Id, currentBalance, cancellationToken);
    }

    private async Task ConfirmExpensesPage(long userId, double balance, CancellationToken cancellationToken)
    {
        _state = BotState.MainMenu;
        var message = $"💸 *Expense added* 💸\n\n💵 *Amount*: {_amount.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}\n\n📂 *Category*: {_subCategoryName}\n\n🏦 *Account*: {_accountName}\n\n📝 *Description*: {_description}\n\n📈 *Balance*: *{balance.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}*";
        await _bot.SendMessage(userId, message, parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.MainMenuKeyboard(), cancellationToken: cancellationToken);
    }
    #endregion
    
    #region AddIncome
    public async Task HandleEnterIncome(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.EnterIncome;
        const string message = "💵 Please enter the amount of your income:";
        await _bot.SendMessage(userId, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterIncomeAmount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterIncomeCategory;

        // TODO: fix can be overwritten by another user
        if (!double.TryParse(update.Message.Text.Replace(",", ""), CultureInfo.InvariantCulture, out _amount))
        {
            await OpenMainMenu(update.Message.Chat.Id, cancellationToken);
            return;
        }

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        var categories = await _incomeService.GetSubCategories(username, dbOptions);

        const string message = "📂 Now, choose a category for this income:";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: TelegramKeyboards.ExpensesCategoryKeyboard(categories), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterIncomeCategory(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterIncomeAccount;

        _subCategoryName = update.Message.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        _subCategoryId = await _incomeService.GetCategoryIdByName(username, dbOptions, _subCategoryName);

        var accounts = await _balanceService.GetAccounts(username, dbOptions);
        var accountsStr = accounts.Select(x => x.Name).ToList();

        const string message = "🏦 Which account should this income be deducted from?";
        await _bot.SendMessage(update.Message.Chat.Id, "Enter account", replyMarkup: TelegramKeyboards.ExpensesAccountKeyboard(accountsStr), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterIncomeAccount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterIncomeDesc;

        _accountName = update.Message.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        _accountId = await _balanceService.GetAccountIdByName(username, dbOptions, _accountName);

        const string message = "📝 Optional: Add a short description for this income.";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterIncomeDesc(Update update, CancellationToken cancellationToken)
    {
        _description = update.Message.Text;

        var req = new AddIncomeRequestModel()
        {
            Amount = _amount,
            AccountId = _accountId,
            CategoryId = _subCategoryId,
            Date = DateTime.UtcNow,
            Description = _description
        };

        var (_, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        await _incomeService.AddIncome(req, dbOptions);

        var currentBalance = _balanceService.GetCurrentBalance(_accountId, dbOptions);

        await ConfirmIncomePage(update.Message.Chat.Id, currentBalance, cancellationToken);
    }

    private async Task ConfirmIncomePage(long userId, double balance, CancellationToken cancellationToken)
    {
        _state = BotState.MainMenu;
        var message = $"💸 *Income added* 💸\n\n💵 *Amount*: {_amount.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}\n\n📂 *Category*: {_subCategoryName}\n\n🏦 *Account*: {_accountName}\n\n📝 *Description*: {_description}\n\n📈 *Balance*: *{balance.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}*";
        await _bot.SendMessage(userId, message, parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.MainMenuKeyboard(), cancellationToken: cancellationToken);
    }
    #endregion

    #region AddTransfer
    
    public async Task HandleEnterTransfer(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.EnterTransfer;
        const string message = "💵 Please enter the amount for transfer:";
        await _bot.SendMessage(userId, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterTransferAmount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterTransferAccountFrom;

        // TODO: fix can be overwritten by another user
        if (!double.TryParse(update.Message.Text.Replace(",", ""), CultureInfo.InvariantCulture, out _amount))
        {
            await OpenMainMenu(update.Message.Chat.Id, cancellationToken);
            return;
        }

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        
        var accounts = await _balanceService.GetAccounts(username, dbOptions);
        var accountsStr = accounts.Select(x => x.Name).ToList();

        const string message = "📂 Which account should this transfer be deducted from:";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: TelegramKeyboards.ExpensesAccountKeyboard(accountsStr), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterTransferAccountFrom(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterTransferAccountTo;

        _accountFromName = update.Message.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        _accountFromId = await _balanceService.GetAccountIdByName(username, dbOptions, _accountFromName);

        var accounts = await _balanceService.GetAccounts(username, dbOptions);
        var accountsStr = accounts.Select(x => x.Name).ToList();

        const string message = "🏦 Which account should this transfer be deducted to?";
        await _bot.SendMessage(update.Message.Chat.Id, "Enter account", replyMarkup: TelegramKeyboards.ExpensesAccountKeyboard(accountsStr), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterTransferAccountTo(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterTransferDesc;

        _accountToName = update.Message.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        _accountToId = await _balanceService.GetAccountIdByName(username, dbOptions, _accountToName);

        const string message = "📝 Optional: Add a short description for this transfer.";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterTransferDesc(Update update, CancellationToken cancellationToken)
    {
        _description = update.Message.Text;

        var req = new AddTransferRequestModel()
        {
            Amount = _amount,
            FromAccountId = _accountFromId,
            ToAccountId = _accountToId,
            Date = DateTime.UtcNow,
            Description = _description
        };

        var (_, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        await _transferService.AddTransfer(req, dbOptions);

        var currentBalanceFrom = _balanceService.GetCurrentBalance(_accountFromId, dbOptions);
        var currentBalanceTo = _balanceService.GetCurrentBalance(_accountToId, dbOptions);

        await ConfirmTransferPage(update.Message.Chat.Id, currentBalanceFrom, currentBalanceTo, cancellationToken);
    }

    private async Task ConfirmTransferPage(long userId, double balanceFrom, double balanceTo, CancellationToken cancellationToken)
    {
        _state = BotState.MainMenu;
        var message = $"💸 *Transfer added* 💸\n\n" +
                      $"💵 *Amount*: {_amount.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}\n\n" +
                      $"🏦 *Account from*: {_accountFromName} *{balanceFrom.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}*\n\n" +
                      $"🏦 *Account to*: {_accountToName} *{balanceTo.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}*\n\n" +
                      $"📝 *Description*: {_description}";
        await _bot.SendMessage(userId, message, parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.MainMenuKeyboard(), cancellationToken: cancellationToken);
    }
    #endregion

    #region AddSaving
    public async Task HandleEnterSaving(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.EnterSaving;
        const string message = "💵 Please enter the amount of your saving:";
        await _bot.SendMessage(userId, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterSavingAmount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterSavingCategory;

        // TODO: fix can be overwritten by another user
        if (!double.TryParse(update.Message.Text.Replace(",", ""), CultureInfo.InvariantCulture, out _amount))
        {
            await OpenMainMenu(update.Message.Chat.Id, cancellationToken);
            return;
        }

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        var categories = await _savingService.GetSubCategories(username, dbOptions);

        const string message = "📂 Now, choose a category for this saving:";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: TelegramKeyboards.ExpensesCategoryKeyboard(categories), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterSavingCategory(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterSavingAccount;

        _subCategoryName = update.Message.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        _subCategoryId = await _savingService.GetCategoryIdByName(username, dbOptions, _subCategoryName);

        var accounts = await _balanceService.GetAccounts(username, dbOptions);
        var accountsStr = accounts.Select(x => x.Name).ToList();

        const string message = "🏦 Which account should this saving be deducted from?";
        await _bot.SendMessage(update.Message.Chat.Id, "Enter account", replyMarkup: TelegramKeyboards.ExpensesAccountKeyboard(accountsStr), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterSavingAccount(Update update, CancellationToken cancellationToken)
    {
        _state = BotState.EnterSavingDesc;

        _accountName = update.Message.Text;

        var (username, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        _accountId = await _balanceService.GetAccountIdByName(username, dbOptions, _accountName);

        const string message = "📝 Optional: Add a short description for this saving.";
        await _bot.SendMessage(update.Message.Chat.Id, message, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
    }

    private async Task HandleEnterSavingDesc(Update update, CancellationToken cancellationToken)
    {
        _description = update.Message.Text;

        var req = new AddIncomeRequestModel()
        {
            Amount = _amount,
            AccountId = _accountId,
            CategoryId = _subCategoryId,
            Date = DateTime.UtcNow,
            Description = _description
        };

        var (_, dbOptions) = _databaseSelectorService.GetUserDatabase(update.Message.Chat.Id);
        await _savingService.AddSaving(req, dbOptions);

        var currentBalance = _balanceService.GetCurrentBalance(_accountId, dbOptions);

        await ConfirmSavingPage(update.Message.Chat.Id, currentBalance, cancellationToken);
    }

    private async Task ConfirmSavingPage(long userId, double balance, CancellationToken cancellationToken)
    {
        _state = BotState.MainMenu;
        var message = $"💸 *Saving added* 💸\n\n💵 *Amount*: {_amount.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}\n\n📂 *Category*: {_subCategoryName}\n\n🏦 *Account*: {_accountName}\n\n📝 *Description*: {_description}\n\n📈 *Balance*: *{balance.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}*";
        await _bot.SendMessage(userId, message, parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.MainMenuKeyboard(), cancellationToken: cancellationToken);
    }
    #endregion

    private async Task OpenViewExpensesMenu(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.ViewExpenses;

        var (_, dbOptions) = _databaseSelectorService.GetUserDatabase(userId);
        var expenses = await _expensesService.GetExpenses(dbOptions, 5);
        
        var message = "📝 *Recent Expenses* 📝\n\n";
        await _bot.SendMessage(userId, message, parseMode: ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        
        CultureInfo culture = new CultureInfo("uk-UA");
        foreach (var expense in expenses)
        {
            string temp = $"💸 *Amount*: `{expense.Amount.ToString("C", culture)}`\n\n📂 *Category*: `{expense.CategoryName}`\n\n🏦 *Account*: `{expense.AccountName}`\n\n📝 *Description*: `{expense.Description}`\n\n💰 *Balance*: `{expense.Balance.ToString("C", culture)}`";
            await _bot.SendMessage(userId, temp, parseMode: ParseMode.Markdown, replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
        }

        await _bot.SendMessage(userId, "LOAD MORE?!", parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.ViewExpensesMenuKeyboard(), cancellationToken: cancellationToken);
    }

    private async Task OpenStatisticsMenu(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.OpenStatistics;
        await _bot.SendMessage(userId, "Statistics", replyMarkup: TelegramKeyboards.StatisticsMenuKeyboard(), cancellationToken: cancellationToken);
    }

    #region Summary
    private async Task OpenSummaryMenu(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.Summary;

        var now = DateTime.UtcNow;

        await DisplaySummary(now, userId, cancellationToken);
    }
    
    private async Task HandleSummaryMonth(Update update, CancellationToken cancellationToken)
    {
        var text = update.Message.Text;
        var array = Array.ConvertAll(text.Split('/'), int.Parse);

        DateTime date = new DateTime(array[1], array[0], 1);

        await DisplaySummary(date, update.Message.Chat.Id, cancellationToken);
    }

    private async Task DisplaySummary(DateTime date, long userId, CancellationToken cancellationToken)
    {
        _summaryYear = date.Year;
        _summaryMonth = date.Month;

        List<string> prevMonth = new();
        for (int i = 0; i < 3; i++)
        {
            date = date.AddMonths(-1);
            prevMonth.Add($"{date.Month}/{date.Year}");
        }

        var (_, dbOptions) = _databaseSelectorService.GetUserDatabase(userId);
        var (income, expenses, savings, unspecified) = await _transactionsService.GetSummary(_summaryYear, _summaryMonth, dbOptions);

        CultureInfo culture = new CultureInfo("uk-UA");

        await _bot.SendMessage(userId, $"📊 *Summary for {_summaryMonth}/{_summaryYear}* 📊\n\n" +
                                       $"💵 *Income*: `{income.ToString("C", culture)}`\n\n" +
                                       $"💸 *Expenses*: `{expenses.ToString("C", culture)}`\n\n" +
                                       $"💰 *Savings*: `{savings.ToString("C", culture)}`\n\n" +
                                       $"💸 *Unspecified*: `{unspecified.ToString("C", culture)}`",
            parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.MonthSummaryKeyboard(prevMonth), cancellationToken: cancellationToken);
    }
    #endregion

    #region IncomeExpenseChart

    private async Task OpenIncomeExpenseChartMenu(long userId, CancellationToken cancellationToken)
    {
        _state = BotState.ExpenseChart;

        var now = DateTime.UtcNow;

        await DisplayExpenseChart(now, userId, cancellationToken);
    }

    private async Task HandleExpenseChartMonth(Update update, CancellationToken cancellationToken)
    {
        var text = update.Message.Text;
        var array = Array.ConvertAll(text.Split('/'), int.Parse);

        DateTime date = new DateTime(array[1], array[0], 1);

        await DisplayExpenseChart(date, update.Message.Chat.Id, cancellationToken);
    }

    private async Task DisplayExpenseChart(DateTime date, long userId, CancellationToken cancellationToken)
    {
        _summaryYear = date.Year;
        _summaryMonth = date.Month;

        List<string> prevMonth = new();
        for (int i = 0; i < 3; i++)
        {
            date = date.AddMonths(-1);
            prevMonth.Add($"{date.Month}/{date.Year}");
        }

        var (_, dbOptions) = _databaseSelectorService.GetUserDatabase(userId);
        var (chart, total) = await _transactionsService.GetExpenseChart(_summaryYear, _summaryMonth, dbOptions);

        string msg = string.Join("\n", chart.Select(x => x.ToString()));

        await _bot.SendMessage(userId, $"📊 *Statistics for {_summaryMonth}/{_summaryYear}* 📊\n💲 *Money spend {total.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))}* 💲\n\n{msg}",
            parseMode: ParseMode.Markdown, replyMarkup: TelegramKeyboards.MonthSummaryKeyboard(prevMonth), cancellationToken: cancellationToken);
    }

    #endregion

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

        var message = await _bot.SendMessage(update.Message.Chat.Id, text: "testing", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
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
    public const string ViewEntries = "📋 View Entries";
    public const string Shortcuts = "⚙️ Shortcuts";
    public const string Statistics = "📊 Statistics";
    #endregion

    public const string Back = "🔙 Back";

    #region Balance Menu
    public const string ShowInUsd = "🔄 Show in USD";
    public const string NetWorth = "Net worth";
    // Back
    #endregion

    #region AddEntry Menu
    public const string EnterExpenses = "➖ Add Expense";
    public const string EnterIncome = "➕ Add Income";
    public const string EnterTransfer = "🔄 Add Transfer";
    public const string EnterSaving = "💰 Add Saving";
    // Back
    #endregion

    #region ViewEntries Menu
    public const string ViewExpenses = "📝 Recent Expenses";
    public const string ViewIncome = "💵 Recent Income";
    // Back
    #endregion

    #region Statistics
    public const string Summary = "📊 Summary";
    public const string ExpenseChart = "💹 Expense Chart";
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
                    new KeyboardButton(ViewEntries),
                ],
                [
                    new KeyboardButton(Statistics),
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
                    new KeyboardButton(EnterTransfer),
                    new KeyboardButton(EnterSaving),
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

    public static ReplyKeyboardMarkup StatisticsMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(
            [
                [
                    new KeyboardButton(Summary),
                    new KeyboardButton(ExpenseChart),
                ],
                [
                    new KeyboardButton(Back),
                ],
            ]
        );
    }

    public static ReplyKeyboardMarkup MonthSummaryKeyboard(List<string> prevMonth)
    {
        // 03/2025
        var result = new ReplyKeyboardMarkup();

        result.AddButtons(
            prevMonth.Select(x => new KeyboardButton(x)).ToArray()
        );

        result.AddNewRow(
            new KeyboardButton(Back)
        );

        return result;
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