using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace DailyQuestChecker
{
    internal class Program
    {
        /// <summary>
        /// 체크 표시 이모티콘
        /// </summary>
        private const string _checkMark = "o";
        /// <summary>
        /// X 표시 이모티콘
        /// </summary>
        private const string _crossMark = "x";

        private readonly static Dictionary<string, string> _commands = new Dictionary<string, string>
        {
            ["check"] = "선택한 항목을 체크하거나 체크해제합니다.",
            ["reset"] = "오늘의 일일퀘스트를 초기화합니다.",
            ["-h"] = "일일퀘스트 체크 프로그램의 도움말을 표시합니다."
        };

        private static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    PrintDailyQuest();
                    return;
                }

                switch (args[0])
                {
                    case "check":
                        RunCheckCommand(args);
                        break;

                    case "reset":
                        RunResetCommand();
                        break;

                    case "-h":
                    default:
                        PrintHelpMessage();
                        return;
                }
                PrintDailyQuest();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        private static void RunCheckCommand(string[] args)
        {
            if (args.Length == 1)
            {
                Console.WriteLine("명령어 사용방법: check [항목번호]");
                return;
            }
            if (int.TryParse(args[1], out int index))
            {
                try
                {
                    DailyQuest dailyQuest = DailyQuest.GetTodayDailyQuest();

                    dailyQuest.Quests[index - 1].HasDone = !dailyQuest.Quests[index - 1].HasDone;
                    DailyQuest.WriteTodayDailyQuestDataOnFile(dailyQuest);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("잘못된 번호입니다.");
                }
            }
            else
            {
                Console.WriteLine("숫자로 입력해주세요.");
            }
        }

        private static void RunResetCommand()
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
                        var defaultDailyQuest = DailyQuest.GetDefaultDailyQuest();
                        DailyQuest.WriteTodayDailyQuestDataOnFile(defaultDailyQuest);
                        Console.WriteLine("오늘의 일일퀘스트가 초기화되었습니다.");
                        return;
                    case "N":
                    case "n":
                        Console.WriteLine("초기화를 하지 않았습니다.");
                        return;
                    default:
                        Console.WriteLine("다시 입력해주세요.");
                        break;
                }
            }
        }

        private static void PrintHelpMessage()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("명령어 목록:");
            foreach (var item in _commands)
            {
                builder.AppendLine($"{' ',2}{item.Key,-20}{item.Value}");
            }
            builder.AppendLine();
            Console.WriteLine(builder.ToString());
        }

        private static void PrintDailyQuest()
        {
            DailyQuest dailyQuest = DailyQuest.GetTodayDailyQuest();

            StringBuilder builder = new StringBuilder();
            if (dailyQuest.Quests.Count == 0)
            {
                Console.WriteLine("현재 일일퀘스트 목록이 없습니다.");
                Console.WriteLine($"만약 기본 일일퀘스트 목록을 변경하였으면 reset 명령어를 실행해보세요.");
                return;
            }
            int length = 0;
            for (int i = 0; i < dailyQuest.Quests.Count; i++)
            {
                builder.Append($"│ {i + 1}. [{(dailyQuest.Quests[i].HasDone ? _checkMark : _crossMark)}] - ");
                builder.AppendLine(dailyQuest.Quests[i].QuestDescription);
                if (dailyQuest.Quests[i].QuestDescription.Length > length)
                {
                    length = dailyQuest.Quests[i].QuestDescription.Length;
                }
            }

            string line = "".PadRight(20 + length, '─');
            builder.Insert(0, $"┌{line}┐\n");
            builder.AppendLine($"└{line}┘");

            Console.WriteLine(builder.ToString());
        }
    }

    /// <summary>
    /// 일일퀘스트에 대한 요소를 가지고 있습니다.
    /// </summary>
    [DataContract]
    public class Quest
    {
        /// <summary>
        /// 일일퀘스트 설명
        /// </summary>
        [DataMember]
        public string QuestDescription { get; set; }

        /// <summary>
        /// 일일퀘스트를 했는지 여부
        /// </summary>
        [DataMember]
        public bool HasDone { get; set; }
    }

    [DataContract]
    public class DailyQuest
    {
        /// <summary>
        /// json 파일이 새로 쓰여진 시간
        /// </summary>
        [DataMember]
        public DateTime RefreshTime { get; set; }

        /// <summary>
        /// 일일퀘스트 리스트
        /// </summary>
        [DataMember]
        public List<Quest> Quests { get; set; }

        /// <summary>
        /// 오늘의 일일퀘스트 항목을 가지고 있는 파일이름
        /// </summary>
        public const string _todayFileName = "today-daily-quests.daily";

        /// <summary>
        /// 기본 일일퀘스트 리스트를 가지고 있는 파일이름
        /// </summary>
        public const string _defaultFileName = "daily-quests.txt";

        /// <summary>
        /// 오늘의 일일퀘스트 데이터를 파일에서 읽어옵니다.
        /// </summary>
        /// <returns>
        /// 오늘의 일일퀘스트 데이터를 반환합니다.<para/>
        /// 만약 데이터의 <see cref="RefreshTime"/>이 현재 시간보다 하루 전이라면 데이터를 초기화하고 새로운 데이터를 반환합니다.<para/>
        /// 만약 파일이 존재하지 않으면 파일을 새로 생성하고 초기화된 데이터를 반환합니다.
        /// </returns>
        public static DailyQuest GetTodayDailyQuest()
        {
            DailyQuest dailyQuest = null;
            var serializer = new JsonSerializer();

            bool serialized = true;
            try
            {
                using (Stream stream = File.Open(_todayFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(stream))
                using (JsonReader jsonReader = new JsonTextReader(reader))
                {
                    dailyQuest = serializer.Deserialize<DailyQuest>(jsonReader);
                }

                // 현재 날짜가 데이터가 기록된 시간의 날짜와 다를 때
                // 오늘의 일일퀘스트 데이터 초기화
                if (DateTime.Now.Day != dailyQuest.RefreshTime.Day)
                {
                    serialized = false;
                }
            }
            catch (NullReferenceException) when (dailyQuest is null)
            {
                serialized = false;
            }
            catch (FileNotFoundException)
            {
                serialized = false;
            }
            finally
            {
                if (!serialized)
                {
                    dailyQuest = GetDefaultDailyQuest();
                    WriteTodayDailyQuestDataOnFile(dailyQuest);
                }
            }

            return dailyQuest;
        }

        /// <summary>
        /// 오늘의 일일퀘스트 파일에 매개변수로 받은 데이터를 작성하고 <see cref="RefreshTime"/>을 현재 시간으로 초기화합니다.
        /// </summary>
        /// <param name="dailyQuest"></param>
        public static void WriteTodayDailyQuestDataOnFile(DailyQuest dailyQuest)
        {
            if (dailyQuest is null)
            {
                dailyQuest = new DailyQuest();
            }
            dailyQuest.RefreshTime = DateTime.Now;

            try
            {
                var serializer = new JsonSerializer();
                using Stream stream = File.Open(_todayFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using StreamWriter writer = new StreamWriter(stream);
                using JsonWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
                serializer.Serialize(jsonWriter, dailyQuest, typeof(DailyQuest));
            }
            catch (FileNotFoundException)
            {
                File.Create(_todayFileName);
                WriteTodayDailyQuestDataOnFile(dailyQuest);
            }
        }

        /// <summary>
        /// 기본 일일퀘스트 파일에서 목록을 가져옵니다. 파일이 없다면 파일을 생성합니다.
        /// </summary>
        /// <returns>
        /// 초기화된 기본 일일퀘스트 목록을 반환합니다.<para/>
        /// </returns>
        public static DailyQuest GetDefaultDailyQuest()
        {
            DailyQuest dailyQuest = new DailyQuest
            {
                RefreshTime = DateTime.Now,
                Quests = new List<Quest>()
            };

            try
            {
                using Stream stream = File.Open(_defaultFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                using StreamReader reader = new StreamReader(stream);
                string input;
                while (reader.Peek() >= 0)
                {
                    input = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        dailyQuest.Quests.Add(new Quest { QuestDescription = input });
                    }
                }
            }
            catch (FileNotFoundException)
            {
                File.Create(_defaultFileName);
                dailyQuest = GetDefaultDailyQuest();
            }

            return dailyQuest;
        }
    }
}
