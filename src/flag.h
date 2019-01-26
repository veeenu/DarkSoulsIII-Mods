#pragma once

#include <QLabel>
#include <QCheckBox>
#include <QPushButton>
#include <QWidget>
#include <QGridLayout>

#include <string>

namespace DS3PracticeTools {

  class Flag : public QWidget {
    Q_OBJECT
    public:
      Flag (QWidget* parent, const std::string&);
    private:
      QGridLayout layout;
      QLabel label;
      QCheckBox checked;
      QPushButton hotkey_button;
  };
};
