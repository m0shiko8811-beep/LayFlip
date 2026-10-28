using static LayFlip.App.NativeMethods;

namespace LayFlip.App;

internal sealed record HotkeyPreset(string Id, string DisplayName, uint Modifiers, uint VirtualKey)
{
    public static readonly HotkeyPreset CtrlAltZ = new("CtrlAltZ", "Ctrl+Alt+Z", MOD_CONTROL | MOD_ALT, 0x5A);
    public static readonly HotkeyPreset Pause = new("Pause", "Pause", 0, 0x13);
    public static readonly HotkeyPreset F8 = new("F8", "F8", 0, 0x77);
    public static readonly HotkeyPreset CtrlShiftSpace = new("CtrlShiftSpace", "Ctrl+Shift+Space", MOD_CONTROL | MOD_SHIFT, 0x20);

    public static readonly IReadOnlyList<HotkeyPreset> All = [CtrlAltZ, Pause, F8, CtrlShiftSpace];

    public static HotkeyPreset Default => CtrlAltZ;

    public static HotkeyPreset FromId(string? id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Default;
}
