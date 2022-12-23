using ILRepacking;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CompilerServices.SymbolWriter;
using SeeOneModLoader.Patch.IL;
using SeeOneModLoader.Patch.Patches;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SeeOneModLoader.Patch
{
    public class Patcher
    {
        public static Dictionary<string, IPatch> PATCHES = new Dictionary<string, IPatch>();
        public static Version FNA_RUNTIME_VERSION = new Version(1, 0, 0, 5);
        public static Version XNA_RUNTIME_VERSION = new Version(1, 0, 0, 5);

        static Patcher()
        {
            PATCHES.Add("FNA - SDL", new FNA_SDL_Patch());
            PATCHES.Add("FNA - Remove SlimInput", new FNA_Remove_SlimInput_Patch());
            PATCHES.Add("FNA - Reroute XNA Guide", new FNA_Reroute_XNA_Guide());
        }

        private class CustomResolver : BaseAssemblyResolver
        {
            public DefaultAssemblyResolver DefaultResolver;

            private Patcher _patcher;
            private AssemblyDefinition _runtimeAssembly;
            private Dictionary<string, AssemblyDefinition> _xnaAssemblies;

            public CustomResolver(Patcher patcher)
            {
                this.DefaultResolver = new DefaultAssemblyResolver();

                this._patcher = patcher;
                this._runtimeAssembly = AssemblyDefinition.ReadAssembly(this._patcher.RuntimeDll);

                if (this._patcher.Engine == Engine.XNA)
                {
                    this._xnaAssemblies = new Dictionary<string, AssemblyDefinition>();

                    foreach (string dll in Directory.GetFiles(this._patcher.XNARuntimePath))
                    {
                        AssemblyDefinition xnaAssembly = AssemblyDefinition.ReadAssembly(dll);
                        this._xnaAssemblies.Add(xnaAssembly.Name.Name, xnaAssembly);
                    }
                }
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                AssemblyDefinition assembly;
                try
                {
                    assembly = DefaultResolver.Resolve(name);
                }
                catch (AssemblyResolutionException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    System.Diagnostics.Debug.WriteLine("Test");
                }

                if (this._patcher.Engine == Engine.XNA)
                {
                    if (this._xnaAssemblies.ContainsKey(name.Name))
                    {
                        return this._xnaAssemblies[name.Name];
                    }
                }
                
                return this._runtimeAssembly;
            }
        }

        public class PatcherStageEventArgs : EventArgs
        {
            public string Message;

            public PatcherStageEventArgs(string message)
            {
                this.Message = message;
            }
        }

        public class PatcherLogEventArgs : EventArgs
        {
            public string Message;

            public PatcherLogEventArgs(string message)
            {
                this.Message = message;
            }
        }

        public class PatcherProgressEventArgs : EventArgs
        {
            public int Progress;
            public int ProgressMax;

            public PatcherProgressEventArgs(int progress, int progressMax)
            {
                this.Progress = progress;
                this.ProgressMax = progressMax;
            }
        }

        public string SteamDirectoryPath;
        public string SteamBinaryPath;
        public string PatcherOutputPath;
        public string PatcherOutputExePath;

        public string RuntimeXNADll;
        public string RuntimeFNADll;

        public string RuntimeDll;

        public string FortressCraftXNAExe;
        public string FortressCraftFNAExe;

        public string XNARuntimePath;

        public Engine Engine;

        public event EventHandler<PatcherLogEventArgs>? Log;
        public event EventHandler<PatcherStageEventArgs>? Stage;
        public event EventHandler<PatcherProgressEventArgs>? Progress;

        private void CopyDllFromFNA (string dll)
        {
            File.Copy(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "FNA", dll), Path.Join(this.PatcherOutputPath, dll), true);
        }

        private void CopyDllFromGame (string dll)
        {
            File.Copy(Path.Join(this.SteamDirectoryPath, dll), Path.Join(this.PatcherOutputPath, dll), true);
        }

        private string OutputPath (string filename)
        {
            return Path.Join(this.PatcherOutputPath, filename);
        }

        private string SteamPath (string filename)
        {
            return Path.Join(this.SteamDirectoryPath, filename);
        }

        public Patcher (Engine engine, string binaryDirPath, string binaryOutputPath)
        {
            this.Engine = engine;

            this.SteamDirectoryPath = binaryDirPath;
            this.SteamBinaryPath = Path.Join(binaryDirPath, "FortressCraft.exe");

            this.PatcherOutputPath = binaryOutputPath;
            this.PatcherOutputExePath = this.SteamBinaryPath;

            this.RuntimeXNADll = Path.Join(this.PatcherOutputPath, "Runtime.XNA.dll");
            this.RuntimeFNADll = Path.Join(this.PatcherOutputPath, "Runtime.FNA.dll");

            this.RuntimeDll = this.Engine == Engine.FNA ? this.RuntimeFNADll : this.RuntimeXNADll;

            this.FortressCraftXNAExe = Path.Join(this.PatcherOutputPath, "FortressCraft.XNA.exe");
            this.FortressCraftFNAExe = Path.Join(this.PatcherOutputPath, "FortressCraft.FNA.exe");

            this.XNARuntimePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "XNA");
        }

        void __PatchForFNA (AssemblyNameReference runtimeReference)
        {
            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Merging FortressCraft DLLs for " + this.Engine.ToString()));
            }

            CopyDllFromFNA("FNA.dll");
            CopyDllFromFNA("FNAHelpers.dll");
            CopyDllFromFNA("FAudio.dll");
            CopyDllFromFNA("libtheorafile.dll");
            CopyDllFromFNA("SDL2.dll");
            CopyDllFromFNA("FNA3D.dll");    

            RepackOptions options = new RepackOptions();
            options.OutputFile = Path.Join(this.PatcherOutputPath, "Runtime.FNA.dll");
            options.InputAssemblies = new string[]
            {
                OutputPath("FNA.dll"),
                SteamPath("Steamworks.NET.dll"),
                SteamPath("XNACommonCrossPlatform.dll"),
                OutputPath("FNAHelpers.dll")
            };
            options.SearchDirectories = new string[0];
            options.Version = FNA_RUNTIME_VERSION;

            if (File.Exists(options.OutputFile))
            {
                File.Delete(options.OutputFile);
            }

            ILRepack repack = new ILRepack(options);
            repack.Repack();

            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Patching runtime DLL scopes"));
            }

            MemoryStream runtimeAssemblyStream = new MemoryStream(File.ReadAllBytes(options.OutputFile));
            AssemblyDefinition runtimeAssembly = AssemblyDefinition.ReadAssembly(runtimeAssemblyStream);

            ScopeFixer runtimeAssemblyScopeFixer = new ScopeFixer(runtimeAssembly, runtimeReference, new List<string> {
                "mscorlib",
                "System.Xml",
                "System",
                "System.Core"
            });
            runtimeAssemblyScopeFixer.Progress += (sender, e) =>
            {
                if (this.Progress != null)
                {
                    this.Progress.Invoke(this, new PatcherProgressEventArgs(e.Progress, e.ProgressMax));
                }
            };
            runtimeAssemblyScopeFixer.Log += (sender, e) =>
            {
                if (this.Log != null)
                {
                    this.Log.Invoke(this, new PatcherLogEventArgs(e.Message));
                }
            };
            runtimeAssemblyScopeFixer.Run();

            List<AssemblyNameReference> runtimeReferences = new List<AssemblyNameReference>(runtimeAssembly.MainModule.AssemblyReferences);
            foreach (AssemblyNameReference nameRef in runtimeReferences)
            {
                if (nameRef.Name.StartsWith("Microsoft.Xna."))
                {
                    runtimeAssembly.MainModule.AssemblyReferences.Remove(nameRef);
                }
            }

            if (File.Exists(this.RuntimeDll))
            {
                File.Delete(this.RuntimeDll);
            }

            runtimeAssembly.Write(this.RuntimeDll);
            runtimeAssemblyStream.Close();

            File.Delete(OutputPath("FNA.dll"));
            File.Delete(OutputPath("FNAHelpers.dll"));
        }

        void __PatchForXNA (AssemblyNameReference runtimeReference)
        {
            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Merging FortressCraft DLLs for " + this.Engine.ToString()));
            }

            List<string> runtimeAssemblies = new List<string>();

            runtimeAssemblies.Add(Path.Join(this.SteamDirectoryPath, "Steamworks.NET.dll"));
            runtimeAssemblies.Add(Path.Join(this.SteamDirectoryPath, "XNACommonCrossPlatform.dll"));

            RepackOptions options = new RepackOptions();
            options.OutputFile = Path.Join(this.PatcherOutputPath, "Runtime.XNA.dll");
            options.InputAssemblies = runtimeAssemblies.ToArray();
            options.Version = XNA_RUNTIME_VERSION;
            options.SearchDirectories = new string[0];

            if (File.Exists(options.OutputFile))
            {
                File.Delete(options.OutputFile);
            }

            ILRepack repack = new ILRepack(options);
            repack.Repack();

            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Patching runtime DLL scopes"));
            }

            ReaderParameters rp = new ReaderParameters();

            DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(this.XNARuntimePath);

            rp.AssemblyResolver = assemblyResolver;

            MemoryStream runtimeAssemblyStream = new MemoryStream(File.ReadAllBytes(options.OutputFile));
            AssemblyDefinition runtimeAssembly = AssemblyDefinition.ReadAssembly(runtimeAssemblyStream, rp);

            ScopeFixer runtimeAssemblyScopeFixer = new ScopeFixer(runtimeAssembly, runtimeReference, new List<string> {
                "Microsoft.Xna.Framework",
                "Microsoft.Xna.Framework.Avatar",
                "Microsoft.Xna.Framework.Game",
                "Microsoft.Xna.Framework.GamerServices",
                "Microsoft.Xna.Framework.Graphics",
                "Microsoft.Xna.Framework.Input.Touch",
                "Microsoft.Xna.Framework.Net",
                "Microsoft.Xna.Framework.Storage",
                "Microsoft.Xna.Framework.Video",
                "Microsoft.Xna.Framework.Xact",
                "mscorlib",
                "System.Xml",
                "System",
                "System.Core"
            });
            runtimeAssemblyScopeFixer.Progress += (sender, e) =>
            {
                if (this.Progress != null)
                {
                    this.Progress.Invoke(this, new PatcherProgressEventArgs(e.Progress, e.ProgressMax));
                }
            };
            runtimeAssemblyScopeFixer.Log += (sender, e) =>
            {
                if (this.Log != null)
                {
                    this.Log.Invoke(this, new PatcherLogEventArgs(e.Message));
                }
            };
            runtimeAssemblyScopeFixer.Run();

            if (File.Exists(this.RuntimeDll))
            {
                File.Delete(this.RuntimeDll);
            }

            runtimeAssembly.Write(this.RuntimeDll);
            runtimeAssemblyStream.Close();
        }

        void __PatchWithRuntime (AssemblyNameReference runtimeReference, string outputPath)
        {
            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Merging FortressCraft DLLs for " + this.Engine.ToString()));
            }

            ReaderParameters rp = new ReaderParameters();
            rp.AssemblyResolver = new CustomResolver(this);

            MemoryStream runtimeAssemblyStream = new MemoryStream(File.ReadAllBytes(this.SteamBinaryPath));
            AssemblyDefinition fcAssembly = AssemblyDefinition.ReadAssembly(runtimeAssemblyStream, rp);

            List<AssemblyNameReference> references = new List<AssemblyNameReference>(fcAssembly.MainModule.AssemblyReferences);
            foreach (AssemblyNameReference reference in references)
            {
                if ((this.Engine == Engine.FNA && reference.Name.StartsWith("Microsoft.Xna")) || reference.Name.StartsWith("Steamworks") || reference.Name.StartsWith("XNACommon"))
                {
                    fcAssembly.MainModule.AssemblyReferences.Remove(reference);
                }
            }
            fcAssembly.MainModule.AssemblyReferences.Add(runtimeReference);

            List<object> _added = new List<object>();
            int n = 0;
            int totalTypes = fcAssembly.MainModule.Types.Count;

            List<string> scopeExceptions;

            if (this.Engine == Engine.XNA)
            {
                scopeExceptions = new List<string> {
                    "FortressCraft.exe",
                    "Microsoft.Xna.Framework",
                    "Microsoft.Xna.Framework.Avatar",
                    "Microsoft.Xna.Framework.Game",
                    "Microsoft.Xna.Framework.GamerServices",
                    "Microsoft.Xna.Framework.Graphics",
                    "Microsoft.Xna.Framework.Input.Touch",
                    "Microsoft.Xna.Framework.Net",
                    "Microsoft.Xna.Framework.Storage",
                    "Microsoft.Xna.Framework.Video",
                    "Microsoft.Xna.Framework.Xact",
                    "mscorlib",
                    "System.Xml",
                    "System",
                    "System.Core"
                };
            }
            else
            {
                scopeExceptions = new List<string> {
                    "FortressCraft.exe",
                    "mscorlib",
                    "System.Xml",
                    "System",
                    "System.Core"
                };
            }

            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Fixing FortressCraft scopes"));
            }

            ScopeFixer scopeFixer = new ScopeFixer(fcAssembly, runtimeReference, scopeExceptions);
            scopeFixer.Progress += (sender, e) =>
            {
                if (this.Progress != null)
                {
                    this.Progress.Invoke(this, new PatcherProgressEventArgs(e.Progress, e.ProgressMax));
                }
            };
            scopeFixer.Log += (sender, e) =>
            {
                if (this.Log != null)
                {
                    this.Log.Invoke(this, new PatcherLogEventArgs(e.Message));
                }
            };
            scopeFixer.Run();

            fcAssembly.Write(outputPath);
            fcAssembly.Dispose();

            runtimeAssemblyStream.Close();
        }

        void __CopyContent ()
        {
            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Checking for content"));
            }

            if (this.Log != null)
            {
                this.Log.Invoke(this, new PatcherLogEventArgs("Checking for content"));
            }

            string patchedContentFolder = Path.Join(this.PatcherOutputPath, "Content");
            string steamContentFolder = Path.Join(this.SteamDirectoryPath, "Content");
            string oggMusicFolder = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Music");

            if (!Directory.Exists(patchedContentFolder))
            {
                foreach (string dirPath in Directory.GetDirectories(steamContentFolder, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(steamContentFolder, patchedContentFolder));
                }

                if (this.Stage != null)
                {
                    this.Stage.Invoke(this, new PatcherStageEventArgs("Copying content"));
                }

                //Copy all the files & Replaces any files with the same name
                string[] contentFiles = Directory.GetFiles(steamContentFolder, "*.*", SearchOption.AllDirectories);
                int i = 0;
                foreach (string newPath in contentFiles)
                {
                    if (this.Progress != null)
                    {
                        this.Progress.Invoke(this, new PatcherProgressEventArgs(i, contentFiles.Length));
                    }

                    File.Copy(newPath, newPath.Replace(steamContentFolder, patchedContentFolder), true);
                    i++;
                }

                if (this.Stage != null)
                {
                    this.Stage.Invoke(this, new PatcherStageEventArgs("Copying OGG music"));
                }

                string[] oggFiles = Directory.GetFiles(oggMusicFolder);
                i = 0;
                foreach (string musicFile in oggFiles)
                {
                    if (this.Progress != null)
                    {
                        this.Progress.Invoke(this, new PatcherProgressEventArgs(i, oggFiles.Length));
                    }

                    var fileName = musicFile.Replace(oggMusicFolder, "");
                    if (fileName.StartsWith("/") || fileName.StartsWith("\\"))
                    {
                        fileName = fileName.Substring(1);
                    }
                    File.Copy(musicFile, Path.Join(patchedContentFolder, "Music", fileName));
                    i++;
                }
            }
            else
            {
                if (this.Log != null)
                {
                    this.Log.Invoke(this, new PatcherLogEventArgs("Content found"));
                }
            }
        }

        public void PatchWithRuntimes ()
        {
            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Checking to see if Runtime patch needed"));
            }

            CopyDllFromGame("Steamworks.NET.dll");
            CopyDllFromGame("XNACommonCrossPlatform.dll");
            CopyDllFromGame("steam_api.dll");
            CopyDllFromGame("steam_appid.txt");

            AssemblyNameReference runtimeReference = new AssemblyNameReference(
                this.Engine == Engine.FNA ? "Runtime.FNA" : "Runtime.XNA",
                this.Engine == Engine.FNA ? FNA_RUNTIME_VERSION : XNA_RUNTIME_VERSION
            );

            bool patchRuntime = false;

            if (this.Engine == Engine.FNA)
            {
                if (File.Exists(this.RuntimeDll))
                {
                    AssemblyDefinition runtimeAssembly = AssemblyDefinition.ReadAssembly(this.RuntimeDll);

                    if (runtimeAssembly.Name.Version < FNA_RUNTIME_VERSION)
                    {
                        patchRuntime = true;
                        runtimeAssembly.Dispose();
                        File.Delete(this.RuntimeDll);
                    }
                    else
                    {
                        runtimeAssembly.Dispose();
                    }
                }
                else
                {
                    patchRuntime = true;
                }
            }
            else if (this.Engine == Engine.XNA)
            {
                if (File.Exists(this.RuntimeDll))
                {
                    AssemblyDefinition runtimeAssembly = AssemblyDefinition.ReadAssembly(this.RuntimeDll);

                    if (runtimeAssembly.Name.Version < XNA_RUNTIME_VERSION)
                    {
                        patchRuntime = true;
                        runtimeAssembly.Dispose();
                        File.Delete(this.RuntimeDll);
                    }
                    else
                    {
                        runtimeAssembly.Dispose();
                    }
                }
                else
                {
                    patchRuntime = true;
                }
            }

            string binaryChecksum = "";
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(this.SteamBinaryPath))
                {
                    byte[] binaryChecksumByteArr = sha256.ComputeHash(fileStream);
                    foreach (byte theByte in binaryChecksumByteArr)
                    {
                        binaryChecksum += theByte.ToString("x2");
                    }
                }
            }

            string assemblyOutputPath = OutputPath("FortressCraft." + binaryChecksum + "." + this.Engine.ToString() + ".exe");

            if (patchRuntime)
            {
                if (this.Engine == Engine.XNA)
                {
                    __PatchForXNA(runtimeReference);
                }
                else
                {
                    __PatchForFNA(runtimeReference);
                }
            }

            if (patchRuntime || !File.Exists(assemblyOutputPath))
            {
                this.__PatchWithRuntime(runtimeReference, assemblyOutputPath);
            }

            if (this.Log != null)
            {
                this.Log.Invoke(this, new PatcherLogEventArgs("Patched assembly at " + assemblyOutputPath));
            }

            this.PatcherOutputExePath = assemblyOutputPath;
            this.__CopyContent();
        }

        public AssemblyDefinition Patch(List<string> patchers)
        {
            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Running FortressCraft patchers"));
            }

            ReaderParameters rp = new ReaderParameters();

            CustomResolver assemblyResolver = new CustomResolver(this);

            rp.AssemblyResolver = assemblyResolver;

            AssemblyDefinition fcAssembly = AssemblyDefinition.ReadAssembly(this.PatcherOutputExePath, rp);

            foreach (string patcher in patchers)
            {
                if (this.Log != null)
                {
                    this.Log.Invoke(this, new PatcherLogEventArgs("Running patcher: " + patcher));
                }

                PATCHES[patcher].Patch(this, fcAssembly);
            }

            return fcAssembly;
        }

        public string Run(AssemblyDefinition assembly)
        {
            if (this.Stage != null)
            {
                this.Stage.Invoke(this, new PatcherStageEventArgs("Writing patched binary"));
            }

            if (!Path.Exists(this.PatcherOutputPath))
            {
                Directory.CreateDirectory(this.PatcherOutputPath);
            }

            string patchedOutputBinaryPath = this.PatcherOutputExePath.Replace(".exe", ".Patched.exe");
            if (this.Log != null)
            {
                this.Log.Invoke(this, new PatcherLogEventArgs("Writing patched binary to " + patchedOutputBinaryPath));
            }
            assembly.Write(patchedOutputBinaryPath);
            return patchedOutputBinaryPath;
        }
    }
}