using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using System.Timers;
using BotV3.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = BotV3.Models.User;
using BotWalletPublish;

namespace BotV3
{
    public class Program
    {
        private static List<User> users = new List<User>();
        private static ITelegramBotClient botClient;
        private static string userQuery;


        //private static System.Timers.Timer aTimer;
        //private static System.Timers.Timer gameTimer;

        public static void Main(string[] args)
        {
            //SetTimerForRequest();
            //820903390:AAHmpT-wxce7Pz-aAu2XhcerWoPAvYvbO0s
            botClient = new TelegramBotClient("632174257:AAEAbDwxgrd-EcsjWCW6E36zkPeH7GiiHao");
            var me = botClient.GetMeAsync().Result;
            botClient.OnMessage += Bot_OnMessageAsync;
            botClient.OnCallbackQuery += Bot_OnCallBackQuery;
            botClient.StartReceiving();

            CreateWebHostBuilder(args).Build().Run();
            Thread.Sleep(int.MaxValue);
        }

        private static async void Bot_OnCallBackQuery(object sender, CallbackQueryEventArgs e)
        {
            User definedUser = users.Find(u => u.Id == e.CallbackQuery.From.Id);
            if (definedUser == null)
            {
                Wallet wall = new Wallet
                {
                    Id = users.Count + 1,
                    Sum = 0
                };
                users.Add(new User(e.CallbackQuery.Message.From.Id, e.CallbackQuery.Message.From.FirstName + " " + e.CallbackQuery.Message.From.LastName, e.CallbackQuery.Message.From.Username, wall));
                definedUser = users.ElementAt(users.Count - 1);
            }

            if (e.CallbackQuery.Data == "Добавить")
            {
                definedUser.SetTransaction = true;
                if (e.CallbackQuery.Message.Text == "1. Доходы")
                {
                    definedUser.TransactionType = Transaction.Operation.Income;
                }
                else if (e.CallbackQuery.Message.Text == "2. Расходы")
                {
                    definedUser.TransactionType = Transaction.Operation.Expenses;
                }

                await botClient.SendTextMessageAsync(
                         chatId: e.CallbackQuery.Message.Chat,
                         text: "Хорошо. Отправьте мне транзакцию, используйте этот формат:\n" + "sum-discription\n",
                         parseMode: ParseMode.Html);
            }
            else if (e.CallbackQuery.Data == "Удалить")
            {
                List<InlineKeyboardButton> replyKeyboardList = new List<InlineKeyboardButton>();

                string text = "Выберие транзакцию, что бы удалить: \n";
                definedUser.DeleteTransaction = true;

                foreach (var tr in definedUser.Wallet.History)
                {
                    text += $"№{tr.Id}. +{tr.Cost} BGN - {tr.Discription}\n";
                    replyKeyboardList.Add(InlineKeyboardButton.WithCallbackData(tr.Id.ToString()));
                }
                InlineKeyboardMarkup replyKeyboard = new InlineKeyboardMarkup(replyKeyboardList);
                await botClient.SendTextMessageAsync(
                     chatId: e.CallbackQuery.Message.Chat,
                     text: text,
                     parseMode: ParseMode.Html,
                     replyMarkup: replyKeyboard
                     );

            }
            else if (e.CallbackQuery.Data == "Посмотреть")
            {
                string text = "Транзакции: \n";

                foreach (var tran in definedUser.Wallet.History)
                {
                    string operation = "-";
                    if (tran.Type == Transaction.Operation.Income)
                    {
                        operation = "+";
                    }
                    text += operation + tran.Cost + " BGN " + tran.Discription + " (" + tran.CreatedDate.ToString("dd.MM HH:mm") + ")\n";
                }

                await botClient.AnswerCallbackQueryAsync(
                    e.CallbackQuery.Id,
                    text: $"\n Баланс: {definedUser.Wallet.Sum} BGN",
                    showAlert: true);

                await botClient.SendTextMessageAsync(
                         chatId: e.CallbackQuery.Message.Chat,
                         text: text + "\n Баланс: " + definedUser.Wallet.Sum + " BGN",
                         parseMode: ParseMode.Html);
            }
            int transId;
            if (int.TryParse(e.CallbackQuery.Data, out transId))
            {
                var trans = definedUser.Wallet.History.Find(u => u.Id == transId);
                if (trans.Type == Transaction.Operation.Income)
                {
                    definedUser.Wallet.Sum -= trans.Cost;
                }
                else
                {
                    definedUser.Wallet.Sum += trans.Cost;
                }

                definedUser.Wallet.History.Remove(trans);
                await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Транзакция №{transId} была удалена", showAlert: true);
            }

            if (definedUser.ShareWithUser == true)
            {
                var userToShare = users.Find(n => n.UserName == e.CallbackQuery.Data);
                userToShare.SetToSharedWalled(ref definedUser.Wallet);
                definedUser.ShareWithUser = false;
                await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"{definedUser.Name} shared with {userToShare.Name}", showAlert: true);
            }
            await botClient.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
        }

