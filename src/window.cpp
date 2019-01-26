#include <QPushButton>
#include <QComboBox>
#include "window.h"
#include "flag.h"

Window::Window(QWidget *parent) 
  : QWidget(parent), layout(), flags() {
  // Set size of the window
  setFixedSize(400, 480);

  std::vector<std::string> labels = {
    "No Damage", "Event Draw",
    "No Death", "Event Disable",
    "Deathcam", "AI Disable",
    "Infinite Stamina", "No Gravity",
    "Infinite Focus", "Hide Character",
    "Infinite Consumables", "Hide Map",
    "One Shot", "Hide Objects"
  };

  for (int i = 0; i < labels.size(); i++) {
    int col = i % 2, row = i / 2;
    flags.push_back(new DS3PracticeTools::Flag(this, labels[i]));
    auto f = flags.back();
    f->setGeometry(col * 200, row * 24, 200, 24);
  }

  QComboBox* qc = new QComboBox(this);
  qc->setContentsMargins(8, 8, 8, 8);
  qc->addItem(QString("Speed 1x"));
  qc->addItem(QString("Speed 2x"));
  qc->addItem(QString("Speed 4x"));
  qc->setGeometry(0, 7*24, 200, 24);

}