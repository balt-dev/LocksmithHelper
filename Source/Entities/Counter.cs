
using System;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.LocksmithHelper.Entities;

[CustomEntity("LocksmithHelper/Counter", "KeyCounter")]
public class Counter : Entity {
    private static MTexture bg = GFX.Gui["strawberryCountBG"];
    private static MTexture key = GFX.Gui["LocksmithHelper/key"];

    public Counter() {
        Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
        Depth = -100;
        Y = 980;
    }

    public override void Update() {
        base.Update();
        textWidth = 0;
        foreach (var kvp in Key.Inventory.Where(kvp => kvp.Value.Count != 0)) {
            textWidth += ActiveFont.Measure(kvp.Value.ToString()).X + 84;
        }
        visualWidth += (textWidth - visualWidth) * (1f - (float) Math.Pow(0.0001, Engine.RawDeltaTime));
    }

    private float textWidth = 0;
    private float visualWidth = 0;

    public override void Render() {
        var x = visualWidth - textWidth;
        foreach (var kvp in Key.Inventory.Where(kvp => kvp.Value.Count != 0)) {
            key.Draw(new(x, Y), Vector2.Zero, kvp.Key.ForceToColor());
            x += 64;
            ActiveFont.DrawOutline(kvp.Value.ToString(), new(x, Y), Vector2.Zero, Vector2.One, Color.White, 2, Color.Black);
            x += ActiveFont.Measure(kvp.Value.ToString()).X + 20;
        }
    }
}