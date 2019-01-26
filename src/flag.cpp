#include "flag.h"

namespace DS3PracticeTools {

  Flag::Flag (QWidget* parent, const std::string& str)
    : QWidget(parent), label(this), checked(this), hotkey_button(this), layout() {
    label.setText(QString(str.c_str()));
    layout.addWidget(&label, 0, 0);
    layout.addWidget(&checked, 0, 1);
    layout.addWidget(&hotkey_button, 0, 2);
    layout.setColumnStretch(0, 1);
    layout.setColumnMinimumWidth(1, 32);
    layout.setColumnMinimumWidth(2, 32);
    layout.setSpacing(0);
    layout.setContentsMargins(8, 2, 8, 2);
    setLayout(&layout);
  }

};