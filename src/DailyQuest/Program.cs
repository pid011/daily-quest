// Copyright (c) Sepi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DailyQuest
{
    internal class Program
    {
        /// <summary>
        /// 완료한 항목에 표시될 문자
        /// </summary>
        private const string CheckMarkEmoji = "✔️";
        private const string CheckMarkText = "O";
        /// <summary>
        /// 완료되지 않은 항목에 표시될 문자
        /// </summary>
        private const string CrossMarkEmoji = "❌";
        private const string CrossMarkText = "X";

        private static readonly Dictionary<string, string> _commands = new Dictionary<string, string>
        {
            ["check"] = "선택한 항목을 체크하거나 체크 해제합니다.",
            ["reset"] = "오늘의 일일퀘스트를 초기화합니다.",
            ["config"] = "기본 일일퀘스트 항목의 파일 경로를 표시하거나 메모장으로 엽니다.",
            ["-v"] = "일일퀘스트 체크 프로그램의 버전을 표시합니다.",
            ["-h"] = "일일퀘스트 체크 프로그램의 도움말을 표시합니다.",
        };

        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            DailyQuestItem item;

            if (args.Length == 0)
            {
                item = DailyQuest.GetTodayDailyQuest();
                PrintDailyQuest(item);
                return;
            }

            string[] commandArgs = new string[args.Length - 1];
            if (args.Length > 1)
            {
                Array.Copy(args, 1, commandArgs, 0, commandArgs.Length);
            }

            switch (args[0])
            {
                case "check":
                    item = DailyQuest.GetTodayDailyQuest();
                    RunCheckCommand(ref item, commandArgs);
                    PrintDailyQuest(item);
                    break;

                case "reset":
                    item = DailyQuest.GetTodayDailyQuest();

                    if (RunResetCommand(ref item))
                    {
                        PrintDailyQuest(item);
                    }
                    break;

                case "config":
                    RunConfigCommand(commandArgs);
                    break;

                case "-v":
                    PrintVersionCommand();
                    break;

                case "-h":
                default:
                    PrintHelpMessage();
                    break;
            }

        }

        private static void RunCheckCommand(ref DailyQuestItem item, string[] commandArgs)
        {
            if (commandArgs.Length == 0)
            {
                Console.WriteLine("명령어 사용방법: check [항목번호] (1개 이상의 항목번호 입력 가능)");
                return;
            }

            SortedSet<int> sortedIntegers = new SortedSet<int>();
            List<int> goodNumbers = new List<int>(commandArgs.Length);

            for (int i = 0; i < commandArgs.Length; i++)
            {
                if (!int.TryParse(commandArgs[i], out int result))
                {
                    continue;
                }
                sortedIntegers.Add(result);
            }

            foreach (var i in sortedIntegers.Distinct())
            {
                try
                {
                    item.Quests[i - 1].HasDone = !item.Quests[i - 1].HasDone;
                    goodNumbers.Add(i);
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }
            }

            if (goodNumbers.Count == 0)
            {
                Console.WriteLine("제대로 된 번호를 입력하지 않아 아무런 항목도 수정되지 않았습니다.");
                return;
            }

            DailyQuest.WriteFileAndRefreshTime(ref item);
            Console.WriteLine($"{string.Join(", ", goodNumbers)}번 항목이 수정 되었습니다.");
        }

        private static bool RunResetCommand(ref DailyQuestItem item)
        {
            Console.WriteLine("현재까지 체크한 항목들이 사라지고 기본 일일퀘스트 항목으로 초기화됩니다.");
            while (true)
            {
                Console.WriteLine("정말로 오늘의 일일퀘스트를 초기화하겠습니까? (y/n)");
                Console.Write("> ");
                string input = Console.ReadLine();
                switch (input)
                {
                    case "Y":
                    case "y":
                        item = DailyQuest.GetDefaultDailyQuest();
                        DailyQuest.WriteFileAndRefreshTime(ref item);
                        Console.WriteLine("오늘의 일일퀘스트가 초기화되었습니다.");
                        return true;
                    case "N":
                    case "n":
                        Console.WriteLine("초기화를 하지 않았습니다.");
                        return false;
                    default:
                        Console.WriteLine("다시 입력해주세요.");
                        break;
                }
            }
        }

        private static void RunConfigCommand(string[] commandArgs)
        {
            string path = DailyQuest.DefaultDailyQuestFilePath;
            if (commandArgs.Length == 0)
            {
                Console.WriteLine(path);
                return;
            }
            if (commandArgs[0] != "--open")
            {
                Console.WriteLine("잘못된 옵션입니다.");
                Console.WriteLine("[--open] 옵션을 사용하여 메모장으로 바로 열 수 있습니다.");
                return;
            }

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT: // Windows
                    System.Diagnostics.Process.Start("notepad.exe", path);
                    break;
                case PlatformID.Unix: // MacOSX, Linux
                default:
                    Console.WriteLine("현재 프로그램에서 지원하지 않는 OS입니다.");
                    break;
            }
        }

        private static void PrintHelpMessage()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("명령어 목록:");
            foreach (KeyValuePair<string, string> item in _commands)
            {
                builder.AppendLine($"{' ',2}{item.Key,-20}{item.Value}");
            }
            Console.WriteLine(builder.ToString());
        }

        private static void PrintVersionCommand()
        {
            AssemblyName assm = typeof(Program).Assembly.GetName();
            Console.WriteLine($"{assm.Name} v{assm.Version.ToString(3)}");
        }

        private static void PrintDailyQuest(DailyQuestItem item)
        {
            static int GetDigitLength(int n) => n < 1 ? 0 : (int)Math.Log10(n) + 1;

            Console.WriteLine();

            if (item.Quests.Count == 0)
            {
                Console.WriteLine("현재 일일퀘스트 목록이 없습니다.");
                Console.WriteLine("만약 기본 일일퀘스트 목록을 변경하였으면 reset 명령어를 실행해보세요.");
                return;
            }

            int max = GetDigitLength(item.Quests.Count);
            int hasDoneCount = 0;

            for (int i = 0; i < item.Quests.Count; i++)
            {
                int length = GetDigitLength(i + 1);
                string number = (i + 1).ToString();

                number = number.PadLeft(max - length + number.Length);
                Console.Write($"  {number}. [");

                if (item.Quests[i].HasDone)
                {
                    hasDoneCount++;
                    if (item.UseEmoji)
                    {
                        Console.Write(CheckMarkEmoji);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(CheckMarkText);
                        Console.ResetColor();
                    }
                }
                else
                {
                    if (item.UseEmoji)
                    {
                        Console.Write(CrossMarkEmoji);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(CrossMarkText);
                        Console.ResetColor();
                    }
                }

                Console.Write("] - ");
                Console.WriteLine(item.Quests[i].QuestDescription);
            }

            Console.WriteLine();

            if (hasDoneCount == item.Quests.Count)
            {
                if (item.UseEmoji)
                {
                    Console.WriteLine("🎉🎉오늘의 일일퀘스트를 모두 끝냈습니다!🎉🎉");
                }
                else
                {
                    Console.WriteLine(":::오늘의 일일퀘스트를 모두 끝냈습니다!:::");
                }
            }
            else
            {
                Console.WriteLine($"현재 총 {item.Quests.Count}개의 항목 중 {hasDoneCount}개의 항목을 완료했습니다.");

                DateTime now = DateTime.Now;
                int hour = 23 - now.Hour;
                int minute = 59 - now.Minute;
                int second = 59 - now.Second;
                Console.WriteLine($"자정까지 {hour}시간 {minute}분 {second}초 남았습니다. 파이팅!{(item.UseEmoji ? "👊" : "")}");
            }
        }
    }

    public class DailyQuest
    {
        /// <summary>
        /// 오늘의 일일퀘스트 항목을 가지고 있는 파일이름
        /// </summary>
        private const string TodayDailyQuestFileName = "daily-quest.today";

        /// <summary>
        /// 기본 일일퀘스트 리스트를 가지고 있는 파일이름
        /// </summary>
        private const string DefaultDailyQuestFileName = "daily-quest.default.txt";

        private const string DataBaseDirectoryName = "database";
        private const string ConfigDirectoryName = "config";

        /// <summary>
        /// 오늘의 일일퀘스트 파일의 위치
        /// </summary>
        public static string TodayDailyQuestFilePath
        {
            get
            {
                string dirPath = Path.Combine(_programDirectoryPath, DataBaseDirectoryName);

                return Path.Combine(Directory.CreateDirectory(dirPath).FullName, TodayDailyQuestFileName);
            }
        }

        /// <summary>
        /// 기본 일일퀘스트 파일의 위치
        /// </summary>
        public static string DefaultDailyQuestFilePath
        {
            get
            {
                string dirPath = Path.Combine(_programDirectoryPath, ConfigDirectoryName);

                return Path.Combine(Directory.CreateDirectory(dirPath).FullName, DefaultDailyQuestFileName);
            }
        }

        /// <summary>
        /// 프로그램 실행파일의 위치
        /// </summary>
        private static string _programDirectoryPath => Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;

        /// <summary>
        /// 오늘의 일일퀘스트 데이터를 파일에서 읽어옵니다.
        /// </summary>
        /// <returns>
        /// 오늘의 일일퀘스트 데이터를 반환합니다.<para/>
        /// 만약 데이터의 <see cref="RefreshTime"/>이 현재 시간보다 하루 전이라면 데이터를 초기화하고 새로운 데이터를 반환합니다.<para/>
        /// 만약 파일이 존재하지 않으면 파일을 새로 생성하고 초기화된 데이터를 반환합니다.
        /// </returns>
        public static DailyQuestItem GetTodayDailyQuest()
        {
            DailyQuestItem item = null;
            bool deserialized = true;

            try
            {
                using FileStream fs = File.OpenRead(TodayDailyQuestFilePath);
                byte[] jsonBytes = new byte[fs.Length];
                int numBytesToRead = (int)fs.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    int n = fs.Read(jsonBytes, numBytesRead, numBytesToRead);
                    if (n == 0)
                    {
                        break;
                    }
                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                var utf8Reader = new Utf8JsonReader(jsonBytes);
                item = JsonSerializer.Deserialize<DailyQuestItem>(ref utf8Reader);

                if (DateTime.Now.Day != item.RefreshTime.Day)
                {
                    deserialized = false;
                }
            }
            catch (FileNotFoundException)
            {
                deserialized = false;
            }
            catch (JsonException)
            {
                throw;
            }

            if (!deserialized)
            {
                item = GetDefaultDailyQuest();
                WriteFileAndRefreshTime(ref item);
            }

            return item;
        }

        /// <summary>
        /// 오늘의 일일퀘스트 파일에 매개변수로 받은 데이터를 작성하고 <see cref="RefreshTime"/>을 현재 시간으로 변경합니다.
        /// </summary>
        /// <param name="item">파일에 작성할 데이터</param>
        public static void WriteFileAndRefreshTime(ref DailyQuestItem item)
        {
            item ??= new DailyQuestItem();
            item.Quests ??= new List<Quest>();
            item.RefreshTime = DateTime.Now;

            using FileStream fs = File.Create(TodayDailyQuestFilePath);
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(item);
            fs.Write(jsonBytes, 0, jsonBytes.Length);
        }

        /// <summary>
        /// 기본 일일퀘스트 파일에서 목록을 가져옵니다. 파일이 없다면 파일을 생성합니다.
        /// </summary>
        /// <returns>
        /// 초기화된 기본 일일퀘스트 목록을 반환합니다.
        /// </returns>
        public static DailyQuestItem GetDefaultDailyQuest()
        {
            DailyQuestItem dailyQuest = new DailyQuestItem
            {
                RefreshTime = DateTime.Now,
                Quests = new List<Quest>()
            };

            try
            {
                using StreamReader sr = File.OpenText(DefaultDailyQuestFilePath);
                string input;

                input = sr.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    if (bool.TryParse(input, out bool useEmoji))
                    {
                        dailyQuest.UseEmoji = useEmoji;
                    }
                    else
                    {
                        dailyQuest.Quests.Add(new Quest { QuestDescription = input });
                    }
                }

                while ((input = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        dailyQuest.Quests.Add(new Quest { QuestDescription = input });
                    }
                }
            }
            catch (FileNotFoundException)
            {
                File.Create(DefaultDailyQuestFilePath);
            }

            return dailyQuest;
        }
    }

    public class DailyQuestItem
    {
        /// <summary>
        /// json 파일이 새로 쓰여진 시간
        /// </summary>
        [JsonPropertyName("refresh_time")]
        public DateTimeOffset RefreshTime { get; set; }

        /// <summary>
        /// 프로그램에서 이모지 사용 여부
        /// </summary>
        [JsonPropertyName("use_emoji")]
        public bool UseEmoji { get; set; }

        /// <summary>
        /// 일일퀘스트 리스트
        /// </summary>
        [JsonPropertyName("quests")]
        public IList<Quest> Quests { get; set; }
    }

    /// <summary>
    /// 일일퀘스트에 대한 요소를 가지고 있습니다.
    /// </summary>
    public class Quest
    {
        /// <summary>
        /// 일일퀘스트 설명
        /// </summary>
        [JsonPropertyName("quest_desc")]
        public string QuestDescription { get; set; }

        /// <summary>
        /// 일일퀘스트를 했는지 여부
        /// </summary>
        [JsonPropertyName("has_done")]
        public bool HasDone { get; set; }
    }
}
