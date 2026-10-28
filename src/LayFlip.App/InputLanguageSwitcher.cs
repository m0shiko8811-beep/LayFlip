using LayFlip.Core;
using static LayFlip.App.NativeMethods;

namespace LayFlip.App;

/// <summary>Asks a window to switch its input language, if the wanted layout is installed.</summary>
internal static class InputLanguageSwitcher
{
    private const int LangHebrew = 0x040D;
    private const int LangEnglishUs = 0x0409;
    private const int PrimaryLangEnglish = 0x09;

    /// <summary>Returns the layout that was requested, or IntPtr.Zero if none was.</summary>
    public static IntPtr SwitchTo(IntPtr hwnd, ConversionDirection direction)
    {
        if (hwnd == IntPtr.Zero)
            return IntPtr.Zero;

        var layout = direction == ConversionDirection.EnglishToHebrew ? FindHebrew() : FindEnglish();
        if (layout == IntPtr.Zero)
            return IntPtr.Zero;

        return PostMessage(hwnd, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, layout) ? layout : IntPtr.Zero;
    }

    private static IntPtr FindHebrew() =>
        InstalledLayouts().FirstOrDefault(h => LangId(h) == LangHebrew);

    private static IntPtr FindEnglish()
    {
        var layouts = InstalledLayouts();
        var us = layouts.FirstOrDefault(h => LangId(h) == LangEnglishUs);
        return us != IntPtr.Zero ? us : layouts.FirstOrDefault(h => (LangId(h) & 0x3FF) == PrimaryLangEnglish);
    }

    private static int LangId(IntPtr hkl) => (int)((long)hkl & 0xFFFF);

    private static IntPtr[] InstalledLayouts()
    {
        var count = GetKeyboardLayoutList(0, null);
        if (count <= 0)
            return [];
        var list = new IntPtr[count];
        var filled = GetKeyboardLayoutList(count, list);
        return list.Take(Math.Max(filled, 0)).ToArray();
    }
}
