using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PracticeTool.Model {
  public class Flag : IMemoryVariable {
    private bool _flagValue;
    private bool _forceValue;
    private string _label;

    public string Label { get => _label; }
    public bool FlagValue {
      get => _flagValue;
      set { _flagValue = value; RaisePropertyChanged("Value"); RaisePropertyChanged("FlagValue"); }
    }
    public bool ForceValue {
      get => _forceValue;
      set { _forceValue = value; RaisePropertyChanged("ForceValue"); }
    }

    public Flag(string label, bool defaultValue = false) {
      this._label = label;
      this._flagValue = defaultValue;
      this.ReadMemory = false;
    }

    private bool _readMemory;
    public bool ReadMemory {
      get => _readMemory;
      set {
        if (value) {
          Thread t = new Thread(() => {
            Thread.Sleep(250);
            _readMemory = true;
          });
          t.Start();
        }
        else {
          _readMemory = false;
        }
      }
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
