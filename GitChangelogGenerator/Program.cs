using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitChangelogGenerator
{
    class Program
    {
        //const string path = @"D:\Projekty\GitChangelogTest";
        static void Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "GitChangelog";
            app.HelpOption("-?|-h|--help");

            var pathArg = app.Option("-p|--path",
            "Ścieżka do repozytorium git (wymagane)", CommandOptionType.SingleValue);

            var branchArg = app.Option("-b|--branch",
            "Branch źródłowy dla changelog (opcjonalne)", CommandOptionType.SingleValue);

            var tagArg = app.Option("-t|--tag",
            "Tag źródłowy lub zakres tag1..tag2 (opcjonalne)", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                string gitCommand = CreateInputCommand(pathArg.Value(), branchArg.Value(), tagArg.Value());
                string rawChangelog = GetRawChangelog(gitCommand);
                var commitList = ParseRawChangelog(rawChangelog);
                WriteToFile(commitList, pathArg.Value());
                return 0;
            });

            app.Execute(args);
        }

        private static string CreateInputCommand(string pathArg, string branchArg, string tagArg)
        {
            if(pathArg == null)
            {
                throw new Exception("Brak podanej ścieżki do repozytorium git");
            }
            string command = $"--git-dir={pathArg.Replace("\\", "/")}/.git --work-tree={pathArg.Replace("\\", "/")} log ";
            if (tagArg != null)
            {
                command += tagArg;
            }
            else if (branchArg != null)
            {
                command += branchArg;
            }

            command += " --format=\"%at>>%s\" --no-merges";
            return command;
        }

        private static void WriteToFile(List<Commit> commitList, string path)
        {
            var commitGroups = commitList.GroupBy(x => x.Category).ToDictionary(g => g.Key, g => g.ToList());
            string output = string.Empty;
            foreach(var group in commitGroups)
            {
                if(group.Key == "new")
                {
                    output += "# Nowości\n";
                }
                else if (group.Key == "chg")
                {
                    output += "# Zmiany\n";
                }
                else if (group.Key == "fix")
                {
                    output += "# Poprawki\n";
                }
                foreach (var commit in group.Value)
                {
                    output += $"* [{commit.Date.ToString()}] {commit.Body}\n";
                }
                output += "\n";
            }
            System.IO.File.WriteAllText(path + @"\changelog.md", output);
        }

        private static List<Commit> ParseRawChangelog(string rawChangelog)
        {
            var commitList = new List<Commit>();
            string[] lines = rawChangelog.Split("\n");
            foreach(var line in lines)
            {
                if(line.Length > 0)
                {
                    var com = new Commit();
                    string[] parts = line.Split(">>");
                    if (parts[1].Substring(0, 3).Equals("new", StringComparison.InvariantCulture) 
                        || parts[1].Substring(0, 3).Equals("fix", StringComparison.InvariantCulture)
                        || parts[1].Substring(0, 3).Equals("chg", StringComparison.InvariantCulture))
                    {
                        com.Date = UnixTimestampToDateTime(long.Parse(parts[0]));
                        com.Category = parts[1].Substring(0, 3);
                        com.Body = parts[1].Substring(5);
                        commitList.Add(com);
                    }
                }
            }
            return commitList;
        }

        public static DateTime UnixTimestampToDateTime(long unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = unixTime * TimeSpan.TicksPerSecond;
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks, System.DateTimeKind.Utc);
        }

        public static string GetRawChangelog(string command)
        {
            var output = RunProcess(command);
            return output;
        }

        private static string RunProcess(string command)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "git.exe";
            p.StartInfo.Arguments = command;
            p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

    }
}
