using Microsoft.Win32.TaskScheduler;

using System;
using System.IO;
using System.Reflection;

namespace Heroesprofile.Uploader.Windows.Core
{
    public class ApplicationDeployment
    {
        private static ApplicationDeployment currentDeployment = null;
        private static bool currentDeploymentInitialized = false;

        private static bool isNetworkDeployed = false;
        private static bool isNetworkDeployedInitialized = false;

        public static bool IsNetworkDeployed
        {
            get {
                if (!isNetworkDeployedInitialized) {
                    bool.TryParse(Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed"), out isNetworkDeployed);
                    isNetworkDeployedInitialized = true;
                }

                return isNetworkDeployed;
            }
        }

        public static ApplicationDeployment CurrentDeployment
        {
            get {
                if (!currentDeploymentInitialized) {
                    currentDeployment = IsNetworkDeployed ? new ApplicationDeployment() : null;
                    currentDeploymentInitialized = true;
                }

                return currentDeployment;
            }
        }

        public Uri ActivationUri
        {
            get {
                Uri.TryCreate(Environment.GetEnvironmentVariable("ClickOnce_ActivationUri"), UriKind.Absolute, out Uri val);
                return val;
            }
        }

        public Version CurrentVersion
        {
            get {
                Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion"), out Version val);
                return val;
            }
        }
        public string DataDirectory
        {
            get { return Environment.GetEnvironmentVariable("ClickOnce_DataDirectory"); }
        }

        public bool IsFirstRun
        {
            get {
                bool.TryParse(Environment.GetEnvironmentVariable("ClickOnce_IsFirstRun"), out bool val);
                return val;
            }
        }

        public DateTime TimeOfLastUpdateCheck
        {
            get {
                DateTime.TryParse(Environment.GetEnvironmentVariable("ClickOnce_TimeOfLastUpdateCheck"), out DateTime value);
                return value;
            }
        }
        public string UpdatedApplicationFullName
        {
            get {
                return Environment.GetEnvironmentVariable("ClickOnce_UpdatedApplicationFullName");
            }
        }

        public Version UpdatedVersion
        {
            get {
                Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_UpdatedVersion"), out Version val);
                return val;
            }
        }

        public Uri UpdateLocation
        {
            get {
                Uri.TryCreate(Environment.GetEnvironmentVariable("ClickOnce_UpdateLocation"), UriKind.Absolute, out Uri val);
                return val;
            }
        }

        public Version LauncherVersion
        {
            get {

                Version.TryParse(Environment.GetEnvironmentVariable("ClickOnce_LauncherVersion"), out Version val);
                return val;
            }
        }

        private ApplicationDeployment()
        {
            // As an alternative solution, we could initialize all properties here
        }
    }

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