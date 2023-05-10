namespace ChatterinoNightlyUpdateCheckerForDiscord
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Text;
    using System.Text.Json;

    public static class Program
    {
        public const string WEBHOOK_USERNAME = "Chatterino Nightly";
        public const string WEBHOOK_AVATAR_URL = "https://camo.githubusercontent.com/6ca305d42786c9dbd0b76f5ade013601b080d71a598e881b4349dff2eafae6c7/68747470733a2f2f666f757274662e636f6d2f696d672f63686174746572696e6f2d69636f6e2d36342e706e67";
        public const string CHANGELOG_LINK = "https://github.com/Chatterino/chatterino2/blob/master/CHANGELOG.md";
        public const string NIGHTLY_LINK = "https://github.com/Chatterino/chatterino2/releases/tag/nightly-build";
        public const string CONTENT_FORMAT_STRING = "New Nightly Version (Updated: <t:{0}:F>):\nLatest Commit Message: ``{1}`` by ``{2}``\nChangelog: <{3}>\nLink: <{4}>";

        public static async Task Main(string[] args)
        {
            DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
            XmlDocument doc = new XmlDocument();
            using (HttpClient client = new HttpClient())
            {
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
        
        private async Task<HttpResponseMessage> PostDiscordMessage(HttpClient client, long timestamp, string title, string author)
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
            return client.PostAsync(url, new StringContent(JsonSerializer.Serialize<WebhookData>(webhookData), Encoding.UTF8, "application/json"));
        }
    }
}
