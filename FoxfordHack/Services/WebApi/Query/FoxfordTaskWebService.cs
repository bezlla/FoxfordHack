﻿using FoxfordHack.Models.ModelParsingToJson;
using FoxfordHack.Models.ModelParsingToJson.Question;
using FoxfordHack.Services.Converters;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
namespace FoxfordHack.Services.WebApi.Query
{
    class FoxfordTaskWebService : BaseQuery
    {
        private static readonly string DefaultURLForTasks = @"https://foxford.ru/lessons/";
        private int courseActivate { get; set; }
        public FoxfordTaskWebService(string cookie, string token, int countThreads = 10, int delay = 500)
        {
            Cookie = cookie;
            CountThreads = countThreads;
            Delay = delay;
            XCSRFToken = token;
        }
        public async Task<List<TaskFoxford>> GetTasksByLesson(int lessonId)
        {
            var url = $"{DefaultURL}/api/lessons/{lessonId}/tasks";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", Cookie);
            client.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);
            var request = await client.GetAsync(url);
            var jsonString = await request.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<TaskFoxford>>(jsonString);
            return result;
        }
        public async Task<ViewQuestionPage> GetAnswerFromTask(Models.DataBaseModels.TaskFoxford task, int lessonId)
        {
            if (task is null)
                return null;
            var url = $"{DefaultURL}/api/lessons/{lessonId}/tasks/{task.TaskFoxfordId}";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", Cookie);
            client.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);
            client.DefaultRequestHeaders.Add("X-CSRF-Token", XCSRFToken);
            var requests = await client.GetAsync(url);
            var jsonString = await requests.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ViewQuestionPage>(jsonString);
            return result;
        }
        public void FailsAnswerFromTaskByLesson(List<Models.DataBaseModels.TaskFoxford> listTasks, int lessonId)
        {
            if (listTasks is null)
                return;
            var tasks = new Task[Math.Min(listTasks.Count, CountThreads)];
            for (int i = 0; i < Math.Min(listTasks.Count,CountThreads); i++)
            {
                tasks[i] = FailsAnswerFromTasks(listTasks, lessonId, i);
            }
            Task.WaitAll(tasks);
        }
        public async Task<bool> SetAnswerForTask(Models.DataBaseModels.TaskFoxford task, int lessonId)
        {
            var answer = ConverterFromTaskAnswerFromDataBase.StringToDictionary(task.Answer);
            var content = new MultipartFormDataContent("------WebKitFormBoundarycRCuRGn6YA4bC3LZ");
            for (int i = 0; i < answer.Count; i++)
            {
                content.Add(new StringContent($"{answer[i].Value}"), $"{answer[i].Key}");
            }
            var url = $"{DefaultURL}/api/lessons/{lessonId}/tasks/{task.TaskFoxfordId}";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Cookie", Cookie);
                client.DefaultRequestHeaders.Add("X-CSRF-Token", XCSRFToken);
                client.DefaultRequestHeaders.Host = "foxford.ru";
                client.DefaultRequestHeaders.Add("X-Skip-Error-Notification", "true");
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; U; ABrowse 0.6; Syllable) AppleWebKit/420+ (KHTML, like Gecko)");
                var request = await client.GetAsync($"{url}");
                request = await client.PostAsync($"{url}/answer_attempts", content);
                var jsonString = await request.Content.ReadAsStringAsync();
                if (request.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return false;
                }
                return true;
            }

        }
        private async Task FailsAnswerFromTasks(List<Models.DataBaseModels.TaskFoxford> tasks, int lessonId,int startIndex)
        {
            for (int i = startIndex; i < tasks.Count; i+= CountThreads)
            {
                await FailsAnswerFromTask(tasks[i], lessonId, tasks.Count) ;
            }
        }
        public async Task<bool> FailsAnswerFromTask(Models.DataBaseModels.TaskFoxford task,int lessonId,int countTasks)
        {
            var url = $"{DefaultURL}/api/lessons/{lessonId}/tasks/{task.TaskFoxfordId}/fails";
            var urlPage = $"{DefaultURL}/api/lessons/{lessonId}/tasks/{task.TaskFoxfordId}";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", Cookie);
            client.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);
            client.DefaultRequestHeaders.Add("X-CSRF-Token", XCSRFToken);
            client.DefaultRequestHeaders.Host = "foxford.ru";
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            client.DefaultRequestHeaders.Referrer = new Uri($"{DefaultURL}/api/lessons/{lessonId}/tasks/{task.TaskFoxfordId}");
            var request = await client.GetAsync(urlPage);
            var jsonString = await request.Content.ReadAsStringAsync();
            if (jsonString == @"{""errors"":""Доступ запрещён""}")
                return true;
            request = await client.PostAsync(url, null);
            courseActivate++;
            Console.WriteLine($"Completed {courseActivate}/{countTasks}");
            await Task.Delay(Delay);
            return true;
        }
    }
}
