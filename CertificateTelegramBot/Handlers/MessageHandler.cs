using Telegram.Bot;
using Telegram.Bot.Types;
using CertificateTelegramBot_Enums;
using CertificateTelegramBot_Main.Data;
using CertificateTelegramBot_Callbacks;
using CertificateTelegramBot_Main.Services;
using Telegram.Bot.Types.ReplyMarkups;
using DbUser = CertificateTelegramBot_Main.Data.User;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace CertificateTelegramBot_Main
{
    //логика для текстовых сообщений
    public partial class CertificateBot
    {
        //обработчик для входящих сообщений, команды и т.п.
        internal static async Task HandleMessageSent(ITelegramBotClient client, Message message, CancellationToken token)
        {
            if (message.Text is not { } messageText) return;

            var chatId = message.Chat.Id;
            var userId = message.From.Id;

            //текущее состояние пользователя
            if (userStates.TryGetValue(chatId, out var currentState))
            {
                await StatementManager(client, message, currentState, token);
                return;
            }

            var user = await db.Users.FindAsync(userId);
            var command = messageText.Split(' ')[0]; //берём первое слово сообщения для корректной работы команд

            switch (command)
            {
                case "/start":
                    if (user == null || (user.Role == UserRole.Student && user.GroupName == null)) //если пользователь не прошёл регистрацию (его нет в БД), то начинаем её
                    {
                        //переводим состояние в регистрацию
                        userStates[chatId] = UserState.AwaitingRegistrationFullName;
                        await client.SendMessage(chatId, "Здравствуйте! Похоже, вы ещё не зарегистрированы.\nПожалуйста, введите ваше ПОЛНОЕ Фамилию Имя Отчество через пробел.", cancellationToken: token);
                    }
                    else if (user.IsAuthorised) //если уже авторизован
                    {
                        //приветствуем пользователя в случае, если он авторизован
                        await client.SendMessage(chatId, $"С возвращением, {user.Name}!", cancellationToken: token);
                        await ViewService.SendOrEditMainMenu(client, chatId, null, user, token);
                    }
                    else //если пользователь зарегался, но ещё не авторизован (админ ещё не рассмотрел его)
                    {
                        await client.SendMessage(chatId, "Вы зарегистрированы, но ваша заявка ещё не принята.", cancellationToken: token);
                    }
                    break;
                //обработка сообщений с командами админа отправляют на соответствующий метод
                case "/approve":
                case "/add_admin":
                case "/remove_admin":
                    await HandleAdminCommands(client, message, user, command, token);
                    break;

                default: //стандартынй ответ заглушка если нет какой-то команды
                    await client.SendMessage(chatId, "Неизвестная команда. Введите /start", cancellationToken: token);
                    break;
            }
        }

        //управление состояниями пользователя
        internal static async Task StatementManager(ITelegramBotClient client, Message message, UserState state, CancellationToken token)
        {
            var chatId = message.Chat.Id;
            var userId = message.From.Id;
            var text = message.Text;

            switch (state) //текущее состояние
            {
                case UserState.AwaitingRegistrationFullName: //состояние ожидания ФИО пользователя
                    var nameParts = text.Split(' ');
                    const int maxLength = 30;
                    //проверка на наличие ФИ
                    if (nameParts.Length < 2 || nameParts.Any(p => p.Length > maxLength)) //единственная проблема, не рассчитано на нестандартные ФИО, где больше 3 слов.
                    {
                        await client.SendMessage(chatId, "Неверный формат. Пожалуйста, введите Ваше Ф.И.О. (если отчества нет, ничего не пишите)", cancellationToken: token);
                        return;
                    }

                    var user = await db.Users.FindAsync(userId);
                    if (user == null) // если пользователя нет, создаём нового
                    {
                        user = new DbUser { TelegramId = userId };
                        db.Users.Add(user);
                    }

                    user.Surname = nameParts[0];
                    user.Name = nameParts[1];
                    user.Patronymic = nameParts.Length > 2 ? nameParts[2] : "";

                    if (PreAdmins.Contains(user.TelegramId))
                    {
                        user.Role = UserRole.Admin;
                    }
                    if (user.Role == UserRole.Admin) //проверка на админа для разветвления регистрации
                    {
                        userStates[chatId] = UserState.AwaitingAdminStudentStatus;
                        //проверка на то, является ли админ студентом для упрощения регистрации
                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                        new [] { InlineKeyboardButton.WithCallbackData("Да, я студент", Callbacks.AdminIsStudent),
                                 InlineKeyboardButton.WithCallbackData("Нет, я сотрудник", Callbacks.AdminIsNotStudent) }
                        });
                        await client.SendMessage(chatId, "Отлично! Мы определили, что вы администратор. \nУточните, пожалуйста, вы также являетесь студентом?", replyMarkup: inlineKeyboard, cancellationToken: token);
                    }
                    else
                    {
                        //продолжение регистрации, если не админ
                        userStates[chatId] = UserState.AwaitingRegistrationGroup;
                        await client.SendMessage(chatId, "Отлично! Теперь введите полное название своей группы (например: 24-КБ-ПР3).", cancellationToken: token);
                    }
                    break;

                //состояние ожидания ввода группы 
                case UserState.AwaitingRegistrationGroup:
                    var userToUpdate = await db.Users.FindAsync(userId);
                    if (userToUpdate != null)
                    {
                        userToUpdate.GroupName = text;
                        userStates[chatId] = UserState.AwaitingRegistrationPhoneNumber;
                        await client.SendMessage(chatId, "Теперь введите ваш номер телефона для связи.", cancellationToken: token);
                    }
                    break;

                //состояние ожидания ввода номера телефона
                case UserState.AwaitingRegistrationPhoneNumber:
                    var finalUserToUpdate = await db.Users.FindAsync(userId);
                    if (finalUserToUpdate != null)
                    {
                        finalUserToUpdate.PhoneNumber = text;
                        if (finalUserToUpdate.Role == UserRole.Admin) //если он является админом, то сразу авторизуем
                        {
                            finalUserToUpdate.IsAuthorised = true;
                        }
                        await db.SaveChangesAsync(token);
                        userStates.TryRemove(chatId, out _);

                        var messageToSend = finalUserToUpdate.IsAuthorised 
                            ? "Вы были распознаны как администратор-студент и авторизованы автоматически! Нажмите /start."
                            : "Спасибо! Вы успешно зарегистрированы. Ваша заявка отправлена на рассмотрение.";
                        await client.SendMessage(chatId, messageToSend, cancellationToken: token);
                    }
                    break;

                //состояние ожидания места требования для обычной справки
                //есть только для обычной, ибо в остальных направление destinaton уже известно
                case UserState.AwaitingCertificateDestination:
                    var destination = text;

                    var userForCertificate = await db.Users.FindAsync(userId);
                    if (userForCertificate == null)
                    {
                        await client.SendMessage(chatId, "Произошла ошибка, пожалуйста, начните заново: /start.", cancellationToken: token);
                        userStates.TryRemove(chatId, out _);
                        return;
                    }
                    //создаем объект справки 
                    var newSimpleCertificate = new Certificate
                    {
                        UserId = userId,
                        User = userForCertificate,
                        CertificateType = "Обычная",
                        Destination = destination,
                        CreatedAt = DateTime.UtcNow,
                        Status = CertificateStatus.Pending //по умолчанию стоит pending ("висит")
                    };

                    db.Certificates.Add(newSimpleCertificate);
                    await db.SaveChangesAsync();
                    userStates.TryRemove(chatId, out _);

                    await client.SendMessage(chatId, $"Справка по месту требования в '{destination}' заказана. Номер заявки: {newSimpleCertificate.CertificateId}", cancellationToken: token);
                    await Task.Delay(1500, token); //искусственное замедление, чтобы не было всё резко
                    await ViewService.SendOrEditMainMenu(client, chatId, null, userForCertificate, token);
                    break;
            }
        }

        //обработчик админских команд, /aprove, /add_admin, /remove_admin
        internal static async Task HandleAdminCommands(ITelegramBotClient client, Message message, DbUser user, string command, CancellationToken token)
        {
            if (user?.Role != UserRole.Admin || !user.IsAuthorised) //проверка на наличие прав и авторизации
            {
                await client.SendMessage(message.Chat.Id, "Эта команда вам недоступна.", cancellationToken: token);
                return;
            }
            //разделяем на два слова, ибо команды работают с айди
            var parts = message.Text.Split(' ');
            if (parts.Length < 2 || !long.TryParse(parts[1], out var targetId))
            {
                await client.SendMessage(message.Chat.Id, "Неверный формат команды. Укажите ID пользователя после команды.", cancellationToken: token);
                return;
            }

            var targetUser = await db.Users.FindAsync(targetId); //ищем пользователя

            switch (command)
            {
                case "/approve": //принятие зарегистрированного пользователя (/approve [ID])
                    if (targetUser == null) { await client.SendMessage(message.Chat.Id, "Пользователь не найден."); return; }
                    targetUser.IsAuthorised = true;
                    await db.SaveChangesAsync(token);
                    try //обработчик ошибки, чтобы в случае блокировки бота человеком - он не уходил в грустинку:(
                    {
                        await client.SendMessage(
                            chatId: targetUser.TelegramId,
                            text: "Ваша заявка на регистрацию была одобрена!\n\nТеперь вам доступны все функции бота. Нажмите /start, чтобы открыть главное меню.",
                            cancellationToken: token
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Не удалось отправить уведомление об одобрении пользователю {targetUser.TelegramId}: {ex.Message}");
                    }
                    await client.SendMessage(message.Chat.Id, $"Пользователь {targetUser.Name} успешно авторизован.");
                    break;

                case "/add_admin": //выдача прав админа (/add_admin [ID]
                    if (targetUser == null)
                    {
                        db.Users.Add(new DbUser { TelegramId = targetId, Role = UserRole.Admin }); //добавляем в бд и даём ему роль, если пользователь новый
                        await client.SendMessage(message.Chat.Id, $"Пользователю {targetId} присвоены права администратора. Он должен запустить бота для регистрации.");
                    }
                    else //иначе просто присуждаем ему эту роль
                    {
                        targetUser.Role = UserRole.Admin;
                        await client.SendMessage(message.Chat.Id, $"Пользователь {targetUser.Name} назначен администратором.");
                    }
                    await db.SaveChangesAsync(token);
                    break;

                case "/remove_admin": //удаление админских прав (/remove_admin [ID])
                    if (targetUser == null || targetUser.Role != UserRole.Admin) { await client.SendMessage(message.Chat.Id, "Пользователь не является администратором."); return; }
                    targetUser.Role = UserRole.Student;
                    await db.SaveChangesAsync(token);
                    await client.SendMessage(message.Chat.Id, $"Пользователь {targetUser.Name} больше не администратор.");
                    break;
            }
        }

    }
}
