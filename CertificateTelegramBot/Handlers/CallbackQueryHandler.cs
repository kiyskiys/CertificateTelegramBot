using CertificateTelegramBot_Main.Data;
using CertificateTelegramBot_Enums;
using CertificateTelegramBot_Callbacks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using CertificateTelegramBot_Main.Services;
using DbUser = CertificateTelegramBot_Main.Data.User;

namespace CertificateTelegramBot_Main
{
    //содержит в себе логику для нажатий на кнопки
    public partial class CertificateBot
    {
        //общий обработчик для inline-кнопок
        internal static async Task HandleQuery(ITelegramBotClient client, CallbackQuery callbackQuery, CancellationToken token)
        {
            if (callbackQuery.Data is not { } callbackData) return;
            if (callbackQuery.Message is not { } message) return;

            var userId = callbackQuery.From.Id;
            var user = await db.Users.FindAsync(userId);
            if (user == null) return;

            //некий костыль, мб если будет не лень, то исправлю другие callback'и с меню, но пока работает можно и не трогать
            //ловит callback на меню и возвращает его
            if (callbackData == Callbacks.BackToMainMenu)
            {
                await ViewService. SendOrEditMainMenu(client, message.Chat.Id, message.MessageId, user, token);
                return;
            }

            await client.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: token);
            //обработка коллбеков админа
            if (callbackData.StartsWith("admin_"))
            {
                await HandleAdminCallbacks(client, callbackQuery, user, token);
            }
            //пока затычка
            else if (callbackData.StartsWith("menu_") || callbackData.StartsWith("order_") || callbackData.StartsWith("help_"))
            {
                await HandleMenuCallbacks(client, callbackQuery, user, token);
            }
        }

        internal static async Task HandleMenuCallbacks(ITelegramBotClient client, CallbackQuery callbackQuery, DbUser user, CancellationToken token)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var callbackData = callbackQuery.Data;

