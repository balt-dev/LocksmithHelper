using System;
using System.Text;
using Celeste.Mod.LocksmithHelper.Entities;
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
        Logger.SetLogLevel(nameof(LocksmithHelper), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(LocksmithHelper), LogLevel.Info);
#endif
    }

    public override void Load() {
        On.Celeste.Level.Update += OnUpdate;
        On.Celeste.Level.LoadLevel += OnLoad;
        On.Celeste.Player.Update += OnPlayerUpdate;
        On.Celeste.Player.Render += OnPlayerRender;
    }

    public override void Unload() {
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

    public static int AtlasOffset { get; internal set; } = 0;

    private static void OnUpdate(On.Celeste.Level.orig_Update orig, Level self)
    {
        orig(self);

        if (LockSettings.ReadyMasterKey.Pressed) {
            LockSettings.ReadyMasterKey.ConsumePress();
            MasterKeyReady = !MasterKeyReady;
        }

        if (LockSettings.LensOfTruth.Pressed) {
            LockSettings.LensOfTruth.ConsumePress();
            ImaginaryView = !ImaginaryView;
        }

        if (!Settings.Instance.DisableFlashes || self.OnInterval(0.4f)) {
            // Random number from 0 to 4, exclusive, non-repeating
            var value = Calc.Random.Next(3);
            if (value >= AtlasOffset)
                value += 1;
            AtlasOffset = value;
        }
    }

    private static void OnPlayerRender(On.Celeste.Player.orig_Render orig, Player self)
    {
        if (MasterKeyReady) {
            var keyWillCopy = Entities.Key.Inventory[LockColor.Master].Count.RealWithView() < 0;
            Draw.Rect(self.TopCenter.X - 2, self.TopCenter.Y - 12, 4, 4, keyWillCopy ? Color.White : Color.Black);
            var color = LockColor.Master.ToColor();
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
                new(Entities.Door.CurseParticle) {Color = LockColor.Red.ToColor() * 0.3f},
                self.Center + new Vector2(Calc.Random.NextFloat(32) - 16, Calc.Random.NextFloat(32) - 16)
            );
        if (Entities.Key.Inventory[LockColor.Green].Count.Real >= 5)
            self.SceneAs<Level>().ParticlesFG.Emit(
                new(Entities.Door.CurseParticle) {Color = LockColor.Green.ToColor() * 0.3f},
                self.Center + new Vector2(Calc.Random.NextFloat(32) - 16, Calc.Random.NextFloat(32) - 16)
            );
        if (Entities.Key.Inventory[LockColor.Blue].Count.Real >= 3)
            self.SceneAs<Level>().ParticlesFG.Emit(
                new(Entities.Door.CurseParticle) {Color = LockColor.Blue.ToColor() * 0.3f},
                self.Center + new Vector2(Calc.Random.NextFloat(32) - 16, Calc.Random.NextFloat(32) - 16)
            );
    }

    public static bool _masterReady;
    public static bool _imaginaryView;
    public static bool MasterKeyReady {get => _masterReady; private set {
        if (((Engine.Scene as Level)?.Tracker?.CountEntities<Entities.Door>() ?? 0) == 0) {
            Logger.Log(LogLevel.Info, nameof(LocksmithHelper), "Tried to enable master key with no doors in room! Suppressing...");
            _masterReady = false;
            return;
        }
        if (value == _masterReady) return;
        if (Entities.Key.Inventory[LockColor.Master].Count.RealWithView() == 0) {
            _masterReady = false;
            return;
        }
        if (value)
            Audio.Play("event:/game/03_resort/door_metal_open");
        else
            Audio.Play("event:/game/03_resort/door_metal_close");
        _masterReady = value;
    }}

    
    public static bool ImaginaryView {get => _imaginaryView; private set {
        if (((Engine.Scene as Level)?.Tracker?.CountEntities<Entities.Door>() ?? 0) == 0) {
            Logger.Log(LogLevel.Info, nameof(LocksmithHelper), "Tried to enable I-View with no doors in room! Suppressing...");
            _imaginaryView = false;
            return;
        }
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