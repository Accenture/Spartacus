using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Modes.SIGN.MsSign
{
    internal sealed class UnmanagedStruct<T> : IDisposable
    where T : struct
    {
        public IntPtr Pointer { get; private set; }

        public UnmanagedStruct()
        {
            Pointer = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
        }

        public void Fill(T value)
        {
            Marshal.StructureToPtr(value, Pointer, false);
        }

        public UnmanagedStruct(T v) : this()
        {
            Marshal.StructureToPtr(v, Pointer, false);
        }

        public void Dispose()
        {
            if (Pointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Pointer);
                Pointer = IntPtr.Zero;
            }
        }
    }
}
