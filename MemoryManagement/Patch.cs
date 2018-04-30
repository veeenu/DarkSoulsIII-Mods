using KeystoneNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MemoryManagement {

  public class PatchCode {
    private byte[] opcodes;
    private UInt64 addr;

    public UInt64 Addr { get => addr; }
    public byte[] Code { get => opcodes; }
    public int Length { get => opcodes == null ? 0 : opcodes.Length; }

    public PatchCode(byte?[] code, UInt64 addr, UInt64[] addresses) {

      this.addr = addr;
      this.opcodes = new byte[code.Length];
      Console.WriteLine("PtachCode");

      int curAddr = 0;
      for(int i = 0; i < code.Length; i++) {
        if(!code[i].HasValue) {
          int j = 0;
          while (i + j < code.Length && !code[i + j].HasValue) j++;
          Console.WriteLine();
          Console.WriteLine(curAddr);
          UInt64 subst = addresses[curAddr++];

          while (j-- >= 0) {
            code[i + j] = (byte)((subst >> (j * 8)) & 0xFF);
          }
        }

        Console.Write(code[i].Value.ToString("X2") + " ");
        this.opcodes[i] = code[i].Value;
      }
      Console.WriteLine();

    }

  }

  public class Patch {

    private PatchCode detour;
    private PatchCode objective;
    private byte[] detourBackup;
    private byte[] objectiveBackup;
    private MemoryManager mm;

    private bool _enabled;
    public bool Enabled { 
      get { return _enabled; }
    }

    public Patch(MemoryManager mm, PatchCode detour, PatchCode objective) {
      _enabled = false;
      this.detour = detour;
      this.objective = objective;
      this.mm = mm;
    }

    public void Enable() {
      if(!_enabled) {
        detourBackup = mm.ReadBytes(detour.Addr, detour.Length);
        objectiveBackup = mm.ReadBytes(objective.Addr, objective.Length);
        mm.WriteBytes(objective.Addr, objective.Code);
        mm.WriteBytes(detour.Addr, detour.Code);
      }

      _enabled = true;
    }

    public void Disable() {
      if (!_enabled) return;

      mm.WriteBytes(detour.Addr, detourBackup);
      mm.WriteBytes(objective.Addr, objectiveBackup);

      _enabled = false;
    }

    public static Patch Assemble(MemoryManager mm, PatchCode detour, PatchCode objective) {
      return new Patch(mm, detour, objective);
    }

  }
}
