using Microsoft.Xna.Framework;

namespace Celeste.Mod.LocksmithHelper;

public static class ColorExt {
    // Modified from https://stackoverflow.com/a/12985385
    public static Color ColorFromHSV(float hue, float sat, float val) {
        int i = (int)(hue * 6);
        float f = hue * 6 - i;
        float p = val * (1 - sat);
        float q = val * (1 - f * sat);
        float t = val * (1 - (1 - f) * sat);

        return (i % 6) switch{
            0 => new(val, t, p),
            1 => new(q, val, p),
            2 => new(p, val, t),
            3 => new(p, q, val),
            4 => new(t, p, val),
            _ => new(val, p, q),
        };
    }
}