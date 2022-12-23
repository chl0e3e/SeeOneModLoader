using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeeOneModLoader.Patch
{
    public interface IPatch
    {
        public void Patch(Patcher patcher, AssemblyDefinition assembly);
    }
}
