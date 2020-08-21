using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Timers;
using System.Text.RegularExpressions;
using System.Threading;

namespace TrabajoForense
{
    class MainClass
    {
        private static System.Timers.Timer aTimer;
        private static Regex rxTitle, rxProcess;
        private static ManualResetEvent resetEvent = new ManualResetEvent(false);
        class TimeEntry
        {
            public String processName;
            public String windowsTitle;
            public DateTime start;
            public DateTime end = DateTime.MinValue;

            public override string ToString()
            {
                return processName.Split(',').Last() + "," + windowsTitle + "," + start.ToString("yyyy/MM/dd HH:mm:ss") + "," + end.ToString("yyyy/MM/dd  dd HH:mm:ss");
            }
        }

        static Dictionary<string, TimeEntry> registry = new Dictionary<string, TimeEntry>();

        private static void SetTimer(int interval)
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(interval);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Process[] processlist = Process.GetProcesses();

            processlist
                .Where(process => !string.IsNullOrEmpty(process.MainWindowTitle) && !registry.ContainsKey(process.MainWindowTitle)).ToList()
                .ForEach(processToAdd =>
                {
                    try
                    {
                        if (rxTitle == null || !rxTitle.IsMatch(processToAdd.MainWindowTitle) && (rxProcess == null || !rxProcess.IsMatch(processToAdd.ProcessName)))
                        {
                            registry.Add(processToAdd.MainWindowTitle, new TimeEntry { processName = processToAdd.ProcessName, windowsTitle = processToAdd.MainWindowTitle, start = DateTime.Now });
                        }
                    }
                    catch (Exception) { }
                });

            List<string> toRemoveList = new List<string>();
            registry
                .Where(regEntry => !processlist.Select(process => process.MainWindowTitle).Contains(regEntry.Key)).ToList()
                .ForEach(processToRemove =>
                {
                    processToRemove.Value.end = DateTime.Now;
                    toRemoveList.Add(processToRemove.Key);
                });

            using (System.IO.StreamWriter file = new System.IO.StreamWriter("log.csv", true))
            {
                registry
                   .Where(processToWrite => processToWrite.Value.end != DateTime.MinValue)
                   .ToList()
                   .ForEach(processToWrite => file.WriteLine(processToWrite.Value.ToString()));
            }

            toRemoveList.AsParallel().ForAll(toRemove => registry.Remove(toRemove));
        }

        static void Main(string[] args)
        {

            var excludeTitle = Forensic.Properties.Settings.Default.excludeTitle;
            if (excludeTitle != null && excludeTitle != "")
            {
                rxTitle = new Regex(excludeTitle, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            var excludeProcess = Forensic.Properties.Settings.Default.excludeProcess;
            if (excludeProcess != null && excludeProcess != "")
            {
                rxProcess = new Regex(excludeProcess, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }


            var interval = Forensic.Properties.Settings.Default.interval;
            if(interval == 0)
            {
                interval = 300000;
            }
            SetTimer(interval);

            resetEvent.WaitOne();

        }


    }
}
