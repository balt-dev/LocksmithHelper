namespace Celeste.Mod.LocksmithHelper;

public class LocksmithHelperModuleSettings : EverestModuleSettings {

    [DefaultButtonBinding(Microsoft.Xna.Framework.Input.Buttons.RightStick, Microsoft.Xna.Framework.Input.Keys.LeftShift)]
    public ButtonBinding ReadyMaster {get; set;}

    [DefaultButtonBinding(Microsoft.Xna.Framework.Input.Buttons.LeftStick, Microsoft.Xna.Framework.Input.Keys.S)]
    public ButtonBinding LensOfTruth {get; set;}

    public bool UseShaders {get; set;} = true;

    [SettingNumberInput(false, 4)]
    public float CounterY {get; set;} = 200;
}