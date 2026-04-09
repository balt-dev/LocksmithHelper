using System;
using System.Numerics;
using System.Text;

namespace Celeste.Mod.LocksmithHelper;

public class LocksmithHelperModuleSession: EverestModuleSession {
    public LockColor? LastSpentColor { get; set; } = null;
    
    public bool MasterKeyEnabled { get; set;  } = false;
    public bool LensOfTruthEnabled { get; set; } = false;
    
    public Complex WhiteCount { get; set; } = Complex.Zero;
    public bool WhiteLocked { get; set;  } = false;
    public Complex OrangeCount { get; set; } = Complex.Zero;
    public bool OrangeLocked { get; set; } = false;
    public Complex PurpleCount { get; set; } = Complex.Zero;
    public bool PurpleLocked { get; set; } = false;
    public Complex RedCount { get; set; } = Complex.Zero;
    public bool RedLocked { get; set; } = false;
    public Complex GreenCount { get; set; } = Complex.Zero;
    public bool GreenLocked { get; set; } = false;
    public Complex BlueCount { get; set; } = Complex.Zero;
    public bool BlueLocked { get; set; } = false;
    public Complex PinkCount { get; set; } = Complex.Zero;
    public bool PinkLocked { get; set; } = false;
    public Complex CyanCount { get; set; } = Complex.Zero;
    public bool CyanLocked { get; set; } = false;
    public Complex BlackCount { get; set; } = Complex.Zero;
    public bool BlackLocked { get; set; } = false;
    public Complex BrownCount { get; set; } = Complex.Zero;
    public bool BrownLocked { get; set; } = false;
    public Complex MasterCount { get; set; } = Complex.Zero;
    public bool MasterLocked { get; set; } = false;
    public Complex PureCount { get; set; } = Complex.Zero;
    public bool PureLocked { get; set; } = false;
    public Complex GlitchCount { get; set; } = Complex.Zero;
    public bool GlitchLocked { get; set; } = false;
    public Complex StoneCount { get; set; } = Complex.Zero;
    public bool StoneLocked { get; set; } = false;

    internal Slot GetSlot(LockColor color) => color switch
    {
        LockColor.White => new(() => WhiteCount, (value) => WhiteCount = value, () => WhiteLocked, (value) => WhiteLocked = value),
        LockColor.Orange => new(() => OrangeCount, (value) => OrangeCount = value, () => OrangeLocked, (value) => OrangeLocked = value),
        LockColor.Purple => new(() => PurpleCount, (value) => PurpleCount = value, () => PurpleLocked, (value) => PurpleLocked = value),
        LockColor.Red => new(() => RedCount, (value) => RedCount = value, () => RedLocked, (value) => RedLocked = value),
        LockColor.Green => new(() => GreenCount, (value) => GreenCount = value, () => GreenLocked, (value) => GreenLocked = value),
        LockColor.Blue => new(() => BlueCount, (value) => BlueCount = value, () => BlueLocked, (value) => BlueLocked = value),
        LockColor.Pink => new(() => PinkCount, (value) => PinkCount = value, () => PinkLocked, (value) => PinkLocked = value),
        LockColor.Cyan => new(() => CyanCount, (value) => CyanCount = value, () => CyanLocked, (value) => CyanLocked = value),
        LockColor.Black => new(() => BlackCount, (value) => BlackCount = value, () => BlackLocked, (value) => BlackLocked = value),
        LockColor.Brown => new(() => BrownCount, (value) => BrownCount = value, () => BrownLocked, (value) => BrownLocked = value),
        LockColor.Master => new(() => MasterCount, (value) => MasterCount = value, () => MasterLocked, (value) => MasterLocked = value),
        LockColor.Pure => new(() => PureCount, (value) => PureCount = value, () => PureLocked, (value) => PureLocked = value),
        LockColor.Glitch => new(() => GlitchCount, (value) => GlitchCount = value, () => GlitchLocked, (value) => GlitchLocked = value),
        LockColor.Stone => new(() => StoneCount, (value) => StoneCount = value, () => StoneLocked, (value) => StoneLocked = value),
        _ => throw new Exception($"Tried to get slot for nonexistent key color {color}!")
    };
    
    internal struct Slot(Func<Complex> countGetter, Action<Complex> countSetter, Func<bool> lockGetter, Action<bool> lockSetter) {
        public Complex Count { get => countGetter(); set => countSetter(value); }
        public bool Locked { get => lockGetter(); set => lockSetter(value); }
        
        public override string ToString() {
            StringBuilder sb = new(Count.AsString());
            if (Locked)
                sb.Append('*');
            return sb.ToString();
        }
    }
}