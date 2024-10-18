using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using System.Windows;
using System.Timers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Policy;

namespace Botest
{
    public class Program
    {
        static readonly int AdminChatId = 1439555515;
        static DateTime lastUpdate = DateTime.Now;
        static readonly string urlBell = @"https://www.ects.ru/images/280/Image/zvonki.jpg";
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                Log("Сохранение данных.");
                SaveStates().GetAwaiter().GetResult();
            };
            EnsureDb();
            AddUser(AdminChatId,"Admin","ygy","Pr-31","main").Wait();
            UserState.States[AdminChatId] = "main";
            GetAllStates().Wait();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 1;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            timer.Disposed += Timer_Disposed;
            System.Timers.Timer upd = new System.Timers.Timer();
            upd.Interval = 3_600_000;
            upd.Elapsed += Upd_Elapsed;
            upd.Start();
            DownloadFiles().Wait();
            XlsManager.GetDataFrom(".\\Downloads\\3.xls", "Пр-31 Пр-32").Wait();
            Console.ReadLine();
        }

        private static void Upd_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Log("Данные обновлены");
            DownloadFiles().Wait();
            lastUpdate = DateTime.Now;
        }

        private static void Timer_Disposed(object? sender, EventArgs e)
        {
            var bot = new TelegramBotClient("7938124760:AAGqNUh_HXZ7ML3htR5rtHdP4VdMncJUeBs");
            Log("Бот запущен");
            bot.StartReceiving(Update,Error);
        }

        private static async Task Update(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
        {
            switch (update.Type)
            {
                case UpdateType.CallbackQuery:
                    {
                        if (update.CallbackQuery != null)
                            await CallBackHandler(client, update.CallbackQuery);
                    }
                    break;
                case UpdateType.Message:
                    {
                        var message = update.Message;
                        if (message.Text != null)
                        {
                            if (message.Text.StartsWith('/'))
                            {
                                await CommandHandler(client, message);
                                break;
                            }
                            await MessageHandler(client, message);
                        }
                    }
                    break;
                default: { } break;
            }
        }

        private static async Task MessageHandler(ITelegramBotClient client, Message message)
        {
            var chatId = message.Chat.Id;
            if (!UserState.States.ContainsKey(chatId))
            {
                UserState.States[chatId] = "reg:Name";
                await AddUser(chatId, message.Chat.FirstName!, message.Chat.LastName!, "", "reg:Name");
                Log("Начал беседу", chatId);
                await client.SendTextMessageAsync(chatId, "Здравствуйте! Я вас не знаю, как вас зовут?");
                List<InlineKeyboardButton> btns = [InlineKeyboardButton.WithCallbackData("Данные", $"GetInfo;{chatId}")];
                InlineKeyboardMarkup ikm = new(btns);
                await client.SendTextMessageAsync(AdminChatId, $"Пользователь {chatId} начал беседу", replyMarkup: ikm);
                return;
            }
            switch (UserState.States[chatId])
            {
                case "reg:Name":
                    {
                        var user = await GetUser(chatId);
                        user.Name = message.Text!.Trim();
                        UserState.States[chatId] = "reg:LastName";
                        user.State = UserState.States[chatId];
                        await UpdateUser(user);
                        await client.SendTextMessageAsync(chatId,$"Записал ваше имя как {message.Text}\nКакая у вас фамииля?");
                    }
                    break;
                case "reg:LastName":
                    {
                        var user = await GetUser(chatId);
                        user.LastName = message.Text!.Trim();
                        UserState.States[chatId] = "reg:Group";
                        user.State = UserState.States[chatId];
                        await UpdateUser(user);
                        await client.SendTextMessageAsync(chatId, $"Записал вашу фамилию как {message.Text}\nВ какой вы группе?\n(В формате ХХ-00)");
                    }
                    break;
                case "reg:Group":
                    {
                        string[] test = message.Text!.Trim().Split(new char[] { '-' });
                        if (test.Length != 2)
                        {
                            await client.SendTextMessageAsync(chatId, "В формате ХХ-00");
                            break;
                        }
                        if (!Regex.IsMatch(test[0], @"^[a-zA-Zа-яА-Я]+$") && Regex.IsMatch(test[1], @"^[a-zA-Zа-яА-Я]+$"))
                        {
                            await client.SendTextMessageAsync(chatId, "В формате ХХ-00");
                            break;
                        }
                        var user = await GetUser(chatId);
                        user.Group = message.Text.Trim();
                        UserState.States[chatId] = "main";
                        user.State = UserState.States[chatId];
                        await UpdateUser(user);
                        await client.SendTextMessageAsync(chatId, $"Записал вашу группу как {message.Text}\nПрофиль заполнен, рад был познакомиться!");
                        List<InlineKeyboardButton> btns = [InlineKeyboardButton.WithCallbackData("Данные", $"GetInfo;{chatId}")];
                        InlineKeyboardMarkup ikm = new(btns);
                        await client.SendTextMessageAsync(AdminChatId, $"Пользователь {chatId} изменил данные", replyMarkup: ikm);
                        await CallBackHandler(client, new CallbackQuery { Data = "Menu", Message = message });
                    }
                    break;

                default: { }break;
            }
        }

        private static async Task CommandHandler(ITelegramBotClient client, Message message)
        {
            var chatId = message.Chat.Id;
            if (!UserState.States.ContainsKey(chatId))
            {
                UserState.States[chatId] = "reg:Name";
                await AddUser(chatId, message.Chat.FirstName!, message.Chat.LastName!, "", "reg:Name");
                Log("Начал беседу", chatId);
                await client.SendTextMessageAsync(chatId, "Здравствуйте! Я вас не знаю, как вас зовут?");

                List<InlineKeyboardButton> btns = [InlineKeyboardButton.WithCallbackData("Данные", $"GetInfo;{chatId}")];
                InlineKeyboardMarkup ikm = new(btns);
                await client.SendTextMessageAsync(AdminChatId, $"Пользователь {chatId} начал беседу", replyMarkup: ikm);
                return;
            }
            switch (message.Text!.Substring(1).ToLower())
            {
                case "start":
                    {
                        var user = await GetUser(chatId);
                        await client.SendTextMessageAsync(chatId, $"Мы уже знакомы, {user.Name}! Для дальнейших действий возпользуйтесь /menu");
                    }break;
                case "profile":
                    {
                        await CallBackHandler(client, new CallbackQuery { Data = "Profile", Message = message });
                    }
                    break;
                case "menu":
                    {
                        await CallBackHandler(client,new CallbackQuery { Data = "Menu", Message = message });
                    }break;
                default: { }break;
            }
        }

        private static async Task CallBackHandler(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message!.Chat.Id;
            string[] datas = callbackQuery.Data!.Trim().Split(new char[] { ';' });
            switch (datas[0])
            {
                case "GetInfo":
                    {
                        string text = "";
                        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
                        optionsBuilder.UseNpgsql("Host=odnaglaziw.online;Port=5432;user id = postgres;Password=DsPs4N8gt3;");
                        using (var context = new UserDbContext(optionsBuilder.Options))
                        {
                            var user = await context.GetUserByIdAsync(Convert.ToInt64(datas[1]));
                            text = $"Имя пользователя: {user.Name}\n" +
                                $"Фамилия пользователя: {user.LastName}\n" +
                                $"Группа пользователя: {user.Group}\n" +
                                $"id пользователя: {user.Id}";
                        }
                        Log($"Запорсил данные об {Convert.ToInt64(datas[1])}",AdminChatId);
                        await client.SendTextMessageAsync(AdminChatId,text);
                    }
                    break;
                case "Menu":
                    {
                        string text = "Вот что я могу:";
                        List<InlineKeyboardButton> btns = [
                        InlineKeyboardButton.WithCallbackData("Изменения","Schedule"),
                        InlineKeyboardButton.WithCallbackData("Звонки","Bells"),
                        InlineKeyboardButton.WithCallbackData("Дней до..","DaysTo"),
                        InlineKeyboardButton.WithCallbackData("Профиль","Profile"),
                        ];
                        InlineKeyboardMarkup ikm = new(btns);
                        await client.SendTextMessageAsync(chatId,text,replyMarkup:ikm);
                    }break;
                case "Schedule":
                    {
                        double min = (DateTime.Now-lastUpdate).TotalMinutes;
                        await using Stream stream = System.IO.File.OpenRead(".\\Downloads\\Pr-31.png");
                        var message = await client.SendPhotoAsync(chatId, InputFile.FromStream(stream, "Pr-31.png"),
                            caption: $"Пока что я могу отправить лишь Пр-31\n" +
                            $"Данные полученые {min} минут назад");
                    }
                    break;
                case "Bells":
                    {
                        await client.SendPhotoAsync(chatId,InputFile.FromUri(urlBell));
                    }
                    break;
                case "DaysTo":
                    {
                        int days = (Convert.ToDateTime("31.12.2024")-DateTime.Now).Days;
                        await client.SendTextMessageAsync(chatId,$"До нового года {days} дней.");
                    }
                    break;
                case "Profile":
                    {
                        string text = "";
                        var user = await GetUser(chatId);
                        text = $"Имя пользователя: {user.Name}\n" +
                            $"Фамилия пользователя: {user.LastName}\n" +
                            $"Группа пользователя: {user.Group}\n" +
                            $"id пользователя: {user.Id}";
                        List<InlineKeyboardButton> btns = [
                        InlineKeyboardButton.WithCallbackData("Изменить","Reg"),
                        InlineKeyboardButton.WithCallbackData("Назад","Menu"),
                        ];
                        InlineKeyboardMarkup ikm = new(btns);
                        await client.SendTextMessageAsync(chatId, text, replyMarkup: ikm);
                    }
                    break;
                case "Reg":
                    {
                        UserState.States[chatId] = "reg:Name";
                        await client.SendTextMessageAsync(chatId,"Введите новое имя:");
                    }break;

                default: { }break;
            }
        }

        private static async Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Log(exception.Message,Int16.MinValue);
        }

        private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (UserState.States.Count > 0) (sender as System.Timers.Timer).Stop();
            (sender as System.Timers.Timer).Dispose();
        }

        public static void Log(string text,long Id)
        {

            Console.WriteLine($"{DateTime.Now}  |  {Id.ToString().PadRight(11)} | {text}");
        }
        public static void Log(string text)
        {

            Console.WriteLine($"{DateTime.Now}  |  {(-1).ToString().PadRight(11)} | {text}");
        }

        static async Task AddUser(long id, string name, string lastname, string group, string state)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            optionsBuilder.UseNpgsql("Host=odnaglaziw.online;Port=5432;user id = postgres;Password=DsPs4N8gt3;");
            using (var context = new UserDbContext(optionsBuilder.Options))
            {
                await context.AddUserAsync(new UserDbContext.User
                {
                    Id = id,
                    Name = name,
                    LastName = lastname,
                    Group = group,
                    State = state
                });
            }
            Log("Пользователь внесён в бд",id);
        }
        static async Task UpdateUser(UserDbContext.User user)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            optionsBuilder.UseNpgsql("Host=odnaglaziw.online;Port=5432;user id = postgres;Password=DsPs4N8gt3;");
            using (var context = new UserDbContext(optionsBuilder.Options))
            {
                await context.UpdateUserAsync(user);
            }
            Log("Пользователь обновил данные профиля", user.Id);
        }
        static async Task<UserDbContext.User> GetUser(long id)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            optionsBuilder.UseNpgsql("Host=odnaglaziw.online;Port=5432;user id = postgres;Password=DsPs4N8gt3;");
            using (var context = new UserDbContext(optionsBuilder.Options))
            {
                return await context.GetUserByIdAsync(id);
            }
        }
        static async Task GetAllStates()
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            optionsBuilder.UseNpgsql("Host=odnaglaziw.online;Port=5432;user id = postgres;Password=DsPs4N8gt3;");
            using (var context = new UserDbContext(optionsBuilder.Options))
            {
                List<UserDbContext.User> users = await context.GetAllUsersAsync();
                foreach (var user in users)
                {
                    UserState.States[user.Id] = user.State ?? "main";
                }
            }
            Log("Статусы пользователей обновлены",-1);
        }
        static async Task SaveStates()
        {
            foreach (var state in UserState.States)
            {
                var user = await GetUser(state.Key);
                user.State = state.Value;
                await UpdateUser(user);
            }
            Log("Данные сохранены.");
        }
        static async Task DownloadFiles()
        {
            string fileUrl_1 = "https://www.ects.ru/images/280/Image/1_kurs_24-25_novoe.xls";
            string fileUrl_2 = "https://www.ects.ru/images/280/Image/2_kurs_24-25_novoe.xls";
            string fileUrl_3 = "https://www.ects.ru/images/280/Image/3_kurs_24-25_novoe.xls";
            string fileUrl_4 = "https://www.ects.ru/images/280/Image/4_kurs_24-25_novoe.xls";
            string destinationPath_1 = ".\\Downloads\\1.xls";
            string destinationPath_2 = ".\\Downloads\\2.xls";
            string destinationPath_3 = ".\\Downloads\\3.xls";
            string destinationPath_4 = ".\\Downloads\\4.xls";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Log("Скачивание файлов...");
                    byte[] fileBytes_1 = await client.GetByteArrayAsync(fileUrl_1);

                    await System.IO.File.WriteAllBytesAsync(destinationPath_1, fileBytes_1);
                    Log($"Файл успешно скачан и сохранён в {destinationPath_1}");
                    byte[] fileBytes_2 = await client.GetByteArrayAsync(fileUrl_2);

                    await System.IO.File.WriteAllBytesAsync(destinationPath_2, fileBytes_2);
                    Log($"Файл успешно скачан и сохранён в {destinationPath_2}");
                    byte[] fileBytes_3 = await client.GetByteArrayAsync(fileUrl_3);

                    await System.IO.File.WriteAllBytesAsync(destinationPath_3, fileBytes_3);
                    Log($"Файл успешно скачан и сохранён в {destinationPath_3}");
                    byte[] fileBytes_4 = await client.GetByteArrayAsync(fileUrl_4);

                    await System.IO.File.WriteAllBytesAsync(destinationPath_4, fileBytes_4);
                    Log($"Файл успешно скачан и сохранён в {destinationPath_4}");

                    byte[] bells = await client.GetByteArrayAsync(urlBell);
                    await System.IO.File.WriteAllBytesAsync(".\\Downloads\\Bells.jpg", bells);
                    Log($"Файл успешно скачан и сохранён в .\\Downloads\\Bells.jpg");
                }
                catch (Exception ex)
                {
                    Log($"Ошибка при скачивании файла: {ex.Message}");
                }
            }
        }
        static void EnsureDb()
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            optionsBuilder.UseNpgsql("Host=odnaglaziw.online;Port=5432;user id = postgres;Password=DsPs4N8gt3;");
            using (var context = new UserDbContext(optionsBuilder.Options))
            {
                context.Database.EnsureCreated();
            }
            Log("База даннах создана, либо уже была создана",-1);
        }
    }
}