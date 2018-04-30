using PracticeTool.Model;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;

namespace PracticeTool.Controls {
  /// <summary>
  /// Interaction logic for DWordControl.xaml
  /// </summary>
  public partial class DWordControl : UserControl {
    public DWordControl() {
      InitializeComponent();
    }

    private static bool IsTextAllowed(string text) {
      Regex regex = new Regex("[^0-9-]+"); //regex that matches disallowed text
      return regex.IsMatch(text);
    }

    public void EvtPreviewTextInput(object sender, TextCompositionEventArgs e) {
      e.Handled = IsTextAllowed(e.Text);
    }

    private void TextBox_GotFocus(object sender, System.Windows.RoutedEventArgs e) {
      var c = (DWordField)this.DataContext;
      c.ReadMemory = false;
    }

    private void TextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e) {
      var c = (DWordField)this.DataContext;
      c.Refresh();
      c.ReadMemory = true;
      /*Thread t = new Thread(() => {
        Thread.Sleep(250);
        c.ReadMemory = true;
      });*/
    }
  }
}
