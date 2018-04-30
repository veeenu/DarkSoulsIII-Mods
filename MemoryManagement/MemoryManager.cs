using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using KeystoneNET;

namespace MemoryManagement {

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
    public UInt64 lpBaseOfDll;
    public uint SizeOfImage;
    public UInt64 EntryPoint;
  }

  [Flags]
  public enum AllocationType {
    Commit = 0x1000,
    Reserve = 0x2000,
    Decommit = 0x4000,
    Release = 0x8000,
    Reset = 0x80000,
    Physical = 0x400000,
    TopDown = 0x100000,
    WriteWatch = 0x200000,
    LargePages = 0x20000000
  }

  [Flags]
  public enum MemoryProtection {
    Execute = 0x10,
    ExecuteRead = 0x20,
    ExecuteReadWrite = 0x40,
    ExecuteWriteCopy = 0x80,
    NoAccess = 0x01,
    ReadOnly = 0x02,
    ReadWrite = 0x04,
    WriteCopy = 0x08,
    GuardModifierflag = 0x100,
    NoCacheModifierflag = 0x200,
    WriteCombineModifierflag = 0x400
  }

  public class MemoryManager {

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern UInt64 VirtualAllocEx(UInt64 hProcess, UInt64 lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern bool VirtualFreeEx(UInt64 hProcess, UInt64 lpAddress, int dwSize, AllocationType dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(UInt64 hProcess, UInt64 lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out UInt64 lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(UInt64 hProcess, UInt64 lpBaseAddress, [Out, MarshalAs(UnmanagedType.AsAny)] object lpBuffer, int dwSize, out UInt64 lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(UInt64 hProcess, UInt64 lpBaseAddress, UInt64 lpBuffer, int dwSize, out UInt64 lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(UInt64 hProcess, UInt64 lpBaseAddress, byte[] lpBuffer, uint nSize, out UInt64 lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(UInt64 hProcess, UInt64 lpBaseAddress, UInt64 lpBuffer, uint nSize, UInt64 lpNumberOfBytesWritten);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool EnumProcessModules(UInt64 hProcess, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)] [In][Out] UInt64[] lphModule, uint cb, [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

    [DllImport("psapi.dll", SetLastError = true)]
    static extern bool GetModuleInformation(UInt64 hProcess, UInt64 hModule, out MODULEINFO lpmodinfo, uint cb);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern UInt64 OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

    [DllImport("psapi", SetLastError = true)]
    static extern uint GetModuleBaseName(UInt64 hProcess, UInt64 hModule, StringBuilder lpBaseName, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(UInt64 hObject);

    private UInt64 hProcess;
    private UInt64 baseAddr;
    private ProcessModule module;

    public UInt64 BaseAddr { get { return baseAddr; } }
    public FileVersionInfo VersionInfo { get { return module.FileVersionInfo;  } }

    private MemoryAllocation mem;
    private Dictionary<string, Patch> patches;

    private bool _isAttached = false;
    public bool IsAttached {
      get { return _isAttached;  }
    }

    public MemoryManager() {
      patches = new Dictionary<string, Patch>();
    }

    public bool Attach(string procName) {

      var procs = Process.GetProcessesByName(procName);
      if (procs.Length <= 0)
        return false;

      var proc = procs.First();
      
      if (proc == null)
        return false;

      foreach(ProcessModule m in proc.Modules) {
        if (m.ModuleName == procName + ".exe") {
          module = m;
          break;
        }
      }
      
      hProcess = OpenProcess(ProcessAccessFlags.All, false, proc.Id);

      UInt64[] hModule = new UInt64[256];
      EnumProcessModules(hProcess, hModule, sizeof(UInt64) * 256, out uint lpcbNeeded);
      baseAddr = hModule[0];

      return _isAttached = true;

    }

    // fuck generics
    public byte ReadByte(UInt64 address) {
      byte[] buf = new byte[1];
      
      ReadProcessMemory(hProcess, address, buf, buf.Length, out UInt64 bRead);

      return buf[0];
    }

    public UInt32 ReadInt32(UInt64 address) {
      byte[] buf = new byte[4];

      ReadProcessMemory(hProcess, address, buf, buf.Length, out UInt64 bRead);

      return BitConverter.ToUInt32(buf, 0);

    }

    public UInt64 ReadInt64(UInt64 address) {
      byte[] buf = new byte[8];

      ReadProcessMemory(hProcess, address, buf, buf.Length, out ulong bRead);

      return BitConverter.ToUInt64(buf, 0);

    }

    public float ReadFloat(UInt64 address) {
      byte[] buf = new byte[4];

      ReadProcessMemory(hProcess, address, buf, buf.Length, out ulong bRead);

      return BitConverter.ToSingle(buf, 0);

    }

    public void WriteByte(UInt64 address, byte b) {
      byte[] buf = new byte[1];
      buf[0] = b;

      WriteProcessMemory(hProcess, address, buf, 1, out ulong bRead);
    }

    public void WriteInt32(UInt64 address, UInt32 b) {
      byte[] buf = BitConverter.GetBytes(b);

      WriteProcessMemory(hProcess, address, buf, (uint)buf.Length, out ulong bRead);
    }

    public void WriteInt64(UInt64 address, UInt64 b) {
      byte[] buf = BitConverter.GetBytes(b);

      WriteProcessMemory(hProcess, address, buf, (uint)buf.Length, out ulong bRead);
    }

    public void WriteFloat(UInt64 address, float f) {
      byte[] buf = BitConverter.GetBytes(f);

      WriteProcessMemory(hProcess, address, buf, (uint)buf.Length, out ulong bRead);
    }

    public byte[] ReadBytes(UInt64 address, long size) {
      byte[] buf = new byte[size];

      ReadProcessMemory(hProcess, address, buf, buf.Length, out ulong bRead);
      if (bRead == (ulong)buf.Length)
        return buf;

      return new byte[1];
    }

    public void WriteBytes(UInt64 address, byte[] buf) {

      WriteProcessMemory(hProcess, address, buf, (uint)buf.Length, out ulong bRead);
    }

    public UInt64? EvalPointerChain(List<UInt64> offs) {

      UInt64 addr = offs.First();
      try {
        for (int i = 1; i < offs.Count; i++) {
          addr = this.ReadInt64(addr);
          addr = addr + offs[i];
        }
      } catch(Exception e) {
        return null;
      }

      return addr;
      
    }

    public UInt64? AobScan(List<uint?> pattern, UInt64 start, int size) {

      byte[] buf = new byte[size];

      ReadProcessMemory(hProcess, start, buf, size, out ulong bRead);

      StringBuilder sb = new StringBuilder();
      for(int xx = 0; xx < pattern.Count; xx++) {
        sb.Append(buf[0x841875 + xx].ToString("X") + " ");
      }
      
      // BMH

      int m = pattern.Count, n = size;

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
          return (UInt64)k;
        else
          k += Math.Max(1, j - skip[buf[k + j]]);
      }

      return null;
    }

    public UInt64 AllocateMemory(UInt64 addr, uint size) {

      var ret = VirtualAllocEx(this.hProcess, addr, size, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ExecuteReadWrite);
      mem = new MemoryAllocation(addr, size);
      return addr;
    }

    public UInt64? Allocate(string name, UInt64 size) {
      UInt64? exist = mem.Get(name);
      if (exist.HasValue) return exist.Value;

      return mem.Alloc(name, size);
    }

    public void AddPatch(string name, PatchCode detour, PatchCode objective) {

      this.patches.Add(name, Patch.Assemble(this, detour, objective));
    }

    public void EnablePatch(string name) {
      if(this.patches.TryGetValue(name, out Patch p)) {
        p.Enable();
      }
    }

    public void DisablePatch(string name) {
      if (this.patches.TryGetValue(name, out Patch p)) {
        p.Disable();
      }
    }

    public static List<uint?> AobPattern(string s) {
      string[] bytes = s.Split(' ');
      List<uint?> pattern = new List<uint?>();
      
      foreach(string b in bytes) {
        if (b == "?" || b == "??" || b == "x" || b == "xx")
          pattern.Add(null);
        else
          pattern.Add(Convert.ToByte(b, 16));
      }

      return pattern;
    }
  }

}
