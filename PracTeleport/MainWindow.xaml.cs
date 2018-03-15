using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PracTeleport {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {

    private bool isDone = false;
    private HwndSource source;
    private IntPtr interopHandle;

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

    private const int VK_F4 = 0x73, VK_F5 = 0x74, VK_F6 = 0x75, VK_F7 = 0x76;

    private HotkeyManager hkMgr;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public MainWindow() {
      InitializeComponent();

      this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
      this.Closing += MainWindow_Closed;
      this.SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object sender, EventArgs e) {
      IntPtr handle = new WindowInteropHelper(this).Handle;
      source = HwndSource.FromHwnd(handle);
      source.AddHook(HwndHook);

      this.interopHandle = handle;
      hkMgr = new HotkeyManager();
      hkMgr.ForEachRegisteredKey((k) => {
        Debug.WriteLine("Registering " + k.ToString("X"));
        RegisterHotKey(handle, HOTKEY_ID, 0, (uint)k);
      });
      /*RegisterHotKey(handle, HOTKEY_ID, 0, VK_F4);
      RegisterHotKey(handle, HOTKEY_ID, 0, VK_F5);
      RegisterHotKey(handle, HOTKEY_ID, 0, VK_F6);
      RegisterHotKey(handle, HOTKEY_ID, 0, VK_F7);*/
    }

    private void MainWindow_Closed(object sender, CancelEventArgs e) {
      this.isDone = true;
      e.Cancel = true;
    }

    void MainWindow_Loaded(object sender, RoutedEventArgs e) {

      Thread thread = new Thread(MemoryLoop);

      thread.Start();

    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
      switch (msg) {
        case WM_HOTKEY:
          switch (wParam.ToInt32()) {
            case HOTKEY_ID:
              int vkey = (((int)lParam >> 16) & 0xFFFF);
              Debug.WriteLine("Processing " + vkey.ToString("X"));
              hkMgr.ProcessHotkey(vkey, (o) => {
                this.Dispatcher.BeginInvoke(new Action(() => {
                  if(o is Button) {
                    ((Button)o).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                  } else if(o is CheckBox) {
                    CheckBox cb = (CheckBox)o;
                    cb.IsChecked = !cb.IsChecked;
                    cb.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                  } else if(o is ComboBox) {
                    ComboBox cmb = (ComboBox)o;
                    cmb.SelectedIndex = (cmb.SelectedIndex + 1) % cmb.Items.Count;
                  }
                }));
              });
              /*if (vkey == VK_F4) {
                //handle global hot key here...
                this.Dispatcher.BeginInvoke(new Action(() => {
                  btnSave.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }));
              } else if(vkey == VK_F5) {
                this.Dispatcher.BeginInvoke(new Action(() => {
                  btnLoad.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }));
              } else if(vkey == VK_F6) {
                this.Dispatcher.BeginInvoke(new Action(() => {
                  cbNoDeath.IsChecked = !cbNoDeath.IsChecked;
                  cbNoDeath.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                }));
              } else if (vkey == VK_F7) {
                this.Dispatcher.BeginInvoke(new Action(() => {
                  cbDeathcam.IsChecked = !cbDeathcam.IsChecked;
                  cbDeathcam.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                }));
              }*/
              handled = true;
              break;
          }
          break;
      }
      return IntPtr.Zero;
    }

    void MemoryLoop() {

      var mm = new MemoryManager();

      this.Dispatcher.BeginInvoke(new Action(() => {
        logBox.Text += "Welcome! Attaching to process... ";
        logBox.ScrollToEnd();
      }));

      while (!mm.Attach("DarkSoulsIII")) {
        Thread.Sleep(TimeSpan.FromSeconds(1));
      }

      this.Dispatcher.BeginInvoke(new Action(() => {
        logBox.Text += "done!\nScanning memory... ";
        logBox.ScrollToEnd();
      }));

      List<uint?> aobXA = new List<uint?> { 0x48, 0x8B, 0x83, null, null, null, null, 0x48, 0x8B, 0x10, 0x48, 0x85, 0xD2 };
      List<uint?> aobBaseB = new List<uint?> { 0x48, 0x8B, 0x1D, null, null, null, 0x04, 0x48, 0x8B, 0xF9, 0x48, 0x85, 0xDB, null, null, 0x8B, 0x11, 0x85, 0xD2, null, null, 0x8D };
      List<uint?> aobBaseF = new List<uint?> { 0x48, 0x8B, 0x05, null, null, null, null, 0x41, 0x0F, 0xB6 };
      List<uint?> aobDebug = new List<uint?> { 0x4C, 0x8D, 0x05, null, null, null, null, 0x48, 0x8D, 0x15, null, null, null, null, 0x48, 0x8B, 0xCB, 0xE8, null, null, null, null, 0x48, 0x83, 0x3D, null, null, null, null, 0x00 };
      List<uint?> aobSpeed = new List<uint?> { 0x88, 0x00, 0x00, 0x00, null, null, null, null, 0xC7, 0x86, 0xA0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0xBF, 0x33 };

      IntPtr? offsXA = mm.AobScan(aobXA, mm.baseAddr, 0x5ffffff);
      IntPtr? offsBaseB = mm.AobScan(aobBaseB, mm.baseAddr, 0x5ffffff);
      IntPtr? offsBaseF = mm.AobScan(aobBaseF, mm.baseAddr, 0x5ffffff);
      IntPtr? offsDebug = mm.AobScan(aobDebug, mm.baseAddr, 0x5ffffff);
      IntPtr? offsSpeed = mm.AobScan(aobSpeed, mm.baseAddr, 0x5ffffff);
      Int32 XA;
      Int64 BaseB, BaseF, AddrDebug, AddrSpeed;

      if (offsXA.HasValue && offsBaseB.HasValue && offsBaseF.HasValue) {
        this.Dispatcher.BeginInvoke(new Action(() => {
          logBox.Text += "Success.\n";
          logBox.ScrollToEnd();
        }));
        XA = mm.readInt32(new IntPtr(mm.baseAddr.ToInt64() + offsXA.Value.ToInt64() + 3));
        //Debug.WriteLine(XA.ToString("X"));

        BaseB = mm.baseAddr.ToInt64() + offsBaseB.Value.ToInt64();
        BaseB = BaseB + mm.readInt32(new IntPtr(BaseB + 3)) + 7;

        BaseF = mm.baseAddr.ToInt64() + offsBaseF.Value.ToInt64();
        BaseF = BaseF + mm.readInt32(new IntPtr(BaseF + 3)) + 7;

        AddrDebug = mm.baseAddr.ToInt64() + offsDebug.Value.ToInt64();
        AddrDebug = AddrDebug + mm.readInt32(new IntPtr(AddrDebug + 3)) + 7;
        Debug.WriteLine(AddrDebug.ToString("X"));

        AddrSpeed = mm.baseAddr.ToInt64() + offsSpeed.Value.ToInt64() + 4;

        List<Int64> pcNoDamage = new List<Int64> { BaseB, 0x80, 0x1a09 }; // bit 7
        // bit 2: no death; bit 4: no stamina cons; bit 5: no fp cons
        List<Int64> pcNoDeath  = new List<Int64> { BaseB, 0x80, XA, 0x18, 0x1C0 };
        List<Int64> pcNoConsume = new List<Int64> { BaseB, 0x80, 0x1EEA }; // bit 3
        List<Int64> pcDeathcam = new List<Int64> { BaseB, 0x90 }; // byte
        List<Int64> pcEventDraw  = new List<Int64> { BaseF, 0xA8 }; // byte
        List<Int64> pcEventDisable  = new List<Int64> { BaseF, 0xD4 }; // byte
        List<Int64> pcResidentSleeper = new List<Int64> { AddrDebug + 9 + 3 }; // byte
        List<Int64> pcOneShot = new List<Int64> { AddrDebug + 0 }; // byte
        List<Int64> pcX = new List<Int64> { BaseB, 0x40, 0x28, 0x80 }; // float
        List<Int64> pcY = new List<Int64> { BaseB, 0x40, 0x28, 0x88 }; // float
        List<Int64> pcZ = new List<Int64> { BaseB, 0x40, 0x28, 0x84 }; // float

        float? posX = null, posY = null, posZ = null;

        this.Dispatcher.BeginInvoke(new Action(() => {

          RoutedEventHandler clkSave = (object sender, RoutedEventArgs e) => {
            IntPtr? px, py, pz;
            px = mm.evalPointerChain(pcX);
            py = mm.evalPointerChain(pcY);
            pz = mm.evalPointerChain(pcZ);

            if(px.HasValue && py.HasValue && pz.HasValue) {
              posX = mm.readFloat(px.Value);
              posY = mm.readFloat(py.Value);
              posZ = mm.readFloat(pz.Value);

              tbX.Text = posX.ToString();
              tbY.Text = posY.ToString();
              tbZ.Text = posZ.ToString();
            } else {
              logBox.Text += "Error saving position\n";
              logBox.ScrollToEnd();
            }
          };

          RoutedEventHandler clkLoad = (object sender, RoutedEventArgs e) => {
            IntPtr? px, py, pz;
            px = mm.evalPointerChain(pcX);
            py = mm.evalPointerChain(pcY);
            pz = mm.evalPointerChain(pcZ);

            if (px.HasValue && py.HasValue && pz.HasValue) {
              if(posX.HasValue && posY.HasValue && posZ.HasValue) {
                mm.writeFloat(px.Value, posX.Value);
                mm.writeFloat(py.Value, posY.Value);
                mm.writeFloat(pz.Value, posZ.Value);
              } else {
                logBox.Text += "Can't load position - none saved!\n";
                logBox.ScrollToEnd();
              }
            }
            else {
              logBox.Text += "Error loading position\n";
              logBox.ScrollToEnd();
            }
          };

          btnSave.Click += clkSave;
          btnLoad.Click += clkLoad;

        }));

        CheckboxListener cblNoDamage = new CheckboxListener(cbNoDamage, pcNoDamage, this, mm, (state, addr) => {
          byte b = mm.readByte(addr);
          b = (byte)(state ? (b | 0b10000000) : (b & 0b01111111));
          mm.writeByte(addr, b);
        });

        // bit 2: no death; bit 4: no stamina cons; bit 5: no fp cons
        CheckboxListener cblNoDeath = new CheckboxListener(cbNoDeath, pcNoDeath, this, mm, (state, addr) => {
          byte b = mm.readByte(addr);
          b = (byte)(state ? (b | 0b00000100) : (b & 0b11111011));
          mm.writeByte(addr, b);
        });

        CheckboxListener cblNoStamina = new CheckboxListener(cbNoStamina, pcNoDeath, this, mm, (state, addr) => {
          byte b = mm.readByte(addr);
          b = (byte)(state ? (b | 0b00010000) : (b & 0b11101111));
          mm.writeByte(addr, b);
        });

        CheckboxListener cblNoFp = new CheckboxListener(cbNoFp, pcNoDeath, this, mm, (state, addr) => {
          byte b = mm.readByte(addr);
          b = (byte)(state ? (b | 0b00100000) : (b & 0b11011111));
          mm.writeByte(addr, b);
        });

        CheckboxListener cblNoConsume = new CheckboxListener(cbNoConsume, pcNoConsume, this, mm, (state, addr) => {
          byte b = mm.readByte(addr);
          b = (byte)(state ? (b | 0b00001000) : (b & 0b11110111));
          mm.writeByte(addr, b);
        });

        CheckboxListener cblDeathcam = new CheckboxListener(cbDeathcam, pcDeathcam, this, mm, (state, addr) => {
          byte b = mm.readByte(addr);
          b = (byte)(state ? 1 : 0);
          mm.writeByte(addr, b);
        });

        CheckboxListener cblEventDraw = new CheckboxListener(cbEventDraw, pcEventDraw, this, mm, (state, addr) => {
          byte b = mm.readByte(addr);
          b = (byte)(state ? 1 : 0);
          mm.writeByte(addr, b);
        });

        CheckboxListener cblEventDisable = new CheckboxListener(cbEventDisable, pcEventDisable, this, mm, (state, addr) => {
          byte b = mm.readByte(addr);
          b = (byte)(state ? 1 : 0);
          mm.writeByte(addr, b);
        });

        CheckboxListener cblResidentSleeper = new CheckboxListener(cbResidentSleeper, pcResidentSleeper, this, mm, (state, addr) => {
          //Debug.WriteLine(addr.ToString("X"));
          byte b = mm.readByte(addr);
          b = (byte)(state ? 1 : 0);
          mm.writeByte(addr, b);
        });

        CheckboxListener cblOneShot = new CheckboxListener(cbOneShot, pcOneShot, this, mm, (state, addr) => {
          //Debug.WriteLine(addr.ToString("X"));
          byte b = mm.readByte(addr);
          b = (byte)(state ? 1 : 0);
          mm.writeByte(addr, b);
        });

        hkMgr.SetObjectLabels(new Dictionary<string, object>() {
          { "save_position", btnSave },
          { "load_position", btnLoad },
          { "no_damage", cbNoDamage },
          { "no_death", cbNoDeath },
          { "no_consume", cbNoConsume },
          { "infinite_fp", cbNoFp},
          { "infinite_stamina", cbNoStamina },
          { "deathcam", cbDeathcam },
          { "event_draw", cbEventDraw },
          { "event_disable", cbEventDisable },
          { "ai_disable", cbResidentSleeper },
          { "one_shot", cbOneShot },
          { "speed", cbSpeed },
        });

        cbSpeed.SelectionChanged += (sender, e) => {
          float[] speeds = new float[] { 1.0f, 1.5f, 2.0f, 4.0f };
          int idx = (sender as ComboBox).SelectedIndex;
          float val = speeds[idx];
          mm.writeFloat((IntPtr)AddrSpeed, val);
        };

        int i = 0;
        while (!isDone) {

          Thread.Sleep(TimeSpan.FromMilliseconds(250));
          this.Dispatcher.BeginInvoke(new Action(() => {
            cblNoDamage.poll();
            cblNoDeath.poll();
            cblNoStamina.poll();
            cblNoFp.poll();
            cblNoConsume.poll();
            cblDeathcam.poll();
            cblEventDraw.poll();
            cblEventDisable.poll();
            cblResidentSleeper.poll();
            cblOneShot.poll();
          }));

          cblNoDamage.evaluate();
          cblNoDeath.evaluate();
          cblNoStamina.evaluate();
          cblNoFp.evaluate();
          cblNoConsume.evaluate();
          cblDeathcam.evaluate();
          cblEventDraw.evaluate();
          cblEventDisable.evaluate();
          cblResidentSleeper.evaluate();
          cblOneShot.evaluate();
        }

      }
      else {
        this.Dispatcher.BeginInvoke(new Action(() => {
          logBox.Text += "Could not find addresses: " + (offsXA ?? IntPtr.Zero).ToString("X") + " " + (offsBaseB ?? IntPtr.Zero).ToString("X") + ".\n";
          logBox.ScrollToEnd();
        }));
      }

//      Debug.WriteLine(mm.baseAddr.ToString("X"));

      this.Dispatcher.BeginInvoke(new Action(() => {
        logBox.Text += "Bye";
        logBox.ScrollToEnd();
      }));
      
      this.Dispatcher.BeginInvoke(new Action(() => {
        Application.Current.Shutdown();
      }));
    }
  }
}
