using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using KianaPet;

class SkinSizeTests {
    const string Kiana = "kiana-fiery-wishing-star-round";
    const string Front = "haibara-ai-front-q";
    const string Side = "haibara-ai-side-q";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static int checks;

    static void Check(bool ok, string label) {
        if (!ok) throw new Exception(label);
        checks++;
    }
    static T Field<T>(object target, string name) {
        return (T)target.GetType().GetField(name, Private).GetValue(target);
    }
    static void ConfigChecks() {
        string legacy = "{\"Schema\":1,\"Size\":112,\"Skin\":\"" + Kiana + "\"}";
        var old = PreferenceReview.Parse(legacy);
        Check(PetCatalog.Load().All(s=>PetSizeSettings.Effective(old.Preferences,s.Id)==112),
              "Old settings must preserve the shared width on every skin.");
        var settings = PreferenceTransfer.Copy(old.Preferences);
        settings.SkinSizes[Front] = 131;
        settings.SkinSizes[Side] = 187;
        var package = PreferenceReview.Parse(PreferenceTransfer.Export(settings,false));
        var merged = PreferenceReview.Merge(new Config(),package,false,false);
        Check(merged.Size==112 && merged.SkinSizes[Front]==131 && merged.SkinSizes[Side]==187,
              "Portable preferences must include both shared and per-skin sizes.");
        Check(PreferenceReview.Merge(merged,old,false,false).SkinSizes.Count==2,
              "An old import without a skin-size field must keep existing overrides.");
        var sizes = PreferenceReview.Differences(old.Preferences,merged).Single(d=>d.Key=="SkinSizes");
        Check(sizes.After.Contains(PetCatalog.Name(Front)) && sizes.After.Contains("131"),
              "Import preview must identify the skin and its saved size.");
        var one = PreferenceTransfer.Copy(old.Preferences);
        one.SkinSizes[Front] = 131;
        var history = new EditHistory();
        var now = DateTime.Now;
        history.Record(old.Preferences,one,now);
        history.Record(one,merged,now.AddMilliseconds(250));
        Check(history.Items.Count==2 && history.Items[0].Label.Contains(PetCatalog.Name(Side))
              && history.Items[1].Label.Contains(PetCatalog.Name(Front)),
              "Rapid edits to different skins must remain separate undo operations.");
        merged.SkinSizes[Front] = 321;
        merged.SkinSizes["missing-skin"] = 130;
        merged.Validate();
        Check(PetSizeSettings.Effective(merged,Front)==merged.Size && merged.SkinSizes.Count==1
              && merged.SkinSizes[Side]==187, "Invalid sizes or outdated skin IDs must not damage valid overrides.");
    }

