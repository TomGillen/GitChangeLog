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
                var notes = ChangeLog.CreateChangeLog(releaseTagFormat, options.Repository);
                File.WriteAllText(options.Output, notes);
            });
        }
    }

    internal class Options
    {
        [Option('t', "tag",
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
    }
}