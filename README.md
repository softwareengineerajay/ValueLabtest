using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Collections;

namespace ConsoleAppForInstallingChromeExtension
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Chrome Extension Details
            const string chromePolicyKeyPath = @"Software\Policies\Google\Chrome\ExtensionInstallForcelist";
            const string chromeExtensionId = "iifaikaiohljmjieccfdpgldfbejbgoi";
            const string chromeExtensionUrl = "https://clients2.google.com/service/update2/crx";

            // Edge Extension Details
            const string edgePolicyKeyPath = @"Software\Policies\Microsoft\Edge\ExtensionInstallForcelist";
            const string edgeExtensionId = "llkdpeohjhgbghjgcmkhchmcigfjkfhl";
            const string edgeExtensionUrl = "https://edge.microsoft.com/extensionwebstorebase/v1/crx";

            bool install = true;

            if (install)
            {
                EnableAcrobatReaderPreferences();
                // Install Chrome Extension
                ForceInstall(chromePolicyKeyPath, chromeExtensionId, chromeExtensionUrl, "Chrome");

                // Install Edge Extension
                ForceInstall(edgePolicyKeyPath, edgeExtensionId, edgeExtensionUrl, "Edge");

                AddManifestEntry(chromeExtensionId);
                AddManifestEntry(edgeExtensionId);
            }
            else
            {
                // Uninstall Chrome Extension
                ForceUninstall(chromePolicyKeyPath, chromeExtensionId, "Chrome");

                // Uninstall Edge Extension
                ForceUninstall(edgePolicyKeyPath, edgeExtensionId, "Edge");
            }
            Process.Start("gpupdate.exe", "/force");
            Console.WriteLine("PDF Extension " + (install ? "" : "un") + "installed. Press Enter to exit");
            Console.ReadKey();
        }

        private static void AddManifestEntry(string extensionId)
        {
            try
            {
                var possiblePaths = new List<string>
                {
                    @"C:\Program Files (x86)\Adobe\Acrobat Reader DC\Reader\Browser\WCChromeExtn\manifest.json",
                    @"C:\Program Files\Adobe\Acrobat Reader DC\Reader\Browser\WCChromeExtn\manifest.json",
                    @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Browser\WCChromeExtn\manifest.json",
                    @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\Browser\WCChromeExtn\manifest.json",
                };

                var mPath = GetManifestPath(@"SOFTWARE\WOW6432Node\Google\Chrome\NativeMessagingHosts\com.adobe.acrobat.chrome_webcapture");
                if (string.IsNullOrEmpty(mPath))
                {
                    mPath = GetManifestPath(@"SOFTWARE\WOW6432Node\Microsoft\Edge\NativeMessagingHosts\com.adobe.acrobat.chrome_webcapture");
                }
                if (!string.IsNullOrEmpty(mPath))
                {
                    possiblePaths.Insert(0, mPath);
                }
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"Manifest file found at: {path}");
                        AddExtensionToAllowedOrigins(path, extensionId);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add policy: {ex.Message}");
            }
        }

        private static void ForceInstall(string policyKeyPath, string extensionId, string extensionUrl, string browser)
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.CreateSubKey(policyKeyPath);
                key.SetValue("1", $"{extensionId};{extensionUrl}");
                key.Close();

                Console.WriteLine($"{browser} extension installation policy added successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add {browser} policy: {ex.Message}");
            }
        }

        static string GetManifestPath(string registryPath)
        {
            
            string defaultValue = null;
            // Open the registry key
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key != null)
                {
                    // Read the (Default) value
                    defaultValue = key.GetValue(null) as string;
                    // Pass null to get the (Default) value
                }
            }
            // Output the result
            if (!string.IsNullOrEmpty(defaultValue))
            {
                Console.WriteLine("Default value: " + defaultValue);
            }
            else
            {
                Console.WriteLine("Value not found or is empty.");
            }
            return defaultValue;
        }

        static void ForceUninstall(string policyKeyPath, string extensionId, string browser)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(policyKeyPath, writable: true))
                {
                    if (key != null)
                    {
                        foreach (var valueName in key.GetValueNames())
                        {
                            string value = key.GetValue(valueName) as string;
                            if (value != null && value.Contains(extensionId))
                            {
                                key.DeleteValue(valueName);
                                Console.WriteLine($"Removed {browser} extension from force list: {valueName}");
                                break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{browser} registry key not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing {browser} extension: {ex.Message}");
            }
        }

        static void AddExtensionToAllowedOrigins(string manifestFilePath, string extensionId)
        {
            if (File.Exists(manifestFilePath))
            {
                try
                {
                    string manifestJson = File.ReadAllText(manifestFilePath);

                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    var manifestObj = serializer.Deserialize<Dictionary<string, object>>(manifestJson);

                    if (manifestObj.ContainsKey("allowed_origins"))
                    {
                        var allowedOrigins = manifestObj["allowed_origins"] as ArrayList;

                        if (allowedOrigins != null)
                        {
                            List<string> allowedOriginsList = new List<string>();

                            foreach (var origin in allowedOrigins)
                            {
                                allowedOriginsList.Add(origin.ToString());
                            }

                            string newOrigin = $"chrome-extension://{extensionId}/";
                            if (!allowedOriginsList.Contains(newOrigin))
                            {
                                allowedOriginsList.Add(newOrigin);

                                manifestObj["allowed_origins"] = allowedOriginsList.ToArray();

                                string updatedManifestJson = serializer.Serialize(manifestObj);

                                File.WriteAllText(manifestFilePath, updatedManifestJson);

                                Console.WriteLine($"Extension ID {extensionId} added to allowed_origins successfully.");
                            }
                            else
                            {
                                Console.WriteLine("Extension ID already exists in allowed_origins.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("allowed_origins key is not an ArrayList.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("allowed_origins key not found in manifest.json.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating manifest.json: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Manifest file not found.");
            }
        }

        static void EnableAcrobatReaderPreferences()
        {
            // List of possible Acrobat Reader versions
            string[] versions = new string[]
            {
            "DC",     // Acrobat Reader DC
            "2020",   // Acrobat Reader 2020
            "2017",   // Acrobat Reader 2017
            "2015",   // Acrobat Reader 2015
            "11.0"    // Acrobat Reader XI
            };

            string[] paths = new string[]
            {
                "Acrobat Reader",
                "Adobe Acrobat"
            };
            foreach (var path in paths)
            {
                foreach (string version in versions)
                {
                    // Define the registry paths for JavaScript and 3D content for each version
                    string jsKey = $@"Software\Adobe\{path}\{version}\JSPrefs";
                    string jsValueName = "bEnableJS";

                    string threeDKey = $@"Software\Adobe\{path}\{version}\3D";
                    string threeDValueName = "b3DEnableContent";

                    string policyKey = $@"SOFTWARE\Policies\Adobe\{path}\{version}\FeatureLockDown";
                    string policyName = "bDisableJavaScript";

                    Console.WriteLine($"Updating Acrobat Reader {version} preferences...");

                    // Enable JavaScript
                    SetRegistryValueLocalMachine(policyKey, policyName, 0);

                    // Enable JavaScript
                    SetRegistryValue(jsKey, jsValueName, 1);

                    // Enable 3D content
                    SetRegistryValue(threeDKey, threeDValueName, 1);
                }
            }

            Console.WriteLine("Acrobat Reader preferences updated for all checked versions.");
        }

        static void SetRegistryValue(string baseKey, string name, int value)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(baseKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue(name, value, RegistryValueKind.DWord);
                        Console.WriteLine($"Set {name} to {value} in {baseKey}");
                    }
                    else
                    {
                        Console.WriteLine($"Registry key '{baseKey}' not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating {name} in {baseKey}: {ex.Message}");
            }
        }

        static void SetRegistryValueLocalMachine(string baseKey, string name, int value)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(baseKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue(name, value, RegistryValueKind.DWord);
                        Console.WriteLine($"Set {name} to {value} in {baseKey}");
                    }
                    else
                    {
                        Console.WriteLine($"Registry key '{baseKey}' not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating {name} in {baseKey}: {ex.Message}");
            }
        }
    }
}
