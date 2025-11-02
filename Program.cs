using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChatterinoNightlyUpdateChecker
{
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
            client.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Wissididom/Chatterino-Nightly-Update-Checker");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            var commits = JsonSerializer.Deserialize<ListCommitsResponse>(await client.GetStringAsync("https://api.github.com/repos/Chatterino/chatterino2/commits?sha=nightly-build").ConfigureAwait(false));
            if (commits is null)
            {
                Console.WriteLine("Failed to parse json!");
                return;
            }
            var commit = commits[0];
            var updated = commit.Commit?.Committer?.Date;
            if (updated is null)
            {
                Console.WriteLine("Failed to get latest commit date!");
                return;
            }
            var timestamp = ((DateTimeOffset)updated).ToUnixTimeSeconds().ToString();
            var fileNeedsUpdate = true;
            if (File.Exists("lastUpdatedValue"))
            {
                var lastUpdatedValue = (await File.ReadAllTextAsync("lastUpdatedValue").ConfigureAwait(false)).Trim();
                if (lastUpdatedValue.Equals(timestamp))
                {
                    Console.WriteLine("Already latest version");
                    fileNeedsUpdate = false;
                }
                else
                {
                    Console.WriteLine("Needs update");
                    var description = commit.Commit?.Message ?? string.Empty;
                    var commitTitle = description.Split('\n')[0].Trim();
                    string commitString;
                    if (string.IsNullOrEmpty(commitTitle))
                    {
                        commitString = commit.HtmlUrl ?? "`N/A`";
                    }
                    else
                    {
                        commitString = commit.HtmlUrl is null ? $"``{commitTitle}``" : $"[``{commitTitle}``](<{commit.HtmlUrl}>)";
                    }
                    var coauthors = new List<string>();
                    if (!string.IsNullOrEmpty(description))
                    {
                        coauthors.AddRange(from line in description.Split('\n') where line.StartsWith("Co-authored-by:") select line["Co-authored-by:".Length..] into coauthor select coauthor[..coauthor.IndexOf("&lt;", StringComparison.InvariantCulture)].Trim());
                    }
                    var authorName = commit.Commit?.Author?.Name ?? commit.Commit?.Committer?.Name;
                    string author;
                    if (authorName is null)
                    {
                        author = commit.Author?.HtmlUrl is null ? "N/A" : $"<{commit.Author.HtmlUrl}>";
                    }
                    else
                    {
                        author = commit.Author?.HtmlUrl is null ? $"``{authorName}``" : $"[{authorName}](<{commit.Author.HtmlUrl}>)";
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
                    if (authorName != "dependabot" && authorName != "dependabot[bot]")
                        Console.WriteLine((await PostDiscordMessage(client, timestamp, commitString, author, description)).StatusCode);
                }
                if (fileNeedsUpdate) await File.WriteAllTextAsync("lastUpdatedValue", timestamp);
            }
            else
            {
                Console.WriteLine("File does not exist");
            }
            if (fileNeedsUpdate) await File.WriteAllTextAsync("lastUpdatedValue", timestamp);
        }

        private static async Task<HttpResponseMessage> PostDiscordMessage(HttpClient client, string timestamp, string commit, string author, string description)
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

        [GeneratedRegex(@"``.+\(#(\d+)\)``")]
        private static partial Regex PrNumberRegex();
    }
}
