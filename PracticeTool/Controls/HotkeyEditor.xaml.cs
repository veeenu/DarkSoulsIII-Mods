using PracticeTool.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PracticeTool.Controls {
  /// <summary>
  /// Interaction logic for HotkeyEditor.xaml
  /// </summary>
  public partial class HotkeyEditor : UserControl {
    private HotkeyDataContext hotkeyDK;

    public HotkeyEditor() {
      InitializeComponent();
      hotkeyDK = new HotkeyDataContext(this); // ugly af
      HotkeyButton.DataContext = hotkeyDK;
      this.DataContextChanged += (sender, e) => {
        hotkeyDK.RaisePropertyChanged("ButtonText");
      };
    }
    private void Button_Click(object sender, RoutedEventArgs e) {
      hotkeyDK.IsListening = true;
    }

    private void Button_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
      if(hotkeyDK.IsListening) {
        Hotkey hk = (Hotkey)DataContext;
        if (e.Key == Key.Escape) {
          hk.HotkeyCode = null;
        }
        else {
          hk.HotkeyCode = HotkeyManager.LabelFromKey(KeyInterop.VirtualKeyFromKey(e.Key));
        }
        hotkeyDK.IsListening = false;
      }
    }
  }

  class HotkeyDataContext : INotifyPropertyChanged {

    private static string nullString = "-";

    private string _buttonText;
    public string ButtonText {
      get {
        if (_listening) return "Press key...";

        if (hk.DataContext == null) return nullString;
        if (!(hk.DataContext is Hotkey)) return nullString;
        
        Hotkey hkk = ((Hotkey)hk.DataContext);
        if (hkk == null) return nullString;
        string hkc = hkk.HotkeyCode;
        if (hkc == null) return nullString;
        return hkc.Replace("_", "__");
      }
      set {
        _buttonText = value;
        RaisePropertyChanged("ButtonText");
      }
    }

    private bool _listening;
    public bool IsListening {
      get => _listening;
      set {
        _listening = value;
        RaisePropertyChanged("IsListening");
        RaisePropertyChanged("ButtonText");
      }
    }

    private HotkeyEditor hk;
    public HotkeyDataContext(HotkeyEditor hk) {
      this.hk = hk;
    }

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;
    public void RaisePropertyChanged(string propertyName) {
      // take a copy to prevent thread issues
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
  }
}
