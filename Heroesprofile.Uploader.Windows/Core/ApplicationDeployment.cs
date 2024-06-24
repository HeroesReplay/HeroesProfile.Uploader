using System;

namespace Heroesprofile.Uploader.Windows.Core
{

    /*
     * https://learn.microsoft.com/en-us/visualstudio/deployment/debugging-clickonce-applications-that-use-system-deployment-application?view=vs-2019
     * https://learn.microsoft.com/en-us/visualstudio/deployment/access-clickonce-deployment-properties-dotnet?view=vs-2022&viewFallbackFrom=vs-2019
     * https://learn.microsoft.com/en-us/visualstudio/deployment/clickonce-deployment-dotnet?view=vs-2022#applicationdeployment-class
     * Starting in .NET 7, you can access properties in the ApplicationDeployment class using environment variables. For more information, see Access ClickOnce deployment properties in .NET.
     */
    public class ApplicationDeployment
    {
        private static ApplicationDeployment? currentDeployment = null;
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

        public static bool IsApplicationDeployed
        {
            get {
                return CurrentDeployment != null;
            }
        }

        public static ApplicationDeployment? CurrentDeployment
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

        /// <summary>
        /// The value of this property is reset whenever the user upgrades from one version to the next. 
        /// If you want to perform an operation only the very first time any version of the application is run, you will need to perform an additional test, such as checking for the existence of a file you created the first time, or storing a flag using Application Settings.
        /// </summary>
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
}