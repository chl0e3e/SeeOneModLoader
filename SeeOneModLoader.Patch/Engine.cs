using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeeOneModLoader.Patch
{
    public enum Engine
    {
        FNA,
        XNA
    }

    public class EngineAttribute : Attribute
    {
        public Engine Engine;

        public EngineAttribute(Engine engine)
        {
            this.Engine = engine;
        }
    }
}
