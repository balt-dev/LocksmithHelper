using System.Numerics;
using Celeste.Mod.Entities;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Monocle;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text;

namespace Celeste.Mod.LocksmithHelper.Entities;

[CustomEntity("LocksmithHelper/Key", "LockpickKey")]
[Tracked]
public class Key : Entity {
    public LockColor KeyColor {get; private set;}
    public enum KeyType {
        Add,
        Multiply,
        Set,
        Star,
        Unstar
    }
    public KeyType Type {get; private set;}
    public Complex Value {get; private set;}

    private static readonly MTexture[] textures = [
        GFX.Game["objects/LocksmithHelper/key/normal"],
        GFX.Game["objects/LocksmithHelper/key/master"],
        GFX.Game["objects/LocksmithHelper/key/star"],
        GFX.Game["objects/LocksmithHelper/key/unstar"],
        GFX.Game["objects/LocksmithHelper/key/set"],
        GFX.Game["objects/LocksmithHelper/key/normal_outline"],
        GFX.Game["objects/LocksmithHelper/key/master_outline"],
        GFX.Game["objects/LocksmithHelper/key/star_outline"],
        GFX.Game["objects/LocksmithHelper/key/unstar_outline"],
        GFX.Game["objects/LocksmithHelper/key/set_outline"]
    ];

    public Key(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        Position -= new Vector2(8, 8); // fuck it
        Type = data.Enum("type", KeyType.Add);
        KeyColor = data.Enum("keyColor", LockColor.Orange);
        Value = ComplexExt.Parse(data.Attr("value", "1"));
        
        Collider = new Hitbox(16, 16);
        Add(new PlayerCollider(OnPlayer));
    }

    public record InventorySlot {
        public required Complex Count {get; set;}
        public required bool Locked {get; set;}

        public override string ToString() {
            StringBuilder sb = new(Count.AsString());
            if (Locked)
                sb.Append('*');
            return sb.ToString();
        }
    };

    public static readonly Dictionary<LockColor, InventorySlot> Inventory = [];


    public override void Render() {
        base.Render();

        var atlasIndex = Type switch {
            KeyType.Star => 2,
            KeyType.Unstar => 3,
            KeyType.Set => 4,
            _ when KeyColor == LockColor.Master => 1,
            _ => 0
        };

        var texture = textures[atlasIndex];
        var outline = textures[atlasIndex + 5];

        texture.DrawCentered(Center, KeyColor.ForceToColor());
        outline.DrawCentered(Center);
        if (Type != KeyType.Star && Type != KeyType.Unstar)
            Door.DrawComplex(Value, BottomRight - new Vector2(Value.AsString().Length * 4 - 2, 0), Color.White, true, Type == KeyType.Multiply, false);
    }

    private void OnPlayer(Player player) {
        RemoveSelf();
        Audio.Play("event:/game/03_resort/door_wood_open", Center);
        for (int i = 0; i < 6; i++) {
            float num = Calc.Random.NextFloat(MathF.PI * 2f);
            (Scene as Level).ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + new Vector2(8, 8) + Calc.AngleToVector(num, 4f), Vector2.Zero, num);
        }

        var color = KeyColor == LockColor.Glitch ? Door.LastSpentColor ?? KeyColor : KeyColor;

        var slot = Inventory[color];
        if (slot.Locked) {
            if (Type == KeyType.Unstar)
                slot.Locked = false;
            return;
        }

        switch (Type) {
            case KeyType.Add:
                slot.Count += Value;
                break;
            case KeyType.Multiply:
                slot.Count *= Value;
                break;
            case KeyType.Set:
                slot.Count = Value;
                break;
            case KeyType.Star:
                slot.Locked = true;
                break;
        }
    }
}
