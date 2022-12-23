using ILRepacking;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CompilerServices.SymbolWriter;
using SeeOneModLoader.Patch.Patches;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SeeOneModLoader.Patch
{
    public class Patcher
    {
        public static Dictionary<string, IPatch> PATCHES = new Dictionary<string, IPatch>();
        static Patcher()
        {
            PATCHES.Add("FNA - SDL", new FNA_SDL_Patch());
            PATCHES.Add("FNA - Remove SlimInput", new FNA_Remove_SlimInput_Patch());
            PATCHES.Add("FNA - Reoute XNA Guide", new FNA_Reroute_XNA_Guide());
        }

        private class CustomResolver : BaseAssemblyResolver
        {
            private DefaultAssemblyResolver _defaultResolver;
            private Patcher _patcher;
            private AssemblyDefinition _runtimeAssembly;

            public CustomResolver(Patcher patcher)
            {
                this._defaultResolver = new DefaultAssemblyResolver();
                this._patcher = patcher;
                this._runtimeAssembly = AssemblyDefinition.ReadAssembly(Path.Join(this._patcher.BinaryOutputPath, "Runtime.dll"));
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                AssemblyDefinition assembly;
                try
                {
                    assembly = _defaultResolver.Resolve(name);
                }
                catch (AssemblyResolutionException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    System.Diagnostics.Debug.WriteLine("Test");
                }
                return this._runtimeAssembly;
            }
        }

        public string BinaryDirectoryPath;
        public string BinaryPath;
        public string BinaryOutputPath;
        public string PatchedBinaryPath;

        private void CopyDllFromPatcher(string dll)
        {
            File.Copy(dll, Path.Join(this.BinaryOutputPath, dll), true);
        }

        private void CopyDllFromGame(string dll)
        {
            File.Copy(Path.Join(this.BinaryDirectoryPath, dll), Path.Join(this.BinaryOutputPath, dll), true);
        }

        public Patcher(string binaryDirPath, string binaryOutputPath)
        {
            this.BinaryDirectoryPath = binaryDirPath;
            this.BinaryOutputPath = binaryOutputPath;
            this.BinaryPath = Path.Join(binaryDirPath, "FortressCraft.exe");
            this.PatchedBinaryPath = Path.Join(BinaryOutputPath, "FortressCraft.exe");

            __MergeBinaries();
        }

        void __MergeBinaries ()
        {
            if (File.Exists(this.PatchedBinaryPath))
            {
                File.Delete(this.PatchedBinaryPath);
            }

            File.Copy(this.BinaryPath, this.PatchedBinaryPath);

            CopyDllFromPatcher("FNA.dll");
            CopyDllFromPatcher("FNAHelpers.dll");
            CopyDllFromPatcher("XNACommonCrossPlatform.dll");
            CopyDllFromPatcher("FAudio.dll");
            CopyDllFromPatcher("libtheorafile.dll");
            CopyDllFromPatcher("SDL2.dll");
            CopyDllFromPatcher("FNA3D.dll");

            CopyDllFromGame("Steamworks.NET.dll");
            CopyDllFromGame("steam_api.dll");
            CopyDllFromGame("steam_appid.txt");

            Version runtimeVersion = new Version(1, 0, 0, 3);

            RepackOptions options = new RepackOptions();
            options.OutputFile = Path.Join(this.BinaryOutputPath, "Runtime.dll");
            options.InputAssemblies = new string[]
            {
                Path.Join(this.BinaryOutputPath, "FNA.dll"),
                Path.Join(this.BinaryOutputPath, "Steamworks.NET.dll"),
                Path.Join(this.BinaryOutputPath, "XNACommonCrossPlatform.dll"),
                Path.Join(this.BinaryOutputPath, "FNAHelpers.dll")
            };
            options.Version = runtimeVersion;
            options.SearchDirectories = new List<String> { this.BinaryOutputPath };

            if (File.Exists(options.OutputFile))
            {
                File.Delete(options.OutputFile);
            }

            ILRepack repack = new ILRepack(options);
            repack.Repack();

            AssemblyNameReference runtimeRef = new AssemblyNameReference("Runtime", runtimeVersion);

            ReaderParameters rp = new ReaderParameters();
            rp.AssemblyResolver = new CustomResolver(this);

            AssemblyDefinition fcAssembly = AssemblyDefinition.ReadAssembly(this.PatchedBinaryPath, rp);

            List<AssemblyNameReference> references = new List<AssemblyNameReference>(fcAssembly.MainModule.AssemblyReferences);
            foreach (AssemblyNameReference reference in references)
            {
                if (reference.Name.StartsWith("Microsoft.Xna") || reference.Name.StartsWith("Steamworks") || reference.Name.StartsWith("XNACommon"))
                {
                    fcAssembly.MainModule.AssemblyReferences.Remove(reference);
                }
            }
            fcAssembly.MainModule.AssemblyReferences.Add(runtimeRef);

            List<object> _added = new List<object>();
            int n = 0;
            int totalTypes = fcAssembly.MainModule.Types.Count;
            void RecursiveScopeChange(object o, string en, int level = 0, AssemblyNameDefinition? scopeOverride=null)
            {
                //
                if (_added.Contains(o))
                {
                    return;
                }
                else
                {
                    _added.Add(o);
                }

                if (o is TypeDefinition)
                {
                    System.Diagnostics.Debug.WriteLine("Patching " + ((TypeDefinition)o).FullName);
                    n += 1;
                }

                foreach (PropertyInfo prop in o.GetType().GetProperties())
                {
                    if (prop.PropertyType.GetInterface(nameof(ICollection)) != null)
                    {
                        var value = prop.GetValue(o);
                        if (value != null)
                        {
                            int i = 0;
                            foreach (object x in (ICollection)value)
                            {
                                //System.Diagnostics.Debug.WriteLine("Going recursive");
                                RecursiveScopeChange(x, en + "." + prop.Name + "[" + i + "]", level + 1, scopeOverride);
                                i = i + 1;
                            }
                        }
                    }
                    else if (prop.PropertyType == typeof(TypeReference))
                    {
                        var value = prop.GetValue(o);
                        if(value != null)
                        {
                            RecursiveScopeChange(value, en + "." + prop.Name, level + 1, scopeOverride);
                        }
                    }
                    else if (prop.Name == "Operand")
                    {
                        var value = prop.GetValue(o);
                        if (value != null)
                        {
                            bool customScope = false;

                            if (!customScope)
                            {
                                RecursiveScopeChange(value, en + "." + prop.Name, level + 1, scopeOverride);
                            }
                        }
                    }
                    else if (prop.Name == "Body")
                    {
                        //System.Diagnostics.Debug.WriteLine("Patching method body");

                        var value = prop.GetValue(o);
                        if (value != null)
                        {
                            RecursiveScopeChange(value, en + "." + prop.Name, level + 1, scopeOverride);
                        }
                    }
                    else if (prop.Name == "Instructions")
                    {
                        var value = prop.GetValue(o);
                        if (value != null)
                        {
                            int i = 0;
                            foreach (object x in (ICollection)value)
                            {
                                RecursiveScopeChange(x, en + "." + prop.Name + "[" + i + "]", level + 1, scopeOverride);
                                i = i + 1;
                            }
                        }
                    }
                    else if (prop.Name == "Scope")
                    {
                        IMetadataScope scopeValue = (IMetadataScope) prop.GetValue(o);

                        if (scopeValue != null)
                        {
                            if (!scopeValue.Name.Equals("FortressCraft.exe") &&
                                !scopeValue.Name.Equals("mscorlib") &&
                                !scopeValue.Name.Equals("Runtime") &&
                                !scopeValue.Name.Equals("System.Xml") &&
                                !scopeValue.Name.Equals("System") &&
                                !scopeValue.Name.Equals("System.Core"))
                            {
                                try
                                {
                                    if (scopeOverride != null)
                                    {
                                        prop.SetValue(o, scopeOverride);
                                    }
                                    else
                                    {
                                        prop.SetValue(o, runtimeRef);
                                    }
                                }
                                catch
                                {
                                    System.Diagnostics.Debug.WriteLine("Scope replacement failed");
                                    System.Diagnostics.Debug.WriteLine("Scope replacement failed");
                                    System.Diagnostics.Debug.WriteLine("Scope replacement failed");
                                    System.Diagnostics.Debug.WriteLine("Scope replacement failed");
                                    System.Diagnostics.Debug.WriteLine("Scope replacement failed");
                                }
                            }
                        }
                    }
                }
            }

            RecursiveScopeChange(fcAssembly.MainModule, "MainModule");

            fcAssembly.Write(Path.Join(this.BinaryOutputPath, "FortressCraft.FNA.exe"));
            fcAssembly.Dispose();

            File.Delete(Path.Join(this.BinaryOutputPath, "FNA.dll"));
            File.Delete(Path.Join(this.BinaryOutputPath, "Steamworks.NET.dll"));
            File.Delete(Path.Join(this.BinaryOutputPath, "XNACommonCrossPlatform.dll"));
            File.Delete(Path.Join(this.BinaryOutputPath, "FNAHelpers.dll"));
            File.Delete(Path.Join(this.BinaryOutputPath, "FortressCraft.exe"));

            string patchedContentFolder = Path.Join(this.BinaryOutputPath, "Content");
            string steamContentFolder = Path.Join(this.BinaryDirectoryPath, "Content");
            string oggMusicFolder = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Music");

            if (!Directory.Exists(patchedContentFolder))
            {
                foreach (string dirPath in Directory.GetDirectories(steamContentFolder, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(steamContentFolder, patchedContentFolder));
                }

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(steamContentFolder, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(steamContentFolder, patchedContentFolder), true);
                }

                foreach (string musicFile in Directory.GetFiles(oggMusicFolder))
                {
                    var fileName = musicFile.Replace(oggMusicFolder, "");
                    if (fileName.StartsWith("/") || fileName.StartsWith("\\"))
                    {
                        fileName = fileName.Substring(1);
                    }
                    File.Copy(musicFile, Path.Join(patchedContentFolder, "Music", fileName));
                }
            }

            this.PatchedBinaryPath = Path.Join(this.BinaryOutputPath, "FortressCraft.FNA.exe");
        }

        public AssemblyDefinition Patch(List<string> patchers)
        {
            ReaderParameters rp = new ReaderParameters();
            rp.AssemblyResolver = new CustomResolver(this);

            AssemblyDefinition fcAssembly = AssemblyDefinition.ReadAssembly(this.PatchedBinaryPath, rp);

            foreach (string patcher in patchers)
            {
                PATCHES[patcher].Patch(this, fcAssembly);
            }

            return fcAssembly;
        }

        public void Run(AssemblyDefinition assembly)
        {
            if (!Path.Exists(this.BinaryOutputPath))
            {
                Directory.CreateDirectory(this.BinaryOutputPath);
            }

            assembly.Write(Path.Join(this.BinaryOutputPath, "FortressCraft.FNA.Patched.exe"));
        }
    }
}