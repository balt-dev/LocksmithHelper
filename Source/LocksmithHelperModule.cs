using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.LocksmithHelper;

public class LocksmithHelperModule : EverestModule {
    public static LocksmithHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(LocksmithHelperModuleSettings);
    public static LocksmithHelperModuleSettings LockSettings => (LocksmithHelperModuleSettings) Instance._Settings;

    public LocksmithHelperModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(LocksmithHelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(LocksmithHelperModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        // TODO: apply any hooks that should always be active
        On.Celeste.Level.ctor += OnCreate;
        On.Celeste.Level.Update += OnUpdate;
        On.Celeste.Level.LoadLevel += OnLoad;
        On.Celeste.Player.Update += OnPlayerUpdate;
        On.Celeste.Player.Render += OnPlayerRender;
    }

    public override void Unload() {
        // TODO: unapply any hooks applied in Load()
        On.Celeste.Level.ctor -= OnCreate;
        On.Celeste.Level.Update -= OnUpdate;
        On.Celeste.Level.LoadLevel -= OnLoad;
        On.Celeste.Player.Update -= OnPlayerUpdate;
        On.Celeste.Player.Render -= OnPlayerRender;
    }

    private static void OnLoad(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        orig(self, playerIntro, isFromLoader);
        Init();
    }

    private static void Init() {
        Entities.Door.LastSpentColor = null;
        foreach (LockColor color in Enum.GetValues<LockColor>()) {
            Entities.Key.Inventory[color] = new() {
                Count = 0,
                Locked = false
            };
        }
        MasterKeyReady = false;
    }

    private static void OnCreate(On.Celeste.Level.orig_ctor orig, Level self)
    {
        orig(self);
        UpdateShaders(self);
    }


    private static void OnUpdate(On.Celeste.Level.orig_Update orig, Level self)
    {
        orig(self);
        UpdateShaders(self);

        if (LockSettings.ReadyMaster.Pressed) {
            LockSettings.ReadyMaster.ConsumePress();
            MasterKeyReady = !MasterKeyReady;
        }

        if (LockSettings.LensOfTruth.Pressed) {
            LockSettings.LensOfTruth.ConsumePress();
            ImaginaryView = !ImaginaryView;
        }
    }

    private static void OnPlayerRender(On.Celeste.Player.orig_Render orig, Player self)
    {
        if (MasterKeyReady) {
            var keyWillCopy = Entities.Key.Inventory[LockColor.Master].Count.Real < 0;
            Draw.Rect(self.TopCenter.X - 2, self.TopCenter.Y - 12, 4, 4, keyWillCopy ? Color.White : Color.Black);
            var color = LockColor.Master.ForceToColor();
            if (keyWillCopy)
                color = new(255 - color.R, 255 - color.G, 255 - color.B);
            Draw.Rect(self.TopCenter.X - 1, self.TopCenter.Y - 11, 2, 2, color);
        }
        if (ImaginaryView) {
            Draw.HollowRect(self.TopCenter.X - 3, self.TopCenter.Y - 13, 6, 6, Color.Magenta);
        }
        orig(self);
    }

    private static void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
    {
        orig(self);
        if (!self.Scene.OnInterval(0.1f)) return;
        if (Entities.Key.Inventory[LockColor.Brown].Count.Real > 0)
            self.SceneAs<Level>().ParticlesFG.Emit(
                Entities.Door.CurseParticle,
                self.Center + new Vector2(Calc.Random.NextFloat(32) - 16, Calc.Random.NextFloat(32) - 16)
            );
        if (Entities.Key.Inventory[LockColor.Brown].Count.Real < 0)
            self.SceneAs<Level>().ParticlesFG.Emit(
                new(Entities.Door.CurseParticle) {Color = new Color(0xff - 0xaa, 0xff - 0x60, 0xff - 0x15) * 0.3f},
                self.Center + new Vector2(Calc.Random.NextFloat(32) - 16, Calc.Random.NextFloat(32) - 16)
            );
        if (Entities.Key.Inventory[LockColor.Red].Count.Real >= 1)
            self.SceneAs<Level>().ParticlesFG.Emit(
                new(Entities.Door.CurseParticle) {Color = LockColor.Red.ForceToColor() * 0.3f},
                self.Center + new Vector2(Calc.Random.NextFloat(32) - 16, Calc.Random.NextFloat(32) - 16)
            );
        if (Entities.Key.Inventory[LockColor.Green].Count.Real >= 5)
            self.SceneAs<Level>().ParticlesFG.Emit(
                new(Entities.Door.CurseParticle) {Color = LockColor.Green.ForceToColor() * 0.3f},
                self.Center + new Vector2(Calc.Random.NextFloat(32) - 16, Calc.Random.NextFloat(32) - 16)
            );
        if (Entities.Key.Inventory[LockColor.Blue].Count.Real >= 3)
            self.SceneAs<Level>().ParticlesFG.Emit(
                new(Entities.Door.CurseParticle) {Color = LockColor.Blue.ForceToColor() * 0.3f},
                self.Center + new Vector2(Calc.Random.NextFloat(32) - 16, Calc.Random.NextFloat(32) - 16)
            );
    }

    private static void UpdateShaders(Level self) {
        MasterFx.Parameters["Time"].SetValue(self.TimeActive);
        PureFx.Parameters["Time"].SetValue(self.TimeActive);
        GlitchFx.Parameters["Time"].SetValue(self.TimeActive);
        GlitchFx.Parameters["Photosensitive"].SetValue(Settings.Instance.DisableFlashes);
        GlitchFx.Parameters["MimicColor"].SetValue(Entities.Door.LastSpentColor?.ForceToColor().ToVector4() ?? Vector4.Zero);
    }

    public static Effect GlitchFx;
    public static Effect MasterFx;
    public static Effect PureFx;
    public static Effect StoneFx;

    public override void LoadContent(bool firstLoad)
    {
        base.LoadContent(firstLoad);
        
        // Due to NOT only doing this on firstLoad, this works with hot reloading!
        GlitchFx = new Effect(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"Shaders/glitch.cso", true).Data);
        MasterFx = new Effect(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"Shaders/master.cso", true).Data);
        PureFx = new Effect(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"Shaders/pure.cso", true).Data);
        StoneFx = new Effect(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"Shaders/stone.cso", true).Data);
    }

    public static bool _masterReady;
    public static bool _imaginaryView;
    public static bool MasterKeyReady {get => _masterReady; private set {
        if (value == _masterReady) return;
        if (Entities.Key.Inventory[LockColor.Master].Count.RealWithView() == 0) return;
        if (value)
            Audio.Play("event:/game/03_resort/door_metal_open");
        else
            Audio.Play("event:/game/03_resort/door_metal_close");
        _masterReady = value;
    }}

    
    public static bool ImaginaryView {get => _imaginaryView; private set {
        if (value == _imaginaryView) return;
        if (value)
            Audio.Play("event:/game/general/assist_screenbottom");
        else
            Audio.Play("event:/game/04_cliffside/whiteblock_fallthru");
        _imaginaryView = value;
        _masterReady = false;
    }}

    [Command("keys", "Shows the player's key inventory.")]
    public static void Inventory() {
        StringBuilder sb = new("Inventory:");
        foreach (var kvp in Entities.Key.Inventory) {
            sb.Append($" {kvp.Key.ToString()}: {kvp.Value.Count.AsString()}");
            if (kvp.Value.Locked)
                sb.Append(" (Locked)");
        }
        Engine.Commands.Log(sb.ToString());
    }
}