using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;

namespace GitChangeLog
{
    public struct GithubRepo
    {
        public string Url { get; set; }
        public string Owner { get; set; }
        public string Name { get; set; }
    }

    public class Github
    {
        private const string ProductHeader = "git-change-log";

        public static GithubRepo? FindGithubOrigin(LibGit2Sharp.IRepository repo, string remoteName = "origin")
        {
            var remote = repo.Network.Remotes[remoteName];
            if (remote != null)
            {
                var httpUrl = new Regex(@"(?<repo_base_url>https://github.com/(?<owner>[^/]+)/(?<name>[^/]+))\.git");
                var httpMatch = httpUrl.Match(remote.Url);
                if (httpMatch.Success)
                {
                    return new GithubRepo
                    {
                        Url = httpMatch.Groups["repo_base_url"].Value,
                        Owner = httpMatch.Groups["owner"].Value,
                        Name = httpMatch.Groups["name"].Value
                    };
                }

                var sshUrl = new Regex(@"git@github.com:(?<owner>[^/]+)/(?<name>[^/]+)\.git");
                var sshMatch = sshUrl.Match(remote.Url);
                if (sshMatch.Success)
                {
                    var owner = sshMatch.Groups["owner"].Value;
                    var name = sshMatch.Groups["name"].Value;
                    return new GithubRepo
                    {
                        Url = $"https://github.com/{owner}/{name}",
                        Owner = owner,
                        Name = name
                    };
                }
            }

            return null;
        }

        public static Task CreateGithubReleases(
            LibGit2Sharp.IRepository repo, string username, string password,
            string owner, string repoName, Regex tagFormat, bool draft)
        {
            var auth = new Credentials(username, password);
            var client = CreateClient(auth);

            return CreateGithubReleases(repo, client, owner, repoName, tagFormat, draft);
        }

        public static Task CreateGithubReleases(
            LibGit2Sharp.IRepository repo, string token,
            string owner, string repoName, Regex tagFormat, bool draft)
        {
            var auth = new Credentials(token);
            var client = CreateClient(auth);

            return CreateGithubReleases(repo, client, owner, repoName, tagFormat, draft);
        }

        private static async Task CreateGithubReleases(LibGit2Sharp.IRepository repo, IGitHubClient client, string owner,
            string repoName, Regex tagFormat, bool draft)
        {
            var releaseTags = TagSearcher.FindReleaseTags(tagFormat, repo);
            var releases = (await client.Repository.Release.GetAll(owner, repoName)).ToList();

            releaseTags.Reverse();
            var missing = releaseTags.Where(tag => releases.All(release => release.TagName != tag.FriendlyName));
            foreach (var tag in missing)
            {
                await client.Repository.Release.Create(owner, repoName, new NewRelease(tag.FriendlyName)
                {
                    Name = tag.FriendlyName,
                    Body = tag.Annotation.Message,
                    Prerelease = tag.FriendlyName.Contains("-"),
                    Draft = draft
                });
            }
        }

        public static Task SyncGithubReleases(
            LibGit2Sharp.IRepository repo, string username, string password,
            string owner, string repoName, bool force)
        {
            var auth = new Credentials(username, password);
            var client = CreateClient(auth);

            return SyncGithubReleases(repo, client, owner, repoName, force);
        }

        public static Task SyncGithubReleases(
            LibGit2Sharp.IRepository repo, string token,
            string owner, string repoName, bool force)
        {
            var auth = new Credentials(token);
            var client = CreateClient(auth);

            return SyncGithubReleases(repo, client, owner, repoName, force);
        }

        private static GitHubClient CreateClient(Credentials credentials)
        {
            return new GitHubClient(new ProductHeaderValue(ProductHeader))
            {
                Credentials = credentials
            };
        }

        private static async Task SyncGithubReleases(LibGit2Sharp.IRepository repo, IGitHubClient client, string owner, string repoName, bool force)
        {
            var releases = await client.Repository.Release.GetAll(owner, repoName);
            
            foreach (var release in releases)
            {
                var tag = repo.Tags[release.TagName];
                if (tag == null || !tag.IsAnnotated)
                    continue;

                var hasContentToSync = !string.IsNullOrWhiteSpace(tag.Annotation.Message) &&
                                        string.IsNullOrWhiteSpace(release.Body);

                if (force || hasContentToSync)
                {
                    await client.Repository.Release.Edit(owner, repoName, release.Id, new ReleaseUpdate
                    {
                        Name = tag.FriendlyName,
                        Body = tag.Annotation.Message,
                        Prerelease = tag.FriendlyName.Contains("-")
                    });
                }
            }
        }
    }
}
