using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Impersonate
{
    internal class Program
    {
        static void Main()
        {
            try
            {
                foreach (DictionaryEntry kvp in Environment.GetEnvironmentVariables())
                {
                    Console.Error.WriteLine($"set {kvp.Key}={kvp.Value}");
                }

                var list = new List<string>(Environment.GetCommandLineArgs());
                list.RemoveAt(0);

                string arguments = string.Join(" ", list);
                Console.Error.WriteLine($"Command-line arguments: {arguments}");

                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = Path.ChangeExtension(Assembly.GetEntryAssembly().Location, "orig.exe");
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;

                    //process.OutputDataReceived += (object sender, DataReceivedEventArgs line) =>
                    //{
                    //    if (line.Data != null)
                    //    {
                    //        Console.WriteLine(line.Data);
                    //    }
                    //};
                    //process.ErrorDataReceived += (object sender, DataReceivedEventArgs line) =>
                    //{
                    //    if (line.Data != null)
                    //    {
                    //        Console.Error.WriteLine(line.Data);
                    //    }
                    //};

                    process.Start();

                    object lockObject = new object();

                    Thread outputThread = new Thread(() =>
                    {
                        string line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            lock (lockObject)
                            {
                                Console.WriteLine(line);
                            }
                        }
                    });

                    Thread errorThread = new Thread(() =>
                    {
                        string line;
                        while ((line = process.StandardError.ReadLine()) != null)
                        {
                            lock (lockObject)
                            {
                                Console.Error.WriteLine(line);
                            }
                        }
                    });

                    new Thread(() =>
                    {
                        string line;
                        while ((line = Console.ReadLine()) != null)
                        {
                            process.StandardInput.WriteLine(line);
                        }
                        process.StandardInput.Close();
                    }).Start();

                    //process.BeginOutputReadLine();
                    //process.BeginErrorReadLine();

                    //string inputLine;
                    //while ((inputLine = Console.ReadLine()) != null)
                    //{
                    //    process.StandardInput.WriteLine(inputLine);
                    //}

                    outputThread.Start();
                    errorThread.Start();

                    outputThread.Join();
                    errorThread.Join();

                    process.WaitForExit();
                    Console.Error.WriteLine($"Process exited with exit code {process.ExitCode}.");
                    Environment.Exit(process.ExitCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Impersonation error: {e.Message}\n{e.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
