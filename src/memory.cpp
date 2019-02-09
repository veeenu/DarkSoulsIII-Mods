#include <tuple>
#include "memory.h"

// TODO consistent snake_case pls
namespace DS3PracticeTools {

  bool lower_equals(const std::string& a, const std::string& b) {
    auto it_a = a.begin();
    auto it_b = b.begin();
    while (it_a != a.end() && it_b != b.end()) {
      if (tolower(*it_a) != tolower(*it_b)) return false;
      ++it_a; ++it_b;
    }
    return true;
  }

  std::tuple<DWORD, HMODULE, MODULEINFO> find_process(const std::string& name) {
    DWORD ret_pid = -1;
    HMODULE ret_base = 0;
    MODULEINFO ret_info;

    DWORD lpidProcess[256];
    unsigned long cbNeeded, count;
    HMODULE hModule[64];
    char modname[30];

    EnumProcesses(lpidProcess, sizeof(lpidProcess), &cbNeeded);
    int nReturned = cbNeeded / sizeof(cbNeeded);

    for (int i = 0; i < nReturned; i++) {
      auto pid = lpidProcess[i];
      auto hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
      if(!hProc) continue;

      EnumProcessModules(hProc, hModule, sizeof(hModule), &count);
      GetModuleBaseNameA(hProc, hModule[0], modname, sizeof(modname));

      std::string procname(modname);
      if (lower_equals(procname, name)) {
        GetModuleInformation(hProc, hModule[0], &ret_info, sizeof(ret_info));
        ret_pid = pid;
        ret_base = hModule[0];
      }

      for (int j = 0; j < 30; j++) modname[j] = 0;
      CloseHandle(hProc);
    }

    return std::make_tuple(ret_pid, ret_base, ret_info);
  }

  uint64_t eval_pointer_chain (HANDLE proc, std::vector<uint64_t> chain) {
    auto addr = chain[0];
    try {
      for (auto it = ++chain.begin(); it != chain.end(); ++it) {
        addr = read<uint64_t>(proc, addr);
        addr = addr + *it;
      }
    } catch (memory_exception e) {
      throw memory_exception("Address can't be used right now");
    }
    return addr;
  }

  ////////////////////////////////////////////////////////////////////////////

  void Process::attach (DWORD pid_, HMODULE base_, MODULEINFO info_) {
    if (ph != nullptr) {
      CloseHandle(ph);
    }
    pid = pid_;
    base = (uint64_t)base_;
    info = info_;
    ph = OpenProcess(PROCESS_ALL_ACCESS, false, pid);

    if (!ph) {
      throw memory_exception("OpenProcess");
    }

    char filename[256];
    DWORD dummy;
    GetModuleFileNameExA(ph, base_, filename, sizeof(filename));
    auto fvi_size = GetFileVersionInfoSizeA(filename , &dummy);
    printf("fvi size %d\n", fvi_size);

    LPBYTE version_info = new BYTE[fvi_size];
    GetFileVersionInfoA(filename, 0, fvi_size, version_info);
    printf("%s\n", filename);

    VS_FIXEDFILEINFO *ffi;
    UINT len;
    VerQueryValueA(version_info, "\\", (LPVOID*)&ffi, &len);

    version = (uint8_t)LOWORD(ffi->dwFileVersionMS);
    delete [] version_info;

    compute_pointers_108();
  }

  void Process::assert_attached () const {
    if (!is_attached()) {
      throw memory_exception("Process is not attached!");
    }
  }

  bool Process::is_attached () const {
    return ph != NULL;
  }

  std::string Process::get_game_version () const {
    return tfm::format("1.%02d", version);
  }

  void Process::compute_pointers_108 () {
    xa         = read<uint32_t>(ph, (uint64_t)base + 0x83BA91 + 3);
    base_b     = base + 0x4C0DDA;
    base_b     = base_b + read<uint32_t>(ph, base_b + 3) + 7;
    base_d     = base + 0x4C6580;
    base_d     = base_d + read<uint32_t>(ph, base_d + 3) + 7;
    base_f     = base + 0x4CA44D;
    base_f     = base_f + read<uint32_t>(ph, base_f + 3) + 7;
    addr_debug = base + 0x8D06F8;
    addr_debug = addr_debug + read<uint32_t>(ph, addr_debug + 3) + 7;
    addr_grend = base + 0x6287AB;
    addr_grend = addr_grend + read<uint32_t>(ph, addr_grend + 2) + 7;
    addr_speed = base + 0x1096F2C + 4;

    ptr_no_damage       = { base_b, 0x80, 0x1A09 };          // 1 << 7
    bit_no_damage       = 1 << 7;
    ptr_no_death        = { base_b, 0x80, xa, 0x18, 0x1C0 }; // 1 << 2
    bit_no_death        = 1 << 2;
    ptr_deathcam        = { base_b, 0x88 };                  // byte
    ptr_inf_stamina     = { base_b, 0x80, xa, 0x18, 0x1C0 }; // 1 << 4
    bit_inf_stamina     = 1 << 4;
    ptr_inf_focus       = { base_b, 0x80, xa, 0x18, 0x1C0 }; // 1 << 5
    bit_inf_focus       = 1 << 5;
    ptr_inf_consum      = { base_b, 0x80, 0x1EDA };          // 1 << 3 WRONG
    bit_inf_consum      = 1 << 3;
    ptr_one_shot        = { addr_debug + 1 };                // byte
    ptr_event_draw      = { base_f, 0xA8 };                  // byte
    ptr_event_disable   = { base_f, 0xD4 };                  // byte
    ptr_ai_disable      = { addr_debug + 9 + 3 };            // byte
    ptr_no_gravity      = { base_d, 0x60, 0x48 };            // 1 << 0
    bit_no_gravity      = 1 << 0;
    ptr_hide_character  = { addr_grend + 2 };                // byte
    ptr_hide_map        = { addr_grend + 0 };                // byte
    ptr_hide_objects    = { addr_grend + 1 };                // byte
    ptr_pos_x           = { base_b, 0x40, 0x28, 0x80 };      // float32
    ptr_pos_y           = { base_b, 0x40, 0x28, 0x88 };      // float32
    ptr_pos_z           = { base_b, 0x40, 0x28, 0x84 };      // float32
    ptr_speed           = { addr_speed };                    // float32
    ptr_instant_quitout = { base + 0x47103D8, 0x250 };       // byte
  }

