using System.Text.RegularExpressions;

namespace GitChangeLog
{
    public class ChangeLog
    {
        public static string CreateChangeLog(Regex tagFormat, string location = ".git")
        {
            var releaseTags = TagSearcher.FindReleaseTags(tagFormat);
            var notes = ChangeLogFormatter.FormatChangeLog(releaseTags);

            return notes;
        }
    }
}