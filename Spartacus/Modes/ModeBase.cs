using Spartacus.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Modes
{
    abstract class ModeBase
    {
        protected Helper Helper = new();

        abstract public void SanitiseAndValidateRuntimeData();

        abstract public void Run();
    }
}
