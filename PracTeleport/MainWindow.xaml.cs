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

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

    private const int VK_F4 = 0x73, VK_F5 = 0x74, VK_F6 = 0x75, VK_F7 = 0x76;

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

      RegisterHotKey(handle, HOTKEY_ID, 0, VK_F4);
      RegisterHotKey(handle, HOTKEY_ID, 0, VK_F5);
      RegisterHotKey(handle, HOTKEY_ID, 0, VK_F6);
      RegisterHotKey(handle, HOTKEY_ID, 0, VK_F7);
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
              if (vkey == VK_F4) {
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
                  cbIframes.IsChecked = !cbIframes.IsChecked;
                  cbIframes.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                }));
              } else if (vkey == VK_F7) {
                this.Dispatcher.BeginInvoke(new Action(() => {
                  cbDeathcam.IsChecked = !cbDeathcam.IsChecked;
                  cbDeathcam.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                }));
              }
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

      IntPtr? offsXA = mm.AobScan(aobXA, mm.baseAddr, 0x5ffffff);
      IntPtr? offsBaseB = mm.AobScan(aobBaseB, mm.baseAddr, 0x5ffffff);
      Int32 XA;
      Int64 BaseB;

      if (offsXA.HasValue && offsBaseB.HasValue) {
        this.Dispatcher.BeginInvoke(new Action(() => {
          logBox.Text += "Success.\n";
          logBox.ScrollToEnd();
        }));
        XA = mm.readInt32(new IntPtr(mm.baseAddr.ToInt64() + offsXA.Value.ToInt64() + 3));
        //Debug.WriteLine(XA.ToString("X"));

        BaseB = mm.baseAddr.ToInt64() + offsBaseB.Value.ToInt64();
        BaseB = BaseB + mm.readInt32(new IntPtr(BaseB + 3)) + 7;

        //List<Int64> addrHyperArmor = new List<Int64> { BaseB, 0x80, XA, 0x40, 0x10 }; // bit 0
        //List<Int64> addrNoDamage   = new List<Int64> { BaseB, 0x80, XA, 0x18, 0x1C0 }; // bit 1
        List<Int64> pcInvuln = new List<Int64> { BaseB, 0x80, 0x1a09 }; // bit 7
        List<Int64> pcDeathcam = new List<Int64> { BaseB, 0x90 }; // byte
        List<Int64> pcX = new List<Int64> { BaseB, 0x40, 0x28, 0x80 }; // float
        List<Int64> pcY = new List<Int64> { BaseB, 0x40, 0x28, 0x88 }; // float
        List<Int64> pcZ = new List<Int64> { BaseB, 0x40, 0x28, 0x84 }; // float

        var isIframesChecked = cbIframes.Dispatcher.Invoke(() => cbIframes.IsChecked);
        var isDeathcamChecked = cbDeathcam.Dispatcher.Invoke(() => cbDeathcam.IsChecked);

        //IntPtr? addrInvuln, addrDeathcam, addrX, addrY, addrZ;

        bool? isInvuln = null, isDeathcam = null;
        float? posX = null, posY = null, posZ = null;

        this.Dispatcher.BeginInvoke(new Action(() => {

          RoutedEventHandler callbackIframes = (object sender, RoutedEventArgs e) => {
            isInvuln = cbIframes.IsChecked;
          };

          cbIframes.Click += callbackIframes;
          //cbIframes.Unchecked += callbackIframes;

          RoutedEventHandler callbackDeathcam = (object sender, RoutedEventArgs e) => {
            isDeathcam = cbDeathcam.IsChecked;
          };

          cbDeathcam.Click += callbackDeathcam;
          //cbDeathcam.Unchecked += callbackDeathcam;

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

        while (!isDone) {

          if (isInvuln.HasValue) {
            IntPtr? addrInvuln = mm.evalPointerChain(pcInvuln);
            byte b;
            if (addrInvuln.HasValue) {
              b = mm.readByte(addrInvuln.Value);
              byte b1 = (byte)(isInvuln.Value ? (b | 0b10000000) : (b & 0b01111111));
              mm.writeByte(addrInvuln.Value, b1);

              this.Dispatcher.Invoke(() => {
                logBox.Text += "Iframes " + (isInvuln.Value ? "activated" : "deactivated") + "\n";
                logBox.ScrollToEnd();
                cbIframes.IsChecked = isInvuln.Value;
              });
            }
            isInvuln = null;

          }
          else {

            IntPtr? addrInvuln = mm.evalPointerChain(pcInvuln);
            if (addrInvuln.HasValue) {

              byte hasIframes = mm.readByte(addrInvuln.Value);
              int b = (hasIframes >> 7) & 0x01;
              this.Dispatcher.Invoke(() => {
                cbIframes.IsChecked = (b == 1);
              });
            }
          }

          if (isDeathcam.HasValue) {
            IntPtr? addrDeathcam = mm.evalPointerChain(pcDeathcam);
            if (addrDeathcam.HasValue) {
              byte b = (byte)(isDeathcam.Value ? 1 : 0);
              mm.writeByte(addrDeathcam.Value, b);
            }

            this.Dispatcher.Invoke(() => {
              logBox.Text += "Deathcam " + (isDeathcam.Value ? "activated" : "deactivated") + "\n";
              logBox.ScrollToEnd();
              cbDeathcam.IsChecked = isDeathcam.Value;
            });
            isDeathcam = null;
          }
          else {
            IntPtr? addrDeathcam = mm.evalPointerChain(pcDeathcam);
            if (addrDeathcam.HasValue) {

              byte hasDeathcam = mm.readByte(addrDeathcam.Value);
              this.Dispatcher.Invoke(() => {
                cbDeathcam.IsChecked = (hasDeathcam != 0);
              });
            }
          }
        }

        Thread.Sleep(TimeSpan.FromMilliseconds(250));

      }
      else {
        this.Dispatcher.BeginInvoke(new Action(() => {
          logBox.Text += "Could not find addresses: " + (offsXA ?? IntPtr.Zero).ToString("X") + " " + (offsBaseB ?? IntPtr.Zero).ToString("X") + ".\n";
          logBox.ScrollToEnd();
        }));
      }

      Debug.WriteLine(mm.baseAddr.ToString("X"));

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
