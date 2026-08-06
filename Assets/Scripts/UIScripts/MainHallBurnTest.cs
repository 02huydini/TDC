/*
    Dev-console test hook for the Main Hall fire VFX (see HealthBar.cs's
    "Nếu HP thấp hơn 30% thì Main Hall sẽ bốc cháy" trigger). DevConsole's
    "burnhall" command calls into this to preview the effect on demand,
    without needing to actually drain HealthBar.lives down to the real
    threshold first.

    Purely visual: this only flips HealthBar's forceBurnOverride flag, which
    the real VFX toggle (HealthBar.Update()) already OR's into its normal
    lives-based check - it never touches HealthBar.lives itself.

    Static wrapper, no MonoBehaviour/scene wiring needed - matches the rest of
    DevConsole's command set (wood/ecto/lives/timescale), which are all thin
    static passthroughs to whatever manager already owns the real state.
*/
public static class MainHallBurnTest {
    public static bool IsBurning => HealthBar.IsBurning;

    public static void Toggle() {
        HealthBar.SetForceBurn(!HealthBar.GetForceBurn());
    }

    public static void SetActiveState(bool on) {
        HealthBar.SetForceBurn(on);
    }
}
