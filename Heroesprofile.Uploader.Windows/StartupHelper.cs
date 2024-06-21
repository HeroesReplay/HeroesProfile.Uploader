using Microsoft.Win32.TaskScheduler;
using System;
using System.IO;
using System.Reflection;

namespace Heroesprofile.Uploader.Windows
{
    public static class StartupHelper
    {
        public static void CreateStartupTask()
        {
            string taskName = "HeroesProfile.Uploader";
            string exePath = Assembly.GetExecutingAssembly().Location;

            using (TaskService ts = new TaskService()) {
                // Check if the task already exists
                var existingTask = ts.FindTask(taskName);
                if (existingTask == null) {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Starts YourAppName with Windows";
                    td.Principal.LogonType = TaskLogonType.InteractiveToken;

                    td.Triggers.Add(new LogonTrigger { UserId = Environment.UserName });

                    td.Actions.Add(new ExecAction(exePath, null, Path.GetDirectoryName(exePath)));

                    ts.RootFolder.RegisterTaskDefinition(taskName, td);
                }
            }
        }

        public static bool IsStartupTaskEnabled()
        {
            string taskName = "HeroesProfile.Uploader";

            using (TaskService ts = new TaskService()) {
                var task = ts.FindTask(taskName);
                return task != null;
            }
        }

        public static void RemoveStartupTask()
        {
            string taskName = "HeroesProfile.Uploader";

            using (TaskService ts = new TaskService()) {
                ts.RootFolder.DeleteTask(taskName, false);
            }
        }
    }
}