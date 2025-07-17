using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using CertificateTelegramBot_Main.Data;
using CertificateTelegramBot_Callbacks;
using CertificateTelegramBot_Enums;
using CertificateTelegramBot.Services.CertificateGeneration;
using DbUser = CertificateTelegramBot_Main.Data.User;

namespace CertificateTelegramBot_Main.Services
{
    public static class ViewService
    {
        public static async Task SendOrEditMainMenu(ITelegramBotClient client, long chatId, int? messageId, DbUser user, CancellationToken token)
        {
            var text = $"Главное меню";
            var isAdmin = user.Role == UserRole.Admin;

            var inlineKeybButtons = new List<List<InlineKeyboardButton>> //инлайн кнопки главного меню
        {
            new() { InlineKeyboardButton.WithCallbackData("Заказать справку", Callbacks.OrderCertificate) },
            new() { InlineKeyboardButton.WithCallbackData("Мои справки", Callbacks.MyCertificates) },
            new() { InlineKeyboardButton.WithCallbackData("Помощь", Callbacks.Help)}
        };

            if (isAdmin) //доп кнопка для админов (админ панель)
            {
                inlineKeybButtons.Add([InlineKeyboardButton.WithCallbackData("Панель администратора", Callbacks.ToggleAdminMode)]);
            }

            var inlineKeybMarkup = new InlineKeyboardMarkup(inlineKeybButtons);

            if (messageId.HasValue) // если есть какое-то сообщение, то мы редактируем его
            {
                await client.EditMessageText(chatId, messageId.Value, text, replyMarkup: inlineKeybMarkup, cancellationToken: token);
            }
            else // если нет, то отправляем новое
            {
                await client.SendMessage(chatId, text, replyMarkup: inlineKeybMarkup, cancellationToken: token);
            }
        }

        //пагинация кнопки "Мои сертификаты"
        public static async Task SendMyCertificatesPage(ITelegramBotClient client, long chatId, int messageId, long userId, int page, CancellationToken token, ApplicationDbContext db)
        {
            const int PageSize = 5;

            var userCertsQuery = db.Certificates //сортировка по найденным сертификатам по времени создания
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await userCertsQuery.CountAsync(token);
            var certsOnPage = await userCertsQuery
                .Skip(page * PageSize)
                .Take(PageSize)
                .ToListAsync(token);

            string responseText;
            if (certsOnPage.Any())
            {
                var certList = certsOnPage.Select(c => $"- {c.CreatedAt:dd.MM.yyyy}: {c.CertificateType} (Статус: *{c.Status}*)");
                responseText = $"Ваши справки (Страница {page + 1}):\n" + string.Join("\n", certList);
            }
            else
            {
                responseText = "Вы еще не заказали ни одной справки.";
            }

            var keyboardRows = new List<List<InlineKeyboardButton>>();
            var paginationButtons = new List<InlineKeyboardButton>();

            if (page > 0)
            {
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("Назад", Callbacks.MyCertificatesPage + (page - 1)));
            }
            if ((page + 1) * PageSize < totalCount)
            {
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("Вперед", Callbacks.MyCertificatesPage + (page + 1)));
            }
            if (paginationButtons.Any()) keyboardRows.Add(paginationButtons);

            keyboardRows.Add([InlineKeyboardButton.WithCallbackData("Назад в главное меню", Callbacks.BackToMainMenu)]);

