using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace PracTeleport {

  public enum ProcessAccessFlags : uint {
    All = 0x001F0FFF,
    Terminate = 0x00000001,
    CreateThread = 0x00000002,
    VirtualMemoryOperation = 0x00000008,
    VirtualMemoryRead = 0x00000010,
    VirtualMemoryWrite = 0x00000020,
    DuplicateHandle = 0x00000040,
    CreateProcess = 0x000000080,
    SetQuota = 0x00000100,
    SetInformation = 0x00000200,
    QueryInformation = 0x00000400,
    QueryLimitedInformation = 0x00001000,
    Synchronize = 0x00100000
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct MODULEINFO {
    public IntPtr lpBaseOfDll;
    public uint SizeOfImage;
    public IntPtr EntryPoint;
  }

  class MemoryManager {

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out, MarshalAs(UnmanagedType.AsAny)] object lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, uint nSize, UIntPtr lpNumberOfBytesWritten);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool EnumProcessModules(IntPtr hProcess, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)] [In][Out] uint[] lphModule, uint cb, [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

    [DllImport("psapi.dll", SetLastError = true)]
    static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, uint cb);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    private IntPtr hProcess;
    public IntPtr baseAddr;

    public MemoryManager() {
    }

    public bool Attach(string procName) {

      var procs = Process.GetProcessesByName(procName);
      if (procs.Length <= 0)
        return false;

      var proc = procs.First();
      
      if (proc == null)
        return false;

      foreach(ProcessModule m in proc.Modules) {
        if(m.ModuleName == procName + ".exe") {
          baseAddr = m.BaseAddress;
          break;
        }
      }
      
      hProcess = OpenProcess(ProcessAccessFlags.All, false, proc.Id);

      return true;

    }

    public byte readByte(IntPtr address) {
      byte[] buf = new byte[1];
      IntPtr bRead = IntPtr.Zero;

      ReadProcessMemory(hProcess, address, buf, buf.Length, out bRead);

      return buf[0];
    }

    public Int32 readInt32(IntPtr address) {
      byte[] buf = new byte[4];
      IntPtr bRead = IntPtr.Zero;

      ReadProcessMemory(hProcess, address, buf, buf.Length, out bRead);

      return BitConverter.ToInt32(buf, 0);

    }

    public Int64 readInt64(IntPtr address) {
      byte[] buf = new byte[8];
      IntPtr bRead = IntPtr.Zero;

      ReadProcessMemory(hProcess, address, buf, buf.Length, out bRead);

      return BitConverter.ToInt64(buf, 0);

    }

    public float readFloat(IntPtr address) {
      byte[] buf = new byte[4];
      IntPtr bRead = IntPtr.Zero;

      ReadProcessMemory(hProcess, address, buf, buf.Length, out bRead);

      return BitConverter.ToSingle(buf, 0);

    }

    public void writeByte(IntPtr address, byte b) {
      byte[] buf = new byte[1];
      buf[0] = b;
      UIntPtr bRead = UIntPtr.Zero;

      WriteProcessMemory(hProcess, address, buf, 1, out bRead);
      Debug.WriteLine(Marshal.GetLastWin32Error() + " " + bRead);
    }

    public void writeFloat(IntPtr address, float f) {
      byte[] buf = BitConverter.GetBytes(f);
      UIntPtr bRead = UIntPtr.Zero;

      WriteProcessMemory(hProcess, address, buf, (uint)buf.Length, out bRead);
    }

    public IntPtr? evalPointerChain(List<Int64> offs) {

      Int64 addr = offs.First();
      try {
        for (int i = 1; i < offs.Count; i++) {
          addr = this.readInt64(new IntPtr(addr));
          addr = addr + offs[i];
        }
      } catch(Exception e) {
        return null;
      }

      return new IntPtr(addr);
      
    }

    public IntPtr? AobScan(List<uint?> pattern, IntPtr start, int size) {

      byte[] buf = new byte[size];
      IntPtr bRead = IntPtr.Zero;

      ReadProcessMemory(hProcess, start, buf, size, out bRead);
      Debug.WriteLine(Marshal.GetLastWin32Error() + " " + bRead.ToString("X") + " " + size.ToString("X"));

      StringBuilder sb = new StringBuilder();
      for(int xx = 0; xx < pattern.Count; xx++) {
        sb.Append(buf[0x841875 + xx].ToString("X") + " ");
      }
      Debug.WriteLine(sb.ToString());

      // BMH

      int m = pattern.Count, n = size;

      //if (m > n)
      //  return null;

      int[] skip = new int[256];

      for (int i = 0; i < 256; i++)
        skip[i] = -1;

      for (int i = 0; i < m; i++) {
        if (pattern[i].HasValue)
          skip[pattern[i].Value] = i;
      }

      int k = m - 1;
      while (k <= n - m) {
        int j = m - 1;
        while (j >= 0 && ((!pattern[j].HasValue) || (pattern[j].Value == buf[k + j]))) {
          --j;
        }
        if (j < 0)
          return new IntPtr(k);
        else
          k += Math.Max(1, j - skip[buf[k + j]]);
      }

      return null;
    }

  }
}
