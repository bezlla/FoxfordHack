﻿using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace FoxfordHack.Models.ModelParsingToJson.Answers.MatchGroup
{
    class MatchGroupAnswer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("correct_item_ids")]
        public List<int> CorrectAnswer { get; set; } = new List<int>();
    }
}