            //обработка разных случаев коллбеков
            switch (callbackData)
            {
                //заказ справки (1 ур)
                case Callbacks.OrderCertificate:
                    var orderMenuText = "Выберите тип справки:";
                    var orderMenuKeyboard = new InlineKeyboardMarkup(new[] //кнопки для выбора вида справки
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("Обычная (по месту требования)", Callbacks.OrderSimple) },
                        [InlineKeyboardButton.WithCallbackData("Специальная", Callbacks.OrderSpecial)],
                        [InlineKeyboardButton.WithCallbackData("Назад в главное меню", Callbacks.BackToMainMenuFromOrder)]
                    });
                    await client.EditMessageText(chatId, messageId, orderMenuText, replyMarkup: orderMenuKeyboard, cancellationToken: token);
                    break;

                //кнопка мои сертифакты
                case Callbacks.MyCertificates:
                    await ViewService.SendMyCertificatesPage(client, chatId, messageId, user.TelegramId, 0, token, db);
                    break;

                case var _ when callbackData.StartsWith(Callbacks.MyCertificatesPage):
                    var page = int.Parse(callbackData.Replace(Callbacks.MyCertificatesPage, ""));
                    await ViewService.SendMyCertificatesPage(client, chatId, messageId, user.TelegramId, page, token, db);
                    break;

                case Callbacks.Help:
                    var helpMenuText = "Раздел помощи:\nВыберите, что вас интересует.";
                    var helpMenuKeyboard = new InlineKeyboardMarkup(new[]
                    {
                    new[] { InlineKeyboardButton.WithCallbackData("Какие виды справок бывают?", Callbacks.HelpCertificateTypes) },
                    [ InlineKeyboardButton.WithCallbackData("Сроки изготовления", Callbacks.HelpDeadlines) ],
                    [ InlineKeyboardButton.WithCallbackData("Назад в главное меню", Callbacks.BackToMainMenuFromHelp) ]
                    });
                    await client.EditMessageText(chatId, messageId, helpMenuText, replyMarkup: helpMenuKeyboard, cancellationToken: token);
                    break;
                // обработка ответ на виды справок
                case Callbacks.HelpCertificateTypes:
                    var certificateTypesText = "*Какие бывают виды справок?*\n\n" +
                               "1. *Обычная (по месту требования)* — универсальная справка, которую вы можете предоставить в любую организацию (например, в банк, на работу).\n\n" +
                               "2. *Специальные* — справки для конкретных государственных органов. Их не нужно заполнять дополнительно, все данные берутся из системы:\n\n\n" +
                               "  - *ПФР* (справка из ПФР, ныне Социального фонда России (СФР), нужна для подтверждения пенсионных прав, стажа, размера пенсии и других выплат)\n\n" +
                               "  - *СФР* (может потребоваться для различных целей, включая подтверждение стажа, размера пенсии, социальных выплат, а также для предоставления в различные организации, такие как банки, военкоматы и другие)\n\n" +
                               "  - *ЕФС* (справка ЕФС-1, или единая форма сведений, подается в Социальный фонд России (СФР) работодателями для отчетности по работникам, включая информацию о трудовой деятельности, страховом стаже и начисленных взносах)\n\n" +
                               "  - *В военкомат* (Справка в военкомат может понадобиться для различных целей, таких как подтверждение обучения для получения отсрочки от призыва, подтверждение трудоустройства для оформления брони или отсрочки, а также для получения документов воинского учета, например, военного билета или справки взамен военного билета)\n\n" +
                               "  - *Справка-вызов на сессию* (Справка-вызов на сессию - это официальный документ, который выдается студентам заочной и очно-заочной форм обучения, подтверждающий необходимость их присутствия в учебном заведении для прохождения сессии. Этот документ позволяет студентам получить учебный отпуск у работодателя, сохраняя при этом рабочее место и средний заработок)";

                    var backToHelpMenuKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Назад в раздел помощи", Callbacks.Help)
                    });

                    await client.EditMessageText(
                        chatId: chatId,
                        messageId: messageId,
                        text: certificateTypesText,
                        replyMarkup: backToHelpMenuKeyboard,
                        parseMode: ParseMode.Markdown, // форматирование для жирного текста (* *)
                        cancellationToken: token);
                    break;

                // обработка ответа на сроки изготовления
                case Callbacks.HelpDeadlines:
                    var deadlinesText = "*Сроки изготовления справок*\n\n" +
                        "Стандартный срок подготовки любой справки составляет *3 рабочих дня*.\n\n" +
                        "Как только справка будет готова, вы получите уведомление в этом чате. Статус готовности также можно отслеживать в разделе «Мои справки».";

                    var backToHelpKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Назад в раздел помощи", Callbacks.Help)
                    });

                    await client.EditMessageText(
                        chatId: chatId,
                        messageId: messageId,
                        text: deadlinesText,
                        replyMarkup: backToHelpKeyboard,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: token);
                    break;

                // обработка заказанной обычной справки
                case Callbacks.OrderSimple:
                    // запрашиваем место, куда нужна справка
                    userStates[chatId] = UserState.AwaitingCertificateDestination;
                    await client.EditMessageText(chatId, messageId, "Введите место требования для справки (например, 'в банк', 'на работу').", cancellationToken: token);
                    break;

                //обработка выбора спец. справки
                case Callbacks.OrderSpecial:
                    var specialMenuText = "Выберите вид специальной справки:";
                    var specialMenuKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("В ПФР", Callbacks.OrderPfr), InlineKeyboardButton.WithCallbackData("В СФР", Callbacks.OrderSfr) },
                        [InlineKeyboardButton.WithCallbackData("В ЕФС", Callbacks.OrderEfs), InlineKeyboardButton.WithCallbackData("В военкомат", Callbacks.OrderVoenkomat)],
                        [InlineKeyboardButton.WithCallbackData("Справка-вызов", Callbacks.OrderCall)],
                        [InlineKeyboardButton.WithCallbackData("Назад", Callbacks.BackToOrderMenu)]
                    });
                    await client.EditMessageText(chatId, messageId, specialMenuText, replyMarkup: specialMenuKeyboard, cancellationToken: token);
                    break;

                // обработка заказа спец. справки
                // они все берут данные только из бд, поэтому можно для них единый "выход"
                case Callbacks.OrderPfr:
                case Callbacks.OrderSfr:
                case Callbacks.OrderEfs:
                case Callbacks.OrderVoenkomat:
                case Callbacks.OrderCall:
                    // получаем текст кнопки для типа справки
                    var buttonText = (callbackQuery.Message.ReplyMarkup)
                        .InlineKeyboard.SelectMany(row => row)
                        .FirstOrDefault(button => button.CallbackData == callbackData)?.Text ?? "Специальная справка";

                    var newCert = new Certificate
                    {
                        UserId = user.TelegramId,
                        CertificateType = buttonText, // "В ПФР", "В СФР" и т.д.
                        Destination = buttonText,     // место назначения такое же, как и тип
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Certificates.Add(newCert);
                    await db.SaveChangesAsync(token);

                    await client.EditMessageText(chatId, messageId, $"Справка '{buttonText}' заказана! Номер заявки: {newCert.CertificateId}", cancellationToken: token);
                    await Task.Delay(2000, token);
                    await ViewService.SendOrEditMainMenu(client, chatId, null, user, token);
                    break;

                case Callbacks.BackToOrderMenu:
                    callbackQuery.Data = Callbacks.OrderCertificate;
                    await HandleMenuCallbacks(client, callbackQuery, user, token);
                    break;

                case Callbacks.BackToMainMenu:
                case Callbacks.BackToMainMenuFromOrder:
                case Callbacks.BackToMainMenuFromHelp:
                    await ViewService.SendOrEditMainMenu(client, chatId, messageId, user, token);
                    break;
            }
        }

        internal static async Task HandleAdminCallbacks(ITelegramBotClient client, CallbackQuery callbackQuery, DbUser user, CancellationToken token)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            var callbackData = callbackQuery.Data;

            if (user.Role != UserRole.Admin) return;

            switch (callbackData)
            {
                case Callbacks.ViewPendingCertificates:
                    await ViewService.SendCertificatesPage(client, callbackQuery.Message, 0, token, db);
                    break;

                case var _ when callbackData.StartsWith(Callbacks.PageCertificates):
                    // обрабатываем нажатие на кнопки пагинации
                    var page = int.Parse(callbackData.Split('_').Last());
                    await ViewService.SendCertificatesPage(client, callbackQuery.Message, page, token, db);
                    break;

                case var _ when callbackData.StartsWith(Callbacks.ViewCertificateDetails):
                    // обрабатываем нажатие на конкретную заявку
                    var certId = int.Parse(callbackData.Split('_').Last());
                    await ViewService.ShowCertificateDetails(client, callbackQuery.Message, certId, token, db);
                    break;

                case var _ when callbackData.StartsWith(Callbacks.SearchCertificatesPage):
                    var partsForSearch = callbackData.Replace(Callbacks.SearchCertificatesPage, "").Split('_');
                    var pageForSearch = int.Parse(partsForSearch[0]);
                    var searchTerm = partsForSearch[1];

                    await ViewService.SendSearchResultsPage(client, chatId, messageId, searchTerm, pageForSearch, token, db);
                    break;

                case var _ when callbackData.StartsWith(Callbacks.ChangeCertificateStatus):
                    // обрабатываем изменение статуса
                    var parts = callbackData.Split('_');
                    var statusCertId = int.Parse(parts[^2]);
                    var newStatus = Enum.Parse<CertificateStatus>(parts.Last());

                    var certToUpdate = await db.Certificates.FindAsync(statusCertId);
                    if (certToUpdate != null)
                    {
                        certToUpdate.Status = newStatus;
                        await db.SaveChangesAsync(token);

                        if (newStatus == CertificateStatus.Ready)
                        {
                            try
                            {
                                await client.SendMessage(
                                    chatId: certToUpdate.UserId, // отправляем студенту по его ID
                                    text: $"Ваша справка '{certToUpdate.CertificateType}' готова к выдаче!",
                                    cancellationToken: token
                                );
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Не удалось отправить уведомление пользователю {certToUpdate.UserId}: {ex.Message}");
                            }
                        }

                        // обновляем сообщение с деталями, чтобы показать новый статус и новый набор кнопок
                        await ViewService.ShowCertificateDetails(client, callbackQuery.Message, statusCertId, token, db);
                    }
                    break;

                case Callbacks.BackToAdminMenu:
                    callbackQuery.Data = Callbacks.ToggleAdminMode;
                    await HandleAdminCallbacks(client, callbackQuery, user, token);
                    break;

                //переключение прав с админа на студента и наоборот
                case Callbacks.ToggleAdminMode:
                    var adminMenuText = "Панель администратора:";
                    var adminMenuKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("Новые заявки на справки", Callbacks.ViewPendingCertificates) },
                        [InlineKeyboardButton.WithCallbackData("Заявки на регистрацию", Callbacks.ListUsersToApprove) ],
                        [InlineKeyboardButton.WithCallbackData("Назад в главное меню", Callbacks.BackToMainMenuFromAdmin)]
                    });
                    await client.EditMessageText(chatId, messageId, adminMenuText, replyMarkup: adminMenuKeyboard, cancellationToken: token);
                    break;

                //возврат в главное меню
                case Callbacks.BackToMainMenu:
                    await ViewService.SendOrEditMainMenu(client, chatId, messageId, user, token);
                    break;

                //обработка списка пользователей на подтверждение 
                case Callbacks.ListUsersToApprove:
                    var usersToApprove = await db.Users
                   .Where(u => !u.IsAuthorised)
                   .ToListAsync(token);

                    string responseText;
                    if (usersToApprove.Count != 0)
                    {
                        // формируем список студентов, которые ещё не авторизованы
                        var userList = usersToApprove
                            .Select(u => $"- {u.Surname} {u.Name} (ID: `{u.TelegramId}`)")
                            .ToList();
                        responseText = "Студенты, ожидающие подтверждения:\n" + string.Join("\n", userList) + "\n\nИспользуйте команду `/approve [ID]` для подтверждения.";
                    }
                    else
                    {
                        responseText = "Нет новых заявок на регистрацию.";
                    }

                    var backToAdminMenuKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад в админ-панель", Callbacks.BackToAdminMenu) }
                    });

                    // редактируем сообщение с админ-меню, показывая на его месте список
                    await client.EditMessageText(chatId, messageId, responseText, replyMarkup: backToAdminMenuKeyboard, parseMode: ParseMode.Markdown, cancellationToken: token);
                    await client.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: token);
                    break;

                //продолжение регистрации на случай, если админом будет студент
                case Callbacks.AdminIsStudent:
                    await client.EditMessageReplyMarkup(chatId, callbackQuery.Message.MessageId, null, cancellationToken: token);
                    userStates[chatId] = UserState.AwaitingRegistrationGroup;
                    await client.SendMessage(chatId, "Пожалуйста, введите название вашей учебной группы.", cancellationToken: token);
                    break;

                //упрощение для админов-сотрудников дирекции, чтобы им не пришлось вводить не нужные данные
                case Callbacks.AdminIsNotStudent:
                    await client.EditMessageReplyMarkup(chatId, callbackQuery.Message.MessageId, null, cancellationToken: token);
                    var userToUpdate = db.Users.Local.FirstOrDefault(u => u.TelegramId == user.TelegramId);
                    if (userToUpdate != null)
                    {
                        userToUpdate.GroupName = "Сотрудник";
                        userToUpdate.IsAuthorised = true;
                        await db.SaveChangesAsync(token);
                        userStates.TryRemove(chatId, out _);

                        await client.SendMessage(chatId, "Вы были распознаны как администратор-сотрудник и авторизованы автоматически!", cancellationToken: token);
                        await ViewService.SendOrEditMainMenu(client, chatId, null, user, token);
                    }
                    break;
            }
        }
    }
}
