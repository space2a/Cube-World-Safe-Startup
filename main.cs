using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cbsafecpu
{
    class main
    {
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        static Process CB_PROCESS = null;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                default:
                    ResumeProcess(CB_PROCESS.Id);
                    return false;
            }
        }

        private static void SuspendProcess(int pid)
        {
            var process = Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess(int pid)
        {
            var process = Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            string CB_PATH = "cubeworld.exe";
            Console.WriteLine("Cube World SAFE STARTUP | v1.0 | github.com/space2a/");
            Console.Title = "Cube World SAFE STARTUP | v1.0 | github.com/space2a/";
            if (Process.GetProcessesByName("cubeworld").Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cube World is already running.");
                Console.ReadLine();
                Environment.Exit(1);
            }

            if (!File.Exists(CB_PATH))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The program was not able to find cubeworld.exe, please put this program where the cubeworld.exe file is located.");
                Console.ReadLine();
                Environment.Exit(1);
            }
            Thread.Sleep(500);

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            CB_PROCESS = new Process();
            CB_PROCESS.StartInfo.FileName = CB_PATH;
            CB_PROCESS.Start();
            Console.Write("Cube World started,");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" you can close this program at any moment but it will not close Cube World, if you want to close both at the same time press the X key (while the window is selected).");
            new Thread(new ThreadStart(Inputs)).Start();
            Thread.Sleep(100);
            Console.ResetColor();

            int d = 0;
            int r = 0;
            DateTime started = DateTime.Now;
            while (true)
            {
                try
                {
                    CB_PROCESS = Process.GetProcessesByName("cubeworld")[0];
                    for (int i = 0; i < 1024; i++)
                    {
                        ResumeProcess(CB_PROCESS.Id);
                        d++;
                        r++;

                        if (d > 70)
                            Thread.Sleep(20);
                        else if (d >= 80)
                            d = 0;
                        else
                            Thread.Sleep(15);

                        if(r == 25)
                        {
                            CB_PROCESS.Refresh();
                            r = 0;
                            if (String.IsNullOrWhiteSpace(CB_PROCESS.MainWindowTitle))
                                Console.WriteLine("Cube World is still loading... (" + (DateTime.Now - started).TotalSeconds.ToString(".0") + " seconds passed)");
                            else
                            {
                                ResumeProcess(CB_PROCESS.Id);
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine("Cube World is no longer loading, thanks for using the program.\nProgram created by github.com/space2a/\nClosing in 10 seconds...");
                                Thread.Sleep(10000);
                                Environment.Exit(0);
                            }
                        }
                        SuspendProcess(CB_PROCESS.Id);
                        Console.SetCursorPosition(0, 5);
                    }
                    ResumeProcess(CB_PROCESS.Id);

                }
                catch (Exception)
                {
                    CB_PROCESS = null;
                    Console.ForegroundColor = ConsoleColor.Red;
                    if(d > 15)
                        Console.WriteLine("An error occured, retrying...");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
            }

        }

        static void Inputs()
        {
            while (CB_PROCESS != null) { }
            Thread.Sleep(1000);
            while (true)
            {
                var key = Console.ReadKey().Key;
                if (key == ConsoleKey.X)
                {
                    if (CB_PROCESS != null)
                    {
                        CB_PROCESS.Kill();
                    }
                    Environment.Exit(0);
                }
            }
        }

    }
}