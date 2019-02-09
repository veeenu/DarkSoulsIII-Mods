#include <tinyformat.h>
#include "widgets.h"

namespace DS3PracticeTools {

  Flag::Flag (QWidget* parent, const std::string& str)
    : QWidget(parent), label(this), hotkey(this), checked(this), layout() {
    label.setText(QString(str.c_str()));
    layout.addWidget(&label,   0, 0, 1, 1, Qt::AlignHCenter);
    layout.addWidget(&hotkey,  0, 1, 1, 1, Qt::AlignHCenter);
    layout.addWidget(&checked, 0, 2, 1, 1, Qt::AlignHCenter);
    layout.setColumnStretch(0, 1);
    layout.setColumnMinimumWidth(1, 32);
    layout.setColumnMinimumWidth(2, 32);
    layout.setSpacing(0);
    layout.setContentsMargins(8, 2, 8, 2);
    setLayout(&layout);

    connect(&checked, &QCheckBox::toggled, [this] (bool checked) {
      if (checked)
        emit flag_on();
      else
        emit flag_off();
    });
  }

  void Flag::toggle () {
    set(!get());
  }

  void Flag::set (bool b) {
    checked.setChecked(b);
  }

  bool Flag::get () {
    return checked.isChecked();
  }

  ////////////////////////////////////////////////////////////////////////////

  Position::Position (QWidget* parent)
    : QWidget(parent), x_edit(this), y_edit(this), z_edit(this),
      load_btn(this), save_btn(this), layout(),
      x(0), y(0), z(0), sx(0), sy(0), sz(0) {

    x_edit.setEnabled(false);
    y_edit.setEnabled(false);
    z_edit.setEnabled(false);
    x_edit.setAlignment(Qt::AlignCenter);
    y_edit.setAlignment(Qt::AlignCenter);
    z_edit.setAlignment(Qt::AlignCenter);

    layout.addWidget(&x_edit, 0, 0, 1, 4);
    layout.addWidget(&y_edit, 0, 4, 1, 4);
    layout.addWidget(&z_edit, 0, 8, 1, 4);
    layout.addWidget(&load_btn, 1, 0, 1, 6);
    layout.addWidget(&save_btn, 1, 6, 1, 6);
    layout.setSpacing(0);
    layout.setContentsMargins(8, 2, 8, 2);
    load_btn.setText("Load");
    save_btn.setText("Save");
    setLayout(&layout);

    connect(this, &Position::load, [this] (float x, float y, float z) {
      update_position(x, y, z);
    });

    connect(&save_btn, &QPushButton::clicked, [this] () {
      sx = x; sy = y, sz = z;
      emit save(sx, sy, sz);
    });

    connect(&load_btn, &QPushButton::clicked, [this] () {
      emit load(sx, sy, sz);
    });
  }

  void Position::click_load () {
    load_btn.animateClick();
  }

  void Position::click_save () {
    save_btn.animateClick();
  }

  void Position::set_lock (bool b) {
    lock = b;
  }

  bool Position::get_lock () const {
    return lock;
  }

  void Position::update_position (float nx, float ny, float nz) {
    x = nx; y = ny; z = nz;
    x_edit.setText(tfm::format("%.2f", x).c_str());
    y_edit.setText(tfm::format("%.2f", y).c_str());
    z_edit.setText(tfm::format("%.2f", z).c_str());
  }

  ////////////////////////////////////////////////////////////////////////////

  Speed::Speed (QWidget* parent)
    : QWidget(parent), combo_box(this), decrease(this), increase(this),
      layout() {
    
    combo_box.addItem(QString("Speed 1x"), QVariant(1.0));
    combo_box.addItem(QString("Speed 1.25x"), QVariant(1.25));
    combo_box.addItem(QString("Speed 1.5x"), QVariant(1.5));
    combo_box.addItem(QString("Speed 2x"), QVariant(2.0));
    combo_box.addItem(QString("Speed 4x"), QVariant(4.0));
    decrease.setText(tr("-"));
    increase.setText(tr("+"));

    decrease.setSizePolicy(QSizePolicy::Ignored, QSizePolicy::Ignored);
    combo_box.setSizePolicy(QSizePolicy::Ignored, QSizePolicy::Ignored);
    increase.setSizePolicy(QSizePolicy::Ignored, QSizePolicy::Ignored);

    decrease.setContentsMargins(0, 0, 0, 0);

    layout.addWidget(&decrease, 2);
    layout.addWidget(&combo_box, 6);
    layout.addWidget(&increase, 2);
    layout.setSpacing(0);
    layout.setContentsMargins(8, 12, 8, 12);

    setLayout(&layout);

    connect(&decrease, &QPushButton::clicked, [this] () {
      auto i = combo_box.currentIndex() - 1;
      if (i < 0)
        i = combo_box.count() - 1;
      
      combo_box.setCurrentIndex(i);
    });

    connect(&increase, &QPushButton::clicked, [this] () {
      auto i = combo_box.currentIndex() + 1;
      if (i >= combo_box.count())
        i = 0;
      
      combo_box.setCurrentIndex(i);
    });

    connect(&combo_box, QOverload<int>::of(&QComboBox::currentIndexChanged), [this] () {
      emit set_speed(combo_box.currentData().toDouble());
    });
  }

  void Speed::click_incr () {
    increase.animateClick();
  }

  void Speed::click_decr () {
    decrease.animateClick();
  }

}