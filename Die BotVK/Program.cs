using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using VkNet.Exception;
using System.Net;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace Die_BotVK
{
    public class Program
    {
        //Классы программы
        static Authorization authorization = new Authorization();
        static ColorConsole colorConsole = new ColorConsole();
        //Компоненты для работы программы
        public static VkApi vkApi = new VkApi();
        static WebClient Web = new WebClient();
        //Данные
        public static long userID = 0;
        public static ulong? Ts;
        public static ulong? Pts;
        public static bool IsActive;
        public static Timer WatchTimer = null;
        public static byte MaxSleepSteps = 3;
        public static int StepSleepTime = 333;
        public static byte CurrentSleepSteps = 1;
        public static string lentaru = "https://lenta.ru/rss/top7";
        public static string meduza = "https://meduza.io/rss/all";
        public static List<string> exceptionList = new List<string>();
        public static List<string> BadWords = new List<string>();
        public static List<string> DefaultAnswer = new List<string>();

        public static string[] tempException;
        public static string[] tempBadWords;
        public static string[] tempDefaultAnswer;

        delegate void MessagesRecievedDelegate(VkApi owner, ReadOnlyCollection<Message> messages);
        static event MessagesRecievedDelegate NewMessages;

        static void Main(string[] args)
        {
            ulong ID = 0;
            bool DoubleAuth = false;
            bool DebugMode = false;

            Console.Title = "Die Bot Vk";
            colorConsole.ColorWriteLine("Die Bot VK OpenSource", ConsoleColor.White, ConsoleColor.White);
            colorConsole.ColorWriteLine("--------------------------", ConsoleColor.White, ConsoleColor.White);
            while (true)
            {
                //Ввод данных для авторизации
                colorConsole.ColorWriteLine("DieBot:/>Введите Логин:", ConsoleColor.White, ConsoleColor.White);
                string Login = Console.ReadLine();
                colorConsole.ColorWriteLine("DieBot:/>Введите Пароль:", ConsoleColor.White, ConsoleColor.White);
                string Password = string.Empty;
                ConsoleKeyInfo key;
                do
                {
                    key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter) break;
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (Password.Length != 0)
                        {
                            Password = Password.Remove(Password.Length - 1);
                            Console.Write("\b \b");
                        }
                    }
                    else
                    {
                        Password += key.KeyChar;
                        Console.Write("*");
                    }
                }
                while (true);
                //Конец ввода данных для авторизации
                Console.WriteLine("");
                //Проверка appID для авторизации
                colorConsole.ColorWriteLine("Введите appID, если вы не знаете что это, то впишите (1):", ConsoleColor.White, ConsoleColor.White);
                string appID = Console.ReadLine();
                if (appID != "1")
                {
                    try
                    {
                        ID = ulong.Parse(appID);
                    }
                    catch (Exception e)
                    {
                        colorConsole.ColorWriteLine(e.Message, ConsoleColor.Red, ConsoleColor.White);
                    }
                }
                else
                {
                    ID = 6702419;
                }
                //Конец проверки appID для авторизации

                //Проверка на двуфакторную ауентификацию
                colorConsole.ColorWriteLine("У вас включена двуфакторная ауентификация? (y/n):", ConsoleColor.White, ConsoleColor.White);
                string DoubleHash = Console.ReadLine();
                if (DoubleHash.ToLower() == "y")
                {
                    DoubleAuth = true;
                    authorization.DoubleCode();
                }
                else if (DoubleHash.ToLower() == "n") { } else return;
                //Конец проверки на двуфакторную ауентификацию

                //Запускаем цикл авторизаций, на случай если плохое соединение
                for (int CountAuth = 1; CountAuth < 4; CountAuth++)
                {
                    colorConsole.ColorWriteLine("DieBot:/>Попытка авторизироватьcя (" + CountAuth + ")", ConsoleColor.Yellow, ConsoleColor.White);
                    if (authorization.Auth(Login, Password, ID, DoubleAuth))
                    {
                        colorConsole.ColorWriteLine("DieBot:/>Авторизация прошла успешно", ConsoleColor.Green, ConsoleColor.White);

                        // Get You ID
                        ReadOnlyCollection<User> MyID = vkApi.Users.Get(new long[] { Convert.ToInt64(vkApi.UserId.Value) });
                        colorConsole.ColorWriteLine("DieBot:/>Вы авторизованы под пользователем " + MyID[0].FirstName + " " + MyID[0].LastName, ConsoleColor.Green, ConsoleColor.White);
                        // End Get You ID

                        //Проверяем проверку на лист исключений
                        GetException(MyID[0].Id);
                        //Конец проверки листа исключений

                        LoadList(MyID[0].Id);

                        // Получаем список друзей
                        var Friends = GetFriends();
                        colorConsole.ColorWriteLine("DieBot:/>Список друзей получен", ConsoleColor.Green, ConsoleColor.White);
                        colorConsole.ColorWriteLine("DieBot:/>Колличество друзей: " + Friends.Count, ConsoleColor.Green, ConsoleColor.White);
                        // Конец получения списка друзей

                        Console.WriteLine("--------------------------");
                        Console.WriteLine("DieBot:/>Введите команду боту:");
                        bool EndWork = false;
                        while (!EndWork)
                        {
                            string command = Console.ReadLine();
                            switch (command.ToLower())
                            {
                                case "getfriends":
                                    {
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Список друзей:");
                                        for (int i = 0; i < Friends.Count; i++)
                                        {
                                            int k = i;
                                            k++;
                                            Console.WriteLine(k + ") " + Friends[i].FirstName + " " + Friends[i].LastName + " ID " + Friends[i].Id);
                                        }
                                        Console.WriteLine("DieBot:/>Конец списка.");
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "checkfriend":
                                    {
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Введите номер друга:");
                                        int numberFriends = Int32.Parse(Console.ReadLine());

                                        numberFriends--;

                                        while (numberFriends > Friends.Count)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine("DieBot:/>Не верный номер!");
                                            Console.ForegroundColor = ConsoleColor.White;
                                        }

                                        if (numberFriends < 0)
                                        {
                                            Console.WriteLine("Вы выбрали своего лучшего друга и одновременно злейшего врага: Самого себя!");
                                        }
                                        else
                                        {
                                            Console.WriteLine(Friends[numberFriends].FirstName + " " + Friends[numberFriends].LastName);
                                            CheckFriend(numberFriends);
                                        }

                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "exit":
                                    {
                                        Environment.Exit(0);
                                    }
                                    break;

                                case "searchmessage":
                                    {
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Введите номер друга:");
                                        int tempID = Int32.Parse(Console.ReadLine());
                                        tempID--;
                                        Console.WriteLine("DieBot:/>Введите сообщение для поиска:");
                                        string tempText = Console.ReadLine();
                                        Console.WriteLine("--------------------------");
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine("DieBot:/>Начат поиск сообщений c пользователем " + Friends[tempID].FirstName + " " + Friends[tempID].LastName);
                                        Console.ForegroundColor = ConsoleColor.White;
                                        SearchMessage(Friends[tempID].Id, tempText);
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "spamattack":
                                    {
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Начать спам аттаку по одному другу или по всем? y(Один)/n(Все):");
                                        string spamAnswer = Console.ReadLine();
                                        Console.WriteLine("DieBot:/>Введите текст спама:");
                                        string spamText = Console.ReadLine();
                                        if (spamAnswer.ToLower() == "y")
                                        {
                                            Console.WriteLine("DieBot:/>Спам одним сообщением? y/n:");
                                            string oneSpawn = Console.ReadLine();
                                            if (oneSpawn.ToLower() == "y")
                                            {
                                                SpamAttack(true, true, spamText);
                                            }
                                            else if (oneSpawn.ToLower() == "n")
                                            {
                                                SpamAttack(true, false, spamText);
                                            }
                                        }
                                        else if (spamAnswer.ToLower() == "n")
                                        {
                                            Console.WriteLine("DieBot:/>Спам одним сообщением? y/n:");
                                            string oneSpawn = Console.ReadLine();
                                            if (oneSpawn.ToLower() == "y")
                                            {
                                                SpamAttack(false, true, spamText);
                                            }
                                            else if (oneSpawn.ToLower() == "n")
                                            {
                                                SpamAttack(false, false, spamText);
                                            }
                                        }
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "eyemod":
                                    {
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Magenta, ConsoleColor.White);
                                        colorConsole.ColorWriteLine("DieBot:/>Мод слежки включен", ConsoleColor.Magenta, ConsoleColor.White);
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Magenta, ConsoleColor.White);
                                        Eye(DebugMode);
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "stopeyemod":
                                    {
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Magenta, ConsoleColor.White);
                                        colorConsole.ColorWriteLine("DieBot:/>Мод слежки выключен", ConsoleColor.Magenta, ConsoleColor.White);
                                        Stop();
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Magenta, ConsoleColor.White);
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "debug":
                                    {
                                        switch (DebugMode)
                                        {
                                            case true:
                                                {
                                                    colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Cyan, ConsoleColor.White);
                                                    colorConsole.ColorWriteLine("DieBot:/>Вы хотите выключть DebugMode? (y/n):", ConsoleColor.Cyan, ConsoleColor.White);
                                                    string tempString = Console.ReadLine();
                                                    switch (tempString)
                                                    {
                                                        case "y":
                                                            {
                                                                colorConsole.ColorWriteLine("DieBot:/>DebugMode выключен", ConsoleColor.Cyan, ConsoleColor.White);
                                                                DebugMode = false;
                                                                colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Cyan, ConsoleColor.White);
                                                            }
                                                            break;

                                                        case "n":
                                                            {
                                                                colorConsole.ColorWriteLine("DieBot:/>DebugMode выключен", ConsoleColor.Cyan, ConsoleColor.White);
                                                                colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Cyan, ConsoleColor.White);
                                                            }
                                                            break;
                                                    }
                                                }
                                                break;

                                            case false:
                                                {
                                                    colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Cyan, ConsoleColor.White);
                                                    colorConsole.ColorWriteLine("DieBot:/>Вы хотите включить DebugMode? (y/n):", ConsoleColor.Cyan, ConsoleColor.White);
                                                    string tempString = Console.ReadLine();
                                                    switch (tempString)
                                                    {
                                                        case "y":
                                                            {
                                                                colorConsole.ColorWriteLine("DieBot:/>DebugMode включен", ConsoleColor.Cyan, ConsoleColor.White);
                                                                DebugMode = true;
                                                                colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Cyan, ConsoleColor.White);
                                                            }
                                                            break;

                                                        case "n":
                                                            {
                                                                colorConsole.ColorWriteLine("DieBot:/>DebugMode выключен", ConsoleColor.Cyan, ConsoleColor.White);
                                                                colorConsole.ColorWriteLine("--------------------------", ConsoleColor.Cyan, ConsoleColor.White);
                                                            }
                                                            break;
                                                    }
                                                }
                                                break;
                                        }

                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "reloadexception":
                                    {
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.White, ConsoleColor.White);
                                        colorConsole.ColorWriteLine("DieBot:/>Загрузка листа исключений", ConsoleColor.White, ConsoleColor.White);
                                        GetException(MyID[0].Id);
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.White, ConsoleColor.White);
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "addbadword":
                                    {
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.White, ConsoleColor.White);
                                        colorConsole.ColorWriteLine("DieBot:/>Введите слово:", ConsoleColor.White, ConsoleColor.White);
                                        string tepmBadWord = Console.ReadLine();
                                        addbadword(MyID[0].Id, tepmBadWord);
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.White, ConsoleColor.White);
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "addexception":
                                    {
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.White, ConsoleColor.White);
                                        colorConsole.ColorWriteLine("DieBot:/>Введите id (только цифры):", ConsoleColor.White, ConsoleColor.White);
                                        string tempId = Console.ReadLine();
                                        addexception(MyID[0].Id, tempId);
                                        colorConsole.ColorWriteLine("--------------------------", ConsoleColor.White, ConsoleColor.White);
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                case "help":
                                    {
                                        Console.WriteLine("--------------------------");
                                        Console.ForegroundColor = ConsoleColor.Blue;
                                        Console.WriteLine("DieBot:/>Help -------------------> Выводит список всех команд");
                                        Console.WriteLine("DieBot:/>GetFriends -------------------> Выводит список всех ваших друзей по номерам, которые нужны для остальных команд");
                                        Console.WriteLine("DieBot:/>CheckFriend -------------------> Выводит информацию о друге");
                                        Console.WriteLine("DieBot:/>SearchMessage -------------------> Ищет совпадения запроса в переписке с пользователем");
                                        Console.WriteLine("DieBot:/>SpamAttack -------------------> Начинает Спам Атаку (Не рекомендуется к использованию)");
                                        Console.WriteLine("DieBot:/>EyeMod -------------------> Запускается отслеживание всех приходящих сообщений");
                                        Console.WriteLine("DieBot:/>StopEyeMod -------------------> Останавливает отслеживание всех приходящих сообщений");
                                        Console.WriteLine("DieBot:/>Debug -------------------> Включает Debug Log (Обычному пользователю без надобности)");
                                        Console.WriteLine("DieBot:/>ReLoadException -------------------> Перезагружает список исключений");
                                        Console.WriteLine("DieBot:/>AddBadWord -------------------> Добавляет плохое слово в память бота");
                                        Console.WriteLine("DieBot:/>AddException -------------------> Добавляет плохое слово в память бота");
                                        Console.WriteLine("DieBot:/>Exit -------------------> Прекращает работу программы");
                                        Console.ForegroundColor = ConsoleColor.White;
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;

                                default:
                                    {
                                        Console.WriteLine("--------------------------");
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("DieBot:/>Такой команды не найдено!");
                                        Console.ForegroundColor = ConsoleColor.White;
                                        Console.WriteLine("--------------------------");
                                        Console.WriteLine("DieBot:/>Введите команду боту:");
                                    }
                                    break;
                            }

                        }
                        break;
                    }
                    else if (!authorization.Auth(args[0], args[1], ID, DoubleAuth))
                    {
                        if (CountAuth == 3)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("DieBot:/>Ошибка в авторизации");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("--------------------------");
                        }
                    }
                }
            }
        }

        public static VkCollection<User> GetFriends()
        {
            VkCollection<User> Friends = vkApi.Friends.Get(new FriendsGetParams
            {
                UserId = vkApi.UserId,
                Fields = ProfileFields.FirstName | ProfileFields.LastName | ProfileFields.Sex | ProfileFields.Status | ProfileFields.BirthDate | ProfileFields.Timezone | ProfileFields.City,
                Order = FriendsOrder.Name
            });
            return Friends;
        }

        public static void Command(long CheckedUserID, string Body)
        {
            switch (CheckBadWords(Body.ToLower()))
            {
                case true: SendMessage(CheckedUserID, "{Die Bot Vk} Употребление не нормативной лексики не хорошо!"); break;

                case false:
                    {
                        bool resultHello = Regex.IsMatch(Body.ToLower(), "\\bпривет\\b");
                        bool resultNews = Regex.IsMatch(Body.ToLower(), "\\bновости\\b");
                        bool resultHowAreYou = Regex.IsMatch(Body.ToLower(), "\\bкак дела\\b");
                        bool resultHowAreYou2 = Regex.IsMatch(Body.ToLower(), "\\bкак дела\\b");
                        bool resultNormal1 = Regex.IsMatch(Body.ToLower(), "\\bнормально\\b");
                        bool resultNormal2 = Regex.IsMatch(Body.ToLower(), "\\bнорм\\b");
                        bool resultNormal3 = Regex.IsMatch(Body.ToLower(), "\\bнормуль\\b");
                        bool resultName1 = Regex.IsMatch(Body.ToLower(), "\\bданил\\b");
                        bool resultName2 = Regex.IsMatch(Body.ToLower(), "\\bданя\\b");
                        bool resultLearn = Regex.IsMatch(Body.ToLower(), "\\bучиbot\\b");

                        if (resultHello)
                        {
                            Random random = new Random();
                            int rnumbe = random.Next(0, 3);
                            switch (rnumbe)
                            {
                                case 0:
                                    {
                                        SendMessage(CheckedUserID, "Привет");
                                    }
                                    break;

                                case 1:
                                    {
                                        SendMessage(CheckedUserID, "Хай");
                                    }
                                    break;

                                case 2:
                                    {
                                        SendMessage(CheckedUserID, "Hello");
                                    }
                                    break;
                            }
                        }
                        else if ((resultHello && resultHowAreYou) || (resultHello && resultHowAreYou2))
                        {
                            Random random = new Random();
                            int rnumbe = random.Next(0, 5);
                            SendMessage(CheckedUserID, "Привет");
                            switch (rnumbe)
                            {
                                case 0:
                                    {
                                        SendMessage(CheckedUserID, "Нормально, ты как?");
                                    }
                                    break;

                                case 1:
                                    {
                                        SendMessage(CheckedUserID, "Норм");
                                    }
                                    break;

                                case 2:
                                    {
                                        SendMessage(CheckedUserID, "Нормально");
                                    }
                                    break;

                                case 3:
                                    {
                                        SendMessage(CheckedUserID, "Норм");
                                        SendMessage(CheckedUserID, "Ты как?");
                                    }
                                    break;

                                case 4:
                                    {
                                        SendMessage(CheckedUserID, "Нормально");
                                        SendMessage(CheckedUserID, "Ты как?");
                                    }
                                    break;
                            }
                        }
                        else if (resultHowAreYou || resultHowAreYou2)
                        {
                            Random random = new Random();
                            int rnumbe = random.Next(0, 5);
                            switch (rnumbe)
                            {
                                case 0:
                                    {
                                        SendMessage(CheckedUserID, "Нормально, ты как?");
                                    }
                                    break;

                                case 1:
                                    {
                                        SendMessage(CheckedUserID, "Норм");
                                    }
                                    break;

                                case 2:
                                    {
                                        SendMessage(CheckedUserID, "Нормально");
                                    }
                                    break;

                                case 3:
                                    {
                                        SendMessage(CheckedUserID, "Норм");
                                        SendMessage(CheckedUserID, "Ты как?");
                                    }
                                    break;

                                case 4:
                                    {
                                        SendMessage(CheckedUserID, "Нормально");
                                        SendMessage(CheckedUserID, "Ты как?");
                                    }
                                    break;
                            }
                        }
                        else if (resultNormal1 || resultNormal2 || resultNormal3)
                        {
                            SendMessage(CheckedUserID, "Это хорошо");
                        }
                        else if (resultName1 || resultName2)
                        {
                            SendMessage(CheckedUserID, "{Die Bot Vk} Бывший пользователь этой страници мертв");
                        }
                        else if (resultNews)
                        {
                            //---СОСТАВНЫЕ КОММАНДЫ---
                            if (Body.ToLower().Contains("новости "))
                            {
                                string Source = Body.ToLower().Substring(8);
                                if (Source.ToLower() == "lenta") News(CheckedUserID, false, "lenta");
                                else if (Source.ToLower() == "meduza") News(CheckedUserID, false, "meduza");
                                else News(CheckedUserID, true, "");
                            }
                            else
                            {
                                News(CheckedUserID, true, "");
                            }
                            //---КОНЕЦ СОСТАВНЫХ КОМАНД---
                        }
                        else if (resultLearn)
                        {
                            Learn(CheckedUserID, Body);
                        }
                        else
                        {
                            string message = "";

                            if (DefaultAnswer.Count > 1)
                            {
                                for (int s = 0; s < DefaultAnswer.Count; s++)
                                {
                                    if (s == 0)
                                    {
                                        message = DefaultAnswer[s];
                                    }
                                    else
                                    {
                                        message += Environment.NewLine + DefaultAnswer[s];
                                    }
                                }

                                SendMessage(CheckedUserID, message);
                            }
                            else
                            {
                                message = DefaultAnswer[0];
                                SendMessage(CheckedUserID, message);
                            }
                        }
                    }
                    break;
            }
        }

        public static void Learn(long UserID, string context)
        {
            try
            {
                if (File.Exists(Environment.CurrentDirectory + "\\Words\\words_Learn.wodb"))
                {
                    System.IO.File.WriteAllLines(Environment.CurrentDirectory + "\\Words\\words_Learn.wodb", new string[] { UserID + " " + context });
                    SendMessage(UserID, "{Die Bot Vk} Ваша команда была записана, доступ к ней будет доступен после модерации!");
                }
                else
                {
                    System.IO.File.WriteAllLines(Environment.CurrentDirectory + "\\Words\\words_Learn.wodb", new string[] { "" });
                }
            }
            catch (Exception er) { colorConsole.ColorWriteLine("Error: " + er.Message, ConsoleColor.Red, ConsoleColor.White); }
        }

        public static void SendMessage(long ID, string Body)
        {
            vkApi.Messages.Send(new MessagesSendParams {
                UserId = ID,
                Message = Body
            });

            colorConsole.ColorWriteLine("DieBot:/>Отправил сообщение: " + Body, ConsoleColor.Cyan, ConsoleColor.White);

            Console.WriteLine("--------------------------");
            Console.WriteLine("DieBot:/>Введите команду боту:");
        }

        public static void CheckFriend(int numberuser)
        {
            var Friends = GetFriends();

            colorConsole.ColorWriteLine("<-------------------------->", ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("DieBot:/>Имя: " + Friends[numberuser].FirstName, ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("DieBot:/>Фамилия: " + Friends[numberuser].LastName, ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("DieBot:/>Статус: " + Friends[numberuser].Status, ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("DieBot:/>Online: " + Friends[numberuser].Online, ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("DieBot:/>День Рождение: " + Friends[numberuser].BirthDate, ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("DieBot:/>Пол: " + Friends[numberuser].Sex, ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("DieBot:/>Временная зона: " + Friends[numberuser].Timezone, ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("DieBot:/>Город: " + Friends[numberuser].City.Title, ConsoleColor.Green, ConsoleColor.White);
            colorConsole.ColorWriteLine("<-------------------------->", ConsoleColor.Green, ConsoleColor.White);
        }

        public static void SearchMessage(long UserID, string Text)
        {
            int tempCount = 0;

            var content = vkApi.Messages.Search(new MessagesSearchParams {
                Query = Text,
                PeerId = UserID
            });

            foreach (var message in content)
            {
                if (message.Text != "" || message.Text != null)
                {
                    tempCount++;
                }
            }

            Console.WriteLine("Количество совпадений: " + tempCount);
        }

        public static void SpamAttack(bool OneUser, bool OneMessage, string Message)
        {
            if (OneUser)
            {
                var Friends = GetFriends();

                Console.WriteLine("DieBot:/>Введите номер пользователя:");
                int tempUser = Int32.Parse(Console.ReadLine());
                tempUser--;
                if (OneMessage)
                {
                    SendMessage(Friends[tempUser].Id, Message);
                }
                else if (!OneMessage)
                {
                    while (!OneMessage)
                    {
                        SendMessage(Friends[tempUser].Id, Message);
                    }
                }
            }
            else if (!OneUser)
            {
                if (OneMessage)
                {
                    var Friends = GetFriends();

                    for (int i = 0; i < Friends.Count; i++)
                    {
                        SendMessage(Friends[i].Id, Message);
                    }
                }
                else if (!OneMessage)
                {
                    while (!OneMessage)
                    {
                        var Friends = GetFriends();

                        for (int i = 0; i < Friends.Count; i++)
                        {
                            SendMessage(Friends[i].Id, Message);
                        }
                    }
                }
            }
        }

        public static void DebugConsole(string[] message)
        {
            colorConsole.ColorWriteLine("---------Debug---------", ConsoleColor.Cyan, ConsoleColor.White);
            for (int m = 0; m < message.Length; m++)
            {
                colorConsole.ColorWriteLine("DieBot->Debug:/>" + message[m], ConsoleColor.Cyan, ConsoleColor.White);
            }
            colorConsole.ColorWriteLine("-----------------------", ConsoleColor.Cyan, ConsoleColor.White);
        }

        public static void GetException(long MyID)
        {
            try
            {
                if (File.Exists(Environment.CurrentDirectory + "\\Exception\\Exception_" + MyID + ".exce"))
                {
                    tempException = System.IO.File.ReadAllLines(Environment.CurrentDirectory + "\\Exception\\Exception_" + MyID + ".exce");
                    for (int i = 0; i < tempException.Length; i++)
                    {
                        exceptionList.Add(tempException[i]);
                    }
                }
                else
                {
                    System.IO.File.WriteAllLines(Environment.CurrentDirectory + "\\Exception\\Exception_" + MyID + ".exce", new string[] { MyID.ToString() });
                }
            }
            catch (Exception er) { colorConsole.ColorWriteLine("Error: " + er.Message, ConsoleColor.Red, ConsoleColor.White); }
        }

        public static void News(long CheckedUserID, bool rnd, string source)
        {
            if (rnd)
            {
                Random NewsRandom = new Random();
                int site = NewsRandom.Next(0, 2);
                string Result;
                try
                {
                    if (site == 0) Result = Web.DownloadString(lentaru);
                    else Result = Web.DownloadString(meduza);

                    XDocument Doc = XDocument.Parse(Result);
                    List<RssNews> a = (from descendant in Doc.Descendants("item")
                                       select new RssNews()
                                       {
                                           Description = descendant.Element("description").Value,
                                           Title = descendant.Element("title").Value,
                                           PublicationDate = descendant.Element("pubDate").Value
                                       }).ToList();
                    string news = "";
                    if (a != null)
                    {
                        int i = NewsRandom.Next(0, a.Count - 1);
                        news = a[i].Title + Environment.NewLine + "--------------------" + Environment.NewLine + a[i].Description;
                        byte[] bytes = Encoding.Default.GetBytes(news);
                        news = Encoding.UTF8.GetString(bytes);
                        SendMessage(CheckedUserID, news);
                    }
                }
                catch { }
            }
            else if (!rnd)
            {
                switch (source)
                {
                    case "lenta":
                        {
                            Random NewsRandom = new Random();
                            string Result;
                            try
                            {
                                Result = Web.DownloadString(lentaru);
                                XDocument Doc = XDocument.Parse(Result);
                                List<RssNews> a = (from descendant in Doc.Descendants("item")
                                                   select new RssNews()
                                                   {
                                                       Description = descendant.Element("description").Value,
                                                       Title = descendant.Element("title").Value,
                                                       PublicationDate = descendant.Element("pubDate").Value
                                                   }).ToList();
                                string news = "";
                                if (a != null)
                                {
                                    int i = NewsRandom.Next(0, a.Count - 1);
                                    news = "Lenta.ru" + Environment.NewLine + a[i].Title + Environment.NewLine + "--------------------" + Environment.NewLine + a[i].Description;
                                    byte[] bytes = Encoding.Default.GetBytes(news);
                                    news = Encoding.UTF8.GetString(bytes);
                                    SendMessage(CheckedUserID, news);
                                }
                            }
                            catch { }
                        }
                        break;

                    case "meduza":
                        {
                            Random NewsRandom = new Random();
                            string Result;
                            try
                            {
                                Result = Web.DownloadString(meduza);
                                XDocument Doc = XDocument.Parse(Result);
                                List<RssNews> a = (from descendant in Doc.Descendants("item")
                                                   select new RssNews()
                                                   {
                                                       Description = descendant.Element("description").Value,
                                                       Title = descendant.Element("title").Value,
                                                       PublicationDate = descendant.Element("pubDate").Value
                                                   }).ToList();
                                string news = "";
                                if (a != null)
                                {
                                    int i = NewsRandom.Next(0, a.Count - 1);
                                    news = "Meduza.io" + Environment.NewLine + a[i].Title + Environment.NewLine + "--------------------" + Environment.NewLine + a[i].Description;
                                    byte[] bytes = Encoding.Default.GetBytes(news);
                                    news = Encoding.UTF8.GetString(bytes);
                                    SendMessage(CheckedUserID, news);
                                }
                            }
                            catch { }
                        }
                        break;
                }
            }
        }

        public static void Eye(bool debug)
        {
            switch (debug)
            {
                case true:
                    {
                        LongPollServerResponse pool = vkApi.Messages.GetLongPollServer(true);
                        StartAsync(pool.Ts, pool.Pts);
                        NewMessages += Watcher_NewMessages;
                        Program.DebugConsole(new string[] {
                            "Ts: " + pool.Ts,
                            "Pts: " + pool.Pts,
                            "NewMessage: " + NewMessages.ToString()
                        });
                    }
                    break;

                case false:
                    {
                        LongPollServerResponse pool = vkApi.Messages.GetLongPollServer(true);
                        StartAsync(pool.Ts, pool.Pts, debug);
                        NewMessages += Watcher_NewMessages;
                    }
                    break;
            }
        }

        public static void Watcher_NewMessages(VkApi owner, ReadOnlyCollection<Message> messages)
        {
            var Friends = Program.GetFriends();
            bool ExceptionUser = false;

            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Type != MessageType.Sended)
                {
                    messages[i].UserId = messages[i].PeerId.Value;
                    ReadOnlyCollection<User> Sender = vkApi.Users.Get(new long[] { Convert.ToInt32(messages[i].UserId) });
                    colorConsole.ColorWriteLine("<-----------Eye-Mod----------->", ConsoleColor.Magenta, ConsoleColor.White);
                    colorConsole.ColorWriteLine("DieBot:/>Новое сообщение от " + Sender[0].FirstName + " " + Sender[0].LastName, ConsoleColor.Magenta, ConsoleColor.White);
                    colorConsole.ColorWriteLine("DieBot:/>Текст сообщения:" +
                    "\n" + messages[i].Text, ConsoleColor.Magenta, ConsoleColor.White);
                    colorConsole.ColorWriteLine("<----------------------------->", ConsoleColor.Magenta, ConsoleColor.White);
                    userID = messages[i].UserId.Value;

                    if (exceptionList.Count > 0)
                    {
                        for (int ex = 0; ex < exceptionList.Count; ex++)
                        {
                            if (exceptionList[ex] == messages[i].PeerId.ToString())
                            {
                                colorConsole.ColorWriteLine("DieBot:/>Этот пользователь в списке исключений!", ConsoleColor.Magenta, ConsoleColor.White);
                                colorConsole.ColorWriteLine("<----------------------------->", ConsoleColor.Magenta, ConsoleColor.White);
                                Console.WriteLine("DieBot:/>Введите команду боту:");
                                ExceptionUser = true;
                            }
                        }

                        if (!ExceptionUser)
                        {
                            Program.Command(messages[i].PeerId.Value, messages[i].Text);
                        }
                    }
                    else Program.Command(messages[i].PeerId.Value, messages[i].Text);
                }
            }
        }

        public static async void StartAsync(ulong? lastTs = null, ulong? lastPts = null, bool debug = false)
        {
            switch (debug)
            {
                case true:
                    {
                        Program.DebugConsole(new string[] {
                            "isActive: " + IsActive,
                            "lastPts: " + lastPts
                            });
                    }
                    break;

                case false: break;
            }
            if (IsActive) colorConsole.ColorWriteLine("Просматриваем сообщения для {0}", ConsoleColor.Blue, ConsoleColor.White);
            IsActive = true;
            await GetLongPoolServerAsync(lastPts);
            if (WatchTimer != null) WatchTimer = null;
            else WatchTimer = new Timer(new TimerCallback(WatchAsync), null, 0, Timeout.Infinite);
            switch (debug)
            {
                case true:
                    {
                        Program.DebugConsole(new string[] {
                            "isActive: " + IsActive,
                            "lastPts: " + lastPts
                            });
                    }
                    break;

                case false: break;
            }
        }

        public static Task<LongPollServerResponse> GetLongPoolServerAsync(ulong? lastPts = null, bool debug = false)
        {
            try
            {
                return Task.Run(() =>
                {
                    return GetLongPoolServer(lastPts);
                });
            }
            catch (Exception error)
            {
                colorConsole.ColorWriteLine("<----------------------------->", ConsoleColor.Magenta, ConsoleColor.White);
                colorConsole.ColorWriteLine(error.Message, ConsoleColor.Magenta, ConsoleColor.White);
                colorConsole.ColorWriteLine("<----------------------------->", ConsoleColor.Magenta, ConsoleColor.White);
                colorConsole.ColorWriteLine("Мод перезапущен", ConsoleColor.Magenta, ConsoleColor.White);
                GC.Collect();
                Stop();
                Eye(debug);
                colorConsole.ColorWriteLine("<----------------------------->", ConsoleColor.Magenta, ConsoleColor.White);
                Console.WriteLine("DieBot:/>Введите команду боту:");
            }
            return null;
        }

        public static async void WatchAsync(object state)
        {
            LongPollHistoryResponse history = await GetLongPoolHistoryAsync();
            if (history.Messages.Count > 0)
            {
                CurrentSleepSteps = 1;
                NewMessages?.Invoke(vkApi, history.Messages);
            }
            else if (CurrentSleepSteps < MaxSleepSteps) CurrentSleepSteps++;
            WatchTimer.Change(CurrentSleepSteps * StepSleepTime, Timeout.Infinite);
        }

        public static Task<LongPollHistoryResponse> GetLongPoolHistoryAsync()
        {
            return Task.Run(() => { return GetLongPoolHistory(); });
        }

        public static LongPollHistoryResponse GetLongPoolHistory()
        {
            if (!Ts.HasValue) GetLongPoolServer(null);
            MessagesGetLongPollHistoryParams rp = new MessagesGetLongPollHistoryParams();
            rp.Ts = Ts.Value;
            rp.Pts = Pts;
            int i = 0;
            LongPollHistoryResponse history = null;
            string errorLog = "";
            while (i < 5 && history == null)
            {
                i++;
                try
                {
                    history = vkApi.Messages.GetLongPollHistory(rp);
                }
                catch (TooManyRequestsException)
                {
                    Thread.Sleep(150);
                    i--;
                }
                catch (Exception ex)
                {
                    errorLog += string.Format("{0} - {1}{2}", i, ex.Message, Environment.NewLine);
                }
            }

            if (history != null)
            {
                Pts = history.NewPts;
                foreach (var m in history.Messages)
                {
                    m.FromId = m.Type == MessageType.Sended ? vkApi.UserId : m.UserId;
                }
            }
            else colorConsole.ColorWriteLine(errorLog, ConsoleColor.Red, ConsoleColor.White);
            return history;
        }

        public static LongPollServerResponse GetLongPoolServer(ulong? lastPts = null, bool debug = false)
        {
            LongPollServerResponse response = vkApi.Messages.GetLongPollServer(false, 2, null);

            switch (debug)
            {
                case true:
                    {
                        Program.DebugConsole(new string[] {
                            "Ts Response: " + response.Ts,
                            "Pts Response: " + response.Pts,
                            "Response: " + response.ToString()
                        });
                    }
                    break;

                case false:
                    {
                    }
                    break;
            }

            Ts = response.Ts;

            Pts = Pts == null ? response.Pts : lastPts;

            return response;
        }

        public static void Stop()
        {
            if (WatchTimer != null) WatchTimer.Dispose();
            IsActive = false;
            WatchTimer = null;
        }

        public static bool CheckBadWords(string body)
        {
            int indexOfSubstring = -1;

            for (int i = 0; i < BadWords.Count; i++)
            {
                indexOfSubstring = body.IndexOf(BadWords[i]);
                if (indexOfSubstring > -1)
                {
                    return true;
                }
            }

            return false;
        }

        public static void LoadList(long MyID)
        {
            try
            {
                if (File.Exists(Environment.CurrentDirectory + "\\Words\\Default_" + MyID + ".wodf"))
                {
                    tempDefaultAnswer = System.IO.File.ReadAllLines(Environment.CurrentDirectory + "\\Words\\Default_" + MyID + ".wodf");
                    for (int i = 0; i < tempDefaultAnswer.Length; i++)
                    {
                        DefaultAnswer.Add(tempDefaultAnswer[i]);
                    }
                }
                else
                {
                    System.IO.File.WriteAllLines(Environment.CurrentDirectory + "\\Words\\Default_" + MyID + ".wodf", new string[] { "" });
                }
            }
            catch (Exception er) { colorConsole.ColorWriteLine("Error: " + er.Message, ConsoleColor.Red, ConsoleColor.White); }

            try
            {
                if (File.Exists(Environment.CurrentDirectory + "\\Words\\Bad_" + MyID + ".wodf"))
                {
                    tempBadWords = System.IO.File.ReadAllLines(Environment.CurrentDirectory + "\\Words\\Bad_" + MyID + ".wodf");
                    for (int i = 0; i < tempBadWords.Length; i++)
                    {
                        BadWords.Add(tempBadWords[i]);
                    }
                }
                else
                {
                    System.IO.File.WriteAllLines(Environment.CurrentDirectory + "\\Words\\Bad_" + MyID + ".wodf", new string[] { "" });
                }
            }
            catch (Exception er) { colorConsole.ColorWriteLine("Error: " + er.Message, ConsoleColor.Red, ConsoleColor.White); }
        }

        public static void addbadword(long MyID, string word)
        {
            bool isNew = true;

            for (int i = 0; i < BadWords.Count; i++)
            {
                if (BadWords[i] == word)
                {
                    isNew = false;
                    break;
                }
            }

            if (isNew)
            {
                BadWords.Clear();
                for (int w = 0; w < tempBadWords.Length; w++)
                {
                    BadWords.Add(tempBadWords[w]);
                }
                BadWords.Add(word);
                System.IO.File.WriteAllLines(Environment.CurrentDirectory + "\\Words\\Bad_" + MyID + ".wodf", BadWords);
                LoadList(MyID);
                colorConsole.ColorWriteLine("DieBot:/>Слово " + word + " добавлено", ConsoleColor.Green, ConsoleColor.White);
            }else colorConsole.ColorWriteLine("DieBot:/>Слово " + word + " уже есть", ConsoleColor.Yellow, ConsoleColor.White);
        }

        public static void addexception(long MyID, string id)
        {
            bool isNew = true;

            for (int i = 0; i < exceptionList.Count; i++)
            {
                if (exceptionList[i] == id)
                {
                    isNew = false;
                    break;
                }
            }

            if (isNew)
            {
                exceptionList.Clear();
                for (int w = 0; w < tempException.Length; w++)
                {
                    exceptionList.Add(tempException[w]);
                }
                exceptionList.Add(id);
                System.IO.File.WriteAllLines(Environment.CurrentDirectory + "\\Exception\\Exception_" + MyID + ".exce", exceptionList);
                GetException(MyID);
                colorConsole.ColorWriteLine("DieBot:/>Пользователь с id: " + id + " добавлено в исключения", ConsoleColor.Green, ConsoleColor.White);
            } else colorConsole.ColorWriteLine("DieBot:/>Пользователь с id: " + id + " уже есть в исключениях", ConsoleColor.Yellow, ConsoleColor.White);
        }
    }

    public class RssNews
    {
        public string Title;
        public string PublicationDate;
        public string Description;
    }
}