    static void Layout(PetWindow pet) {
        pet.UpdateLayout();
        pet.Dispatcher.Invoke(new Action(delegate{}), DispatcherPriority.ApplicationIdle);
        pet.UpdateLayout();
    }
    static Native.Rect Bounds(PetWindow pet) {
        Native.Rect rect;
        Check(Native.GetWindowRect(new WindowInteropHelper(pet).Handle,out rect),"Missing window bounds.");
        return rect;
    }
    static void CheckWidth(PetWindow pet, int width, string label) {
        Layout(pet);
        var sprite = Field<Image>(pet,"sprite");
        double dpi=PresentationSource.FromVisual(sprite).CompositionTarget.TransformToDevice.M11;
        Check(sprite.Width==width && Math.Abs(sprite.ActualWidth-width)*dpi<=1
              && Math.Abs(sprite.Height-width*208.0/192)<.02,
              label + " expected " + width + ", got Width=" + sprite.Width + ", ActualWidth=" +
              sprite.ActualWidth + ", Height=" + sprite.Height + ", window width=" + pet.ActualWidth + ".");
    }
    static void Anchor(Native.Rect a, Native.Rect b, string label) {
        Check(Math.Abs(a.Bottom-b.Bottom)<=1 && Math.Abs(a.Left+a.Right-b.Left-b.Right)<=2,
              label + " moved the foot anchor from (" + (a.Left+a.Right)/2.0 + "," + a.Bottom
              + ") to (" + (b.Left+b.Right)/2.0 + "," + b.Bottom + ").");
    }
    static void Runtime(PetWindow pet, SettingsWindow settings) {
        CheckWidth(pet,112,"Initial Kiana");
        pet.SetSkinSize(Side,201);
        CheckWidth(pet,112,"Edit inactive side-view skin");

        settings.NavigateCategory("外观与互动","");
        settings.UpdateLayout();
        Field<ComboBox>(settings,"appearancePreviewSkin").SelectedValue=Front;
        var slider=Field<Slider>(settings,"appearanceSkinSizeSlider");
        slider.Value=241;
        settings.UpdateLayout();
        Check(pet.Settings.Skin==Kiana && !pet.Settings.SkinSizes.ContainsKey(Front)
              && Field<Image>(settings,"appearancePreviewImage").Width==241,
              "Editing the preview slider must show a true-size preview without saving or changing skin.");
        Field<Button>(settings,"appearanceSaveSkinSize").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(Store.Load().SkinSizes[Front]==241 && pet.Settings.Size==112,
              "Save via UI must affect only the selected preview skin.");
        CheckWidth(pet,112,"Save inactive front-view skin");

        var area=Forms.Screen.FromHandle(new WindowInteropHelper(pet).Handle).WorkingArea;
        var initial=Bounds(pet);
        var handle=new WindowInteropHelper(pet).Handle;
        Native.SetWindowPos(handle,IntPtr.Zero,area.Left+(area.Width-initial.Width)/2,
            area.Top+Math.Max(0,(area.Height-initial.Height)*2/3),0,0,0x0015);
        var anchor=Bounds(pet);
        for(int i=0;i<4;i++){
            pet.SelectSkin(Front);
            CheckWidth(pet,241,"Manual switch to Haibara");
            Anchor(anchor,Bounds(pet),"Manual switch to Haibara");
            var automatic = typeof(PetWindow).GetMethod("ApplyAutomaticSkin",Private);
            Check((bool)automatic.Invoke(pet,new object[]{Kiana,2.0+i}),"Automatic skin switch failed.");
            CheckWidth(pet,112,"Automatic switch back to Kiana");
            Anchor(anchor,Bounds(pet),"Switch back to Kiana");
        }
        pet.SelectSkin(Front);
        CheckWidth(pet,241,"Manual switch before changing shared width");
        pet.SetSharedSize(122);
        CheckWidth(pet,241,"Dedicated skin preserved after shared sizing");
        settings.UpdateStatus();
        Field<ComboBox>(settings,"appearancePreviewSkin").SelectedValue=Front;
        Field<Button>(settings,"appearanceInheritSize").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        CheckWidth(pet,122,"Clearing the active override via UI");
        Check(!Store.Load().SkinSizes.ContainsKey(Front) && Store.Load().SkinSizes[Side]==201,
              "Restoring defaults must persist just that skin's removal.");

        Native.SetWindowPos(handle,IntPtr.Zero,area.Left,area.Top,0,0,0x0015);
        pet.SetSkinSize(Front,320);
        CheckWidth(pet,320,"Largest supported sprite");
        var clamped=Bounds(pet);
        Check(clamped.Left>=area.Left && clamped.Top>=area.Top && clamped.Right<=area.Right+1 &&
              clamped.Bottom<=area.Bottom+1,"Growing at the screen edge must keep the pet in its monitor's work area.");

        var imported=PreferenceTransfer.Copy(pet.Settings);
        imported.Skin="haibara-ai";
        imported.SkinSizes["haibara-ai"]=151;
        imported.X=area.Right-30; imported.Y=area.Bottom-30;
        pet.ReplacePreferences(imported,false);
        CheckWidth(pet,151,"Import a non-Q skin size without importing position");
        Anchor(clamped,Bounds(pet),"Import without position");
        pet.SetSkinSize("haibara-ai",165);
        CheckWidth(pet,165,"Edit imported non-Q size");
        pet.UndoLastEdit();
        CheckWidth(pet,151,"Undo must resize the actual non-Q pet");
        Check(Store.Load().SkinSizes["haibara-ai"]==151,"Undo must persist the previous skin size.");
    }
    [STAThread]
    static int Main(string[] args) {
        Store.Root=args[0];
        string result=Path.Combine(Store.Root,"result.txt");
        try {
            ConfigChecks();
            Store.Save(new Config{Size=112,Skin=Kiana,MusicEnabled=false,LinkChatGPT=false,
                Walking=false,Gravity=false,HideFullscreen=false,AmbientActions=false,SkinShuffleEnabled=false,
                SkinShuffleOnStart=false,IdleSleep=false,SleepSchedule=false,AutoScenesEnabled=false,
                SmoothTransitions=false,Bubbles=false,LaunchChatGPTOnStart=false,LaunchMusicOnStart=false,
                SkinShuffleActions=new string[0]});
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
            var pet=new PetWindow(true){ShowActivated=false};
            Exception failure=null;
            pet.Loaded+=delegate {
                pet.Dispatcher.BeginInvoke(new Action(delegate {
                    try {
                        Field<DispatcherTimer>(pet,"animation").Stop();
                        Runtime(pet,app.Windows.OfType<SettingsWindow>().Single());
                    }catch(Exception e){failure=e;}
                    finally {
                        foreach(var window in app.Windows.OfType<SettingsWindow>().ToArray())window.Close();
                        pet.Close();
                    }
                }),DispatcherPriority.ApplicationIdle);
            };
            app.Run(pet);
            if(failure!=null)throw failure;
            File.WriteAllText(result,"PASS per-skin sizes: " + checks + " checks");
            return 0;
        }catch(Exception e){File.WriteAllText(result,e.ToString());return 1;}
    }
}