        static async void Bot_OnMessageAsync(object sender, MessageEventArgs e)
        {
            userQuery = e.Message.Text;
            var currentUser = users.Find(u => u.Id == e.Message.From.Id);
            if (currentUser == null)
            {
                Wallet wall = new Wallet
                {
                    Id = users.Count + 1,
                    Sum = 0
                };
                users.Add(new User(e.Message.From.Id, e.Message.From.FirstName + " " + e.Message.From.LastName, e.Message.From.Username, wall));
                currentUser = users.ElementAt(users.Count - 1);
            }

            if (currentUser.SetTransaction == true)
            {
                string[] parsedTrans = userQuery.Split('-');
                Transaction transactionToAdd = null;

                try
                {
                    if (currentUser.TransactionType == Transaction.Operation.Income)
                    {
                        transactionToAdd = new Transaction
                        {
                            Id = currentUser.Wallet.History.Count + 1,
                            Type = Transaction.Operation.Income,
                            Cost = Convert.ToInt32(parsedTrans[0]),
                            CreatedDate = DateTime.Now,
                            Discription = parsedTrans[1],
                        };
                    }
                    else
                    {
                        transactionToAdd = new Transaction
                        {
                            Id = currentUser.Wallet.History.Count + 1,
                            Type = Transaction.Operation.Expenses,
                            Cost = Convert.ToInt32(parsedTrans[0]),
                            CreatedDate = DateTime.Now,
                            Discription = parsedTrans[1],

                        };
                    }
                }
                catch (Exception)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: $"Проверьте веденные данные, запишите сумму и название транзакции через тире",
                        parseMode: ParseMode.Html);
                    currentUser.SetTransaction = false;
                }

                var res = currentUser.Wallet.AddTransaction(transactionToAdd);
                if (res.IsCompletedSuccessfully)
                {
                    await botClient.SendTextMessageAsync(
                         chatId: e.Message.Chat,
                         text: $"Транзакция № {transactionToAdd.Id} добавлена",
                         parseMode: ParseMode.Html);
                    currentUser.SetTransaction = false;
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                         chatId: e.Message.Chat,
                         text: $"Проверьте формат введенных данных",
                         parseMode: ParseMode.Html);
                }
            }
            if (userQuery == "/start@cuutiesBot")
            {
                InlineKeyboardMarkup replyKeyboard = new InlineKeyboardMarkup(new[]
                         {
                             InlineKeyboardButton.WithCallbackData("Добавить"),
                             InlineKeyboardButton.WithCallbackData("Удалить")
                         });

                await botClient.SendTextMessageAsync(
                         chatId: e.Message.Chat,
                         text: "1. Доходы",
                         parseMode: ParseMode.Html,
                         replyMarkup: replyKeyboard);

                await botClient.SendTextMessageAsync(
                         chatId: e.Message.Chat,
                         text: "2. Расходы",
                         parseMode: ParseMode.Html,
                         replyMarkup: replyKeyboard);

                await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: "3. Статистика",
                        parseMode: ParseMode.Html,
                        replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton().CallbackData = "Посмотреть"));
            }
            else if (userQuery == "/sharewallet@cuutiesBot")
            {
                var members = await botClient.GetChatAdministratorsAsync(e.Message.Chat);

                List<InlineKeyboardButton> replyKeyboardList = new List<InlineKeyboardButton>();

                string text = "Выберие участника, что бы поделиться кошельком: \n";
                currentUser.ShareWithUser = true;

                foreach (var member in members)
                {
                    text += $"@{member.User.Username}, {member.User.FirstName}\n";
                    replyKeyboardList.Add(InlineKeyboardButton.WithCallbackData(member.User.Username));
                }
                InlineKeyboardMarkup replyKeyboard = new InlineKeyboardMarkup(replyKeyboardList);
                await botClient.SendTextMessageAsync(
                     chatId: e.Message.Chat,
                     text: text,
                     parseMode: ParseMode.Html,
                     replyMarkup: replyKeyboard
                     );
            }
        }
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
           WebHost.CreateDefaultBuilder(args)
               .UseStartup<Startup>();

    }
}

//public async static void SendMessage(MessageEventArgs e, string text)
//{
//    await botClient.SendTextMessageAsync(
//                chatId: e.Message.Chat,
//                text: text,
//                parseMode: ParseMode.Html);

//}

//private static void SetTimerForRequest()
//{
//    aTimer = new System.Timers.Timer(10000);
//    // Hook up the Elapsed event for the timer. 
//    aTimer.Elapsed += OnTimedEventAsync;
//    aTimer.AutoReset = true;
//    aTimer.Enabled = true;
//}


//private static async void OnTimedEventAsync(Object source, ElapsedEventArgs e)
//{
//    var request = new HttpRequestMessage(HttpMethod.Get, "https://botv320190418040054.azurewebsites.net");
//    HttpClient client = new HttpClient();
//    var response = await client.SendAsync(request);
//    if (response.IsSuccessStatusCode)
//    {
//        HomeController.lastTimerRequest = DateTime.Now;
//    }
//}