            await client.EditMessageText(chatId, messageId, responseText, replyMarkup: new InlineKeyboardMarkup(keyboardRows), parseMode: ParseMode.Markdown, cancellationToken: token);
        }

        public static async Task SendSearchResultsPage(ITelegramBotClient client, long chatId, int messageId, string searchTerm, int page, CancellationToken token, ApplicationDbContext db)
        {
            const int PageSize = 5;

            var searchResultsQuery = db.Certificates
                .Include(c => c.User)
                .Where(c => c.User.Surname.ToLower().Contains(searchTerm.ToLower()))
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await searchResultsQuery.CountAsync(token);
            var certsOnPage = await searchResultsQuery
                .Skip(page * PageSize)
                .Take(PageSize)
                .ToListAsync(token);

            var text = $"Результаты поиска по '{searchTerm}' (Страница {page + 1}):";
            var keyboardRows = new List<List<InlineKeyboardButton>>();

            if (certsOnPage.Count != 0)
            {
                foreach (var cert in certsOnPage)
                {
                    var buttonText = $"📄 {cert.User.Surname} {cert.User.Name.FirstOrDefault()}. - {cert.CertificateType} ({cert.Status})";
                    keyboardRows.Add(
                        [InlineKeyboardButton.WithCallbackData(buttonText, Callbacks.ViewCertificateDetails + cert.CertificateId)]
                    );
                }
            }
            else
            {
                text = $"По запросу '{searchTerm}' ничего не найдено.";
            }

            var paginationButtons = new List<InlineKeyboardButton>();
            if (page > 0)
            {
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("Назад", $"{Callbacks.SearchCertificatesPage}{page - 1}_{searchTerm}"));
            }
            if ((page + 1) * PageSize < totalCount)
            {
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("Вперед", $"{Callbacks.SearchCertificatesPage}{page + 1}_{searchTerm}"));
            }
            if (paginationButtons.Count > 0) keyboardRows.Add(paginationButtons);

            keyboardRows.Add([InlineKeyboardButton.WithCallbackData("Вернуться в админ-панель", Callbacks.BackToAdminMenu)]);

            await client.EditMessageText(chatId, messageId, text, replyMarkup: new InlineKeyboardMarkup(keyboardRows), cancellationToken: token);
        }

        public static async Task SendCertificatesPage(ITelegramBotClient client, Message message, int page, CancellationToken token, ApplicationDbContext db)
        {
            const int PageSize = 5; // 5 справок на одной странице

            var pendingCertsQuery = db.Certificates
                .Include(c => c.User) // подгружаем данные о пользователе
                .Where(c => c.Status == CertificateStatus.Pending)
                .OrderBy(c => c.CreatedAt);

            var totalCount = await pendingCertsQuery.CountAsync(token);

            var certsOnPage = await pendingCertsQuery
                .Skip(page * PageSize)
                .Take(PageSize)
                .ToListAsync(token);

            var text = $"Новые заявки на справки (Страница {page + 1}):";
            var keyboardRows = new List<List<InlineKeyboardButton>>();

            if (certsOnPage.Count != 0)
            {
                foreach (var cert in certsOnPage)
                {
                    // для каждой справки создаем кнопку
                    var buttonText = $"📄 {cert.User.Surname} - {cert.CertificateType} ({cert.CreatedAt:dd.MM})";
                    keyboardRows.Add(
            [
                InlineKeyboardButton.WithCallbackData(buttonText, Callbacks.ViewCertificateDetails + cert.CertificateId)
            ]);
                }
            }
            else
            {
                text = "Нет новых заявок на справки.";
            }
            var paginationButtons = new List<InlineKeyboardButton>();
            if (page > 0)
            {
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("Назад", Callbacks.PageCertificates + (page - 1)));
            }
            if ((page + 1) * PageSize < totalCount)
            {
                paginationButtons.Add(InlineKeyboardButton.WithCallbackData("Вперед", Callbacks.PageCertificates + (page + 1)));
            }
            if (paginationButtons.Count != 0) keyboardRows.Add(paginationButtons);

            keyboardRows.Add([InlineKeyboardButton.WithCallbackData("Вернуться в админ-панель", Callbacks.BackToAdminMenu)]);

            var finalKeyboard = new InlineKeyboardMarkup(keyboardRows);

            //await client.EditMessageText(message.Chat.Id, message.MessageId, text, replyMarkup: new InlineKeyboardMarkup(keyboardRows), cancellationToken: token);
            await client.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: token);
            await client.SendMessage(message.Chat.Id, text, replyMarkup: finalKeyboard, cancellationToken: token);
        }

        public static async Task ShowCertificateDetails(ITelegramBotClient client, Message message, int certificateId, CancellationToken token, ApplicationDbContext db)
        {
            var cert = await db.Certificates.Include(c => c.User).FirstOrDefaultAsync(c => c.CertificateId == certificateId, token);
            if (cert == null)
            {
                await client.EditMessageText(message.Chat.Id, message.MessageId, "Ошибка: справка не найдена.", cancellationToken: token);
                return;
            }

            // удаляем старое сообщение со списком, чтобы не было мусора
            await client.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: token);

            var fileToSend = TestingCertificate.GenerateCertificateFile(cert);

            var text = $"""
                 *Заявка #{cert.CertificateId}*
                 
                 *Студент:* {cert.User.Surname} {cert.User.Name} {cert.User.Patronymic}
                 *Группа:* {cert.User.GroupName}
                 *Телефон:* {cert.User.PhoneNumber}
                 
                 *Тип справки:* {cert.CertificateType}
                 *Место требования:* {cert.Destination}
                 *Дата заказа:* {cert.CreatedAt:dd.MM.yyyy HH:mm}
                 
                 *Текущий статус: {cert.Status}*
                 """;

            var keyboardRows = new List<List<InlineKeyboardButton>>();
            var statusButtons = new List<InlineKeyboardButton>();

            // показываем релевантные кнопки для смены статуса
            if (cert.Status == CertificateStatus.Pending)
            {
                statusButtons.Add(InlineKeyboardButton.WithCallbackData("✅ Принять в работу", Callbacks.ChangeCertificateStatus + cert.CertificateId + "_" + CertificateStatus.InProgress));
                statusButtons.Add(InlineKeyboardButton.WithCallbackData("❌ Отклонить", Callbacks.ChangeCertificateStatus + cert.CertificateId + "_" + CertificateStatus.Rejected));
            }
            else if (cert.Status == CertificateStatus.InProgress)
            {
                statusButtons.Add(InlineKeyboardButton.WithCallbackData("✅ Готова к выдаче", Callbacks.ChangeCertificateStatus + cert.CertificateId + "_" + CertificateStatus.Ready));
            }

            if (statusButtons.Count > 0) keyboardRows.Add(statusButtons);

            keyboardRows.Add([InlineKeyboardButton.WithCallbackData("К списку заявок", Callbacks.ViewPendingCertificates)]);

            // отправляем сообщение с файлом
            await client.SendDocument(message.Chat.Id, fileToSend, caption: text, replyMarkup: new InlineKeyboardMarkup(keyboardRows), parseMode: ParseMode.Markdown, cancellationToken: token);
        }
    }
}
