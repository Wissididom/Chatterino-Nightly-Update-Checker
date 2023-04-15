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
                doc.LoadXml(await client.GetStringAsync("https://github.com/Chatterino/chatterino2/releases.atom"));
                foreach (XmlNode entry in doc.DocumentElement!.ChildNodes)
                {
                    if (entry.Name == "entry")
                    {
                        if (entry["title"]!.InnerText == "Nightly Release")
                        {
                            DateTime dt = DateTime.Now.AddDays(-1);
                            string updated = entry["updated"]!.InnerText.Trim();
                            DateTime updatedDate = DateTime.Parse(updated);
                            long timestamp = ((DateTimeOffset)updatedDate).ToUnixTimeSeconds();
                            if (File.Exists("lastUpdatedValue"))
                            {
                                string lastUpdatedValue = File.ReadAllText("lastUpdatedValue");
                                if (!lastUpdatedValue.Trim().Equals(updated.Trim())) {
                                    XmlDocument commits = new XmlDocument();
                                    commits.LoadXml(await client.GetStringAsync("https://github.com/Chatterino/chatterino2/commits/master.atom"));
                                    foreach (XmlNode commit in commits.DocumentElement!.ChildNodes) {
                                        if (commit.Name == "entry") {
                                            string title = commit["title"]!.InnerText.Trim(); // Latest Commit Title
                                            XmlNode? authorNameNode = null;
                                            foreach (XmlNode a in commit["author"]!.ChildNodes) {
                                                if (a.Name == "name") authorNameNode = a;
                                            }
                                            string author = authorNameNode!.InnerText.Trim(); // Latest Commit Author
                                            WebhookData webhookData = new WebhookData{
                                                Username = WEBHOOK_USERNAME,
                                                AvatarUrl = WEBHOOK_AVATAR_URL,
                                                AllowedMentions = new Dictionary<string, string[]>{
                                                    { "parse", new string[0] }
                                                },
                                                Content = String.Format(CONTENT_FORMAT_STRING, timestamp, title, author, CHANGELOG_LINK, NIGHTLY_LINK)
                                            };
                                            Console.WriteLine(client.PostAsync($"{Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL")}?wait=true", new StringContent(JsonSerializer.Serialize<WebhookData>(webhookData), Encoding.UTF8, "application/json")).Result.StatusCode);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Already latest version!");
                            }
                            File.WriteAllText("lastUpdatedValue", updated);
                        }
                    }
                }
            }
        }
    }
}
