using Google.Apis.DriveActivity.v2.Data;
using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.DataLayer.Enums;
using GSheetTelegramBot.Web.Interfaces;
using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GSheetTelegramBot.Web.Services
{
    public class TelegramService
    {
        private readonly TelegramBotClient _botClient;
        private CancellationTokenSource? _cts;
        private readonly IServiceProvider _serviceProvider;

        public TelegramService(string token, IServiceProvider serviceProvider)
        {
            _botClient = new TelegramBotClient(token);
            _serviceProvider = serviceProvider;
        }

        public void StartReceivingAsync()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ProcessBotUpdates());
        }

        private async Task ProcessBotUpdates()
        {
            var offset = 0;
            while (!_cts.IsCancellationRequested)
            {
                var updates = await _botClient.GetUpdatesAsync(offset, cancellationToken: _cts.Token);
                foreach (var update in updates)
                {
                    try
                    {
                        if (update.Message != null)
                        {
                            await HandleMessageAsync(update.Message);
                        }
                        else if (update.CallbackQuery != null)
                        {
                            await HandleCallbackQueryAsync(update.CallbackQuery);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex.Message}");
                    }

                    offset = update.Id + 1;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), _cts.Token);
            }
        }

        public void StopReceiving()
        {
            _cts?.Cancel();
        }

        private async Task HandleMessageAsync(Message message)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var chatId = message.Chat.Id;
                var isAwaitingEmailInput = await userService.IsAwaitingEmailInput(chatId);
                if (isAwaitingEmailInput)
                {
                    if (message.Text == "Указать другой Email")
                    {
                        await userService.SetAwaitingEmailInputStatus(chatId);
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "Пожалуйста, введите новый адрес электронной почты:"
                        );
                    }
                    else if ( userService.IsValidEmail(message.Text))
                    {
                       await userService.RegisterUserEmailAsync(chatId, message.Text);

                       await HandleChangeEmailCommandAsync(chatId);
                       }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "Адрес электронной почты невалидный. Пожалуйста введитве правильный адрес:"
                        );
                    }
                }

                var isAwaitedConfirmation = await userService.IsAwaitingEmailConfirmation(chatId);
                if (isAwaitedConfirmation)
                {
                    if (message.Text == "Указать другой Email")
                    {
                        await userService.SetAwaitingEmailInputStatus(chatId);
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "Пожалуйста, введите новый адрес электронной почты:"
                        );
                    }
                    
                }
                else
                {
                    switch (message.Text)
                    {
                        case "/start":
                            var userExist = await userService.UserExists(chatId);
                            if (!userExist)
                            {
                                await userService.CreateUserAsync(chatId);
                                await _botClient.SendTextMessageAsync(
                                    chatId,
                                    "Пожалуйста, введите адрес почты, на которую Вы планируете получать уведомления от таблиц:"
                                );
                            }
                            else
                            {
                                var isEmailConfirmed = await userService.IsEmailConfirmed(chatId);
                                if (isEmailConfirmed)
                                {
                                    var userRole = await userService.GetUserRoleAsync(chatId);
                                    await SendMainMenuAsync(chatId, userRole);
                                }
                                else
                                {
                                    await HandleChangeEmailCommandAsync(chatId);
                                }
                            }
                            break;
                        case "Указать другой Email":
                            var isAwaitingEmailConfirmation = await userService.IsAwaitingEmailConfirmation(chatId);
                            if (!isAwaitingEmailConfirmation)
                            {
                                await _botClient.SendTextMessageAsync(
                                    chatId,
                                    "Пожалуйста, введите новый адрес электронной почты:"
                                );
                                await userService.SetAwaitingEmailInputStatus(chatId);
                            }
                            break;
                        case "📋 Мои подписки": 
                            await ShowSubscriptionsMenuAsync(chatId);
                            break;
                        case "📚 Список таблиц":
                            await ShowAllTablesMenuAsync(chatId);
                            break;
                        case "🔧 Настройки":
                            await ShowSettingsMenuAsync(chatId);
                            break;
                        case "👑 Администрирование":
                            await ShowAdminPanelAsync(chatId);
                            break;
                    }
                }
            }
        }

        public async Task SendMainMenuAsync(long chatId, UserRole userRole)
        {
            List<List<KeyboardButton>> mainMenuButtons = new List<List<KeyboardButton>>();

            mainMenuButtons.Add(new List<KeyboardButton> { new KeyboardButton("📋 Мои подписки") });

            if (userRole == UserRole.Admin || userRole == UserRole.SuperAdmin)
            {
                mainMenuButtons.Add(new List<KeyboardButton> { new KeyboardButton("📚 Список таблиц") });
            }

            if (userRole == UserRole.SuperAdmin)
            {
                mainMenuButtons.Add(new List<KeyboardButton> { new KeyboardButton("👑 Администрирование") }); 
            }

            mainMenuButtons.Add(new List<KeyboardButton> { new KeyboardButton("🔧 Настройки") }); 

            var mainMenu = new ReplyKeyboardMarkup(mainMenuButtons)
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };

            await _botClient.SendTextMessageAsync(chatId, "Главное меню", replyMarkup: mainMenu);
        }

        private async Task ShowSubscriptionsMenuAsync(long chatId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                var user = await userService.FindByChatIdAsync(chatId);

                if (user == null)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Пользователь не найден.");
                    return;
                }

                var subscriptions = await subscriptionService.GetSubscriptionsByUserId(user.Id);
                if (subscriptions == null || !subscriptions.Any())
                {
                    await _botClient.SendTextMessageAsync(chatId, "У вас нет активных подписок.");
                    return;
                }

                var inlineKeyboardButtons = new List<List<InlineKeyboardButton>>();
                foreach (var subscription in subscriptions)
                {
                    if (!string.IsNullOrWhiteSpace(subscription.TableName))
                    {
                        inlineKeyboardButtons.Add(new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData($"📊 {subscription.TableName}", $"manageSubscriptions_{subscription.Id}")
                        });
                    }
                }

                inlineKeyboardButtons.Add(new List<InlineKeyboardButton>
                    { InlineKeyboardButton.WithCallbackData("Главное меню", "mainMenu") });

                var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboardButtons);
                await _botClient.SendTextMessageAsync(chatId, "Ваши подписки:", replyMarkup: inlineKeyboard);
            }
        }

        private async Task ShowSubscriptionManagementMenuAsync(long chatId, int subscriptionId, int messageId, IUserService userService, IGoogleTableService googleTableService)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("⚡ Мгновенные уведомления", $"setInstantNotification_{subscriptionId}") },
                new [] { InlineKeyboardButton.WithCallbackData("🌙 Дневные уведомления", $"setDailyNotification_{subscriptionId}") },
                new [] { InlineKeyboardButton.WithCallbackData("✨ Мгновенные + Дневные", $"setBothNotifications_{subscriptionId}") },
                new [] { InlineKeyboardButton.WithCallbackData("❌ Отписаться", $"unsubscribe_{subscriptionId}") },
                new [] { InlineKeyboardButton.WithCallbackData("🔙 Назад к подпискам", "backToSubscriptions") }
            });

            await _botClient.EditMessageTextAsync(chatId, messageId, "Настройте тип уведомлений для подписки:", replyMarkup: inlineKeyboard);
        }

        private async Task ShowAllTablesMenuAsync(long chatId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var googleTableService = scope.ServiceProvider.GetRequiredService<IGoogleTableService>();

                var tables = await googleTableService.GetAllTablesAsync(); 
                if (tables == null || !tables.Any())
                {
                    await _botClient.SendTextMessageAsync(chatId, "В системе нет доступных таблиц.");
                    return;
                }

                var inlineKeyboardButtons = tables.Select(table =>
                    new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData($"📊 {table.Name}", $"manageTable_{table.Id}")
                    }).ToList();

                inlineKeyboardButtons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("🔙 Назад", "mainMenu") });

                var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboardButtons);
                await _botClient.SendTextMessageAsync(chatId, "Список всех таблиц, добавленных в приложение:", replyMarkup: inlineKeyboard);
            }
        }

        private async Task ShowTableManagementMenuAsync(long chatId, int googleTableId, int messageId)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🗑 Удалить таблицу из системы", $"deleteTable_{googleTableId}") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад к таблицам", "backToTables") }
            });

            await _botClient.EditMessageTextAsync(chatId, messageId, "Управление таблицей:", replyMarkup: inlineKeyboard);
        }

        private async Task ShowSettingsMenuAsync(long chatId)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⏰ Таймзона", "timezone") },
                new[] { InlineKeyboardButton.WithCallbackData("🌙 Дневные уведомления", "dailyNotifications") },
                new[] { InlineKeyboardButton.WithCallbackData("✉️ Сменить Email", "changeEmail") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "mainMenu") }
            });

            await _botClient.SendTextMessageAsync(chatId, "Настройки:", replyMarkup: inlineKeyboard);
        }

        private async Task ShowTimezoneSelectionMenuAsync(long chatId)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Киев (GMT+2)", "timezoneKiev") },
                new[] { InlineKeyboardButton.WithCallbackData("Москва (GMT+3)", "timezoneMoscow") },
                new[] { InlineKeyboardButton.WithCallbackData("Баку (GMT+4)", "timezoneBaku") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад к Настройкам", "backToSettings") }
            });

            await _botClient.SendTextMessageAsync(chatId, "Выберите ваш часовой пояс:", replyMarkup: inlineKeyboard);
        }

        private async Task ShowHoursSelectionMenuAsync(long chatId)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(
                Enumerable.Range(16, 8) 
                    .Select(hour => InlineKeyboardButton.WithCallbackData($"{hour}:00", $"setHour_{hour}"))
                    .Chunk(3) 
                    .Select(row => row.ToList())
                    .ToList()
            );

            await _botClient.SendTextMessageAsync(chatId, "Выберите часы для дневных уведомлений:", replyMarkup: inlineKeyboard);
        }

        private async Task ShowMinutesSelectionMenuAsync(long chatId, int selectedHour)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(
                new[]
                    {
                        "00", "10", "20", "30", "40", "50"
                    }
                    .Select(minute => InlineKeyboardButton.WithCallbackData($"{selectedHour}:{minute}", $"setTime_{selectedHour}{minute}"))
                    .Chunk(3)
                    .Select(row => row.ToList())
                    .ToList()
            );

            await _botClient.SendTextMessageAsync(chatId, "Теперь выберите минуты:", replyMarkup: inlineKeyboard);
        }

        private async Task ShowAdminPanelAsync(long chatId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var currentUser = await userService.FindByChatIdAsync(chatId);

                if (currentUser?.Role != UserRole.SuperAdmin)
                {
                    await _botClient.SendTextMessageAsync(chatId, "У вас нет прав на выполнение данной операции.");
                    return;
                }

                var users = await userService.GetAllUsersAsync();
                var filteredUsers = users.Where(u => u.Role != UserRole.SuperAdmin).ToList();

                if (!filteredUsers.Any())
                {
                    await _botClient.SendTextMessageAsync(chatId, "В системе нет доступных пользователей для изменения роли.");
                    return;
                }

                var keyboardButtons = filteredUsers
                    .Select(u => InlineKeyboardButton.WithCallbackData($"{u.Email} - {u.Role}", $"changeRole_{u.Id}"))
                    .Chunk(2)
                    .Select(chunk => chunk.ToList())
                    .ToList();

                var inlineKeyboard = new InlineKeyboardMarkup(keyboardButtons);

                await _botClient.SendTextMessageAsync(chatId, "Выберите пользователя для изменения роли:", replyMarkup: inlineKeyboard);
            }
        }

        private async Task ShowRoleChangeOptionsAsync(long chatId, int userId)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Юзер", $"setRole_{userId}_User"),
                InlineKeyboardButton.WithCallbackData("Админ", $"setRole_{userId}_Admin"),
                InlineKeyboardButton.WithCallbackData("Назад", "mainMenu")
            });

            await _botClient.SendTextMessageAsync(chatId, "Выберите новую роль пользователя:", replyMarkup: inlineKeyboard);
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var googleTableService = scope.ServiceProvider.GetRequiredService<IGoogleTableService>();
                var chatId = callbackQuery.Message.Chat.Id;
                var callbackData = callbackQuery.Data;

                var actionAndParams = ParseCallbackData(callbackData);


                switch (actionAndParams.Action)
                {
                    case "manageSubscriptions":
                        await ShowSubscriptionManagementMenuAsync(chatId, actionAndParams.ParamId, callbackQuery.Message.MessageId, userService, googleTableService);
                        break;
                    case "mainMenu":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        break;
                    case "setInstantNotification":
                        await UpdateSubscriptionSettingsAsync(chatId, actionAndParams.ParamId, true, false);
                        break;
                    case "setDailyNotification":
                        await UpdateSubscriptionSettingsAsync(chatId, actionAndParams.ParamId, false, true);
                        break;
                    case "setBothNotifications":
                        await UpdateSubscriptionSettingsAsync(chatId, actionAndParams.ParamId, true, true);
                        break;
                    case "unsubscribe":
                        await UnsubscribeAsync(chatId, actionAndParams.ParamId);
                        break;
                    case "backToSubscriptions":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        await ShowSubscriptionsMenuAsync(chatId);
                        break;
                    case "backToSettings":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        await ShowSettingsMenuAsync(chatId);
                        break;
                    case "manageTable":
                        await ShowTableManagementMenuAsync(chatId, actionAndParams.ParamId, callbackQuery.Message.MessageId);
                        break;
                    case "deleteTable":
                        var googleTableId = actionAndParams.ParamId;
                        await DeleteTableAsync(chatId, googleTableId);
                        break;
                    case "backToTables":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        await ShowAllTablesMenuAsync(chatId);
                        break;
                    case "timezone":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        await ShowTimezoneSelectionMenuAsync(callbackQuery.Message.Chat.Id);
                        break;
                    case "timezoneKiev":
                    case "timezoneMoscow":
                    case "timezoneBaku":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        await SetUserTimezoneAsync(chatId, callbackData);
                        break;
                    case "dailyNotifications":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        await ShowHoursSelectionMenuAsync(callbackQuery.Message.Chat.Id);
                        break;
                    case "setHour":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        await ShowMinutesSelectionMenuAsync(callbackQuery.Message.Chat.Id, actionAndParams.ParamId);
                        break;
                    case "setTime":
                        await DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                        var timeValue = actionAndParams.ParamId.ToString("D4"); 

                        var hour = int.Parse(timeValue.Substring(0, 2));
                        var minute = int.Parse(timeValue.Substring(2, 2));
                        await SetUserNotificationTimeAsync(callbackQuery.Message.Chat.Id, new TimeSpan(hour, minute, 0));
                        break;
                    case "changeRole":
                        await ShowRoleChangeOptionsAsync(callbackQuery.Message.Chat.Id, actionAndParams.ParamId);
                        break;
                    case "changeEmail":
                        var isAwaitingEmailConfirmation = await userService.IsAwaitingEmailConfirmation(chatId);
                        if (!isAwaitingEmailConfirmation)
                        {
                            await _botClient.SendTextMessageAsync(
                                chatId,
                                "Пожалуйста, введите новый адрес электронной почты:"
                            );
                            await userService.SetAwaitingEmailInputStatus(chatId);
                        }
                        break;
                    case "setRole":
                        var parts = callbackQuery.Data.Split('_');
                        if (parts.Length < 3) return; 

                        var userId = int.Parse(parts[1]);
                        var newRole = parts[2] == "Admin" ? UserRole.Admin : UserRole.User;

                        await ChangeUserRole(callbackQuery.Message.Chat.Id, userId, newRole);
                        break;
                }

                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            }
        }

        private (string Action, int ParamId) ParseCallbackData(string callbackData)
        {
            var parts = callbackData.Split('_');
            var action = parts[0];
            var paramId = parts.Length > 1 ? int.Parse(parts[1]) : 0;

            return (action, paramId);
        }

        private async Task HandleChangeEmailCommandAsync(long chatId)
        {
            var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Указать другой Email" },
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            await _botClient.SendTextMessageAsync(
                chatId,
                "На Вашу электронную почту выслано письмо для подтверждения регистрации. Пожалуйста, подтвердите регистрацию либо укажите другой адрес электронной почты.",
                replyMarkup: replyKeyboardMarkup
            );
        }

        private async Task SetUserTimezoneAsync(long chatId, string timezoneAction)
        {
            var timeZoneId = timezoneAction switch
            {
                "timezoneKiev" => "Europe/Kiev",
                "timezoneMoscow" => "Europe/Moscow",
                "timezoneBaku" => "Asia/Baku",
                _ => throw new ArgumentException("Недопустимый часовой пояс")
            };

            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var user = await userService.FindByChatIdAsync(chatId);
                if (user != null)
                {
                    await userService.UpdateUserTimeSettings(user.Id, timeZoneId, user.DailySummaryTime);
                    await _botClient.SendTextMessageAsync(chatId, $"Часовой пояс установлен на {timeZoneId}.");
                }
            }
        }

        private async Task SetUserNotificationTimeAsync(long chatId, TimeSpan time)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var user = await userService.FindByChatIdAsync(chatId);
                if (user != null)
                {
                    await userService.UpdateUserTimeSettings(user.Id, user.TimeZoneId, time);
                    await _botClient.SendTextMessageAsync(chatId, $"Время дневных уведомлений установлено на {time.Hours}:{time.Minutes:D2}.");
                }
            }
        }

        public async Task ChangeUserRole(long chatId, int userId, UserRole newRole)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                await userService.ChangeUserRoleAsync(userId, newRole);
                string roleName = newRole.ToString();
                await _botClient.SendTextMessageAsync(chatId, $"Роль пользователя успешно изменена на {roleName}.");
            }
        }

        private async Task DeleteTableAsync(long chatId, int googleTableId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var googleTableService = scope.ServiceProvider.GetRequiredService<IGoogleTableService>();
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                var  notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var googleSheetId = await googleTableService.GetGoogleSheetIdByIdAsync(googleTableId);
                var tableName = await googleTableService.GetGoogleSheetNameByIdAsync(googleTableId);
                if (!string.IsNullOrEmpty(googleSheetId))
                {
                    var success = await googleTableService.DeleteGoogleTableAsync(googleSheetId);
                    if (success)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Таблица успешно удалена из системы.");
                        var subscriptions = await subscriptionService.GetSubscriptionsByGoogleSheetId(googleSheetId);
                        foreach (var subscription in subscriptions)
                            await subscriptionService.RemoveSubscriptionAsync(subscription.GoogleSheetId, subscription.UserId);

                        await notificationService.NotifyAdminsAboutTableDeletion(tableName, "");
                        await notificationService.NotifyUsersAboutSubscriptionDeletion(subscriptions, tableName);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Не удалось удалить таблицу. Попробуйте снова.");
                    }
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Таблица не найдена.");
                }
                await ShowAllTablesMenuAsync(chatId);
            }
        }

        public async Task SendTextMessageAsync(long chatId, string message, ParseMode parseMode = ParseMode.Markdown)
        {
            await _botClient.SendTextMessageAsync(chatId, message, parseMode: parseMode);
        }

        private async Task UpdateSubscriptionSettingsAsync(long chatId, int subscriptionId, bool instantNotifications, bool dailySummary)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                var user = await userService.FindByChatIdAsync(chatId);
                if (user == null)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Пользователь не найден.");
                    return;
                }

                var subscription = await subscriptionService.FindSubscriptionByIdAsync(subscriptionId);
                if (subscription == null)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Подписка не найдена.");
                    return;
                }

                subscription.InstantNotifications = instantNotifications;
                subscription.DailySummary = dailySummary;

                var success = await subscriptionService.UpdateSubscriptionAsync(subscription);
                if (!success)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Не удалось обновить настройки подписки.");
                    return;
                }

                await _botClient.SendTextMessageAsync(chatId, "Настройки подписки успешно обновлены.");
            }
        }

        private async Task UnsubscribeAsync(long chatId, int subscriptionId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                var user = await userService.FindByChatIdAsync(chatId);
                if (user == null)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Пользователь не найден.");
                    return;
                }

                var success = await subscriptionService.RemoveSubscriptionByIdAsync(subscriptionId);
                if (!success)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Не удалось отписаться от таблицы.");
                    return;
                }

                await _botClient.SendTextMessageAsync(chatId, "Вы успешно отписались от таблицы.");
            }
        }

        private async Task DeleteMessageAsync(long chatId, int messageId)
        {
            try
            {
                await _botClient.DeleteMessageAsync(chatId, messageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
            
            }
        }
        public async Task SendPdfDocumentAsync(long chatId, Stream pdfStream, string pdfFileName, CancellationToken cancellationToken)
        {
            try
            {
                await _botClient.SendDocumentAsync(
                    chatId: chatId,
                    document: new InputFileStream(pdfStream, pdfFileName),
                    cancellationToken: cancellationToken
                );
                Console.WriteLine("PDF document sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send the PDF document: {ex.Message}");
            }
        }
    }
}
