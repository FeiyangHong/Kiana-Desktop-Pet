using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KianaPet;

class CharacterAnimationTests {
    public sealed class Track {
        public string File { get; set; }
        public int Columns { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public int Duration { get; set; }
        public int[] Order { get; set; }
        public int[] Start { get; set; }
    }

    static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    static int tracksChecked, samplesChecked;

    static void Assert(bool ok, string message) {
        if (!ok) throw new InvalidDataException(message);
    }
    static void Field(PetWindow pet, string name, object value) {
        typeof(PetWindow).GetField(name, PrivateInstance).SetValue(pet, value);
    }
    static void Call(PetWindow pet, string name, params object[] args) {
        typeof(PetWindow).GetMethod(name, PrivateInstance).Invoke(pet, args);
    }
    static BitmapSource Atlas(string path) {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
    static string Hash(BitmapSource pixels, Int32Rect rect) {
        var normalized = new FormatConvertedBitmap(pixels, PixelFormats.Pbgra32, null, 0);
        int stride = rect.Width * 4;
        byte[] buffer = new byte[stride * rect.Height];
        normalized.CopyPixels(rect, buffer, stride, 0);
        using (var hash = SHA256.Create()) {
            return Convert.ToBase64String(hash.ComputeHash(buffer));
        }
    }
    static void CheckPose(BitmapSource pose, Track track, string expected, string where) {
        Assert(pose != null && pose.PixelHeight == track.FrameHeight, where + " lost resolution");
        Assert(pose.PixelWidth * 208 == pose.PixelHeight * 192, where + " wrong stage aspect");
        int left = (pose.PixelWidth - track.FrameWidth) / 2;
        Assert(left >= 0, where + " source wider than stage");
        string hash = Hash(pose, new Int32Rect(left, 0, track.FrameWidth, track.FrameHeight));
        Assert(hash == expected, where + " character pixels disagree with the approved atlas");
    }
    static void SelectSkin(PetWindow pet, string id) {
        pet.Settings.Skin = id;
        Call(pet, "LoadSkin");
        pet.ResizePet();
    }
    static BitmapSource RuntimeFrame(PetWindow pet, string id, double frameMs) {
        double elapsed = frameMs;
        string state = id;
        string previous = "";
        if (id == "music-sit") {
            state = "music-quiet";
        } else if (id == "music-rise") {
            state = "music-gentle";
            previous = "music-quiet";
        } else if (id == "music-quiet") {
            elapsed += AnimationAssets.Duration(pet.Settings.Skin, "music-sit");
        }
        Field(pet, "state", state);
        Field(pet, "drawnMusicState", previous);
        Field(pet, "musicRising", false);
        Call(pet, "DrawAnimation", elapsed);
        var sprite = (Image)typeof(PetWindow).GetField("sprite", PrivateInstance).GetValue(pet);
        return sprite.Source as BitmapSource;
    }
    static void CheckTrack(PetWindow pet, string skin, string directory, string key, Track track) {
        string style = key == "music-quiet-sway" ? "sway" : key == "music-quiet-nod" ? "nod" : "classic";
        string id = key.StartsWith("music-quiet") ? "music-quiet" : key;
        pet.Settings.MusicSeatedStyle = style;
        Assert(AnimationAssets.Supports(skin, id), skin + " missing " + id);
        Assert(AnimationAssets.Duration(skin, id, style) == track.Duration, skin + " duration " + key);
        var action = AnimationGallery.Catalog.Single(c => c.Id == id);
        var preview = new AnimationPreviewSource(skin, style);
        var atlas = Atlas(Path.Combine(directory, track.File));
        // The first/middle/final authored frames may use different hold lengths, even within a skin.
        foreach (int i in new[]{0, track.Order.Length / 2, track.Order.Length - 1}) {
            int cell = track.Order[i];
            double stop = i + 1 < track.Start.Length ? track.Start[i + 1] : track.Duration;
            double ms = (track.Start[i] + stop) / 2.0;
            var crop = new Int32Rect(cell % track.Columns * track.FrameWidth,
                cell / track.Columns * track.FrameHeight, track.FrameWidth, track.FrameHeight);
            string expected = Hash(atlas, crop);
            string note = skin + "/" + key + " frame " + i;
            CheckPose(preview.Frame(action, ms).Source, track, expected, "preview " + note);
            CheckPose(RuntimeFrame(pet, id, ms), track, expected, "desktop " + note);
            if (i == 0) {
                CheckPose(preview.Frame(action, ms + track.Duration).Source, track, expected, "loop " + note);
                if (id != "music-sit" && id != "music-rise") {
                    CheckPose(RuntimeFrame(pet, id, ms + track.Duration), track, expected, "desktop loop " + note);
                }
            }
            samplesChecked++;
        }
        tracksChecked++;
    }
    static MenuItem DrinkMenuItem(PetWindow pet) {
        var menu = (ContextMenu)typeof(PetWindow).GetMethod("BuildPetMenu", PrivateInstance).Invoke(pet, null);
        var group = menu.Items.OfType<MenuItem>().Single(item => Convert.ToString(item.Header) == "互动与休息");
        return group.Items.OfType<MenuItem>().SingleOrDefault(item => Convert.ToString(item.Header) == "喝口饮料");
    }
    static void Legacy(PetWindow pet) {
        string plain = "kiana-winter-wish";
        SelectSkin(pet, plain);
        Assert(DrinkMenuItem(pet) == null, "unsupported drink appears in the legacy menu");
        var plainPreview = new AnimationPreviewSource(plain);
        var pat = AnimationGallery.Catalog.Single(c => c.Id == "pat");
        var groom = AnimationGallery.Catalog.Single(c => c.Id == "groom");
        var pos = plainPreview.Frame(groom, 250);
        Assert(pos.Source != null && pos.Source.PixelWidth == 192, "legacy base fallback is unavailable");
        var desktop = RuntimeFrame(pet, "pat", 250);
        var legacyPat = plainPreview.Frame(pat, 250);
        var full = new Int32Rect(0, 0, 192, 208);
        Assert(Hash(desktop, full) == Hash(legacyPat.Source, full), "legacy touch fallback mismatch");
        Assert(!MusicSprites.Supported(plain), "plain Kiana has no seated artwork");
        string round = "time-runner-kiana-round";
        SelectSkin(pet, round);
        var detail = new AnimationPreviewSource(round).Frame(groom, 900);
        Assert(detail.Source != null && detail.Source.PixelWidth == 384, "legacy ambient artwork is unavailable");
        Assert(MusicSprites.Supported(round), "legacy Q music missing");
    }
    [STAThread] static int Main(string[] args) {
        try {
            Store.Root = args[0];
            Store.Save(new Config { LinkChatGPT=false, MusicEnabled=false, LaunchChatGPTOnStart=false,
                LaunchMusicOnStart=false, SkinShuffleEnabled=false, Walking=false, SmoothTransitions=false,
                ReduceMotion=false, Bubbles=false });
            // Never show the window. Loaded starts native hooks and app integrations.
            var pet = new PetWindow(true);
            pet.Settings.AnimationSpeeds["music-quiet"] = 1;
            var skins = PetCatalog.Load();
            string[] ids = { "haibara-ai", "haibara-ai-front-q", "haibara-ai-side-q" };
            var expected = new HashSet<string>(AnimationGallery.Catalog.Select(c => c.Id));
            expected.Add("music-quiet-sway");
            expected.Add("music-quiet-nod");
            foreach (string id in ids) {
                var skin = skins.Single(p => p.Id == id);
                SelectSkin(pet, id);
                var drink = DrinkMenuItem(pet);
                Assert(drink != null && drink.IsEnabled, id + " missing drink in the interaction menu");
                drink.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Assert((string)typeof(PetWindow).GetField("interaction", PrivateInstance).GetValue(pet) == "drink",
                    id + " interaction menu did not trigger the drink");
                string manifest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", skin.AnimationManifest);
                var tracks = Store.Json.Deserialize<Dictionary<string, Track>>(File.ReadAllText(manifest));
                Assert(expected.SetEquals(tracks.Keys), id + " action set does not match the gallery");
                foreach (var item in tracks) CheckTrack(pet, id, Path.GetDirectoryName(manifest), item.Key, item.Value);
            }
            Legacy(pet);
            Assert(tracksChecked == 69, "incomplete three-character coverage");
            Console.WriteLine("PASS character-animation-tests: {0} tracks, {1} first/middle/final samples in both renderers, preview and desktop loops, drink menu, 2 legacy skins.", tracksChecked, samplesChecked);
            return 0;
        } catch (Exception error) {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
