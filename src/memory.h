#pragma once

#include <windows.h>
#include <Psapi.h>
#include <algorithm>
#include <vector>
#include <string>
#include <cstdint>
#include <tinyformat.h>

namespace DS3PracticeTools {

  class Process;
  typedef void (Process::*ProcessFlagSetter) (bool);

  std::tuple<DWORD, HMODULE, MODULEINFO> find_process (const std::string& name);

  class memory_exception : public std::domain_error {
    using std::domain_error::domain_error;
  };

  template<typename T> T read (HANDLE proc, uint64_t addr) {
    T dest;
    uint64_t bytes_read;
    auto ret = ReadProcessMemory(proc, (void*)addr, &dest, sizeof(T), &bytes_read);
    if (ret == 0) {
      throw memory_exception(tfm::format("ReadProcessMemory: %x", GetLastError()));
    }
    return dest;
  }

  template<typename T> void write (HANDLE proc, uint64_t addr, T data) {
    uint64_t bytes_written;
    auto ret = WriteProcessMemory (proc, (void*)addr, &data, sizeof(T), &bytes_written);
    if (ret == 0) {
      throw memory_exception(tfm::format("ReadProcessMemory: %x", GetLastError()));
    }
  }

  uint64_t eval_pointer_chain (HANDLE proc, std::vector<uint64_t> chain);

  class Process {
    private:
      DWORD pid;
      uint64_t base;
      MODULEINFO info;
      HANDLE ph;
      uint8_t version;

      uint64_t base_b;
      uint64_t base_d;
      uint64_t base_f;
      uint64_t addr_debug;
      uint64_t addr_speed;
      uint64_t addr_grend;
      uint32_t xa;

      std::vector<uint64_t> ptr_no_damage;
      std::vector<uint64_t> ptr_no_death;
      std::vector<uint64_t> ptr_deathcam;
      std::vector<uint64_t> ptr_inf_stamina;
      std::vector<uint64_t> ptr_inf_focus;
      std::vector<uint64_t> ptr_inf_consum;
      std::vector<uint64_t> ptr_one_shot;
      std::vector<uint64_t> ptr_event_draw;
      std::vector<uint64_t> ptr_event_disable;
      std::vector<uint64_t> ptr_ai_disable;
      std::vector<uint64_t> ptr_no_gravity;
      std::vector<uint64_t> ptr_hide_character;
      std::vector<uint64_t> ptr_hide_map;
      std::vector<uint64_t> ptr_hide_objects;
      std::vector<uint64_t> ptr_pos_x;
      std::vector<uint64_t> ptr_pos_y;
      std::vector<uint64_t> ptr_pos_z;
      std::vector<uint64_t> ptr_speed;
      std::vector<uint64_t> ptr_instant_quitout;

      uint8_t bit_no_damage;
      uint8_t bit_no_death;
      uint8_t bit_inf_stamina;
      uint8_t bit_inf_focus; 
      uint8_t bit_inf_consum; 
      uint8_t bit_no_gravity;

      void compute_pointers_108 ();
    public:
      Process () = default;
      void attach (DWORD, HMODULE, MODULEINFO);
      void assert_attached () const;
      bool is_attached () const;
      std::string get_game_version () const;

      void set_no_damage (bool);
      void set_no_death (bool);
      void set_deathcam (bool);
      void set_inf_stamina (bool);
      void set_inf_focus (bool);
      void set_inf_consum (bool);
      void set_one_shot (bool);
      void set_event_draw (bool);
      void set_event_disable (bool);
      void set_ai_disable (bool);
      void set_no_gravity (bool);
      void set_hide_character (bool);
      void set_hide_map (bool);
      void set_hide_objects (bool);
      void set_position (float, float, float);
      std::tuple<float, float, float> get_position ();
      void set_speed (float);
      void instant_quitout ();
  };

};
