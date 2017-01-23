using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace GitChangeLog
{
    public class ChangeLogFormatter
    {
        public static string FormatChangeLog(List<Tag> releases)
        {
            var releaseNotes = new StringBuilder();

            releaseNotes.AppendLine("# Release Notes");
            releaseNotes.AppendLine();
            
            foreach (var release in releases)
            {
                var name = release.FriendlyName;
                var date = release.Annotation.Tagger.When.UtcDateTime;
                var message = StripTitle(name, release.Annotation.Message).Trim();
                
                releaseNotes.AppendLine($"## {name} ({date:yyyy-MM-dd})");
                releaseNotes.AppendLine();
                releaseNotes.AppendLine(message);
                releaseNotes.AppendLine();
            }

            return releaseNotes.ToString();
        }

        private static string StripTitle(string title, string message)
        {
            var matcher = new Regex(@"^#*\s*" + title + ".*", RegexOptions.IgnoreCase);
            if (matcher.IsMatch(message))
            {
                var newLine = message.TrimStart().IndexOf('\n');
                if (newLine != -1)
                    message = message.Substring(newLine + 1);
            }

            return message;
        }
    }
}
