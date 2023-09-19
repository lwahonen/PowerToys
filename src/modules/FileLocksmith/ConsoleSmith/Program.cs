// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using FileLocksmith.Interop;

namespace ConsoleSmith
{
    internal sealed class Program
    {
        private static ObservableCollection<ProcessResult> processes = new ObservableCollection<ProcessResult>();
        private static string[] options = Array.Empty<string>();

        // Lock object for synchronization;
        private static object _syncLock = new object();

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("ConsoleSmith.exe <options> <files>" +
                    "\n\nOptions implemented:\n" +
                    "\n/list List all current locks to the console" +
                    "\n/wait Wait until all the locks to a file are released" +
                    "\n/kill Try to kill every process holding a lock to the files listed\n");
            }

            string[] paths = args.Where(x => !x.StartsWith("/", StringComparison.CurrentCulture)).ToArray();
            options = args.Where(x => x.StartsWith("/", StringComparison.CurrentCulture)).ToArray();
            List<Task> tasks = new();

            foreach (ProcessResult p in FindProcesses(paths))
            {
                if (options.Contains("/list"))
                {
                    Console.WriteLine(
                        "LIVE PID:" + p.pid +
                        "\tUser:" + p.user +
                        "\tExecutable:" + p.name +
                        "\tFiles:" + string.Join(";", p.files));
                }

                lock (_syncLock)
                {
                    processes.Add(p);
                }

                tasks.Add(WatchProcess(p));

                if (options.Contains("/kill"))
                {
                    EndTask(p);
                }
            }

            bool waitForProcesses = options.Contains("/wait") && processes.Count > 0;
            if (!waitForProcesses)
            {
                Environment.Exit(0);
            }
            else
            {
                foreach (Task t in tasks)
                {
                    t.Wait();
                }
            }
        }

        private static List<ProcessResult> FindProcesses(string[] paths)
        {
            var results = new List<ProcessResult>();
            results = NativeMethods.FindProcessesRecursive(paths).ToList();
            return results;
        }

        private static async Task WatchProcess(ProcessResult process)
        {
            try
            {
                Process handle = Process.GetProcessById((int)process.pid);
                await Task.Run(() => handle.WaitForExit());

                if (handle.HasExited)
                {
                    if (options.Contains("/list"))
                    {
                        Console.WriteLine(
                            "KILLED PID:" + process.pid +
                            "\tUser:" + process.user +
                            "\tExecutable:" + process.name +
                            "\tFiles:" + string.Join(";", process.files));
                    }

                    lock (_syncLock)
                    {
                        processes.Remove(process);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't add a waiter to wait for a process to exit. PID = {process.pid} and Name = {process.name}.", ex);
                lock (_syncLock)
                {
                    processes.Remove(process); // If we couldn't get an handle to the process or it has exited in the meanwhile, don't show it.
                }
            }
        }

        public static void EndTask(ProcessResult selectedProcess)
        {
            try
            {
                Process handle = Process.GetProcessById((int)selectedProcess.pid);
                try
                {
                    handle.Kill();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't kill process {selectedProcess.name} with PID {selectedProcess.pid}.", ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't get an handle to kill process {selectedProcess.name} with PID {selectedProcess.pid}. Likely has been killed already.", ex);
                lock (_syncLock)
                {
                    processes.Remove(selectedProcess); // If we couldn't get an handle to the process, remove it from the list, since it's likely been killed already.
                }
            }
        }
    }
}
