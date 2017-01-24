using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitChangeLog
{
    public class TagSearcher
    {
        public static List<Tag> FindReleaseTags(Regex tagFormat, IRepository repo)
        {
            var commits = repo.Commits.Select(c => c.Sha);
            var commitsSet = new HashSet<string>(commits);
            var releaseTags = repo.Tags
                .Where(tag => tag.IsAnnotated)
                .Where(tag => tagFormat.IsMatch(tag.FriendlyName))
                .Where(tag => commitsSet.Contains(tag.PeeledTarget.Sha))
                .OrderByDescending(tag => tag.Annotation.Tagger.When)
                .ToList();

            return releaseTags;
        }
    }
}