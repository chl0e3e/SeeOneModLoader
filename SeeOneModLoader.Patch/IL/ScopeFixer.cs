using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static SeeOneModLoader.Patch.Patcher;

namespace SeeOneModLoader.Patch.IL
{
    public class ScopeFixer
    {
        public class ScopeFixerLogEventArgs : EventArgs
        {
            public string Message;

            public ScopeFixerLogEventArgs(string message)
            {
                Message = message;
            }
        }

        public class ScopeFixerProgressEventArgs : EventArgs
        {
            public int Progress;
            public int ProgressMax;

            public ScopeFixerProgressEventArgs(int progress, int progressMax)
            {
                Progress = progress;
                ProgressMax = progressMax;
            }
        }

        private List<object> _added;
        private List<string> _scopeExceptions;
        private AssemblyDefinition _assemblyDefinition;
        private IMetadataScope _scope;
        private int _currentTypeIndex;

        public event EventHandler<ScopeFixerLogEventArgs>? Log;
        public event EventHandler<ScopeFixerProgressEventArgs>? Progress;

        public ScopeFixer(AssemblyDefinition assemblyDefinition, IMetadataScope scope, List<string> scopeExceptions)
        {
            this._assemblyDefinition = assemblyDefinition;
            this._added = new List<object>();
            this._scope = scope;
            this._scopeExceptions = scopeExceptions;
        }

        public void Run()
        {
            this._added.Clear();
            Recurse(this._assemblyDefinition.MainModule, "MainModule");
        }

        public void Recurse(object o, string en, int level = 0)
        {
            if (this._added.Contains(o))
            {
                return;
            }
            else
            {
                this._added.Add(o);
            }

            if (o is TypeDefinition && this._assemblyDefinition.MainModule.Types.Contains(o))
            {
                if (this.Log != null)
                {
                    this.Log.Invoke(this, new ScopeFixerLogEventArgs("Patching " + ((TypeDefinition)o).FullName));
                }
                if (this.Progress != null)
                {
                    this.Progress.Invoke(this, new ScopeFixerProgressEventArgs(++this._currentTypeIndex, this._assemblyDefinition.MainModule.Types.Count));
                }
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
                            Recurse(x, en + "." + prop.Name + "[" + i + "]", level + 1);
                            i = i + 1;
                        }
                    }
                }
                else if (prop.PropertyType == typeof(TypeReference))
                {
                    var value = prop.GetValue(o);
                    if (value != null)
                    {
                        Recurse(value, en + "." + prop.Name, level + 1);
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
                            Recurse(value, en + "." + prop.Name, level + 1);
                        }
                    }
                }
                else if (prop.Name == "Body")
                {
                    //System.Diagnostics.Debug.WriteLine("Patching method body");

                    var value = prop.GetValue(o);
                    if (value != null)
                    {
                        Recurse(value, en + "." + prop.Name, level + 1);
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
                            Recurse(x, en + "." + prop.Name + "[" + i + "]", level + 1);
                            i = i + 1;
                        }
                    }
                }
                else if (prop.Name == "Scope")
                {
                    IMetadataScope? scopeValue = (IMetadataScope?) prop.GetValue(o);

                    if (scopeValue != null)
                    {
                        if (!this._scopeExceptions.Contains(scopeValue.Name))
                        {
                            try
                            {
                                prop.SetValue(o, this._scope);
                            }
                            catch
                            {
                                System.Diagnostics.Debug.WriteLine("Scope replacement failed");
                            }
                        }
                    }
                }
            }
        }
    }
}
