# LayFlip

A small Windows tray app that fixes text you typed in the wrong keyboard layout, between English and Hebrew.

You meant to type a Hebrew word, but the keyboard was set to English, so you got:

    akuo

Press the hotkey and it becomes:

    שלום

It works the other way too, from Hebrew back to English.

## Download

Get `LayFlip.exe` from the [Releases page](https://github.com/m0shiko8811-beep/LayFlip/releases). It is a single file and does not need .NET installed. Run it and an icon appears in the system tray.

Windows may show a SmartScreen warning because the exe is not code signed. Click More info, then Run anyway.

## How to use

1. Select the wrong text, or just leave the caret at the end of the line.
2. Press **Ctrl+Alt+Z**.

The text is replaced with the fixed version, and the keyboard is switched to the matching language.

To use a different key, right-click the tray icon and open **Hotkey**. The tray menu also has **Start with Windows**, which starts LayFlip when you sign in, and **Exit**.

## Limitations

- It does not work inside programs that run as administrator.
- The automatic keyboard language switch needs the Hebrew keyboard layout to be installed in Windows.
- Known issue: rarely (about 1 press in 200) nothing happens and the text stays as it was. Press the hotkey again.

## Build from source

Requires the .NET 10 SDK.

```
dotnet build LayFlip.sln
dotnet test LayFlip.sln
dotnet publish src/LayFlip.App/LayFlip.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

The result is `dist\LayFlip.exe`.

## Troubleshooting log

Set the environment variable `LAYFLIP_LOG` to `1` before starting LayFlip. Each hotkey press is then logged to `%TEMP%\LayFlip.log`. Only text lengths are logged, never the text itself.

## License

MIT. See [LICENSE](LICENSE).
