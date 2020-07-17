// Copyright (c) Sepi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DailyQuest
{
    public class DailyQuestItem
    {
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
}
