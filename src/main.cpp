#include <QApplication>
#include <iostream>
#include <fstream>
#include <Shlwapi.h>
#include "window.h"
#include "common.h"

// Legit disgusting. Tyvm global hooks' stateless lambdas,
// look what you've done to my beautiful code
DS3PracticeTools::Window* window;

const char* default_settings = 
  "[hotkeys]\n\n"
  "no_damage = \"VK_F1\"\n"
  "no_death = \"VK_F1\"\n"
  "deathcam = \"VK_F8\"\n"
  "inf_stamina = \"VK_F1\"\n"
  "inf_focus = \"VK_F1\"\n"
  "inf_consum = \"VK_F1\"\n"
  "one_shot = \"VK_F1\"\n"
  "event_draw = \"VK_F2\"\n"
  "event_disable = \"VK_F2\"\n"
  "ai_disable = \"VK_F2\"\n"
  "no_gravity = \"VK_F3\"\n"
  "hide_character = \"unset\"\n"
  "hide_map = \"unset\"\n"
  "hide_objects = \"unset\"\n"
  "load_position = \"VK_F4\"\n"
  "save_position = \"VK_F5\"\n"
  "decr_speed = \"VK_F6\"\n"
  "incr_speed = \"VK_F7\"\n"
  "insta_quitout = \"VK_F9\"\n"
  "attach = \"VK_F10\"\n"
  ;

/*BOOL FileExists(std::string path) {
  DWORD dwAttrib = GetFileAttributes(std::wstring(path.begin(), path.end()).c_str());

  return (dwAttrib != INVALID_FILE_ATTRIBUTES && 
         !(dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
}*/

std::string get_config_file_name () {
  char szExeFileName[MAX_PATH]; 
  GetModuleFileNameA(NULL, szExeFileName, MAX_PATH);
  std::string wf(szExeFileName);
  uint64_t lastslash = wf.find_last_of('\\');
  std::string dirname(wf.begin(), wf.begin() + lastslash + 1);
  return dirname + std::string("DarkSoulsIII-PracticeTool.toml");
}

int main(int argc, char **argv) {
  QApplication app (argc, argv);

  std::string conf_name = get_config_file_name();
  // if (!FileExists(conf_name)) {
  if (!PathFileExistsA(conf_name.c_str())) {
    FILE* fp = fopen(conf_name.c_str(), "w");
    fprintf(fp, "%s\n", default_settings);
    fclose(fp);
  }

  window = new DS3PracticeTools::Window();
  window->show();

  auto hook = SetWindowsHookEx(WH_KEYBOARD_LL, [](int nCode, WPARAM wParam, LPARAM lParam)->LRESULT {
    if (nCode == HC_ACTION) {
      switch (wParam) {
        case WM_KEYDOWN:
          // Don't really care, should just slim this down to
          // an "if ncode == HC_ACTION && wParam == WM_KEYUP"
          break;
        case WM_KEYUP:
          window->keyup(PKBDLLHOOKSTRUCT(lParam)->vkCode);
          break;
      }
    }
    return CallNextHookEx(NULL, nCode, wParam, lParam);
  }, 0, 0);

  QObject::connect(window, &QWidget::destroyed, [&hook] () {
    UnhookWindowsHookEx(hook);
  });

  return app.exec();
}