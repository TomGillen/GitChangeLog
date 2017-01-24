using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;
using LibGit2Sharp;

namespace GitChangeLog.CommandLine
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var parsed = Parser.Default.ParseArguments<CollectOptions, GithubReleaseOptions, GithubReleaseSyncOptions>(args);
            
            var result = parsed.MapResult(
                (CollectOptions options) => Collect(options),
                (GithubReleaseOptions options) => GithubRelease(options),
                (GithubReleaseSyncOptions options) => GithubReleaseSync(options),
                _ => 1);

            return result;
        }

        private static int Collect(CollectOptions options)
        {
            using (var repo = new Repository(options.Repository))
            {
                string baseUrl = null;
                if (options.SourceLinks || options.CompareLinks)
                {
                    if (string.IsNullOrEmpty(options.Owner) || string.IsNullOrEmpty(options.RepoName))
                        baseUrl = Github.FindGithubOrigin(repo, options.Remote)?.Url;
                    else
                        baseUrl = $"https://github.com/{options.Owner}/{options.RepoName}.git";

                    if (baseUrl == null)
                    {
                        Console.WriteLine($"Cannot find github URL at remote '{options.Remote}' and no owner and name specified.");
                        return 1;
                    }
                }

                var releaseTagFormat = new Regex(options.TagFormat);
                var notes = ChangeLog.CreateChangeLog(releaseTagFormat, repo, options.Header, baseUrl, options.SourceLinks, options.CompareLinks);
                File.WriteAllText(options.Output, notes);

                return 0;
            }
        }

        private static int GithubRelease(GithubReleaseOptions options)
        {
            using (var repo = new Repository(options.Repository))
            {
                if (string.IsNullOrEmpty(options.Owner) || string.IsNullOrEmpty(options.RepoName))
                {
                    var remote = Github.FindGithubOrigin(repo, options.Remote);
                    if (remote == null)
                    {
                        Console.WriteLine($"Cannot find github URL at remote '{options.Remote}' and no owner and name specified.");
                        return 1;
                    }

                    options.Owner = remote?.Owner;
                    options.RepoName = remote?.Name;
                }

                var tagFormat = new Regex(options.TagFormat);

                if (options.Token != null)
                    Github.CreateGithubReleases(repo, options.Token, options.Owner, options.RepoName, tagFormat, options.Draft).Wait();
                else if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
                    Github.CreateGithubReleases(repo, options.Username, options.Password, options.Owner, options.RepoName, tagFormat, options.Draft).Wait();
                else
                    Console.WriteLine("A GitHub username/password or OAuth token must be provided");
            }

            return 0;
        }

        private static int GithubReleaseSync(GithubReleaseSyncOptions options)
        {
            using (var repo = new Repository(options.Repository))
            {
                if (string.IsNullOrEmpty(options.Owner) || string.IsNullOrEmpty(options.RepoName))
                {
                    var remote = Github.FindGithubOrigin(repo, options.Remote);
                    if (remote == null)
                    {
                        Console.WriteLine($"Cannot find github URL at remote '{options.Remote}' and no owner and name specified.");
                        return 1;
                    }

                    options.Owner = remote?.Owner;
                    options.RepoName = remote?.Name;
                }

                if (options.Token != null)
                    Github.SyncGithubReleases(repo, options.Token, options.Owner, options.RepoName, options.Force).Wait();
                else if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
                    Github.SyncGithubReleases(repo, options.Username, options.Password, options.Owner, options.RepoName, options.Force).Wait();
                else
                    Console.WriteLine("A GitHub username/password or OAuth token must be provided");
            }

            return 0;
        }
    }

    internal interface IOptions
    {
        [Option('r', "repo",
            Default = ".git",
            HelpText = "Path to the git repository.")]
        string Repository { get; set; }

        [Option("remote",
            Default = "origin",
            HelpText = "The remote to inspect to find the GitHub repository location.")]
        string Remote { get; set; }

        [Option("owner",
            HelpText = "The GitHub repository owner. When omitted, the repository location will be inferred from the local repository's remotes.")]
        string Owner { get; set; }

        [Option("name",
            HelpText = "The GitHub repository name. When omitted, the repository location will be inferred from the local repository's remotes.")]
        string RepoName { get; set; }
    }

    [Verb("collect", HelpText = "Collects all annotated tags into a change log and writes the resulting log to file")]
    internal class CollectOptions : IOptions
    {
        public string Repository { get; set; }
        public string Remote { get; set; }
        public string Owner { get; set; }
        public string RepoName { get; set; }

        [Option('t', "tag-format",
            Default = @"[vV]\d\.\d(\.\d){0,2}",
            HelpText = "Regex format for release tags")]
        public string TagFormat { get; set; }

        [Option('o', "output",
            Default = "CHANGELOG.md",
            HelpText = "Output change log file name")]
        public string Output { get; set; }

        [Option("header",
            HelpText = "Change log file header")]
        public string Header { get; set; }       

        [Option("source-links",
            Default = false,
            HelpText = "Enable links to source tree on github hosted repositories")]
        public bool SourceLinks { get; set; }

        [Option("compare-links",
            Default = false,
            HelpText = "Enable links to comparisons with previous releases on github hosted repositories")]
        public bool CompareLinks { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Write plain change log", new CollectOptions());
                yield return new Example("Write change log with heading", new CollectOptions {Header = "# Release Notes"});
                yield return new Example("Write fancy log", new CollectOptions {Header = "# Release Notes", SourceLinks = true, CompareLinks = true});
            }
        }
    }

    [Verb("release", HelpText = "Creates missing GitHub releases for all release tags")]
    internal class GithubReleaseOptions : IOptions
    {
        public string Repository { get; set; }
        public string Remote { get; set; }
        public string Owner { get; set; }
        public string RepoName { get; set; }

        [Option("token",
            HelpText = "Github OAuth access token",
            SetName = "auth")]
        public string Token { get; set; }

        [Option('u', "username",
            HelpText = "GitHub authentication username")]
        public string Username { get; set; }

        [Option('p', "password",
            HelpText = "GitHub authentication password",
            SetName = "auth")]
        public string Password { get; set; }

        [Option('t', "tag-format",
            Default = @"[vV]\d\.\d(\.\d){0,2}",
            HelpText = "Regex format for release tags")]
        public string TagFormat { get; set; }

        [Option("draft",
            HelpText = "Create draft releases")]
        public bool Draft { get; set; }
    }

    [Verb("release-sync", HelpText = "Updates GitHub releases with information stored in their associated annotated tags")]
    internal class GithubReleaseSyncOptions : IOptions
    {
        public string Repository { get; set; }
        public string Remote { get; set; }
        public string Owner { get; set; }
        public string RepoName { get; set; }

        [Option("token",
            HelpText = "Github OAuth access token",
            SetName = "auth")]
        public string Token { get; set; }

        [Option('u', "username",
            HelpText = "GitHub authentication username")]
        public string Username { get; set; }

        [Option('p', "password",
            HelpText = "GitHub authentication password",
            SetName = "auth")]
        public string Password { get; set; }

        [Option('f', "force",
            Default = false,
            HelpText = "Always replace release information with tag information")]
        public bool Force { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Fill missing release descriptions", new GithubReleaseSyncOptions { Token = "a1b2c3d4" });
                yield return new Example("Username/password auth", new GithubReleaseSyncOptions { Username = "JSmith", Password = "Password1" });
                yield return new Example("Force replace release descriptions", new GithubReleaseSyncOptions {Token = "a1b2c3d4", Force=true});
            }
        }
    }
}