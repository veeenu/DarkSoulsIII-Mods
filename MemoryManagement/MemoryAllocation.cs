using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryManagement {
  class MemoryAllocation {

    private UInt64 address;
    private UInt64 size;
    private UInt64 used;
    private Dictionary<string, UInt64> allocs;

    public MemoryAllocation(UInt64 address, UInt64 size) {
      this.address = address;
      this.size = size;
      this.used = 0;
      this.allocs = new Dictionary<string, UInt64>();
    }

    public UInt64? Get(string addr) {
      if(allocs.TryGetValue(addr, out UInt64 l)) {
        return address + l;
      }
      return null;
    }

    public UInt64? Alloc(string addr, UInt64 asize) {

      if(used + asize > size || allocs.ContainsKey(addr)) {
        return null;
      }

      allocs.Add(addr, used);
      used += asize;

      return Get(addr);
    }
  }
}
