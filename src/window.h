#ifndef WINDOW_H
#define WINDOW_H

#include <QWidget>
#include <QGridLayout>
#include "flag.h"

class QPushButton;
class Window : public QWidget {
  public:
    explicit Window(QWidget *parent = 0);
  private:
    QGridLayout layout;
    std::vector<DS3PracticeTools::Flag*> flags;
};

#endif // WINDOW_H