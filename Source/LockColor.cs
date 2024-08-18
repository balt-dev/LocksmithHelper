using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.LocksmithHelper;

public enum LockColor {
    White,
    Orange,
    Purple,
    Red,
    Green,
    Blue,
    Pink,
    Cyan,
    Black,
    Brown,
    Master,
    Pure,
    Glitch,
    Stone
}

public static class LockExt {
    public static Color? ToColor(this LockColor self) {
        return self switch {
            LockColor.White => new(0xeb, 0xe8, 0xe4),
            LockColor.Orange => new(0xdc, 0x8c, 0x32),
            LockColor.Purple => new(0xaa, 0x50, 0xc8),
            LockColor.Red => new(0xc8, 0x37, 0x37),
            LockColor.Green => new(0x35, 0x9f, 0x50),
            LockColor.Blue => new(0x5f, 0x71, 0xa0),
            LockColor.Pink => new(0xcf, 0x70, 0x9f),
            LockColor.Cyan => new(0x50, 0xaf, 0xaf),
            LockColor.Black => new(0x36, 0x30, 0x29),
            LockColor.Brown => new(0xaa, 0x60, 0x15),
            _ => null
        };
    }

    private static float glitchMul = 1.0f;

    public static Color ForceToColor(this LockColor self, bool rawGlitch = false) {
        if (Settings.Instance.DisableFlashes)
            glitchMul = 0.7f;
        else
            glitchMul = Calc.Random.NextFloat(0.6f) + 0.4f;
        var glitchCol = (Entities.Door.LastSpentColor != null && !rawGlitch ?
                ToColor((LockColor) Entities.Door.LastSpentColor) ?? new(0xA0, 0xA0, 0xA0) :
                new(0xA0, 0xA0, 0xA0)) * glitchMul;
        glitchCol.A = 255;
        return ToColor(self) ?? self switch {
            LockColor.Glitch => glitchCol,
            LockColor.Master => new(0xeb, 0xdd, 0x5e),
            LockColor.Pure => new(0xe6, 0xee, 0xf3),
            LockColor.Stone => new(0x80, 0x88, 0x90),
            _ => new(0xff, 0x00, 0xff) // Something's wrong!
        };
    }

    public static bool IsDark(this LockColor self) {
        return self is LockColor.Brown or LockColor.Black;
    }
}