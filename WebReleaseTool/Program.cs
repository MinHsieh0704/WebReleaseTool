using Min_Helpers;
using Min_Helpers.LogHelper;
using Min_Helpers.PrintHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WebReleaseTool
{
    class Program
    {
        static Print PrintService { get; set; } = null;
        static Log LogService { get; set; } = null;

        [STAThread]
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            try
            {
                LogService = new Log();
                PrintService = new Print(LogService);

                LogService.Write("");
                PrintService.Log("App Start", Print.EMode.info);

                PrintService.NewLine();

                FolderBrowserDialog dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                string basePath = dialog.SelectedPath;
                string workspacePath = $"{basePath}\\src";

                PrintService.Write("Project Folder: ", Print.EMode.info);
                PrintService.WriteLine(basePath, Print.EMode.message);

                if (!Directory.Exists(workspacePath))
                    throw new Exception($"src folder not found");
                if (!File.Exists($"{workspacePath}\\package.json"))
                    throw new Exception($"package.json not found");

                JObject packageJson = JsonConvert.DeserializeObject<JObject>(File.ReadAllText($"{workspacePath}\\package.json"));
                string projectName = packageJson["name"].ToString();
                string projectVersion = packageJson["version"].ToString();
                string projectDescription = packageJson["description"].ToString();

                PrintService.Write("Project Name: ", Print.EMode.info);
                PrintService.WriteLine(projectName, Print.EMode.message);

                PrintService.Write("Project Version: ", Print.EMode.info);
                PrintService.WriteLine(projectVersion, Print.EMode.message);

                PrintService.NewLine();

                PrintService.Write("Please Input New Verson: ", Print.EMode.question);
                string projectNewVersion = Console.ReadLine();
                PrintService.NewLine();

                List<string> paths = new List<string>()
                {
                    $"{workspacePath}\\package.json",
                    $"{workspacePath}\\package-lock.json"
                };

                for (int i = 0; i < paths.Count; i++)
                {
                    if (File.Exists(paths[i]))
                    {
                        string path = Path.GetFileName(paths[i]);

                        JObject input = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(paths[i]));

                        if (input["name"] != null) input["name"] = projectName;
                        if (input["version"] != null) input["version"] = projectNewVersion;

                        File.WriteAllText(paths[i], JsonConvert.SerializeObject(input, Formatting.Indented) + "\n");

                        PrintService.WriteLine($"Update \"{path}\" Success", Print.EMode.success);
                    }
                }

                PrintService.NewLine();

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();

                    string root = Directory.GetDirectoryRoot(basePath);

                    process.StandardInput.WriteLine(root.Replace("\\", ""));
                    Thread.Sleep(10);

                    process.StandardInput.WriteLine($"cd {workspacePath}");
                    Thread.Sleep(10);

                    process.StandardInput.WriteLine("git add package.json");
                    Thread.Sleep(10);

                    process.StandardInput.WriteLine("git add package-lock.json");
                    Thread.Sleep(10);

                    process.StandardInput.WriteLine($"git commit -m \"v{projectNewVersion}\"");
                    Thread.Sleep(10);

                    PrintService.WriteLine($"Git Commit Success", Print.EMode.success);
                    Thread.Sleep(10);

                    if (File.Exists($"{workspacePath}\\public\\change.log"))
                        File.Delete($"{workspacePath}\\public\\change.log");

                    string count = "5000";

                    process.StandardInput.WriteLine($"git log --date=format:\"%Y/%m/%d %H:%M:%S\" --pretty=format:\"%cd -> author: %<(10,trunc)%an, message: %s\" > \".\\public\\change.log\" -{count}");
                    while (!File.Exists($"{workspacePath}\\public\\change.log")) Thread.Sleep(10);

                    process.StandardInput.WriteLine($"git add public\\change.log");
                    process.StandardInput.WriteLine($"git commit --amend --no-edit");
                    Thread.Sleep(10);

                    process.StandardInput.WriteLine($"exit");

                    process.WaitForExit();

                    PrintService.WriteLine($"Git Log Export Success", Print.EMode.success);
                }

                PrintService.NewLine();
            }
            catch (Exception ex)
            {
                ex = ExceptionHelper.GetReal(ex);
                PrintService.Log($"App Error, {ex.Message}", Print.EMode.error);
            }
            finally
            {
                PrintService.Log("App End", Print.EMode.info);
                Console.ReadKey();

                Environment.Exit(0);
            }
        }
    }
}
