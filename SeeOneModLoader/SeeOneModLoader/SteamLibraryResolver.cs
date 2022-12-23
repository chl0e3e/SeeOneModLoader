using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Diagnostics;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace SeeOneModLoader
{
    public class SteamLibraryResolver
    {
        private static string FORTRESSCRAFT_STEAM_APP_FOLDER_NAME = "FortressCraft  Chapter 1";
        private string? _steamPath = null;

        public SteamLibraryResolver()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                ///Library/Application Support/Steam/steamapps/common/
                this._steamPath = Path.Join(HomePath, "Library", "Application Support", "Steam", "steamapps");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process[] processlist = Process.GetProcesses();

                foreach (Process theprocess in processlist)
                {
                    if (theprocess.ProcessName.ToLower().EndsWith("steam"))
                    {
                        if (theprocess.MainModule != null)
                        {
                            string procPath = theprocess.MainModule.FileName;
                            this._steamPath = Path.GetDirectoryName(procPath);
                        }
                    }
                }
            }
        }

        public string? Resolve()
        {
            if (this._steamPath == null)
            {
                return null;
            }

            string fullPathToLibraryFolders = Path.Join(this._steamPath, "steamapps", "libraryfolders.vdf");
            VProperty libraryFolders = VdfConvert.Deserialize(File.ReadAllText(fullPathToLibraryFolders));
            foreach(VProperty i in libraryFolders.Value)
            {
                foreach (VProperty y in i.Value)
                {
                    if (y.Key == "path")
                    {
                        string relativeFCDirectory = Path.Join(y.Value.ToString(), "steamapps", "common", FORTRESSCRAFT_STEAM_APP_FOLDER_NAME);
                        if (Path.Exists(relativeFCDirectory))
                        {
                            return relativeFCDirectory;
                        }
                    }
                }
            }

            return null;
        }
    }
}
