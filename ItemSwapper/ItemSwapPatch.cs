using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryManagement;
using System.Threading;

namespace ItemSwapper {
  class ItemSwapPatch {

    private MemoryManager mm;
    private UInt64? pickupPtr;

    public ItemSwapPatch() {
      mm = new MemoryManager();
    }

    public void Enable() {

      while (!mm.Attach("DarkSoulsIII")) {
        Thread.Sleep(TimeSpan.FromSeconds(1));
      }

      mm.AllocateMemory(mm.BaseAddr - 0x10000, 0x1000);

      pickupPtr = mm.Allocate("pickup_ptr", 8);
      UInt64? itemSwapBase = mm.Allocate("item_swap_base", 64);
      UInt64? itemSwapAob = mm.AobScan(new List<uint?> { 0x8B, 0x4B, 0x20, 0x41, 0x89, 0x0E }, mm.BaseAddr, 0x5ffffff) + mm.BaseAddr;

      if (pickupPtr.HasValue && itemSwapBase.HasValue && itemSwapAob.HasValue) {

        PatchCode itemSwapDetour = new PatchCode(
          new byte?[] { 0xE9, null, null, null, null, 0x90 },
          itemSwapAob.Value,
          new UInt64[] { itemSwapBase.Value - itemSwapAob.Value - 5 }
        );

        PatchCode itemSwapObjective = new PatchCode(
          new byte?[] { 0x48, 0xB9, null, null, null, null, null, null, null, null, 0x48, 0x89, 0x19, 0x8B, 0x4B, 0x20, 0x41, 0x89, 0x0E, 0xE9, null, null, null, null },
          itemSwapBase.Value,
          new UInt64[] {
            pickupPtr.Value,
            itemSwapAob.Value - itemSwapBase.Value - 17
          }
        );

        mm.AddPatch("ItemSwap", itemSwapDetour, itemSwapObjective);

        mm.EnablePatch("ItemSwap");
      }
    }

    public UInt32? ReadValue() {
      UInt64? p = mm.EvalPointerChain(new List<UInt64> { pickupPtr.Value, 0x58 });

      if (p.HasValue) {
        return mm.ReadInt32(p.Value);
      }

      return null;
    }

    public void WriteValue(UInt32 i) {
      UInt64? p = mm.EvalPointerChain(new List<UInt64> { pickupPtr.Value, 0x58 });

      if (p.HasValue) {
        mm.WriteInt32(p.Value, i);
      }
    }

    public void Disable() {
        mm.DisablePatch("ItemSwap");
    }
  }
}
