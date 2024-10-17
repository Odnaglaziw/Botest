using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using System.Windows;
using System.Timers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Botest
{
    public class Program
    {
        static readonly int AdminChatId = 1439555515;
        public static void Main(string[] args)
        {
            EnsureDb();
            GetAllStates();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 1;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            timer.Disposed += Timer_Disposed;
            Console.ReadLine();
        }

        private static void Timer_Disposed(object? sender, EventArgs e)
        {
            var bot = new TelegramBotClient("7938124760:AAGqNUh_HXZ7ML3htR5rtHdP4VdMncJUeBs");
            Log("Бот запущен");
            bot.StartReceiving(Update,Error);
        }

        private static async Task Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (!UserState.States.ContainsKey(update.Message.Chat.Id))
            {
                UserState.States[update.Message.Chat.Id] = "reg:Name";
                AddUser(update.Message.Chat.Id, update.Message.Chat.FirstName, update.Message.Chat.FirstName, "", "reg:Name");
                Log("Начал беседу", update.Message.Chat.Id);
                await client.SendTextMessageAsync(update.Message.Chat.Id, "Здравствуйте! Я вас не знаю, как вас зовут?");
            }
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
        }

        private static async Task CommandHandler(ITelegramBotClient client, Message message)
        {
        }

        private static async Task CallBackHandler(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
        }

        private static async Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
        }

        private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (UserState.States.Count > 0) (sender as System.Timers.Timer).Stop();
            (sender as System.Timers.Timer).Dispose();
        }

        static void Log(string text,long Id)
        {

            Console.WriteLine($"{DateTime.Now}  |  {Id.ToString().PadRight(11)} | {text}");
        }
        static void Log(string text)
        {

            Console.WriteLine($"{DateTime.Now}  |  {(-1).ToString().PadRight(11)} | {text}");
        }

        static async void AddUser(long id, string name, string lastname, string group, string state)
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
        static async void GetAllStates()
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