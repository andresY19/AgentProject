using Microsoft.Win32;
using MonitorTrackerForm.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApiAgent.Controllers
{
    public class InstalledPrograms
    {
        const string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";


        public static List<InstalledProgramsViewModel> GetInstalledPrograms()
        {
            List<InstalledProgramsViewModel> lii = new List<InstalledProgramsViewModel>();
            lii.AddRange(GetInstalledProgramsFromRegistry(RegistryView.Registry32));
            lii.AddRange(GetInstalledProgramsFromRegistry(RegistryView.Registry64));
            return lii;
        }

        private static List<InstalledProgramsViewModel> GetInstalledProgramsFromRegistry(RegistryView registryView)
        {
            try
            {


                var result = new List<string>();
                List<InstalledProgramsViewModel> lli = new List<InstalledProgramsViewModel>();

                using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView).OpenSubKey(registry_key))
                {
                    foreach (string subkey_name in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            if (IsProgramVisible(subkey))
                            {
                                var displayName = (string)subkey.GetValue("DisplayName");
                                Int32 size = 0;
                                if ((Int32?)subkey.GetValue("EstimatedSize") != null)
                                    size = (Int32)subkey.GetValue("EstimatedSize");

                                var vertion = (string)subkey.GetValue("DisplayVersion");
                                string installdate = string.Empty;
                                
                                DateTime dateValue;
                                InstalledProgramsViewModel ip = new InstalledProgramsViewModel();

                                if (DateTime.TryParseExact((string)subkey.GetValue("InstallDate"), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                                    ip.InstalledDate = dateValue;

                                ip.Name = displayName;
                                ip.Size = size.ToString();
                                ip.Vertion = vertion;

                                if (!string.IsNullOrEmpty(installdate))
                                    ip.InstalledDate = DateTime.Parse(installdate);
                                ip.User = Environment.UserName;
                                ip.Pc = Environment.MachineName;

                                if (lli.Where(y => y.Name == displayName).Count() == 0)
                                    lli.Add(ip);
                            }
                        }
                    }
                }

                return lli;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static bool IsProgramVisible(RegistryKey subkey)
        {
            var name = (string)subkey.GetValue("DisplayName");
            var releaseType = (string)subkey.GetValue("ReleaseType");
            //var unistallString = (string)subkey.GetValue("UninstallString");
            var systemComponent = subkey.GetValue("SystemComponent");
            var parentName = (string)subkey.GetValue("ParentDisplayName");

            return
                !string.IsNullOrEmpty(name)
                && string.IsNullOrEmpty(releaseType)
                && string.IsNullOrEmpty(parentName)
                && (systemComponent == null);
        }
    }
}