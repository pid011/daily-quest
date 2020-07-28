// Copyright (c) Sepi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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

        private static readonly Dictionary<string, string> s_commands = new Dictionary<string, string>
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

            foreach (int i in sortedIntegers.Distinct())
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
            foreach (KeyValuePair<string, string> item in s_commands)
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
}
