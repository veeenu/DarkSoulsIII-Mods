using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PracTeleport {
  class CheckboxListener {
    public delegate void Callback(bool b, IntPtr addr);

    private MemoryManager mm;
    private MainWindow w;
    private List<Int64> pc;
    private CheckBox cb;
    private Callback callback;
    private ConcurrentQueue<Tuple<bool, IntPtr>> q;

    public CheckboxListener(CheckBox cb_, List<Int64> pc_, MainWindow w_, MemoryManager mm_, Callback callback_) {
      q = new ConcurrentQueue<Tuple<bool, IntPtr>>();
      mm = mm_;
      pc = pc_;
      cb = cb_;
      w = w_;
      callback = callback_;
    }

    public void poll() {
      //w.Dispatcher.BeginInvoke(new Action(() => {
      if (cb.IsChecked.HasValue) {
        IntPtr? addr = mm.evalPointerChain(pc);
        if (addr.HasValue && q.IsEmpty)
          q.Enqueue(new Tuple<bool, IntPtr>(cb.IsChecked.Value, addr.Value));
      }
      //}));
    }

    public void evaluate() {
      Tuple<bool, IntPtr> t = new Tuple<bool, IntPtr>(false, IntPtr.Zero);
      if(q.TryDequeue(out t)) {
        callback(t.Item1, t.Item2);
      }
        
    }
  }
}
