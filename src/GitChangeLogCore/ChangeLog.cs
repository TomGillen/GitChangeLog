using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitChangeLog
{
    public class ChangeLog
    {
        public static string CreateChangeLog(Regex tagFormat, string location = ".git", string header = "# Release Notes", bool sourceLinks = true, bool compareLinks = true)
        {
            var hostBaseUrl = FindOrigin(location);
            var releaseTags = TagSearcher.FindReleaseTags(tagFormat, location);
            var notes = ChangeLogFormatter.FormatChangeLog(header, releaseTags, hostBaseUrl, sourceLinks, compareLinks);

            return notes;
        }

        private static string FindOrigin(string location)
        {
            using (var repo = new Repository(location))
            {
                var origin = repo.Network.Remotes["origin"];
                if (origin != null)
                {
                    var github = new Regex(@"(?<repo_base_url>https://github.com/.*?)\.git");
                    var match = github.Match(origin.Url);
                    if (match.Success)
                        return match.Groups["repo_base_url"].Value;
                }

                return null;
            }
        }
    }
}