using System.Web;

namespace ChatterinoNightlyUpdateChecker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    public static partial class Program
    {
        private const string WebhookUsername = "Chatterino Nightly";
        private const string WebhookAvatarUrl = "https://user-images.githubusercontent.com/41973452/272541622-52457e89-5f16-4c83-93e7-91866c25b606.png";
        private const string NightlyLink = "https://github.com/Chatterino/chatterino2/releases/tag/nightly-build";
        private const string ContentFormatString = "New Nightly Version (Updated: <t:{0}:F>):\nLatest Commit: {1} by {2}\nLink: <{3}>\nPull Request: {4}";

        public static async Task Main(string[] args)
        {
            DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
            using var client = new HttpClient();
            var doc = new XmlDocument();
            doc.LoadXml(await client.GetStringAsync("https://github.com/Chatterino/chatterino2/commits/nightly-build.atom").ConfigureAwait(false));
            var updated = doc.GetElementsByTagName("updated")[0]!.InnerText;
            var updatedDate = DateTime.Parse(updated);
            var timestamp = ((DateTimeOffset)updatedDate).ToUnixTimeSeconds();
            var fileNeedsUpdate = true;
            if (File.Exists("lastUpdatedValue"))
            {
                var lastUpdatedValue = await File.ReadAllTextAsync("lastUpdatedValue").ConfigureAwait(false);
                if (lastUpdatedValue.Trim().Equals(updated.Trim()))
                {
                    Console.WriteLine("Already latest version");
                    fileNeedsUpdate = false;
                }
                else
                {
                    Console.WriteLine("Needs update");
                    var entry = doc.GetElementsByTagName("entry")[0]!;
                    var commitLink = string.Empty;
                    var commitTitle = string.Empty;
                    var description = string.Empty;
                    string? authorName = null;
                    string? authorUrl = null;
                    var coauthors = new List<string>();
                    foreach (XmlNode node in entry.ChildNodes)
                    {
                        Console.WriteLine(node.Name + " - " + node.InnerText);
                        switch (node.Name)
                        {
                            case "link":
                                commitLink = node.Attributes?["href"]?.Value.Trim() ?? string.Empty;
                                break;
                            case "title":
                                commitTitle = node.InnerText.Trim();
                                break;
                            case "author":
                                foreach (XmlNode option in node.ChildNodes)
                                {
                                    switch (option.Name)
                                    {
                                        case "name":
                                            authorName = option.InnerText.Trim();
                                            break;
                                        case "uri":
                                            authorUrl = option.InnerText.Trim();
                                            break;
                                    }
                                }
                                continue;
                            case "content":
                                description = node.InnerText;
                                coauthors.AddRange(from line in description.Split('\n') where line.StartsWith("Co-authored-by:") select line["Co-authored-by:".Length..] into coauthor select coauthor[..coauthor.IndexOf("&lt;", StringComparison.InvariantCulture)].Trim());
                                break;
                        }
                    }
                    string commit;
                    if (string.IsNullOrEmpty(commitTitle))
                    {
                        commit = string.IsNullOrEmpty(commitLink) ? "`N/A`" : $"<{commitLink}>";
                    }
                    else
                    {
                        commit = string.IsNullOrEmpty(commitLink) ? $"``{commitTitle}``" : $"[``{commitTitle}``](<{commitLink}>)";
                    }
                    string author;
                    if (authorName is null)
                    {
                        author = authorUrl is null ? "N/A" : $"<{authorUrl}>";
                    }
                    else
                    {
                        author = authorUrl is null ? $"``{authorName}``" : $"[{authorName}](<{authorUrl}>)";
                    }
                    if (coauthors.Count > 0)
                    {
                        author += " (with ";
                        var first = true;
                        foreach (var coauthor in coauthors)
                        {
                            if (first)
                            {
                                author += $"``{coauthor}``";
                            }
                            else
                            {
                                author += $", ``{coauthor}``";
                            }
                            first = false;
                        }
                        author += ")";
                    }
                    description = HtmlToPlainText(description);
                    if (authorName != "dependabot")
                        Console.WriteLine((await PostDiscordMessage(client, timestamp, commit, author, description)).StatusCode);
                }
            }
            else
            {
                Console.WriteLine("File does not exist");
            }
            if (fileNeedsUpdate) await File.WriteAllTextAsync("lastUpdatedValue", updated);
        }

        private static string HtmlToPlainText(string html)
        {
            Console.WriteLine(html);
            var plainTextMatch = PreHtmlRegex().Match(html);
            return plainTextMatch.Success ? HttpUtility.HtmlDecode(plainTextMatch.Groups[1].Value.Trim()) : html;
        }

        private static async Task<HttpResponseMessage> PostDiscordMessage(HttpClient client, long timestamp, string commit, string author, string description)
        {
            var url = $"{Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL")}?wait=true";
            var pr = "`N/A`";
            var pattern = PrNumberRegex();
            var match = pattern.Match(commit);
            if (match.Success)
            {
                var prNumber = match.Groups[1].Value;
                pr = $"[#{prNumber}](<https://github.com/Chatterino/chatterino2/pull/{prNumber}>)";
            }
            Console.WriteLine(ContentFormatString, timestamp, commit, author, NightlyLink, pr);
            var dcContent = string.Format(ContentFormatString, timestamp, commit, author, NightlyLink, pr);
            if (!string.IsNullOrEmpty(description))
            {
                dcContent += $"\nDescription:\n```\n{description}\n```";
            }
            var webhookData = new WebhookData
            {
                Username = WebhookUsername,
                AvatarUrl = WebhookAvatarUrl,
                AllowedMentions = new Dictionary<string, string[]>{
                    { "parse", [] }
                },
                Content = dcContent
            };
            var webhookJson = JsonSerializer.Serialize(webhookData);
            var content = new StringContent(webhookJson, Encoding.UTF8, "application/json");
            return await client.PostAsync(url, content);
        }

        [GeneratedRegex(@"<pre.+?>((?:.|\n)+)<\/pre>")]
        private static partial Regex PreHtmlRegex();
        [GeneratedRegex(@"``.+\(#(\d+)\)``")]
        private static partial Regex PrNumberRegex();
    }
}
