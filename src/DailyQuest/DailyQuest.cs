// Copyright (c) Sepi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace DailyQuest
{
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
            item.Quests ??= new List<DailyQuestItem.Quest>();
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
                Quests = new List<DailyQuestItem.Quest>()
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
                        dailyQuest.Quests.Add(new DailyQuestItem.Quest { QuestDescription = input });
                    }
                }

                while ((input = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        dailyQuest.Quests.Add(new DailyQuestItem.Quest { QuestDescription = input });
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
}
