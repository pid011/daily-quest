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

namespace DailyQuestChecker
{
    internal class Program
    {
        /// <summary>
        /// 완료한 항목에 표시될 문자
        /// </summary>
        private const string CheckMarkEmoji = "✔️";
        private const string CheckMarkText = "x";
        /// <summary>
        /// 완료되지 않은 항목에 표시될 문자
        /// </summary>
        private const string CrossMarkEmoji = "❌";
        private const string CrossMarkText = " ";

        private static readonly Dictionary<string, string> _commands = new Dictionary<string, string>
        {
            ["check"] = "선택한 항목을 체크하거나 체크 해제합니다.",
            ["reset"] = "오늘의 일일퀘스트를 초기화합니다.",
            ["-v"] = "일일퀘스트 체크 프로그램의 버전을 표시합니다.",
            ["-h"] = "일일퀘스트 체크 프로그램의 도움말을 표시합니다.",
        };

        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            DailyQuestItem item;

            // 인자를 입력하지 않고 그냥 프로그램을 실행할 경우 그냥 일퀘목록만 출력
            if (args.Length == 0)
            {
                item = DailyQuest.GetTodayDailyQuest();
                PrintDailyQuest(item);
            }
            else
            {
                switch (args[0])
                {
                    case "check":
                        item = DailyQuest.GetTodayDailyQuest();
                        RunCheckCommand(ref item, args);
                        PrintDailyQuest(item);
                        break;

                    case "reset":
                        item = DailyQuest.GetTodayDailyQuest();

                        // Reset명령어 실행 후 유저가 초기화를 하면 초기화된 목록 출력
                        if (RunResetCommand(ref item))
                        {
                            PrintDailyQuest(item);
                        }
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
        }

        private static void RunCheckCommand(ref DailyQuestItem item, string[] args)
        {
            // args의 개수가 1개이면 항목번호를 입력하지 않은 것이므로 명령어 사용방법 출력
            if (args.Length <= 1)
            {
                Console.WriteLine("명령어 사용방법: check [항목번호] (1개 이상의 항목번호 입력 가능)");
                return;
            }

            List<int> integers = new List<int>(args.Length - 1);
            List<int> goodNumbers = new List<int>(integers.Count);

            // 정수가 아닌 입력 걸러내기
            for (int i = 1; i < args.Length; i++)
            {
                if (int.TryParse(args[i], out int result))
                {
                    integers.Add(result);
                }
            }
            // 입력된 숫자를 오름차순으로 정렬
            integers.Sort();

            // 중복된 숫자를 제거하고 반복문 실행
            foreach (var i in integers.Distinct())
            {
                try
                {
                    // 해당 항목의 bool 값을 반대 값으로 변경
                    item.Quests[i - 1].HasDone = !item.Quests[i - 1].HasDone;
                    // 값을 변경의 항목의 번호 추가
                    goodNumbers.Add(i);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // 만약 입력된 번호가 현재 존재하는 일일퀘스트 번호와 맞지 않으면 그냥 넘어가기
                    continue;
                }
            }

            // goodNumbers의 개수가 0개면 아무런 항목도 수정되지 않은 것임
            if (goodNumbers.Count == 0)
            {
                Console.WriteLine("제대로 된 번호를 입력하지 않아 아무런 항목도 수정되지 않았습니다.");
            }
            else
            {
                DailyQuest.WriteFileAndRefreshTime(ref item);
                Console.WriteLine($"{string.Join(", ", goodNumbers)}번 항목이 수정 되었습니다.");
            }
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
                        // 파일에서 기본 일일퀘스트 목록을 가져오기
                        item = DailyQuest.GetDefaultDailyQuest();
                        // 오늘의 일일퀘스트 파일에 기본 일일퀘스트 데이터 쓰기
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
            Console.WriteLine($"{assm.Name} v{assm.Version.ToString(3)}"); // 프로그램의 버전 출력
        }

        private static void PrintDailyQuest(DailyQuestItem item)
        {
            StringBuilder builder = new StringBuilder().AppendLine();
            if (item.Quests.Count == 0) // 갯수가 0이면 목록이 존재하지 않는다는 메시지 출력
            {
                builder
                    .AppendLine("현재 일일퀘스트 목록이 없습니다.")
                    .AppendLine("만약 기본 일일퀘스트 목록을 변경하였으면 reset 명령어를 실행해보세요.");
            }
            else
            {
                // 목록의 마지막 항목 번호를 뜻하는 목록의 갯수의 자리수 가져오기
                int max = GetDigitLength(item.Quests.Count);
                int hasDoneCount = 0;

                for (int i = 0; i < item.Quests.Count; i++)
                {
                    bool hasDone = item.Quests[i].HasDone;
                    if (hasDone)
                    {
                        hasDoneCount++;
                    }

                    int length = GetDigitLength(i + 1);
                    string number = (i + 1).ToString();
                    // 오른쪽 정렬
                    number = number.PadLeft(max - length + number.Length);
                    builder.Append($"  {number}. ");
                    if (item.UseEmoji)
                    {
                        builder.Append($"[{(hasDone ? CheckMarkEmoji : CrossMarkEmoji)}] - ");
                    }
                    else
                    {
                        builder.Append($"[{(hasDone ? CheckMarkText : CrossMarkText)}] - ");
                    }
                    builder.AppendLine(item.Quests[i].QuestDescription);
                }
                builder.AppendLine();

                // 모든 항목이 체크 되어있을 때
                if (hasDoneCount == item.Quests.Count)
                {
                    if (item.UseEmoji)
                    {
                        builder.AppendLine("🎉🎉오늘의 일일퀘스트를 모두 끝냈습니다!🎉🎉");
                    }
                    else
                    {
                        builder.AppendLine(":::오늘의 일일퀘스트를 모두 끝냈습니다!:::");
                    }
                }
                else
                {
                    builder.AppendLine($"현재 총 {item.Quests.Count}개의 항목 중 {hasDoneCount}개의 항목을 완료했습니다.");

                    DateTime now = DateTime.Now;
                    int hour = 23 - now.Hour;
                    int minute = 59 - now.Minute;
                    int second = 59 - now.Second;
                    builder.AppendLine($"자정까지 {hour}시간 {minute}분 {second}초 남았습니다. 파이팅!{(item.UseEmoji ? "👊" : "")}");
                }
            }

            Console.WriteLine(builder.ToString());

            // 자연수의 자릿수를 구하는 로컬 함수
            static int GetDigitLength(int n) => n < 1 ? 0 : (int)Math.Log10(n) + 1;
        }
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

    public class DailyQuest
    {
        /// <summary>
        /// 오늘의 일일퀘스트 항목을 가지고 있는 파일이름
        /// </summary>
        private const string TodayDailyQuestFileName = "today-daily-quests.daily";

        /// <summary>
        /// 기본 일일퀘스트 리스트를 가지고 있는 파일이름
        /// </summary>
        private const string DefaultDailyQuestFileName = "daily-quests.txt";

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

                // 디렉토리가 존재하지 않으면 새로 만들고 파일경로 반환
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

                // 디렉토리가 존재하지 않으면 새로 만들고 파일경로 반환
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
                // 파일에서 json으로 구성된 오늘의 일일퀘스트 목록 가져오기
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
                // json 데이터를 DailyQuestItem으로 역직렬화
                item = JsonSerializer.Deserialize<DailyQuestItem>(ref utf8Reader);

                // 현재 날짜가 데이터가 기록된 시간의 날짜와 다를 때
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
                // 기본 일일퀘스트로 다시 덮어쓰기
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
            // item이 null일 경우 새 인스턴스 생성
            item ??= new DailyQuestItem();
            item.Quests ??= new List<Quest>();
            // item 객체의 RefreshTime을 현재로 수정
            item.RefreshTime = DateTime.Now;

            // 데이터를 새로 덮어쓰기
            using FileStream fs = File.Create(TodayDailyQuestFilePath);
            // item을 json으로 직렬화 후 파일에 쓰기
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

                // 파일의 첫 줄에서 이모지 사용 여부 읽어오기
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

                // 파일에서 한 줄씩 읽어오기
                while ((input = sr.ReadLine()) != null)
                {
                    // 읽어온 문자열이 비어있거나 띄어쓰기만 있으면 건너뛰기
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        dailyQuest.Quests.Add(new Quest { QuestDescription = input });
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // 파일이 존재하지 않는 경우 새로 만들고 처음에 생성한 인스턴스를 그대로 반환하기
                File.Create(DefaultDailyQuestFilePath);
            }

            return dailyQuest;
        }
    }
}
