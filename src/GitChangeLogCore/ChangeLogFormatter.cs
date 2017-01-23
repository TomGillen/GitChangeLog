using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitChangeLog
{
    public class ChangeLogFormatter
    {
        public static string FormatChangeLog(
            string header, 
            List<Tag> releases, 
            string githubUrl, 
            bool sourceLinks,
            bool compareLinks)
        {
            var releaseNotes = new StringBuilder();

            if (!string.IsNullOrEmpty(header))
            {
                releaseNotes.AppendLine(header);
                releaseNotes.AppendLine();
            }

            for (var i = 0; i < releases.Count; i++)
            {
                var release = releases[i];

                var name = release.FriendlyName;
                var date = release.Annotation.Tagger.When.UtcDateTime;
                var message = StripTitle(name, release.Annotation.Message).Trim();

                if (sourceLinks && !string.IsNullOrEmpty(githubUrl))
                {
                    releaseNotes.AppendLine($"## [{name}]({githubUrl}/tree/{release.PeeledTarget.Sha}) ({date:yyyy-MM-dd})");
                }
                else
                    releaseNotes.AppendLine($"## {name} ({date:yyyy-MM-dd})");

                if (compareLinks && !string.IsNullOrEmpty(githubUrl) && (i < releases.Count - 1))
                {
                    var previous = releases[i + 1];
                    releaseNotes.AppendLine($"[Compare with {previous.FriendlyName}]({githubUrl}/compare/{previous.PeeledTarget.Sha}...{release.PeeledTarget.Sha})");
                }

                releaseNotes.AppendLine();
                releaseNotes.AppendLine(message);
                releaseNotes.AppendLine();
            }

            return releaseNotes.ToString().Trim();
        }

        private static string StripTitle(string title, string message)
        {
            var matcher = new Regex(@"^#*\s*\[?" + title + ".*", RegexOptions.IgnoreCase);
            if (matcher.IsMatch(message))
            {
                message = message.TrimStart();
                var newLine = message.IndexOf('\n');
                if (newLine != -1)
                    message = message.Substring(newLine + 1);
            }

            return message;
        }
    }
}