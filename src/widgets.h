#pragma once

#include <QWidget>
#include <QLabel>
#include <QCheckBox>
#include <QLineEdit>
#include <QPushButton>
#include <QComboBox>
#include <QGridLayout>

#include <string>

namespace DS3PracticeTools {

  class Flag : public QWidget {
    Q_OBJECT
    public:
      Flag (QWidget* parent, const std::string&);
      void toggle ();
      void set (bool b);
      bool get ();
    signals:
      void flag_on ();
      void flag_off ();
    private:
      QGridLayout layout;
      QLabel label;
      QLabel hotkey;
      QCheckBox checked;
  };

  class Position : public QWidget {
    Q_OBJECT
    public:
      Position (QWidget* parent);
      void update_position (float nx, float ny, float nz);

      void click_load ();
      void click_save ();
      void set_lock (bool);
      bool get_lock () const;
    signals:
      void load (float x, float y, float z);
      void save (float x, float y, float z);
    private:
      bool lock;
      float x, y, z;
      float sx, sy, sz;

      QGridLayout layout;
      QLineEdit x_edit;
      QLineEdit y_edit;
      QLineEdit z_edit;
      QPushButton load_btn;
      QPushButton save_btn;
  };

  class Speed : public QWidget {
    Q_OBJECT
    public:
      Speed (QWidget* parent);
      void click_incr ();
      void click_decr ();
    signals:
      void set_speed(double speed);
    private:
      QHBoxLayout layout;
      QComboBox combo_box;
      QPushButton decrease;
      QPushButton increase;
  };
};
