using System.IO;
using System.Text.RegularExpressions;
using CommandLine;

namespace GitChangeLog.CommandLine
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var parsed = Parser.Default.ParseArguments<Options>(args);
            parsed.WithParsed(options =>
            {
                var releaseTagFormat = new Regex(options.TagFormat);
                var notes = ChangeLog.CreateChangeLog(releaseTagFormat, options.Repository, options.Header, options.SourceLinks, options.CompareLinks);
                File.WriteAllText(options.Output, notes);
            });
        }
    }

    internal class Options
    {
        [Option('t', "tag-format",
            Default = @"[vV]\d\.\d(\.\d){0,2}",
            HelpText = "Regex format for release tags")]
        public string TagFormat { get; set; }

        [Option('o', "output",
            Default = "CHANGELOG.md",
            HelpText = "Output change log file name")]
        public string Output { get; set; }

        [Option('r', "repo",
            Default = ".git",
            HelpText = "Path to the git repository")]
        public string Repository { get; set; }

        [Option("header",
            Default = "# Release Notes",
            HelpText = "Change log file header")]
        public string Header { get; set; }

        [Option("source-links",
            Default = true,
            HelpText = "Enable links to source tree on github hosted repositories")]
        public bool SourceLinks { get; set; }

        [Option("compare-links",
            Default = true,
            HelpText = "Enable links to comparisons with previous releases on github hosted repositories")]
        public bool CompareLinks { get; set; }
    }
}