
using System;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.LocksmithHelper.Entities;

[CustomEntity("LocksmithHelper/Counter", "KeyCounter")]
[Tracked]
public class Counter : Entity {
    private static MTexture bg = GFX.Gui["strawberryCountBG"];
    private static MTexture key = GFX.Gui["LocksmithHelper/key"];
    private static MTexture masterKey = GFX.Gui["LocksmithHelper/master_key"];
    private static MTexture glitchKey = GFX.Gui["LocksmithHelper/glitch_key"];


    public Counter() {
        Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
        Depth = -100;
        Y = LocksmithHelperModule.LockSettings.CounterY;
    }

    public override void Update() {
        base.Update();
        textWidth = 0;
        foreach (var kvp in Key.Inventory.Where(kvp => kvp.Value.Count != 0 || kvp.Value.Locked)) {
            if (textWidth == 0) textWidth += 40;
            textWidth += ActiveFont.Measure(kvp.Value.ToString()).X + 84;
        }
        visualWidth += (textWidth - visualWidth) * (1f - (float) Math.Pow(0.0001, Engine.RawDeltaTime));
    }

    private float textWidth = 0;
    private float visualWidth = 0;

    public override void Render() {
        if (visualWidth - bg.Width * 2 > 0)
            Draw.Rect(0, Y, visualWidth - bg.Width * 2 + 4, bg.Height * 2, Color.Black);
        bg.Draw(new(visualWidth - bg.Width * 2, Y), Vector2.Zero, Color.White, 2);

        X = visualWidth - textWidth + (textWidth > 0 ? 20 : 0);
        foreach (var kvp in Key.Inventory.Where(kvp => kvp.Value.Count != 0 || kvp.Value.Locked)) {
            (kvp.Key switch {
                LockColor.Master => masterKey,
                LockColor.Glitch => glitchKey,
                _ => key
            }).Draw(new(X, Y), Vector2.Zero, kvp.Key.ToColor(true));
            X += 64;
            ActiveFont.DrawOutline(kvp.Value.ToString(), new(X, Y), Vector2.Zero, Vector2.One, Color.White, 2, Color.Black);
            X += ActiveFont.Measure(kvp.Value.ToString()).X + 20;
        }
    }
}