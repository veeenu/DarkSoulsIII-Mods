using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PracticeTool.Model {
  public class FloatField : IMemoryVariable {
    private float _value;
    private bool _forceValue;
    private string _label;
    private string _textValue;

    public string Label { get => _label; }
    public float Value {
      get => _value;
      set {
        _value = value;
        _textValue = _value.ToString();
        if(!ReadMemory) {
          RaisePropertyChanged("Value");
        }
        RaisePropertyChanged("TextValue");
      }
    }

    public String TextValue {
      get => _textValue;
      set {
        if (Single.TryParse(value, out Single v)) {
          Value = v;
          if (!ReadMemory) {
            RaisePropertyChanged("Value");
          }
          RaisePropertyChanged("TextValue");
        }
      }
    }
    public bool ForceValue {
      get => _forceValue;
      set { _forceValue = value; RaisePropertyChanged("ForceValue"); }
    }

    private bool _readMemory;
    public bool ReadMemory {
      get => _readMemory;
      set {
        if(value) {
          Thread t = new Thread(() => {
            Thread.Sleep(250);
            _readMemory = true;
          });
          t.Start();
        } else {
          _readMemory = false;
        }
      }
    }

    public FloatField(string label) {
      this._label = label;
      this.ReadMemory = false;
    }

    public void Refresh() {
      RaisePropertyChanged("Value");
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
