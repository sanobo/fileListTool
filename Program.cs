using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileListTool
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string folderPath = args.Length > 0
                ? args[0]
                : ReadFolderPath();

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                Console.Error.WriteLine("フォルダパスが指定されていません。");
                return 1;
            }

            folderPath = folderPath.Trim().Trim('"');

            if (!Directory.Exists(folderPath))
            {
                Console.Error.WriteLine("指定されたフォルダが見つかりません: " + folderPath);
                return 1;
            }

            Console.WriteLine("集計中: " + Path.GetFullPath(folderPath));
            Console.WriteLine();

            FolderSummary summary = SummarizeFolder(folderPath);
            PrintSummary(summary);
            return 0;
        }

        private static string ReadFolderPath()
        {
            Console.Write("集計するフォルダのパスを入力してください: ");
            return Console.ReadLine();
        }

        private static FolderSummary SummarizeFolder(string rootPath)
        {
            var summary = new FolderSummary
            {
                RootPath = Path.GetFullPath(rootPath)
            };

            var pending = new Stack<string>();
            pending.Push(summary.RootPath);

            while (pending.Count > 0)
            {
                string current = pending.Pop();
                summary.FolderCount++;

                try
                {
                    foreach (string file in Directory.EnumerateFiles(current))
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            summary.FileCount++;
                            summary.TotalBytes += info.Length;
                        }
                        catch (Exception ex)
                        {
                            summary.SkippedFiles++;
                            summary.Errors.Add(file + " : " + ex.Message);
                        }
                    }

                    foreach (string dir in Directory.EnumerateDirectories(current))
                    {
                        pending.Push(dir);
                    }
                }
                catch (Exception ex)
                {
                    summary.SkippedFolders++;
                    summary.Errors.Add(current + " : " + ex.Message);
                }
            }

            return summary;
        }

        private static void PrintSummary(FolderSummary summary)
        {
            Console.WriteLine("対象フォルダ : " + summary.RootPath);
            Console.WriteLine("フォルダ数   : " + summary.FolderCount.ToString("N0"));
            Console.WriteLine("ファイル数   : " + summary.FileCount.ToString("N0"));
            Console.WriteLine("合計サイズ   : " + FormatBytes(summary.TotalBytes) +
                              " (" + summary.TotalBytes.ToString("N0") + " バイト)");

            if (summary.SkippedFiles > 0 || summary.SkippedFolders > 0)
            {
                Console.WriteLine();
                Console.WriteLine("アクセスできなかったファイル: " + summary.SkippedFiles.ToString("N0"));
                Console.WriteLine("アクセスできなかったフォルダ: " + summary.SkippedFolders.ToString("N0"));

                int shown = 0;
                foreach (string error in summary.Errors)
                {
                    if (shown >= 10)
                    {
                        Console.WriteLine("  ... ほか " + (summary.Errors.Count - shown).ToString("N0") + " 件");
                        break;
                    }

                    Console.WriteLine("  - " + error);
                    shown++;
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? value.ToString("N0") + " " + units[unitIndex]
                : value.ToString("N2") + " " + units[unitIndex];
        }

        private sealed class FolderSummary
        {
            public string RootPath { get; set; }
            public int FolderCount { get; set; }
            public int FileCount { get; set; }
            public long TotalBytes { get; set; }
            public int SkippedFiles { get; set; }
            public int SkippedFolders { get; set; }
            public List<string> Errors { get; } = new List<string>();
        }
    }
}
