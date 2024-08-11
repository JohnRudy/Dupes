using System.Security.Cryptography;
using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;

namespace Dupes
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string? directory;

            if (args.Length == 0)
            {
                Console.WriteLine("Enter directory path. (C:/Users/.../)");
                directory = Console.ReadLine();
            }
            else
                directory = args[0];

            if (directory == null)
                Console.WriteLine("Invalid Path");

            if (Directory.Exists(directory))
            {
                try
                {
                    Console.WriteLine($"The directory '{directory}' exists.");
                    List<List<String>> duplicates = FindDuplicates(directory);
                    Console.WriteLine($"Found {duplicates.Count} duplicates");

                    Console.WriteLine($"Start cleaning duplications? [y/n]");
                    string? affirmation = Console.ReadLine();
                    if (affirmation != null)
                        if (affirmation.ToLower() != "y")
                            return;

                    if (duplicates.Count > 0)
                    {
                        CleanDuplicates(duplicates);
                    }
                }
                catch (Exception exc)
                {
                    Console.CursorVisible = true;
                    Console.WriteLine($"An error occured: {exc.Message}");
                }
            }
            else
                Console.WriteLine($"The directory '{directory}' does not exist.");
        }

        private static void CleanDuplicates(List<List<String>> duplicates)
        {
            Console.WriteLine("**********************************************************");
            Console.WriteLine("*****                     WARNING                   ******");
            Console.WriteLine("**********************************************************");
            Console.WriteLine("***      YOU ARE ABOUT TO DELETE FILES PERMANENTLY     ***");
            Console.WriteLine("***                  PROCEED WITH CAUTION              ***");
            Console.WriteLine("***                     STOP WITH CTRL-C               ***");
            Console.WriteLine("**********************************************************");

            
            bool ShowPreview = false;
            Console.WriteLine("open files with default applications to view contents? [y/n]");
            string? affirmation = Console.ReadLine();
            if (affirmation != null)
                ShowPreview = affirmation.ToLower() == "y";

            foreach (List<String> duplicated in duplicates)
            {
                if (ShowPreview)
                {
                    PreviewFile(duplicated[0]);
                }

                int file_index = 1;
                foreach (string file in duplicated)
                {
                    Console.WriteLine($"- {file_index}: {file}");
                    file_index++;
                }
                Console.WriteLine("Which file index to KEEP?");
                string? index = Console.ReadLine();
                if (index == null)
                    continue;

                try
                {
                    int parsed = int.Parse(index);
                    if (parsed - 1 < 0 || parsed - 1 > duplicated.Count - 1)
                    {
                        Console.WriteLine("Invalid index");
                        continue;
                    }
                    Console.WriteLine($"Keeping: {duplicated[parsed - 1]}");
                    duplicated.RemoveAt(parsed - 1);
                    foreach (string file in duplicated)
                        DeleteFile(file);
                }
                catch
                {
                    Console.WriteLine("Invalid index");
                }
                Console.WriteLine("\n");
            }
        }

        private static void PreviewFile(string filepath)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filepath, out string mimeType))
                mimeType = "application/octet-stream"; // Default MIME type if not found
            Console.WriteLine($"MIME type: {mimeType}");

            if (mimeType.StartsWith("application"))
                Console.WriteLine("Cannot preview application");
            else
                OpenPreview(filepath);
        }

        private static void OpenPreview(string filepath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filepath,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening file: {ex.Message}");
            }
        }

        private static void DeleteFile(string filepath)
        {
            try
            {
                Console.WriteLine($"Deleting {filepath}");
                File.Delete(filepath);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Error: {exc.Message}");
            }
        }

        private static List<List<string>> FindDuplicates(string directory)
        {
            var filesHashes = new Dictionary<string, List<string>>();
            var duplicates = new List<List<string>>();

            // Get the total number of files to process
            var totalFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).Length;
            int fileCount = 0;

            foreach (var filePath in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                string fileHash = CalculateHash(filePath);

                if (!filesHashes.ContainsKey(fileHash))
                {
                    filesHashes[fileHash] = new List<string>();
                }

                filesHashes[fileHash].Add(filePath);

                // Update progress bar
                fileCount++;
                PrintProgressBar(fileCount, totalFiles, prefix: "Progress:", suffix: "Complete", length: 50);
            }

            // Filter out hash entries that have more than one file (i.e., duplicates)
            foreach (var paths in filesHashes.Values)
            {
                if (paths.Count > 1)
                {
                    duplicates.Add(paths);
                }
            }

            return duplicates;
        }

        public static string CalculateHash(string file_path)
        {
            byte[] hash;
            using (var sha = SHA256.Create())
            {
                using var stream = File.OpenRead(file_path);
                hash = sha.ComputeHash(stream);
            }
            return System.Text.Encoding.Default.GetString(hash);
        }

        private static void PrintProgressBar(int iteration, int total, string? prefix = null, string? suffix = null, int decimals = 1, int length = 50, char fill = '█', char bar_empty = ' ')
        {
            Console.CursorVisible = false;
            float percentage = 100 * (iteration / (float)total);
            int filled_length = (int)(length * iteration / (float)total);
            string bar = new string(fill, filled_length) + new string(bar_empty, length - filled_length);
            string percentageString = percentage.ToString($"F{decimals}");
            Console.Write($"\r{prefix} | {bar} | {percentageString}% {suffix}");
            if (iteration == total)
            {
                Console.CursorVisible = true;
                Console.WriteLine("");
            }
        }
    }
}