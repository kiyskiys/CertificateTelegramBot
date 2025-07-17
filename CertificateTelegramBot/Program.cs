using Telegram.Bot;
using System.Collections.Concurrent;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using CertificateTelegramBot_Main.Data;
using CertificateTelegramBot_Enums;
using Microsoft.Extensions.Configuration;

namespace CertificateTelegramBot_Main
{
    public partial class CertificateBot
    {
        internal static string BotToken; //токен бота
        internal static List<long> PreAdmins = []; //поля для админов до регистрации
        internal static readonly ApplicationDbContext db = new();
        internal static ITelegramBotClient _botClient;
        //словарь для хранения состояний пользователя <тгайди, состояние>
        internal static readonly ConcurrentDictionary<long, UserState> userStates = new();

        public static async Task Main()
        {
            var builder = new ConfigurationBuilder() //настройка подключения файла appsettings.json
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration configuration = builder.Build();

            BotToken = configuration.GetSection("BotConfiguration:BotToken").Value; //получаем из файла токен
            PreAdmins = configuration.GetSection("BotConfiguration:PreAdminIds").Get<List<long>>(); //получаем из файла ID админов до реги

            // проверка на случай, если что-то не так с файлом appsettings
            if (string.IsNullOrEmpty(BotToken) || PreAdmins == null || !PreAdmins.Any())
            {
                Console.WriteLine("Ошибка: BotToken или SuperAdminIds не настроены в appsettings.json. Нажмите любую клавишу для выхода.");
                Console.ReadKey();
                return;
            }


            await db.Database.EnsureCreatedAsync(); //проверка на существование БД
            _botClient = new TelegramBotClient(BotToken); //токен тг бота
            using var cts = new CancellationTokenSource();

            _botClient.StartReceiving(
                updateHandler: HandleUpdate,
                errorHandler: OnError,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMe();
            Console.WriteLine($"Бот @{me.Username} запущен...");
            await Task.Delay(-1, cts.Token);
        }

        //обработка всего, что происходит в тг, некий "диспетчер"
        internal static async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken token)
        {
            try
            {
                Task handler = update.Type switch
                {
                    UpdateType.Message => HandleMessageSent(client, update.Message, token),
                    UpdateType.CallbackQuery => HandleQuery(client, update.CallbackQuery, token),
                    _ => Task.CompletedTask
                };
                await handler;
            }
            catch (Exception ex)
            {
                await OnError(client, ex, token);
            }
        }

        //обработка ошибок
        internal static Task OnError(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMessage);
            File.AppendAllText("error_log.txt", $"{DateTime.UtcNow}: {errorMessage}\n\n");
            return Task.CompletedTask;
        }
    }
}