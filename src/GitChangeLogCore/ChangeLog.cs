using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitChangeLog
{
    public class ChangeLog
    {
        public static string CreateChangeLog(Regex tagFormat, Repository repo, string header = "# Release Notes",
            string repoUrl = null, bool sourceLinks = true, bool compareLinks = true)
        {
            var releaseTags = TagSearcher.FindReleaseTags(tagFormat, repo);
            var notes = ChangeLogFormatter.FormatChangeLog(header, releaseTags, repoUrl, sourceLinks, compareLinks);

            return notes;
        }
    }
}