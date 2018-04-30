using Newtonsoft.Json;
using PracticeTool.Model;
using System.Collections.Generic;
using System.IO;

namespace PracticeTool.ViewModels {
  class HotkeyViewModel {
    private HotkeyManager hkm;
    public Hotkey NoDamage { get; set; }
    public Hotkey NoDeath { get; set; }
    public Hotkey NoStaminaCons { get; set; }
    public Hotkey NoFpCons { get; set; }
    public Hotkey NoGoodsCons { get; set; }
    public Hotkey NoCollision { get; set; }
    public Hotkey Deathcam { get; set; }
    public Hotkey EventDraw { get; set; }
    public Hotkey EventDisable { get; set; }
    public Hotkey AIDisable { get; set; }
    public Hotkey OneShot { get; set; }
    public Hotkey HideChar { get; set; }
    public Hotkey HideMap { get; set; }
    public Hotkey HideObj { get; set; }
    public Hotkey ChangeSpeed { get; set; }
    public Hotkey MoveUp { get; set; }
    public Hotkey MoveDown { get; set; }
    public Hotkey SavePos { get; set; }
    public Hotkey LoadPos { get; set; }

    private List<Hotkey> hotkeys;
    private HotkeyCodeFn registerHotkey;

    public delegate void HotkeyCodeFn(uint k);

    public HotkeyViewModel(HotkeyCodeFn reg) {
      hotkeys = new List<Hotkey>();
      registerHotkey = reg;
    }
     
    public void LoadConfig() {
      hotkeys = new List<Hotkey> {
        NoDamage, NoDeath, NoStaminaCons, NoFpCons,
        NoGoodsCons, NoCollision, Deathcam, EventDraw,
        EventDisable, AIDisable, OneShot, HideChar,
        HideMap, HideObj, MoveUp, MoveDown,
        SavePos, LoadPos, ChangeSpeed
      };

      hotkeys.ForEach(i => {
        i.PropertyChanged += (sender, e) => {
          if(e.PropertyName == "HotkeyCode") {
            Hotkey hk = (Hotkey)sender;
            int? kc = HotkeyManager.KeycodeFromLabel(hk.HotkeyCode);
            if(kc.HasValue)
              registerHotkey((uint)kc.Value);
            SaveConfig();
          }
        };
      });

      string jsons;
      if (!File.Exists("PracticeTool.json")) {
        var asm = this.GetType().Assembly;
        var stream = asm.GetManifestResourceStream("PracticeTool.Resources.defaultHotkeyConfig.json");
        var sr = new StreamReader(stream);
        File.WriteAllText("PracticeTool.json", sr.ReadToEnd());
      }

      jsons = File.ReadAllText("PracticeTool.json");

      Dictionary<string, string> conf = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsons);
      hotkeys.ForEach(i => {
        if(conf.TryGetValue(i.ConfigLabel, out string s)) {
          i.HotkeyCode = s;
        }
      });
    }

    public void SaveConfig() {
      Dictionary<string, string> conf = new Dictionary<string, string>();
      hotkeys.ForEach(i => {
        conf.Add(i.ConfigLabel, i.HotkeyCode);
      });

      string confstr = JsonConvert.SerializeObject(conf, Formatting.Indented);
      File.WriteAllText("PracticeTool.json", confstr);
    }

    public bool PerformAction(int hkcode) {
      List<Hotkey> found = hotkeys.FindAll(i => i.HotkeyCode == HotkeyManager.LabelFromKey(hkcode));
      found
        .ForEach(i => {
          i.PerformAction();
        });
      return found.Count > 0;
    }

    public void TriggerChange() {
      hotkeys.ForEach(i => {
        i.TriggerChange();
      });
    }
  }
}
