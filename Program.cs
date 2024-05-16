using System;
using System.Text;
using System.Data;
using System.Data.Common;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
using MongoDB.Driver;
using MongoDB.Bson;

namespace bot
{
    internal class Program
    {
        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;
  
        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            MongoClient client = new MongoClient("mongodb://botUserAdmin:password@192.168.0.108:27017/?authSource=admin");

            try
            {
                var result = client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
            }
            catch (Exception ex) { Console.WriteLine(ex); }

            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            var message = update.Message;

                            var user = message.From;

                            Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");

                            var chat = message.Chat;

                            switch (message.Type)
                            {
                                case MessageType.Text:
                                    {
                                        if (message.Text == "/start")
                                        {

                                            var replyKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>()
                                                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Бронирование стирки"),
                                            new KeyboardButton("График дежурств")
                                        }
                                        })
                                            {
                                                ResizeKeyboard = true,
                                            };

                                            using (var cursor = await client.ListDatabasesAsync())
                                            {
                                                var databaseNames = cursor.ToList();
                                                foreach (string databaseName in databaseNames)
                                                {
                                                    await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                databaseName,
                                                replyMarkup: replyKeyboard);
                                                }
                                            }

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите функцию",
                                                replyMarkup: replyKeyboard);

                                            return;
                                        }

                                        if (message.Text == "Бронирование стирки")
                                        {
                                            var replyKeyboard = new ReplyKeyboardMarkup(
                                                new List<KeyboardButton[]>()
                                                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Создание"),
                                            new KeyboardButton("Изменение"),
                                            new KeyboardButton("Удаление")
                                        }
                                        })
                                            {
                                                ResizeKeyboard = true,
                                            };

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Бронирование стиральной машины",
                                                replyMarkup: replyKeyboard); 

                                            return;
                                        }

                                        if (message.Text == "Создание")
                                        {
                                            var replyKeyboard = new ReplyKeyboardMarkup(
                                                new List<KeyboardButton[]>()
                                                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Понедельник"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Вторник"),
                                            new KeyboardButton("Среда"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Сетверг"),
                                            new KeyboardButton("Пятница"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Суббота"),
                                            new KeyboardButton("Воскресенье")
                                        }
                                        })
                                            {
                                                ResizeKeyboard = true,
                                            };

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите день",
                                                replyMarkup: replyKeyboard);

                                            return;
                                        }

                                        if (message.Text == "Понедельник" || message.Text == "Вторник" || message.Text == "Среда" || message.Text == "Четверг" || message.Text == "Пятница" || message.Text == "Суббота" || message.Text == "Воскресенье")
                                        {
                                            var replyKeyboard = new ReplyKeyboardMarkup(
                                                new List<KeyboardButton[]>()
                                                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Утро"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("День"),
                                            new KeyboardButton("Вечер"),
                                        }
                                        })
                                            {
                                                ResizeKeyboard = true,
                                            };

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время суток",
                                                replyMarkup: replyKeyboard);

                                            return;
                                        }

                                        if (message.Text == "Утро")
                                        {
                                            var replyKeyboard = new ReplyKeyboardMarkup(
                                                new List<KeyboardButton[]>()
                                                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("1:00"),
                                            new KeyboardButton("2:00"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("3:00"),
                                            new KeyboardButton("4:00"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("5:00"),
                                            new KeyboardButton("6:00"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("7:00"),
                                            new KeyboardButton("8:00"),
                                        }
                                        })
                                            {
                                                ResizeKeyboard = true,
                                            };

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время",
                                                replyMarkup: replyKeyboard);

                                            return;
                                        }

                                        if (message.Text == "График дежурств")
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Понедельник: 300 \n" +
                                                "Вторник: 301 \n" +
                                                "Среда: 307 \n" +
                                                "Четверг: 302 \n"+
                                                "Пятница: 303 \n" +
                                                "Суббота: 304 \n" +
                                                "Воскресенье: 305 \n"
                                                );
                                            return;
                                        }

                                        if (message.Text == "Изменение")
                                        {
                                            var replyKeyboard = new ReplyKeyboardMarkup(
                                                new List<KeyboardButton[]>()
                                                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Понедельник"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Вторник"),
                                            new KeyboardButton("Среда"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Сетверг"),
                                            new KeyboardButton("Пятница"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Суббота"),
                                            new KeyboardButton("Воскресенье")
                                        }
                                        })
                                            {
                                                ResizeKeyboard = true,
                                            };

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите день",
                                                replyMarkup: replyKeyboard);

                                            return;
                                        }

                                        if (message.Text == "Удаление")
                                        {

                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Бронирование успешно удалено!");

                                            return;
                                        }
                                        return;
                                    }
                            }
                            return;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",_ => error.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }


        static async Task Main()
        {

            _botClient = new TelegramBotClient("6943456714:AAEehGSa1zFNTPSLNNC4kzcCqJiOIRgSsdc");
            _receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }, ThrowPendingUpdates = true };
            using var cts = new CancellationTokenSource();

            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"{me.FirstName} запущен!");

            await Task.Delay(-1);
        }
    }
}