  void Process::set_no_damage (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_no_damage);
    auto r = read<uint8_t>(ph, addr);
    write<uint8_t>(ph, addr, b ? (r | bit_no_damage) : (r & ~bit_no_damage));
  }

  void Process::set_no_death (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_no_death);
    auto r = read<uint8_t>(ph, addr);
    write<uint8_t>(ph, addr, b ? (r | bit_no_death) : (r & ~bit_no_death));
  }

  void Process::set_deathcam (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_deathcam);
    write<uint8_t>(ph, addr, b ? 1 : 0);
  }

  void Process::set_inf_stamina (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_inf_stamina);
    auto r = read<uint8_t>(ph, addr);
    write<uint8_t>(ph, addr, b ? (r | bit_inf_stamina) : (r & ~bit_inf_stamina));
  }

  void Process::set_inf_focus (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_inf_focus);
    auto r = read<uint8_t>(ph, addr);
    write<uint8_t>(ph, addr, b ? (r | bit_inf_focus) : (r & ~bit_inf_focus));
  }

  void Process::set_inf_consum (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_inf_consum);
    auto r = read<uint8_t>(ph, addr);
    write<uint8_t>(ph, addr, b ? (r | bit_inf_consum) : (r & ~bit_inf_consum));
  }

  void Process::set_one_shot (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_one_shot);
    write<uint8_t>(ph, addr, b ? 1 : 0);
  }

  void Process::set_event_draw (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_event_draw);
    write<uint8_t>(ph, addr, b ? 1 : 0);
  }

  void Process::set_event_disable (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_event_disable);
    write<uint8_t>(ph, addr, b ? 1 : 0);
  }

  void Process::set_ai_disable (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_ai_disable);
    write<uint8_t>(ph, addr, b ? 1 : 0);
  }

  void Process::set_no_gravity (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_no_gravity);
    auto r = read<uint8_t>(ph, addr);
    write<uint8_t>(ph, addr, !b ? (r | bit_no_gravity) : (r & ~bit_no_gravity));
  }

  void Process::set_hide_character (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_hide_character);
    write<uint8_t>(ph, addr, b ? 0 : 1);
  }

  void Process::set_hide_map (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_hide_map);
    write<uint8_t>(ph, addr, b ? 0 : 1);
  }

  void Process::set_hide_objects (bool b) {
    auto addr = eval_pointer_chain(ph, ptr_hide_objects);
    write<uint8_t>(ph, addr, b ? 0 : 1);
  }

  void Process::set_position (float x, float y , float z) {
    auto addr_x = eval_pointer_chain(ph, ptr_pos_x);
    auto addr_y = eval_pointer_chain(ph, ptr_pos_y);
    auto addr_z = eval_pointer_chain(ph, ptr_pos_z);
    write<float>(ph, addr_x, x);
    write<float>(ph, addr_y, y);
    write<float>(ph, addr_z, z);
  }

  std::tuple<float, float, float> Process::get_position () {
    auto addr_x = eval_pointer_chain(ph, ptr_pos_x);
    auto addr_y = eval_pointer_chain(ph, ptr_pos_y);
    auto addr_z = eval_pointer_chain(ph, ptr_pos_z);
    float x = read<float>(ph, addr_x);
    float y = read<float>(ph, addr_y);
    float z = read<float>(ph, addr_z);
    return std::make_tuple(x, y, z);
  }

  void Process::set_speed (float s) { 
    auto addr = eval_pointer_chain(ph, ptr_speed);
    write<float>(ph, addr, s);
  }
  
  void Process::instant_quitout () {
    auto addr = eval_pointer_chain(ph, ptr_instant_quitout);
    write<uint8_t>(ph, addr, 1);
  }
  
};