using MemoryManagement;
using PracticeTool.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeTool.ViewModels {
  public class Addresses {
    private UInt32 XA;
    private UInt64 BaseB;
    private UInt64 BaseD;
    private UInt64 BaseF;
    private UInt64 AddrDebug;
    private UInt64 AddrSpeed;
    private UInt64 AddrGameRend;

    private List<UInt64> PtrNoDamage;
    private List<UInt64> PtrNoDeath;
    private List<UInt64> PtrNoConsume;
    private List<UInt64> PtrNoCollision;
    private List<UInt64> PtrDeathcam;
    private List<UInt64> PtrEventDraw;
    private List<UInt64> PtrEventDisable;
    private List<UInt64> PtrAIDisable;
    private List<UInt64> PtrOneShot;
    private List<UInt64> PtrHideMap;
    private List<UInt64> PtrHideChar;
    private List<UInt64> PtrHideObj;

    private List<UInt64> PtrPosX;
    private List<UInt64> PtrPosY;
    private List<UInt64> PtrPosZ;
    private List<UInt64> PtrSpeed;
    private List<UInt64> PtrHP;
    private List<UInt64> PtrFP;
    private List<UInt64> PtrSP;

    private byte bNoDamage;
    private byte bNoDeath;
    private byte bNoStaminaCons;
    private byte bNoFpCons;
    private byte bNoGoodsCons;
    private byte bNoCollision;

    public Flag NoDamage { get; private set; }
    public Flag NoDeath { get; private set; }
    public Flag NoStaminaCons { get; private set; }
    public Flag NoFpCons { get; private set; }
    public Flag NoCollision { get; private set; }
    public Flag NoGoodsCons { get; private set; }
    public Flag Deathcam { get; private set; }
    public Flag EventDraw { get; private set; }
    public Flag EventDisable { get; private set; }
    public Flag AIDisable { get; private set; }
    public Flag OneShot { get; private set; }
    public Flag HideMap { get; private set; }
    public Flag HideChar { get; private set; }
    public Flag HideObj { get; private set; }
    public FloatField PosX { get; private set; }
    public FloatField PosY { get; private set; }
    public FloatField PosZ { get; private set; }
    public FloatField Speed { get; private set; }
    public DWordField HP { get; private set; }
    public DWordField FP { get; private set; }
    public DWordField SP { get; private set; }

    private MemoryManager mm;
    private Dictionary<IMemoryVariable, PropertyChangedEventHandler> changeListeners;
    private Dictionary<IMemoryVariable, MemoryReader> readers;

    public Addresses(MemoryManager mm) {
      this.mm = mm;

      NoDamage = new Flag("No damage");
      NoDeath = new Flag("No death");
      NoStaminaCons = new Flag("Infinite stamina");
      NoFpCons = new Flag("Infinite FP");
      NoGoodsCons = new Flag("No consumption");
      Deathcam = new Flag("Deathcam");
      EventDraw = new Flag("Draw events");
      EventDisable = new Flag("Disable events");
      AIDisable = new Flag("Disable AI");
      OneShot = new Flag("One shot");
      HideChar = new Flag("Hide character");
      HideMap = new Flag("Hide map");
      HideObj = new Flag("Hide objects");
      NoCollision = new Flag("Noclip");
      PosX = new FloatField("X");
      PosY = new FloatField("Y");
      PosZ = new FloatField("Z");
      Speed = new FloatField("Speed");
      HP = new DWordField("HP");
      FP = new DWordField("FP");
      SP = new DWordField("SP");

      HP.ReadMemory = true;
      FP.ReadMemory = true;
      SP.ReadMemory = true;
      PosX.ReadMemory = true;
      PosY.ReadMemory = true;
      PosZ.ReadMemory = true;

      changeListeners = new Dictionary<IMemoryVariable, PropertyChangedEventHandler>();
      readers = new Dictionary<IMemoryVariable, MemoryReader>();
    }

    public void Enforce() {
      PropertyChangedEventArgs e = new PropertyChangedEventArgs("Value");

      foreach (var i in readers) {
        if (i.Key.ReadMemory && !i.Key.ForceValue) {
          i.Value.Invoke();
        }
      }

      foreach (var i in changeListeners) {
        if (i.Key.ForceValue) {
          i.Value.Invoke(i.Key, e);
        }
      }
    }

    private void SetupFlags() {
      changeListeners.Add(NoDamage, this.MakeBitHandler(PtrNoDamage, bNoDamage));
      changeListeners.Add(NoDeath, this.MakeBitHandler(PtrNoDeath, bNoDeath));
      changeListeners.Add(NoStaminaCons, this.MakeBitHandler(PtrNoDeath, bNoStaminaCons));
      changeListeners.Add(NoFpCons, this.MakeBitHandler(PtrNoDeath, bNoFpCons));
      changeListeners.Add(NoGoodsCons, this.MakeBitHandler(PtrNoConsume, bNoGoodsCons));
      changeListeners.Add(Deathcam, this.MakeByteHandler(PtrDeathcam));
      changeListeners.Add(EventDraw, this.MakeByteHandler(PtrEventDraw));
      changeListeners.Add(EventDisable, this.MakeByteHandler(PtrEventDisable));
      changeListeners.Add(AIDisable, this.MakeByteHandler(PtrAIDisable));
      changeListeners.Add(OneShot, this.MakeByteHandler(PtrOneShot));
      changeListeners.Add(HideChar, this.MakeByteHandler(PtrHideChar, true));
      changeListeners.Add(HideMap, this.MakeByteHandler(PtrHideMap, true));
      changeListeners.Add(HideObj, this.MakeByteHandler(PtrHideObj, true));
      changeListeners.Add(NoCollision, this.MakeBitHandler(PtrNoCollision, bNoCollision, true));
      changeListeners.Add(Speed, this.MakeFloatHandler(PtrSpeed));
      changeListeners.Add(PosX, this.MakeFloatHandler(PtrPosX));
      changeListeners.Add(PosY, this.MakeFloatHandler(PtrPosY));
      changeListeners.Add(PosZ, this.MakeFloatHandler(PtrPosZ));
      changeListeners.Add(HP, this.MakeDWordHandler(PtrHP));
      changeListeners.Add(FP, this.MakeDWordHandler(PtrFP));
      changeListeners.Add(SP, this.MakeDWordHandler(PtrSP));

      readers.Add(HP, MakeDWordReader(PtrHP, HP));
      readers.Add(FP, MakeDWordReader(PtrFP, FP));
      readers.Add(SP, MakeDWordReader(PtrSP, SP));
      readers.Add(PosX, MakeFloatReader(PtrPosX, PosX));
      readers.Add(PosY, MakeFloatReader(PtrPosY, PosY));
      readers.Add(PosZ, MakeFloatReader(PtrPosZ, PosZ));

      foreach (var i in changeListeners) {
        i.Key.PropertyChanged += i.Value;
      }
    }

    #region Write event handlers
    private PropertyChangedEventHandler MakeBitHandler(List<UInt64> pchain, byte bmask, bool reverse = false) {
      return (sender, e) => {
        if (e.PropertyName != "Value") return;
        Flag f = (Flag)sender;

        bool wasReading = f.ReadMemory;
        f.ReadMemory = false;
        UInt64? addr = mm.EvalPointerChain(pchain);
        if (addr.HasValue) {
          byte b = mm.ReadByte(addr.Value);
          bool vcheck = reverse ? (!f.FlagValue) : f.FlagValue;
          b = (byte)(vcheck ? (b | bmask) : (b & ~bmask));
          mm.WriteByte(addr.Value, b);
        }
        f.ReadMemory = wasReading;
      };
    }

    private PropertyChangedEventHandler MakeByteHandler(List<UInt64> pchain, bool reverse = false) {
      return (sender, e) => {
        if (e.PropertyName != "Value") return;
        Flag f = (Flag)sender;

        bool wasReading = f.ReadMemory;
        f.ReadMemory = false;
        UInt64? addr = mm.EvalPointerChain(pchain);
        if (addr.HasValue) {
          bool vcheck = reverse ? (!f.FlagValue) : f.FlagValue;
          byte b = (byte)(vcheck ? 1 : 0);
          mm.WriteByte(addr.Value, b);
        }
        f.ReadMemory = wasReading;
      };
    }

    private PropertyChangedEventHandler MakeFloatHandler(List<UInt64> pchain) {
      return (sender, e) => {
        if (e.PropertyName != "Value") return;
        FloatField f = (FloatField)sender;

        bool wasReading = f.ReadMemory;
        f.ReadMemory = false;
        UInt64? addr = mm.EvalPointerChain(pchain);
        if (addr.HasValue) {
          mm.WriteFloat(addr.Value, f.Value);
        }
        f.ReadMemory = wasReading;
      };
    }

    private PropertyChangedEventHandler MakeDWordHandler(List<UInt64> pchain) {
      return (sender, e) => {
        if (e.PropertyName != "Value") return;

        DWordField f = (DWordField)sender;

        bool wasReading = f.ReadMemory;
        f.ReadMemory = false;
        UInt64? addr = mm.EvalPointerChain(pchain);
        if (addr.HasValue) {
          mm.WriteInt32(addr.Value, f.Value);
        }
        f.ReadMemory = wasReading;
      };
    }
    #endregion

    #region Memory readers
    private delegate void MemoryReader();

    private MemoryReader MakeFloatReader(List<UInt64> pchain, FloatField field) {
      return () => {
        UInt64? addr = mm.EvalPointerChain(pchain);
        if (addr.HasValue) {
          Single ret = mm.ReadFloat(addr.Value);
          field.Value = ret;
        }
      };
    }

    private MemoryReader MakeDWordReader(List<UInt64> pchain, DWordField field) {
      return () => {
        UInt64? addr = mm.EvalPointerChain(pchain);
        if (addr.HasValue) {
          UInt32 ret = mm.ReadInt32(addr.Value);
          field.Value = ret;
        }
      };
    }
    #endregion

    #region Common addresses
    private static Addresses CommonAddresses(MemoryManager mm) {
      Addresses a = new Addresses(mm);

      var AobXA = MemoryManager.AobPattern("48 8B 83 ?? ?? ?? ?? 48 8B 10 48 85 D2");
      var AobBaseB = MemoryManager.AobPattern("48 8B 1D ?? ?? ?? 04 48 8B F9 48 85 DB ?? ?? 8B 11 85 D2 ?? ?? 8D");
      var AobBaseD = MemoryManager.AobPattern("48 8B 0D ?? ?? ?? ?? 48 85 C9 74 26 44");
      var AobBaseF = MemoryManager.AobPattern("48 8B 05 ?? ?? ?? ?? 41 0F B6");
      var AobDebug = MemoryManager.AobPattern("4C 8D 05 ?? ?? ?? ?? 48 8D 15 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 83 3D ?? ?? ?? ?? 00");
      var AobSpeed = MemoryManager.AobPattern("88 00 00 00 ?? ?? ?? ?? C7 86 A0 00 00 00 00 00 80 BF 33");
      var AobGameRend = MemoryManager.AobPattern("80 3D ?? ?? ?? ?? ?? 48 8B 15 ?? ?? ?? ?? 0F");

      UInt64? offsXA = mm.AobScan(AobXA, mm.BaseAddr, 0x5ffffff);
      UInt64? offsBaseB = mm.AobScan(AobBaseB, mm.BaseAddr, 0x5ffffff);
      UInt64? offsBaseD = mm.AobScan(AobBaseD, mm.BaseAddr, 0x5ffffff);
      UInt64? offsBaseF = mm.AobScan(AobBaseF, mm.BaseAddr, 0x5ffffff);
      UInt64? offsDebug = mm.AobScan(AobDebug, mm.BaseAddr, 0x5ffffff);
      UInt64? offsSpeed = mm.AobScan(AobSpeed, mm.BaseAddr, 0x5ffffff);
      UInt64? offsGameRend = mm.AobScan(AobGameRend, mm.BaseAddr, 0x5ffffff);

      a.BaseB = mm.BaseAddr + offsBaseB.Value;
      a.BaseB = a.BaseB + mm.ReadInt32(a.BaseB + 3) + 7;

      a.BaseD = mm.BaseAddr + offsBaseD.Value;
      a.BaseD = a.BaseD + mm.ReadInt32(a.BaseD + 3) + 7;

      a.BaseF = mm.BaseAddr + offsBaseF.Value;
      a.BaseF = a.BaseF + mm.ReadInt32(a.BaseF + 3) + 7;

      a.AddrDebug = mm.BaseAddr + offsDebug.Value;
      a.AddrDebug = a.AddrDebug + mm.ReadInt32(a.AddrDebug + 3) + 7;

      a.AddrSpeed = mm.BaseAddr + offsSpeed.Value + 4;

      a.AddrGameRend = mm.BaseAddr + offsGameRend.Value;
      a.AddrGameRend = a.AddrGameRend + mm.ReadInt32(a.AddrGameRend + 2) + 7;

      a.XA = mm.ReadInt32(mm.BaseAddr + offsXA.Value + 3);

      /**
       * Pointer chains
       */

      a.PtrNoDamage = new List<UInt64> { a.BaseB, 0x80, 0x1a09 }; // bit 7
      a.bNoDamage = 1 << 7;

      // bit 2: no death; bit 4: no stamina cons; bit 5: no fp cons
      a.PtrNoDeath = new List<UInt64> { a.BaseB, 0x80, a.XA, 0x18, 0x1C0 };
      a.bNoDeath = 1 << 2;
      a.bNoStaminaCons = 1 << 4;
      a.bNoFpCons = 1 << 5;

      a.PtrEventDraw = new List<UInt64> { a.BaseF, 0xA8 }; // byte
      a.PtrEventDisable = new List<UInt64> { a.BaseF, 0xD4 }; // byte
      a.PtrAIDisable = new List<UInt64> { a.AddrDebug + 9 + 3 }; // byte

      a.PtrPosX = new List<UInt64> { a.BaseB, 0x40, 0x28, 0x80 }; // float
      a.PtrPosY = new List<UInt64> { a.BaseB, 0x40, 0x28, 0x88 }; // float
      a.PtrPosZ = new List<UInt64> { a.BaseB, 0x40, 0x28, 0x84 }; // float

      a.PtrHideMap = new List<UInt64> { a.AddrGameRend + 0 }; // byte
      a.PtrHideObj = new List<UInt64> { a.AddrGameRend + 1 }; // byte
      a.PtrHideChar = new List<UInt64> { a.AddrGameRend + 2 }; // byte

      a.PtrNoCollision = new List<UInt64> { a.BaseD, 0x60, 0x48 }; // bit 0
      a.bNoCollision = 1 << 0;

      a.PtrSpeed = new List<UInt64> { a.AddrSpeed };

      a.PtrHP = new List<UInt64> { a.BaseB, 0x80, a.XA, 0x18, 0xD8 };
      a.PtrFP = new List<UInt64> { a.BaseB, 0x80, a.XA, 0x18, 0xE4 };
      a.PtrSP = new List<UInt64> { a.BaseB, 0x80, a.XA, 0x18, 0xF0 };

      return a;
    }
    #endregion

    #region Addresses for 1.08
    public static Addresses Addresses08(MemoryManager mm) {
      Addresses a = CommonAddresses(mm);

      // sbagliato
      a.PtrNoConsume = new List<UInt64> { a.BaseB, 0x80, 0x1EDA }; // bit 3
      a.bNoGoodsCons = 1 << 3;

      a.PtrDeathcam = new List<UInt64> { a.BaseB, 0x88 }; // byte, 0x90 per 1.12
      a.PtrOneShot = new List<UInt64> { a.AddrDebug + 1 }; // byte

      a.SetupFlags();

      return a;
    }
    #endregion

    #region Addresses for 1.12
    public static Addresses Addresses12(MemoryManager mm) {
      Addresses a = CommonAddresses(mm);


      a.PtrNoConsume = new List<UInt64> { a.BaseB, 0x80, 0x1EEA }; // bit 3
      a.bNoGoodsCons = 1 << 3;

      a.PtrDeathcam = new List<UInt64> { a.BaseB, 0x90 }; // byte
      a.PtrOneShot = new List<UInt64> { a.AddrDebug + 0 }; // byte

      a.SetupFlags();

      return a;
    }
    #endregion

  }
}
