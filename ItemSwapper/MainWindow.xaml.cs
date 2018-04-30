using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ItemSwapper {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {

    private MainWindowViewModel viewModel;
    private ItemSwapPatch patch;
    private DispatcherTimer timer;

    public MainWindow() {
      foreach (var f in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
        Debug.Print(f);
      }

      viewModel = new MainWindowViewModel();

      var asm = this.GetType().Assembly;
      var stream = asm.GetManifestResourceStream("ItemSwapper.Resources.items.json");
      var sr = new StreamReader(stream);
      viewModel.Items = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());

      InitializeComponent();
      base.DataContext = viewModel;

      patch = new ItemSwapPatch();
      patch.Enable();

      Closing += MainWindow_Closing;
      timer = new DispatcherTimer();
      timer.Tick += (object sender, EventArgs e) => {
        UInt32? val = patch.ReadValue();
        if (val.HasValue)
          viewModel.CurrentItem = val.Value.ToString("x8");
      };
      timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
      timer.Start();
    }

    private void MainWindow_Closing(object sender, CancelEventArgs e) {
      timer.Stop();
      patch.Disable();
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      patch.WriteValue(viewModel.SelectedItemCode);
    }
  }

  class MainWindowViewModel : INotifyPropertyChanged {
    private Dictionary<string, string> _items;
    private string _currentItem;
    private string _itemsText;

    public string ItemsText {
      get => _itemsText;
      set { _itemsText = value; RaisePropertyChanged("ItemsText"); RaisePropertyChanged("CurrentItem"); }
    }

    public string CurrentItem {
      get => _currentItem;
      set { _currentItem = value; RaisePropertyChanged("CurrentItem"); }
    }

    public UInt32 SelectedItemCode {
      get {
        if (_itemsText != null && _items.TryGetValue(_itemsText, out string ret)) {
          return Convert.ToUInt32(ret, 16);
        }
        return 0;
      }
      set { }
    }

    public Dictionary<String, String> Items {
      get { return _items; }
      set { _items = value; RaisePropertyChanged("Items");  }
    }

    #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Methods

    private void RaisePropertyChanged(string propertyName) {
      // take a copy to prevent thread issues
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
  }
}
