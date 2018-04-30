using MemoryManagement;
using PracticeTool.ViewModels;
using PracticeTool.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PracticeTool {
  public partial class MainWindow : Window {
    private MemoryManager mm;
    private Addresses addresses;
    private HotkeyViewModel hotkeyViewModel;
    private Thread memLoopThread;
    private bool isDone = false;

    private bool isGoingUp = false;
    private bool isGoingDown = false;

    private float? savedX;
    private float? savedY;
    private float? savedZ;

    private HwndSource source;
    private IntPtr interopHandle;
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private NotifyString log;

    public MainWindow() {
      this.isDone = false;

      InitializeComponent();

      log = new NotifyString();
      this.LogView.DataContext = log;

      this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
      this.Closing += MainWindow_Closed;
      this.SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object sender, EventArgs e) {
      IntPtr handle = new WindowInteropHelper(this).Handle;
      source = HwndSource.FromHwnd(handle);
      source.AddHook(HwndHook);

      this.interopHandle = handle;

      hotkeyViewModel = new HotkeyViewModel(
        k => {
          Dispatcher.BeginInvoke(new Action(() => {
            bool r = RegisterHotKey(handle, HOTKEY_ID, 0, k);
          }));
        }
      );
    }

    void MainWindow_Loaded(object sender, RoutedEventArgs e) {
      memLoopThread = new Thread(MemoryLoop);

      memLoopThread.Start();
    }

    private void MainWindow_Closed(object sender, CancelEventArgs e) {
      this.isDone = true;
      e.Cancel = true;
      if(!memLoopThread.IsAlive) {
        Application.Current.Shutdown();
      }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
      switch (msg) {
        case WM_HOTKEY:
          Debug.WriteLine("WM HOTKEY");
          switch (wParam.ToInt32()) {
            case HOTKEY_ID:
              int vkey = (((int)lParam >> 16) & 0xFFFF);
              Debug.WriteLine("Processing " + vkey.ToString("X"));
              handled = hotkeyViewModel.PerformAction(vkey);
              break;
          }
          break;
      }
      return IntPtr.Zero;
    }

    private void MemoryLoop() {
      log.Log += "Attaching process... ";
      mm = new MemoryManager();
      if(mm.Attach("DarkSoulsIII")) {
        log.Log += "Success!\n";
      } else {
        log.Log += "\nFailure.\nPlease restart Dark Souls III and the tool.";
        this.isDone = true;
        return;
      }

      log.Log += "Game version is 1." + mm.VersionInfo.FileMinorPart.ToString() + ".\n";
      switch (mm.VersionInfo.FileMinorPart) {
        case 8:
          addresses = Addresses.Addresses08(mm);
          break;
        case 12:
          addresses = Addresses.Addresses12(mm);
          break;
        default:
          log.Log += "This version is unsupported!";
          break;
      }

      hotkeyViewModel.NoDamage = new Hotkey("No Damage", "no_damage", ToggleFlagFactory(addresses.NoDamage));
      hotkeyViewModel.NoDeath = new Hotkey("No Death", "no_death", ToggleFlagFactory(addresses.NoDeath));
      hotkeyViewModel.NoStaminaCons = new Hotkey("Infinite stamina", "inf_stamina", ToggleFlagFactory(addresses.NoStaminaCons));
      hotkeyViewModel.NoFpCons = new Hotkey("Infinite FP", "inf_fp", ToggleFlagFactory(addresses.NoFpCons));
      hotkeyViewModel.NoGoodsCons = new Hotkey("No consumption", "inf_consumables", ToggleFlagFactory(addresses.NoGoodsCons));
      hotkeyViewModel.NoCollision = new Hotkey("Noclip", "no_clip", ToggleFlagFactory(addresses.NoCollision));
      hotkeyViewModel.Deathcam = new Hotkey("Deathcam", "deathcam", ToggleFlagFactory(addresses.Deathcam));
      hotkeyViewModel.EventDraw = new Hotkey("Draw events", "event_draw", ToggleFlagFactory(addresses.EventDraw));
      hotkeyViewModel.EventDisable = new Hotkey("Disable events", "event_disable", ToggleFlagFactory(addresses.EventDisable));
      hotkeyViewModel.AIDisable = new Hotkey("Disable AI", "ai_disable", ToggleFlagFactory(addresses.AIDisable));
      hotkeyViewModel.OneShot = new Hotkey("One shot", "one_shot", ToggleFlagFactory(addresses.OneShot));
      hotkeyViewModel.HideMap = new Hotkey("Hide map", "hide_map", ToggleFlagFactory(addresses.HideMap));
      hotkeyViewModel.HideChar = new Hotkey("Hide character", "hide_char", ToggleFlagFactory(addresses.HideChar));
      hotkeyViewModel.HideObj = new Hotkey("Hide objects", "hide_obj", ToggleFlagFactory(addresses.HideObj));
      hotkeyViewModel.ChangeSpeed = new Hotkey("Speed", "change_speed", ChangeSpeedFactory());
      hotkeyViewModel.MoveUp = new Hotkey("Move up", "move_up", () => { isGoingUp = true; });
      hotkeyViewModel.MoveDown = new Hotkey("Move down", "move_down", () => { isGoingDown = true; });
      hotkeyViewModel.SavePos = new Hotkey("Save position", "save_pos", SavePosition);
      hotkeyViewModel.LoadPos = new Hotkey("Load position", "load_pos", LoadPosition);
      hotkeyViewModel.LoadConfig();

      this.Dispatcher.BeginInvoke(new Action(() => {
        HotkeySettingsPanel.DataContext = hotkeyViewModel;
        HotkeySettingsPanel.Load();
      }));

      this.cbSpeed.SelectionChanged += (sender, e) => {
        float[] speeds = new float[] { 1.0f, 1.5f, 2.0f, 4.0f };
        int idx = (sender as ComboBox).SelectedIndex;
        addresses.Speed.Value = speeds[idx];
      };

      this.Dispatcher.Invoke(() => {
        this.DataContext = addresses;
      });

      while(!this.isDone) {
        Thread.Sleep(TimeSpan.FromMilliseconds(33));
        addresses.Enforce();

        if(isGoingUp && !isGoingDown) {
          bool b = addresses.PosZ.ReadMemory;
          addresses.PosZ.ReadMemory = false;
          addresses.PosZ.Value = addresses.PosZ.Value + 1f;
          addresses.PosZ.ReadMemory = b;
        } else if(isGoingDown && !isGoingUp) {
          bool b = addresses.PosZ.ReadMemory;
          addresses.PosZ.ReadMemory = false;
          addresses.PosZ.Value = addresses.PosZ.Value - 1f;
          addresses.PosZ.ReadMemory = b;
        }
        isGoingDown = isGoingUp = false;
      }

      this.Dispatcher.BeginInvoke(new Action(() => {
        Application.Current.Shutdown();
      }));
    }

    private Hotkey.HotkeyAction ToggleFlagFactory(Flag flag) {
      return () => {
        flag.FlagValue = !flag.FlagValue;
      };
    }

    private Hotkey.HotkeyAction ChangeSpeedFactory() {
      return () => {
        float[] speeds = new float[] { 1.0f, 1.5f, 2.0f, 4.0f };
        int idx = this.cbSpeed.SelectedIndex;
        int newidx = (idx + 1) % speeds.Length;
        addresses.Speed.Value = speeds[newidx];
        this.cbSpeed.SelectedIndex = newidx;
      };
    }

    private void SavePosition() {
      savedX = addresses.PosX.Value;
      savedY = addresses.PosY.Value;
      savedZ = addresses.PosZ.Value;
      log.Log += String.Format("Saved position ({0}, {1}, {2}).\n", savedX.Value, savedY.Value, savedZ.Value);
    }

    private void LoadPosition() {
      if (savedX.HasValue && savedY.HasValue && savedZ.HasValue) {
        addresses.PosX.ReadMemory = false;
        addresses.PosY.ReadMemory = false;
        addresses.PosZ.ReadMemory = false;
        addresses.PosX.Value = savedX.Value;
        addresses.PosY.Value = savedY.Value;
        addresses.PosZ.Value = savedZ.Value;
        addresses.PosX.ReadMemory = true;
        addresses.PosY.ReadMemory = true;
        addresses.PosZ.ReadMemory = true;
        log.Log += String.Format("Loaded position ({0}, {1}, {2}).\n", savedX.Value, savedY.Value, savedZ.Value);
      }
    }

    private void ButtonYUp_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
      isGoingUp = true;
    }

    private void ButtonYUp_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
      isGoingUp = false;
    }

    private void ButtonYDown_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
      isGoingDown = true;
    }

    private void ButtonYDown_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
      isGoingDown = false;
    }

    private void ButtonPosSave_Click(object sender, RoutedEventArgs e) {
      SavePosition();
    }

    private void ButtonPosLoad_Click(object sender, RoutedEventArgs e) {
      LoadPosition();
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
      Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
      e.Handled = true;
    }
  }

  class NotifyString : INotifyPropertyChanged {
    private string _log;
    public string Log {
      get => _log;
      set {
        _log = value;
        RaisePropertyChanged("Log");
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
