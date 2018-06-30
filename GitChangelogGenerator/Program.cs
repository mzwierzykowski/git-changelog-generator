using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitChangelogGenerator
{
    class Program
    {
        const string path = @"D:\Projekty\GitChangelogTest";
        static void Main(string[] args)
        {
            string rawChangelog = GetRawChangelog(path);
            var commitList = ParseRawChangelog(rawChangelog);
            WriteToFile(commitList);
        }

        private static void WriteToFile(List<Commit> commitList)
        {
            var commitGroups = commitList.GroupBy(x => x.Category).ToDictionary(g => g.Key, g => g.ToList());
            string output = string.Empty;
            foreach(var group in commitGroups)
            {
                if(group.Key == "new")
                {
                    output += "Nowości:\n";
                }
                else if (group.Key == "chg")
                {
                    output += "Zmiany:\n";
                }
                else if (group.Key == "fix")
                {
                    output += "Poprawki:\n";
                }
                foreach (var commit in group.Value)
                {
                    output += $"{commit.Date.ToString()} | {commit.Body}\n";
                }
                output += "\n";
            }
            System.IO.File.WriteAllText(path + @"\changelog.txt", output);
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
                    com.Date = UnixTimestampToDateTime(long.Parse(parts[0]));
                    com.Category = parts[1].Substring(0, 3);
                    com.Body = parts[1].Substring(5);
                    commitList.Add(com);
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

        public static string GetRawChangelog(string path)
        {
            var output = RunProcess(string.Format("--git-dir={0}/.git --work-tree={1} log --format=\"%at>>%s\"", path.Replace("\\", "/"), path.Replace("\\", "/")));
            return output;
        }

        private static string RunProcess(string command)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "git.exe";
            p.StartInfo.Arguments = command;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }

    }
}
