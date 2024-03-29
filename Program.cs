﻿namespace ChatterinoNightlyUpdateCheckerForDiscord
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Text;
    using System.Text.Json;

    public static class Program
    {
        public const string WEBHOOK_USERNAME = "Chatterino Nightly";
        public const string WEBHOOK_AVATAR_URL = "https://user-images.githubusercontent.com/41973452/272541622-52457e89-5f16-4c83-93e7-91866c25b606.png";
        public const string CHANGELOG_LINK = "https://github.com/Chatterino/chatterino2/blob/master/CHANGELOG.md";
        public const string NIGHTLY_LINK = "https://github.com/Chatterino/chatterino2/releases/tag/nightly-build";
        public const string CONTENT_FORMAT_STRING = "New Nightly Version (Updated: <t:{0}:F>):\nLatest Commit Message: ``{1}`` by ``{2}``\nChangelog: <{3}>\nLink: <{4}>";

        public static async Task Main(string[] args)
        {
            DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
            using (HttpClient client = new HttpClient())
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(await client.GetStringAsync("https://github.com/Chatterino/chatterino2/commits/nightly-build.atom"));
                string updated = doc.GetElementsByTagName("updated")[0]!.InnerText;
                DateTime updatedDate = DateTime.Parse(updated);
                long timestamp = ((DateTimeOffset)updatedDate).ToUnixTimeSeconds();
                bool fileNeedsUpdate = true;
                if (File.Exists("lastUpdatedValue"))
                {
                    string lastUpdatedValue = File.ReadAllText("lastUpdatedValue");
                    if (lastUpdatedValue.Trim().Equals(updated.Trim()))
                    {
                        Console.WriteLine("Already latest version");
                        fileNeedsUpdate = false;
                    }
                    else
                    {
                        Console.WriteLine("Needs update");
                        XmlNode entry = doc.GetElementsByTagName("entry")[0]!;
                        string title = "N/A";
                        string author = "N/A";
                        foreach (XmlNode node in entry.ChildNodes)
                        {
                            if (node.Name == "title") title = node.InnerText.Trim();
                            if (node.Name == "author")
                            {
                                foreach (XmlNode option in node.ChildNodes)
                                {
                                    if (option.Name == "name") author = option.InnerText.Trim();
                                }
                            }
                        }
                        if (author != "dependabot")
                            Console.WriteLine((await PostDiscordMessage(client, timestamp, title, author)).StatusCode);
                    }
                }
                else
                {
                    Console.WriteLine("File does not exist");
                }
                if (fileNeedsUpdate) File.WriteAllText("lastUpdatedValue", updated);
            }
        }
        
        private static async Task<HttpResponseMessage> PostDiscordMessage(HttpClient client, long timestamp, string title, string author)
        {
            string url = $"{Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL")}?wait=true";
            WebhookData webhookData = new WebhookData
            {
                Username = WEBHOOK_USERNAME,
                AvatarUrl = WEBHOOK_AVATAR_URL,
                AllowedMentions = new Dictionary<string, string[]>{
                    { "parse", new string[0] }
                },
                Content = String.Format(CONTENT_FORMAT_STRING, timestamp, title, author, CHANGELOG_LINK, NIGHTLY_LINK)
            };
            string webhookJson = JsonSerializer.Serialize<WebhookData>(webhookData);
            StringContent content = new StringContent(webhookJson, Encoding.UTF8, "application/json");
            return await client.PostAsync(url, content);
        }
    }
}
