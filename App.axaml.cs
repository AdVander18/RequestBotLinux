using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RequestBotLinux.Views;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Text.RegularExpressions;

namespace RequestBotLinux
{
    public partial class App : Application
    {
        public static DataBase Database { get; private set; }
        public static TelegramBotClient BotClient => _botClient;
        private static TelegramBotClient _botClient;
        private string taskHelp = "/help";
        private string taskDone = "Заявка принята в работу👍";


        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            var dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "bot.db");
            Database = new DataBase(dbPath);
            Console.WriteLine($"Database path: {Database.DbPath}");
        }
        public static event Action<string> BotStatusChanged;

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            try
            {
                _botClient = new TelegramBotClient("7299350943:AAGnUiWRM_pzS_emlIF_EodBotbxac4F5QI");
                var receiverOptions = new ReceiverOptions();

                _botClient.StartReceiving(
                    updateHandler: async (client, update, ct) => await HandleUpdateAsync(client, update, ct),
                    errorHandler: async (client, exception, ct) => await HandleErrorAsync(client, exception, ct),
                    receiverOptions: receiverOptions
                );

                BotStatusChanged?.Invoke("[Система] Бот успешно запущен!");
            }
            catch (Exception ex)
            {
                BotStatusChanged?.Invoke($"[Ошибка] Не удалось подключиться к боту: {ex.Message}");
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            try
            {
                if (update.Message is { } message)
                {
                    var user = message.From;
                    long chatId = message.Chat.Id;
                    string messageText = message.Text ?? string.Empty;
                    // Обработка команды /start
                    if (messageText.Contains("/start"))
                    {
                        await botClient.SendTextMessageAsync(chatId,
                            "Для создания заявки используйте:\n" +
                            "/help [Фамилия] [Кабинет] [Описание] [Срок]\n" +
                            "Пример:\n" +
                            "/help Иванов 404 Не работает принтер 3 дня");
                    }

                    // Обработка команды /help
                    if (messageText.ToLower().StartsWith(taskHelp.ToLower()))
                    {
                        var pattern = @"^/help\s+([^\d]+)\s+(\d+)\s+(.*?)(?:\s+(\d+)\s+(день|дня|дней|месяц|месяца|месяцев))?$";
                        var match = Regex.Match(messageText, pattern, RegexOptions.IgnoreCase);

                        if (match.Success)
                        {
                            string lastName = match.Groups[1].Value.Trim();
                            string cabinetNumber = match.Groups[2].Value.Trim();
                            string description = match.Groups[3].Value.Trim();
                            string urgencyValue = match.Groups[4].Success ? match.Groups[4].Value : null;
                            string urgencyUnit = match.Groups[5].Success ? match.Groups[5].Value.ToLower() : null;

                            DateTime deadline = CalculateDeadline(urgencyValue, urgencyUnit);

                            if (!Database.CheckCabinetExists(cabinetNumber))
                            {
                                await botClient.SendTextMessageAsync(chatId, "⚠️ Кабинет не найден!");
                                return;
                            }

                            await Database.AddTaskMessageAsync(
                                new RequestBotLinux.Models.User
                                {
                                    Username = user.Username,
                                    FirstName = user.FirstName,
                                    LastName = "" // Добавьте значение по умолчанию, если требуется
                                },
                                chatId,
                                lastName,
                                cabinetNumber,
                                description,
                                deadline
                            );

                            await botClient.SendTextMessageAsync(chatId, taskDone);
                            return;
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId,
                                "❌ Неверный формат!\nПример:\n/help Иванов 404 Описание проблемы 3 дня");
                            return;
                        }
                    }

                    // Сохранение обычного сообщения
                    await Database.AddMessageAsync(
                        user.Username ?? user.Id.ToString(),
                        chatId,
                        messageText
                    );

                    // Обновление UI
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                            desktop.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.LoadMessages();
                            mainWindow.LoadUsers();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                Dispatcher.UIThread.Post(() =>
                {
                    MessageBoxManager.GetMessageBoxStandard(
                        "Ошибка",
                        $"Ошибка обработки: {ex.Message}",
                        ButtonEnum.Ok,
                        Icon.Error
                    ).ShowAsync();
                });
            }
        }
        private DateTime CalculateDeadline(string urgencyValue, string urgencyUnit)
        {
            if (!int.TryParse(urgencyValue, out int value)) value = 1;

            return urgencyUnit switch
            {
                "день" or "дня" or "дней" => DateTime.Now.AddDays(value),
                "месяц" or "месяца" or "месяцев" => DateTime.Now.AddMonths(value),
                _ => DateTime.Now.AddMonths(1)
            };
        }

        private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
        {
            new Window { Content = new TextBlock { Text = $"Ошибка бота: {exception.Message}" } }.Show();
            await Task.CompletedTask;
        }
        public static async Task<bool> CheckBotConnectionAsync()
        {
            if (_botClient == null) return false;

            try
            {
                var bot = await _botClient.GetMeAsync();
                var me = await _botClient.GetMeAsync();
                Console.WriteLine($"Bot connected: {me.Username}");
                return bot.IsBot;
            }
            catch
            {
                return false;
            }

        }

    }
}