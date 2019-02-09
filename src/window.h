#pragma once

#include <QPushButton>
#include <QMessageBox>
#include <QTextEdit>
#include <thread>
#include "widgets.h"
#include "memory.h"

namespace DS3PracticeTools {
  class Window : public QWidget {
    public:
      explicit Window(QWidget *parent = 0);
      void keyup(DWORD vk_code);
      void instaqo();
    private:

      Process p;
      Speed* speed;
      Position* position;
      std::vector<Flag*> flags;

      QTextEdit* te;
      QPushButton* attach_btn;
      QPushButton* instant_quitout_btn;
      QGridLayout layout;

      std::thread position_updater;
      HHOOK hook;

      std::vector<std::pair<DWORD, std::function<void(void)>>> hotkey_bindings;
  };
}