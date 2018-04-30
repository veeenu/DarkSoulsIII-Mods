using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeTool.Model {
  public class Hotkey : INotifyPropertyChanged {
    public string Label { get; private set; }
    public string ConfigLabel { get; private set; }
    private string _hotkeyCode;
    public string HotkeyCode {
      get => _hotkeyCode;
      set {
        _hotkeyCode = value;
        RaisePropertyChanged("HotkeyCode");
      }
    }
    private HotkeyAction hkAction;

    public delegate void HotkeyAction();
    public Hotkey(string label, string configLabel, HotkeyAction action) {
      Label = label;
      ConfigLabel = configLabel;
      hkAction = action;
    }

    public void PerformAction() {
      hkAction.Invoke();
    }

    public void TriggerChange() {
      RaisePropertyChanged("HotkeyCode");
    }

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;
    private void RaisePropertyChanged(string propertyName) {
      // take a copy to prevent thread issues
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
  }
}
