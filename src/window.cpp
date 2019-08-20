#include <tinyformat.h>
#include <cpptoml.h>
#include <unordered_map>
#include "window.h"
#include "common.h"

namespace DS3PracticeTools {

  Window::Window(QWidget *parent) 
    : QWidget(parent), layout(), flags() {
    setFixedSize(400, 480);

    te = new QTextEdit(this);
    te->setEnabled(false);
    te->setGeometry(0, 11*24, 400, 480-11*24);

    std::vector<std::pair<std::string, const ProcessFlagSetter>> labels = {
      { "No Damage",              &Process::set_no_damage },
      { "Event Draw",             &Process::set_event_draw },
      { "No Death",               &Process::set_no_death },
      { "Event Disable",          &Process::set_event_disable },
      { "Deathcam",               &Process::set_deathcam },
      { "AI Disable",             &Process::set_ai_disable },
      { "Infinite Stamina",       &Process::set_inf_stamina },
      { "No Gravity",             &Process::set_no_gravity },
      { "Infinite Focus",         &Process::set_inf_focus },
      { "Hide Character",         &Process::set_hide_character },
      { "Infinite Consumables",   &Process::set_inf_consum },
      { "Hide Map",               &Process::set_hide_map },
      { "One Shot",               &Process::set_one_shot },
      { "Hide Objects",           &Process::set_hide_objects }
    }; 

    printf("build flags\n");
    for (int i = 0; i < labels.size(); i++) {
      int col = i % 2, row = i / 2;
      flags.push_back(new Flag(this, labels[i].first));
      auto f = flags.back();
      f->setGeometry(col * 200, row * 24, 200, 24);
      auto fst = labels[i].first;
      auto snd = labels[i].second;
      connect(f, &Flag::flag_on, [this, fst, snd] () {
        try {
          p.assert_attached();
          std::invoke(snd, p, true);
        } catch (memory_exception e) {
          te->append(tfm::format("%s: %s", fst, e.what()).c_str());
        }
      });
      connect(f, &Flag::flag_off, [this, fst, snd] () {
        try {
          p.assert_attached();
          std::invoke(snd, p, false);
        } catch (memory_exception e) {
          te->append(tfm::format("%s: %s", fst, e.what()).c_str());
        }
      });
    }

    printf("build position\n");
    position = new Position(this);
    position->setGeometry(200, 7*24, 200, 48);

    connect(position, &Position::load, [this] (double x, double y, double z) {
      te->append(tfm::format("Position loaded %.2f %.2f %.2f", x, y, z).c_str());
      int attempts = 30;
      while (position->get_lock() && attempts-- > 0);
      if (position->get_lock()) {
        te->append("Couldn't load position");
        return;
      }
      try {
        position->set_lock(true);
        p.set_position(x, y, z);
        position->set_lock(false);
      } catch (memory_exception e) {
        te->append(tfm::format("Loading position: %s", e.what()).c_str());
      }
    });

    connect(position, &Position::save, [this] (double x, double y, double z) {
      te->append(tfm::format("Position saved %.2f %.2f %.2f", x, y, z).c_str());
    });

    printf("build speed\n");
    speed = new Speed(this);
    speed->setGeometry(0, 7*24, 200, 48);

    connect(speed, &Speed::set_speed, [this] (double d) {
      te->append(tfm::format("Speed set to %.2f", d).c_str());
        try {
          p.assert_attached();
          p.set_speed(d);
        } catch (memory_exception e) {
          te->append(tfm::format("Set speed: %s", e.what()).c_str());
        }
    });

    printf("build attach & insta\n");
    attach_btn = new QPushButton("Open process", this);
    attach_btn->setGeometry(0, 9*24, 200, 48);

    instant_quitout_btn = new QPushButton("Instant quitout", this);
    instant_quitout_btn->setGeometry(200, 9*24, 200, 48);

    connect(attach_btn, &QPushButton::clicked, [this] () {

      te->append("Attaching to process...");
      auto params = find_process("DarkSoulsIII.exe");
      p.attach(std::get<0>(params), std::get<1>(params), std::get<2>(params));
      if(p.is_attached())
        te->append(tfm::format("Dark Souls III ver%s attached", p.get_game_version()).c_str());
      else
        te->append("Failed. :(");
    });

    connect(instant_quitout_btn, &QPushButton::clicked, [this] () {
      p.instant_quitout();
    });

    printf("build pos thread\n");
    position_updater = std::thread([this] () {
      while (true) {
        if (!position->get_lock() && p.is_attached()) {
          try {
            auto t = p.get_position();
            position->set_lock(true);
            position->update_position(std::get<0>(t), std::get<1>(t), std::get<2>(t));
            position->set_lock(false);
          } catch (memory_exception e) {

          }
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(33));
      }
    });

    printf("build hotkeys\n");
    // At least move this to a different function

    std::unordered_map<std::string, DWORD> hotkey_string_mappings({
      { "VK_LBUTTON", VK_LBUTTON },
      { "VK_RBUTTON", VK_RBUTTON },
      { "VK_CANCEL", VK_CANCEL },
      { "VK_MBUTTON", VK_MBUTTON },
      { "VK_XBUTTON1", VK_XBUTTON1 },
      { "VK_XBUTTON2", VK_XBUTTON2 },
      { "VK_BACK", VK_BACK },
      { "VK_TAB", VK_TAB },
      { "VK_CLEAR", VK_CLEAR },
      { "VK_RETURN", VK_RETURN },
      { "VK_SHIFT", VK_SHIFT },
      { "VK_CONTROL", VK_CONTROL },
      { "VK_MENU", VK_MENU },
      { "VK_PAUSE", VK_PAUSE },
      { "VK_CAPITAL", VK_CAPITAL },
      { "VK_KANA", VK_KANA },
      { "VK_HANGUL", VK_HANGUL },
      { "VK_JUNJA", VK_JUNJA },
      { "VK_FINAL", VK_FINAL },
      { "VK_HANJA", VK_HANJA },
      { "VK_KANJI", VK_KANJI },
      { "VK_ESCAPE", VK_ESCAPE },
      { "VK_CONVERT", VK_CONVERT },
      { "VK_NONCONVERT", VK_NONCONVERT },
      { "VK_ACCEPT", VK_ACCEPT },
      { "VK_MODECHANGE", VK_MODECHANGE },
      { "VK_SPACE", VK_SPACE },
      { "VK_PRIOR", VK_PRIOR },
      { "VK_NEXT", VK_NEXT },
      { "VK_END", VK_END },
      { "VK_HOME", VK_HOME },
      { "VK_LEFT", VK_LEFT },
      { "VK_UP", VK_UP },
      { "VK_RIGHT", VK_RIGHT },
      { "VK_DOWN", VK_DOWN },
      { "VK_SELECT", VK_SELECT },
      { "VK_PRINT", VK_PRINT },
      { "VK_EXECUTE", VK_EXECUTE },
      { "VK_SNAPSHOT", VK_SNAPSHOT },
      { "VK_INSERT", VK_INSERT },
      { "VK_DELETE", VK_DELETE },
      { "VK_HELP", VK_HELP },
      { "0", '0' },
      { "1", '1' },
      { "2", '2' },
      { "3", '3' },
      { "4", '4' },
      { "5", '5' },
      { "6", '6' },
      { "7", '7' },
      { "8", '8' },
      { "9", '9' },
      { "A", 'A' },
      { "B", 'B' },
      { "C", 'C' },
      { "D", 'D' },
      { "E", 'E' },
      { "F", 'F' },
      { "G", 'G' },
      { "H", 'H' },
      { "I", 'I' },
      { "J", 'J' },
      { "K", 'K' },
      { "L", 'L' },
      { "M", 'M' },
      { "N", 'N' },
      { "O", 'O' },
      { "P", 'P' },
      { "Q", 'Q' },
      { "R", 'R' },
      { "S", 'S' },
      { "T", 'T' },
      { "U", 'U' },
      { "V", 'V' },
      { "W", 'W' },
      { "X", 'X' },
      { "Y", 'Y' },
      { "Z", 'Z' },
      { "VK_LWIN", VK_LWIN },
      { "VK_RWIN", VK_RWIN },
      { "VK_APPS", VK_APPS },
      { "VK_SLEEP", VK_SLEEP },
      { "VK_NUMPAD0", VK_NUMPAD0 },
      { "VK_NUMPAD1", VK_NUMPAD1 },
      { "VK_NUMPAD2", VK_NUMPAD2 },
      { "VK_NUMPAD3", VK_NUMPAD3 },
      { "VK_NUMPAD4", VK_NUMPAD4 },
      { "VK_NUMPAD5", VK_NUMPAD5 },
      { "VK_NUMPAD6", VK_NUMPAD6 },
      { "VK_NUMPAD7", VK_NUMPAD7 },
      { "VK_NUMPAD8", VK_NUMPAD8 },
      { "VK_NUMPAD9", VK_NUMPAD9 },
      { "VK_MULTIPLY", VK_MULTIPLY },
      { "VK_ADD", VK_ADD },
      { "VK_SEPARATOR", VK_SEPARATOR },
      { "VK_SUBTRACT", VK_SUBTRACT },
      { "VK_DECIMAL", VK_DECIMAL },
      { "VK_DIVIDE", VK_DIVIDE },
      { "VK_F1", VK_F1 },
      { "VK_F2", VK_F2 },
      { "VK_F3", VK_F3 },
      { "VK_F4", VK_F4 },
      { "VK_F5", VK_F5 },
      { "VK_F6", VK_F6 },
      { "VK_F7", VK_F7 },
      { "VK_F8", VK_F8 },
      { "VK_F9", VK_F9 },
      { "VK_F10", VK_F10 },
      { "VK_F11", VK_F11 },
      { "VK_F12", VK_F12 },
      { "VK_F13", VK_F13 },
      { "VK_F14", VK_F14 },
      { "VK_F15", VK_F15 },
      { "VK_F16", VK_F16 },
      { "VK_F17", VK_F17 },
      { "VK_F18", VK_F18 },
      { "VK_F19", VK_F19 },
      { "VK_F20", VK_F20 },
      { "VK_F21", VK_F21 },
      { "VK_F22", VK_F22 },
      { "VK_F23", VK_F23 },
      { "VK_F24", VK_F24 },
      { "VK_NUMLOCK", VK_NUMLOCK },
      { "VK_SCROLL", VK_SCROLL },
      { "VK_LSHIFT", VK_LSHIFT },
      { "VK_RSHIFT", VK_RSHIFT },
      { "VK_LCONTROL", VK_LCONTROL },
      { "VK_RCONTROL", VK_RCONTROL },
      { "VK_LMENU", VK_LMENU },
      { "VK_RMENU", VK_RMENU },
      { "VK_BROWSER_BACK", VK_BROWSER_BACK },
      { "VK_BROWSER_FORWARD", VK_BROWSER_FORWARD },
      { "VK_BROWSER_REFRESH", VK_BROWSER_REFRESH },
      { "VK_BROWSER_STOP", VK_BROWSER_STOP },
      { "VK_BROWSER_SEARCH", VK_BROWSER_SEARCH },
      { "VK_BROWSER_FAVORITES", VK_BROWSER_FAVORITES },
      { "VK_BROWSER_HOME", VK_BROWSER_HOME },
      { "VK_VOLUME_MUTE", VK_VOLUME_MUTE },
      { "VK_VOLUME_DOWN", VK_VOLUME_DOWN },
      { "VK_VOLUME_UP", VK_VOLUME_UP },
      { "VK_MEDIA_NEXT_TRACK", VK_MEDIA_NEXT_TRACK },
      { "VK_MEDIA_PREV_TRACK", VK_MEDIA_PREV_TRACK },
      { "VK_MEDIA_STOP", VK_MEDIA_STOP },
      { "VK_MEDIA_PLAY_PAUSE", VK_MEDIA_PLAY_PAUSE },
      { "VK_LAUNCH_MAIL", VK_LAUNCH_MAIL },
      { "VK_LAUNCH_MEDIA_SELECT", VK_LAUNCH_MEDIA_SELECT },
      { "VK_LAUNCH_APP1", VK_LAUNCH_APP1 },
      { "VK_LAUNCH_APP2", VK_LAUNCH_APP2 },
      { "VK_OEM_1", VK_OEM_1 },
      { "VK_OEM_PLUS", VK_OEM_PLUS },
      { "VK_OEM_COMMA", VK_OEM_COMMA },
      { "VK_OEM_MINUS", VK_OEM_MINUS },
      { "VK_OEM_PERIOD", VK_OEM_PERIOD },
      { "VK_OEM_2", VK_OEM_2 },
      { "VK_OEM_3", VK_OEM_3 },
      { "VK_OEM_4", VK_OEM_4 },
      { "VK_OEM_5", VK_OEM_5 },
      { "VK_OEM_6", VK_OEM_6 },
      { "VK_OEM_7", VK_OEM_7 },
      { "VK_OEM_8", VK_OEM_8 },
      { "VK_OEM_102", VK_OEM_102 },
      { "VK_PROCESSKEY", VK_PROCESSKEY },
      { "VK_PACKET", VK_PACKET },
      { "VK_ATTN", VK_ATTN },
      { "VK_CRSEL", VK_CRSEL },
      { "VK_EXSEL", VK_EXSEL },
      { "VK_EREOF", VK_EREOF },
      { "VK_PLAY", VK_PLAY },
      { "VK_ZOOM", VK_ZOOM },
      { "VK_NONAME", VK_NONAME },
      { "VK_PA1", VK_PA1 },
      { "VK_OEM_CLEAR", VK_OEM_CLEAR }
    });

    std::unordered_map<std::string, std::function<void(void)>> hotkey_callbacks({
      { "no_damage", [this] () { flags[0]->toggle(); } },
      { "event_draw", [this] () { flags[1]->toggle(); } },
      { "no_death", [this] () { flags[2]->toggle(); } },
      { "event_disable", [this] () { flags[3]->toggle(); } },
      { "deathcam", [this] () { flags[4]->toggle(); } },
      { "ai_disable", [this] () { flags[5]->toggle(); } },
      { "inf_stamina", [this] () { flags[6]->toggle(); } },
      { "no_gravity", [this] () { flags[7]->toggle(); } },
      { "inf_focus", [this] () { flags[8]->toggle(); } },
      { "hide_character", [this] () { flags[9]->toggle(); } },
      { "inf_consum", [this] () { flags[10]->toggle(); } },
      { "hide_map", [this] () { flags[11]->toggle(); } },
      { "one_shot", [this] () { flags[12]->toggle(); } },
      { "hide_objects", [this] () { flags[13]->toggle(); } },
      { "load_position", [this] () { position->click_load(); } },
      { "save_position", [this] () { position->click_save(); } },
      { "decr_speed", [this] () { speed->click_decr(); } },
      { "incr_speed", [this] () { speed->click_incr(); } },
      { "insta_quitout", [this] () { instant_quitout_btn->animateClick(); } },
      { "attach", [this] () { attach_btn->animateClick(); } }
    });

    try {

      auto cfg = cpptoml::parse_file(get_config_file_name());
      auto hotkey_table = cfg->get_table("hotkeys");

      for(auto k : hotkey_callbacks) {
        auto v = hotkey_table->get_qualified_as<std::string>(k.first);
        if (v && *v != "unset") {
          auto it = hotkey_string_mappings.find(*v);
          if (it == hotkey_string_mappings.end()) {
            // te->append(tfm::format("Unknown key mapping for %s: %s", k.first, (*v).c_str()).c_str());
          } else {
            auto p = std::make_pair((*it).second, k.second);
            hotkey_bindings.push_back(std::move(p));
          }
        }
      }
    } catch (cpptoml::parse_exception e) {
      QMessageBox qmb;
      qmb.setText(tfm::format("Parse error. Hotkeys might be partially working or not working. (%s)", e.what()).c_str());
      qmb.exec();
    }
  }

  void Window::keyup (DWORD vk_code) {
    for (auto kv : hotkey_bindings) {
      if (kv.first == vk_code) {
        std::invoke(kv.second);
      }
    }
  }
}