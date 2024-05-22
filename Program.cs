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
using Telegram.Bot.Args;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Numerics;
using System.Threading;
using System.Collections.Concurrent;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace bot
{
    internal class Program
    {
        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;

        //Переменные, использующиеся в программе
        private static string dayAnswer = null;
        private static List<string> times = new List<string> { "1:00", "2:00", "3:00", "4:00", "5:00", "6:00", "7:00", "8:00", "9:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00", "22:00", "23:00", "00:00" };

        //Подключение базы данных и импорт таблиц
        private static MongoClient client = new MongoClient("mongodb://botUserAdmin:password@192.168.0.108:27017/?authSource=admin");
        private static IMongoDatabase database = client.GetDatabase("data");
        private static IMongoCollection<BsonDocument> stirka = database.GetCollection<BsonDocument>("stirka");
        private static IMongoCollection<BsonDocument> uborka = database.GetCollection<BsonDocument>("uborka");
        private static ConcurrentDictionary<string, ObjectId> buttonTextToObjectIdMap = new ConcurrentDictionary<string, ObjectId>();

        //Функция удаления прошедших бронирований из базы данных
        public static void DeletePastRecords()
        {
            var currentDate = DateTime.UtcNow.Date;
            var hour = currentDate.Hour;
            var filter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Lt("date", currentDate), Builders<BsonDocument>.Filter.Lt("time", hour));
            var filter2 = (Builders<BsonDocument>.Filter.Lt("date", currentDate.AddDays(-1)));
            var deleteResultStirka = stirka.DeleteMany(filter);
            var deleteResultUborka = uborka.DeleteMany(filter2);
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            DeletePastRecords(); //Удаляем прошедшие бронирования из базы данных

            //Кнопки дни недели
            KeyboardButton mon = new KeyboardButton("Понедельник");
            KeyboardButton tue = new KeyboardButton("Вторник");
            KeyboardButton wed = new KeyboardButton("Среда");
            KeyboardButton thu = new KeyboardButton("Четверг");
            KeyboardButton fri = new KeyboardButton("Пятница");
            KeyboardButton sat = new KeyboardButton("Суббота");
            KeyboardButton sun = new KeyboardButton("Воскресенье");

            //Кнопки меню
            var menuKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>() { new KeyboardButton[] { new KeyboardButton("Бронирование стирки"), new KeyboardButton("График дежурств") } }) { ResizeKeyboard = true };

            //Кнопки функции бронирования стирки
            var changeKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>() { 
                new KeyboardButton[] { new KeyboardButton("Вывод"), new KeyboardButton("Создание"), new KeyboardButton("Удаление") },
                new KeyboardButton[] { new KeyboardButton("Главное меню") } }) 
            { ResizeKeyboard = true };

            //Кнопки время суток
            var dayKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>() { 
                new KeyboardButton[] { new KeyboardButton("Утро") }, new KeyboardButton[] { new KeyboardButton("День"), new KeyboardButton("Вечер") },
                new KeyboardButton[] { new KeyboardButton("Главное меню") } })
            { ResizeKeyboard = true };

            //Определяем текущее время и генерируем кнопки в соответсвии с ним (чтобы не выводились кнопки того времени, которое уже прошло)
            var currentTime = DateTime.Now.ToLocalTime();
            var hour = currentTime.Hour;

            List<KeyboardButton[]> morningButtons = new List<KeyboardButton[]>();
            List<KeyboardButton[]> dayButtons = new List<KeyboardButton[]>();
            List<KeyboardButton[]> eveningButtons = new List<KeyboardButton[]>();
            List<KeyboardButton[]> dayKeysButtons = new List<KeyboardButton[]>();

            for (int h = 1; h <= 8; h++)
            {
                if (h > hour || hour >= 17)
                {
                    morningButtons.Add(new KeyboardButton[] { new KeyboardButton($"{h}:00") });
                }
            }

            for (int h = 9; h <= 16; h++)
            {
                if (h > hour)
                {
                    dayButtons.Add(new KeyboardButton[] { new KeyboardButton($"{h}:00") });
                }
            }

            for (int h = 17; h <= 24; h++)
            {
                if (h > hour)
                {
                    string displayHour = h == 24 ? "00:00" : $"{h}:00";
                    eveningButtons.Add(new KeyboardButton[] { new KeyboardButton(displayHour) });
                }
            }

            morningButtons.Add(new KeyboardButton[] { new KeyboardButton("Главное меню") });
            dayButtons.Add(new KeyboardButton[] { new KeyboardButton("Главное меню") });
            eveningButtons.Add(new KeyboardButton[] { new KeyboardButton("Главное меню") });

            List<KeyboardButton[]> buttonsToSend = null;
            if (hour >= 1 && hour < 9)
            {
                buttonsToSend = morningButtons;
                dayKeysButtons.Add(new KeyboardButton[] { new KeyboardButton("Утро") });
            }
            else if (hour >= 9 && hour < 17)
            {
                buttonsToSend = dayButtons;
                dayKeysButtons.Add(new KeyboardButton[] { new KeyboardButton("День") });
            }
            else
            {
                buttonsToSend = eveningButtons;
                dayKeysButtons.Add(new KeyboardButton[] { new KeyboardButton("Вечер") });
            }
            dayKeysButtons.Add(new KeyboardButton[] { new KeyboardButton("Главное меню") });
            var keyboardMarkup = new ReplyKeyboardMarkup(buttonsToSend)
            {
                ResizeKeyboard = true
            };
            List<KeyboardButton[]> buttonsDayTosend = dayKeysButtons;
            var keyboardDays = new ReplyKeyboardMarkup(buttonsDayTosend)
            {
                ResizeKeyboard = true
            };

            var morningTimeKeyboard = new ReplyKeyboardMarkup(morningButtons);
            var dayTimeKeyboard = new ReplyKeyboardMarkup(dayButtons);
            var eveningTimeKeyboard = new ReplyKeyboardMarkup(eveningButtons);

            var factmorningTimeKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>() { 
                 new KeyboardButton[] { new KeyboardButton("1:00"), new KeyboardButton("2:00") }, 
                 new KeyboardButton[] { new KeyboardButton("3:00"), new KeyboardButton("4:00") }, 
                 new KeyboardButton[] { new KeyboardButton("5:00"), new KeyboardButton("6:00") }, 
                 new KeyboardButton[] { new KeyboardButton("7:00"), new KeyboardButton("8:00") },
                 new KeyboardButton[] { new KeyboardButton("Главное меню") } })
             { ResizeKeyboard = true };

             var factdayTimeKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>() {
                 new KeyboardButton[] { new KeyboardButton("9:00"), new KeyboardButton("10:00") },
                 new KeyboardButton[] { new KeyboardButton("11:00"), new KeyboardButton("12:00") },
                 new KeyboardButton[] { new KeyboardButton("13:00"), new KeyboardButton("14:00") },
                 new KeyboardButton[] { new KeyboardButton("15:00"), new KeyboardButton("16:00") },
                 new KeyboardButton[] { new KeyboardButton("Главное меню") } })
             { ResizeKeyboard = true };

             var facteveningTimeKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>() {
                 new KeyboardButton[] { new KeyboardButton("17:00"), new KeyboardButton("18:00") },
                 new KeyboardButton[] { new KeyboardButton("19:00"), new KeyboardButton("20:00") },
                 new KeyboardButton[] { new KeyboardButton("21:00"), new KeyboardButton("22:00") },
                 new KeyboardButton[] { new KeyboardButton("23:00"), new KeyboardButton("00:00") },
                 new KeyboardButton[] { new KeyboardButton("Главное меню") } })
             { ResizeKeyboard = true };

            //Определяем дни недели последующих 7 дней и создаем кнопки дней недели
            DateTime date = DateTime.Now;
            DateTime[] nextSevenDays = new DateTime[7];
            List<KeyboardButton> buttons = new List<KeyboardButton>();
            
            for(int i = 0; i < 7; i++)
            {
                nextSevenDays[i] = date.AddDays(i);
            }

            for(int i = 0; i < nextSevenDays.Length; i++)
            {
                if (nextSevenDays[i].DayOfWeek.ToString() == "Monday")
                {
                    buttons.Add(mon);
                }
                if (nextSevenDays[i].DayOfWeek.ToString() == "Tuesday")
                {
                    buttons.Add(tue);
                }
                if (nextSevenDays[i].DayOfWeek.ToString() == "Wednesday")
                {
                    buttons.Add(wed);
                }
                if (nextSevenDays[i].DayOfWeek.ToString() == "Thursday")
                {
                    buttons.Add(thu);
                }
                if (nextSevenDays[i].DayOfWeek.ToString() == "Friday")
                {
                    buttons.Add(fri);
                }
                if (nextSevenDays[i].DayOfWeek.ToString() == "Saturday")
                {
                    buttons.Add(sat);
                }
                if (nextSevenDays[i].DayOfWeek.ToString() == "Sunday")
                {
                    buttons.Add(sun);
                }
            }

            var weekKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>() {
                new KeyboardButton[] { buttons[0] },
                new KeyboardButton[] { buttons[1], buttons[2] },
                new KeyboardButton[] { buttons[3], buttons[4] },
                new KeyboardButton[] { buttons[5], buttons[6] },
                new KeyboardButton[] { new KeyboardButton("Главное меню") } })
            { ResizeKeyboard = true };

            //Определяем день недели текущего дня и переводим на русский для обработки ответов пользователя
            string dayOfWeek = date.DayOfWeek.ToString();
            if(date.DayOfWeek.ToString() == "Monday")
            {
                dayOfWeek = "Понедельник";
            }
            if (date.DayOfWeek.ToString() == "Tuesday")
            {
                dayOfWeek = "Вторник";
            }
            if (date.DayOfWeek.ToString() == "Wednesday")
            {
                dayOfWeek = "Среда";
            }
            if (date.DayOfWeek.ToString() == "Thursday")
            {
                dayOfWeek = "Четверг";
            }
            if (date.DayOfWeek.ToString() == "Friday")
            {
                dayOfWeek = "Пятница";
            }
            if (date.DayOfWeek.ToString() == "Saturday")
            {
                dayOfWeek = "Суббота";
            }
            if (date.DayOfWeek.ToString() == "Sunday")
            {
                dayOfWeek = "Воскресенье";
            }

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
                                        //Если пользователь отправил сообщение /start, выводится главное меню
                                        if (message.Text == "/start")
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите функцию",
                                                replyMarkup: menuKeyboard);

                                            return;
                                        }

                                        //Если пользователь нажал на бронирование стирки, ему выводятся все связанные с ней функции
                                        if (message.Text == "Бронирование стирки")
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Бронирование стиральной машины",
                                                replyMarkup: changeKeyboard); 

                                            return;
                                        }

                                        //Если пользователь нажал на создание, ему выводятся дни недели
                                        if (message.Text == "Создание")
                                        {
                                            await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите день",
                                                replyMarkup: weekKeyboard);

                                            return;
                                        }

                                        //Если пользователь выбрал день недели, ему выводится время
                                        if (message.Text == "Понедельник" || message.Text == "Вторник" || message.Text == "Среда" || message.Text == "Четверг" || message.Text == "Пятница" || message.Text == "Суббота" || message.Text == "Воскресенье")
                                        {
                                            dayAnswer = message.Text; //Проверяем, равняется ли ответ текущему дню недели

                                            //Если равняется, выводится только доступное время
                                            if (dayAnswer == dayOfWeek)
                                            {
                                                await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время суток",
                                                replyMarkup: keyboardDays);
                                                dayAnswer = message.Text;
                                            }
                                            //Если нет, выводится все время с 1:00 до 00:00
                                            else
                                            {
                                                await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время суток",
                                                replyMarkup: dayKeyboard);
                                                dayAnswer = message.Text; 
                                            }
                                            return;
                                        }

                                        if (message.Text == "Утро")
                                        {
                                            //Если равняется, выводится только доступное время
                                            if (dayAnswer == dayOfWeek)
                                            {
                                                await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время",
                                                replyMarkup: morningTimeKeyboard);
                                                return;
                                            }
                                            //Если нет, выводится все утреннее время с 1:00 до 8:00
                                            else
                                            {
                                                await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время",
                                                replyMarkup: factmorningTimeKeyboard);
                                                return;
                                            }
                                        }

                                        if (message.Text == "День")
                                        {
                                            //Если равняется, выводится только доступное время
                                            if (dayAnswer == dayOfWeek)
                                            {
                                                await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время",
                                                replyMarkup: dayTimeKeyboard);
                                                return;
                                            }
                                            //Если нет, выводится все дневное время с 9:00 до 16:00
                                            else
                                            {
                                                await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время",
                                                replyMarkup: factdayTimeKeyboard);
                                                return;
                                            }
                                        }

                                        if (message.Text == "Вечер")
                                        {
                                            //Если равняется, выводится только доступное время
                                            if (dayAnswer == dayOfWeek)
                                            {
                                                await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время",
                                                replyMarkup: eveningTimeKeyboard);
                                                return;
                                            }
                                            //Если нет, выводится все вечернее время с 17:00 до 00:00
                                            else
                                            {
                                                await botClient.SendTextMessageAsync(
                                                chat.Id,
                                                "Выберите время",
                                                replyMarkup: facteveningTimeKeyboard);
                                                return;
                                            }
                                        }

                                        //Если пользователь выбрал время, вносим запись в базу данных
                                        if (times.Contains(message.Text))
                                        {
                                            var sender = message.From;
                                            if (dayAnswer == "Понедельник")
                                            {
                                                for (int i = 0; i < nextSevenDays.Length; i++)
                                                {
                                                    if (nextSevenDays[i].DayOfWeek.ToString() == "Monday")
                                                    {
                                                        var filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("time", message.Text),
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date));

                                                        var user_filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id)
                                                        );

                                                        var userRecordsOnDate = await stirka.CountDocumentsAsync(user_filter);
                                                        var existingRecord = await stirka.Find(filter).FirstOrDefaultAsync();

                                                        if (userRecordsOnDate >= 2)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Вы можете делать не более двух записей в день");
                                                        }
                                                        else if (existingRecord != null)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Это время уже забронировано");
                                                        }
                                                        else
                                                        {
                                                            BsonDocument stirkaData = new BsonDocument
                                                            {
                                                                { "time", message.Text},
                                                                { "date",  nextSevenDays[i].Date },
                                                                { "id", sender.Id },
                                                                { "name", sender.FirstName }
                                                            };
                                                            await stirka.InsertOneAsync(stirkaData);
                                                            await botClient.SendTextMessageAsync(chat.Id, "Запись успешно сделана!");
                                                        }
                                                    }
                                                }
                                            }

                                            if (dayAnswer == "Вторник")
                                            {
                                                for (int i = 0; i < nextSevenDays.Length; i++)
                                                {
                                                    if (nextSevenDays[i].DayOfWeek.ToString() == "Tuesday")
                                                    {
                                                        var filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("time", message.Text),
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id));

                                                        var user_filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id)
                                                        );

                                                        var userRecordsOnDate = await stirka.CountDocumentsAsync(user_filter);
                                                        var existingRecord = await stirka.Find(filter).FirstOrDefaultAsync();

                                                        if (userRecordsOnDate >= 2)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Вы можете делать не более двух записей в день");
                                                        }
                                                        else if (existingRecord != null)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Это время уже забронировано");
                                                        }
                                                        else
                                                        {
                                                            BsonDocument stirkaData = new BsonDocument
                                                            {
                                                                { "time", message.Text},
                                                                { "date",  nextSevenDays[i].Date },
                                                                { "id", sender.Id },
                                                                { "name", sender.FirstName }
                                                            };
                                                            await stirka.InsertOneAsync(stirkaData);
                                                            await botClient.SendTextMessageAsync(chat.Id, "Запись успешно сделана!");
                                                        }
                                                    }
                                                }
                                            }

                                            if (dayAnswer == "Среда")
                                            {
                                                for (int i = 0; i < nextSevenDays.Length; i++)
                                                {
                                                    if (nextSevenDays[i].DayOfWeek.ToString() == "Wednesday")
                                                    {
                                                        var filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("time", message.Text),
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id));

                                                        var user_filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id)
                                                        );

                                                        var userRecordsOnDate = await stirka.CountDocumentsAsync(user_filter);
                                                        var existingRecord = await stirka.Find(filter).FirstOrDefaultAsync();

                                                        if (userRecordsOnDate >= 2)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Вы можете делать не более двух записей в день");
                                                        }
                                                        else if (existingRecord != null)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Это время уже забронировано");
                                                        }
                                                        else
                                                        {
                                                            BsonDocument stirkaData = new BsonDocument
                                                            {
                                                                { "time", message.Text},
                                                                { "date",  nextSevenDays[i].Date },
                                                                { "id", sender.Id },
                                                                { "name", sender.FirstName }
                                                            };
                                                            await stirka.InsertOneAsync(stirkaData);
                                                            await botClient.SendTextMessageAsync(chat.Id, "Запись успешно сделана!");
                                                        }
                                                    }
                                                }
                                            }

                                            if (dayAnswer == "Четверг")
                                            {
                                                for (int i = 0; i < nextSevenDays.Length; i++)
                                                {
                                                    if (nextSevenDays[i].DayOfWeek.ToString() == "Thursday")
                                                    {
                                                        var filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("time", message.Text),
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id));

                                                        var user_filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id)
                                                        );

                                                        var userRecordsOnDate = await stirka.CountDocumentsAsync(user_filter);
                                                        var existingRecord = await stirka.Find(filter).FirstOrDefaultAsync();

                                                        if (userRecordsOnDate >= 2)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Вы можете делать не более двух записей в день");
                                                        }
                                                        else if (existingRecord != null)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Это время уже забронировано");
                                                        }
                                                        else
                                                        {
                                                            BsonDocument stirkaData = new BsonDocument
                                                            {
                                                                { "time", message.Text},
                                                                { "date",  nextSevenDays[i].Date },
                                                                { "id", sender.Id },
                                                                { "name", sender.FirstName }
                                                            };
                                                            await stirka.InsertOneAsync(stirkaData);
                                                            await botClient.SendTextMessageAsync(chat.Id, "Запись успешно сделана!");
                                                        }
                                                    }
                                                }
                                            }

                                            if (dayAnswer == "Пятница")
                                            {
                                                for (int i = 0; i < nextSevenDays.Length; i++)
                                                {
                                                    if (nextSevenDays[i].DayOfWeek.ToString() == "Friday")
                                                    {
                                                        var filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("time", message.Text),
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id));

                                                        var user_filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id)
                                                        );

                                                        var userRecordsOnDate = await stirka.CountDocumentsAsync(user_filter);
                                                        var existingRecord = await stirka.Find(filter).FirstOrDefaultAsync();

                                                        if (userRecordsOnDate >= 2)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Вы можете делать не более двух записей в день");
                                                        }
                                                        else if (existingRecord != null)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Это время уже забронировано");
                                                        }
                                                        else
                                                        {
                                                            BsonDocument stirkaData = new BsonDocument
                                                            {
                                                                { "time", message.Text},
                                                                { "date",  nextSevenDays[i].Date },
                                                                { "id", sender.Id },
                                                                { "name", sender.FirstName }
                                                            };
                                                            await stirka.InsertOneAsync(stirkaData);
                                                            await botClient.SendTextMessageAsync(chat.Id, "Запись успешно сделана!");
                                                        }
                                                    }
                                                }
                                            }

                                            if (dayAnswer == "Суббота")
                                            {
                                                for (int i = 0; i < nextSevenDays.Length; i++)
                                                {
                                                    if (nextSevenDays[i].DayOfWeek.ToString() == "Saturday")
                                                    {
                                                        var filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("time", message.Text),
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id));

                                                        var user_filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id)
                                                        );

                                                        var userRecordsOnDate = await stirka.CountDocumentsAsync(user_filter);
                                                        var existingRecord = await stirka.Find(filter).FirstOrDefaultAsync();

                                                        if (userRecordsOnDate >= 2)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Вы можете делать не более двух записей в день");
                                                        }
                                                        else if (existingRecord != null)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Это время уже забронировано");
                                                        }
                                                        else
                                                        {
                                                            BsonDocument stirkaData = new BsonDocument
                                                            {
                                                                { "time", message.Text},
                                                                { "date",  nextSevenDays[i].Date },
                                                                { "id", sender.Id },
                                                                { "name", sender.FirstName }
                                                            };
                                                            await stirka.InsertOneAsync(stirkaData);
                                                            await botClient.SendTextMessageAsync(chat.Id, "Запись успешно сделана!");
                                                        }
                                                    }
                                                }
                                            }

                                            if (dayAnswer == "Воскресенье")
                                            {
                                                for (int i = 0; i < nextSevenDays.Length; i++)
                                                {
                                                    if (nextSevenDays[i].DayOfWeek.ToString() == "Sunday")
                                                    {
                                                        var filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("time", message.Text),
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id));

                                                        var user_filter = Builders<BsonDocument>.Filter.And(
                                                            Builders<BsonDocument>.Filter.Eq("date", nextSevenDays[i].Date),
                                                            Builders<BsonDocument>.Filter.Eq("id", sender.Id)
                                                        );

                                                        var userRecordsOnDate = await stirka.CountDocumentsAsync(user_filter);
                                                        var existingRecord = await stirka.Find(filter).FirstOrDefaultAsync();

                                                        if (userRecordsOnDate >= 2)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Вы можете делать не более двух записей в день");
                                                        }
                                                        else if (existingRecord != null)
                                                        {
                                                            await botClient.SendTextMessageAsync(chat.Id, "Это время уже забронировано");
                                                        }
                                                        else
                                                        {
                                                            BsonDocument stirkaData = new BsonDocument
                                                            {
                                                                { "time", message.Text},
                                                                { "date",  nextSevenDays[i].Date },
                                                                { "id", sender.Id },
                                                                { "name", sender.FirstName }
                                                            };
                                                            await stirka.InsertOneAsync(stirkaData);
                                                            await botClient.SendTextMessageAsync(chat.Id, "Запись успешно сделана");
                                                        }
                                                    }
                                                }
                                            }
                                            return;
                                        }

                                        //Если пользователь выбрал график дежурств, ему выводится содержимое базы данных графика дежурств (по возрастанию даты)
                                        if (message.Text == "График дежурств")
                                        {
                                            var filter = new BsonDocument();
                                            var sortDefinition = Builders<BsonDocument>.Sort.Ascending("date");
                                            var documents = await uborka.Find(filter).Sort(sortDefinition).ToListAsync();

                                            foreach (var document in documents)
                                            {
                                                string dateTime = document["date"].ToLocalTime().ToString("dd.MM");
                                                string roomb = document["room"].AsString;
                                                await botClient.SendTextMessageAsync(chat.Id, "Дата: " + dateTime + "\n" + "Комната: " + roomb);
                                            }
                                            return;
                                        }

                                        //Если пользователь выбрал удаление, ему выводятся все его бронирования
                                        if (message.Text == "Удаление")
                                        {
                                            var senderId = message.From.Id;
                                            var filter = Builders<BsonDocument>.Filter.Eq("id", senderId);
                                            var userRecords = await stirka.Find(filter).ToListAsync();

                                            if (userRecords.Count == 0)
                                            {
                                                await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: "У вас нет активных бронирований");
                                                return;
                                            }

                                            var keyboardButtons = userRecords.Select(record => {
                                                var buttonText = $"{record["date"].ToLocalTime().ToString("dd.MM")} в {record["time"]}";
                                                buttonTextToObjectIdMap[buttonText] = record["_id"].AsObjectId;
                                                return new KeyboardButton(buttonText);
                                            }).ToArray();

                                            var replyKeyboard = new ReplyKeyboardMarkup(new List<KeyboardButton[]>() { keyboardButtons, new KeyboardButton[] { new KeyboardButton("Главное меню") } })
                                            {
                                                ResizeKeyboard = true,
                                                OneTimeKeyboard = true
                                            };

                                            await botClient.SendTextMessageAsync(
                                                chatId: message.Chat.Id,
                                                text: "Выберите бронирование для удаления:",
                                                replyMarkup: replyKeyboard);
                                            return;
                                        }

                                        //Если пользователь выбрал бронирование, то происходит его удаление
                                        if (buttonTextToObjectIdMap.ContainsKey(message.Text))
                                        {
                                            var callbackData = message.Text; 
                                            if (buttonTextToObjectIdMap.TryGetValue(callbackData, out var recordId))
                                            {
                                                var filter = Builders<BsonDocument>.Filter.Eq("_id", recordId);
                                                var result = await stirka.DeleteOneAsync(filter);

                                                if (result.DeletedCount > 0)
                                                {
                                                    await botClient.SendTextMessageAsync(message.Chat.Id, "Запись успешно удалена", replyMarkup: menuKeyboard);
                                                    buttonTextToObjectIdMap.TryRemove(callbackData, out _);
                                                }
                                                else
                                                {
                                                    await botClient.SendTextMessageAsync(message.Chat.Id, "Не удалось найти запись для удаления");
                                                }
                                            }
                                            else
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat.Id, "Некорректные данные для удаления");
                                            }
                                        }

                                        //Если пользователь выбрал вывод, то выводится содержимое базы данных графика стирки
                                        if (message.Text == "Вывод")
                                        {
                                            var documents = stirka.Find(new BsonDocument()).ToList();

                                            if(documents.Count == 0)
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat.Id, "На ближайшую неделю нет записей");
                                            }
                                            else
                                            {
                                                foreach (var document in documents)
                                                {
                                                    string dateTime = document["date"].ToLocalTime().ToString("dd.MM");
                                                    string time = document["time"].ToString();
                                                    string name = document["name"].ToString();
                                                    await botClient.SendTextMessageAsync(chat.Id, "Имя: " + name + "\n" + "Дата: " + dateTime + "\n" + "Время: " + time);
                                                }
                                            }
                                            return;
                                        }

                                        //Если пользователь выбрал главное меню, то выводится главное меню
                                        if (message.Text == "Главное меню")
                                        {
                                            await botClient.SendTextMessageAsync(chat.Id, "Выберите функцию", replyMarkup: menuKeyboard);
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
            using var cts = new CancellationTokenSource();
            var _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };

            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"{me.FirstName} запущен!");

            await Task.Delay(-1);
        }
    }
}
