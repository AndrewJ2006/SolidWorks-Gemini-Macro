# Gemini SolidWorks Task Pane Add-in

## Requirements

- 64-bit SolidWorks and a 64-bit .NET Framework 4.7.1 class library. The add-in also runs on machines with .NET Framework 4.8.
- Visual Studio 2022 with the .NET Framework 4.7.1 Developer Pack and Desktop development with .NET.
- Microsoft Edge WebView2 Runtime (Evergreen recommended).
- NuGet package `Microsoft.Web.WebView2`.
- SolidWorks interop assemblies matching the installed SolidWorks major version: `SolidWorks.Interop.sldworks`, `SolidWorks.Interop.swconst`, and `SolidWorks.Interop.swpublished`.

The SolidWorks interop assemblies are normally installed with SolidWorks under its `api\redist` directory. If package versions are not compatible with the installed release, add COM references in Visual Studio instead of mixing releases. The references must resolve to `SolidWorks.Interop.sldworks.dll`, `SolidWorks.Interop.swconst.dll`, and `SolidWorks.Interop.swpublished.dll`.

## Build setup

1. Open `GeminiSwAddin.csproj` in Visual Studio.
2. Select `Release`, platform `x64`, and target `.NET Framework 4.7.1`.
3. Restore `packages.config` from NuGet. It installs `Microsoft.Web.WebView2` version `1.0.1343.22`.
4. Remove `SolidWorksStubs.cs` from the project or set its Build Action to `None`.
5. Add references to `SolidWorks.Interop.sldworks` and `SolidWorks.Interop.swconst` from the SolidWorks API redistributable folder. Set `Embed Interop Types` to `False`.
6. Build. The output is under `bin\x64\Release\GeminiSwAddin.dll` (the exact folder can vary by configuration).

## Register with SolidWorks

Close SolidWorks before registration. Open an Administrator Command Prompt and run the .NET Framework `RegAsm.exe` matching the bitness of SolidWorks:

```text
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" "C:\path\to\GeminiSwAddin.dll" /codebase
```

The SolidWorks add-in descriptor is written machine-wide, while the startup preference is per-user. For an existing registration, unregister that DLL first:

```text
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" "C:\path\to\GeminiSwAddin.dll" /unregister
```

Do not use the 32-bit `Framework` RegAsm for 64-bit SolidWorks.

## Use in SolidWorks

1. Start SolidWorks and open a part, assembly, or drawing.
2. Open **Tools > Add-Ins**.
3. Enable **Gemini** in **Active Add-Ins**. Enable **Start Up** too if it should load automatically.
4. Select the Gemini tab in the right Task Pane. The first load creates a persistent WebView2 profile at `%LOCALAPPDATA%\GeminiSwAddin\WebView2UserData`.
5. Sign in to `gemini.google.com` once. Future sessions reuse that profile and its cookies.
6. With the desired model view visible, click **Capture & Paste Viewport**. The add-in exports the active viewport, places the image on the Windows clipboard, focuses Gemini's prompt, and sends `Ctrl+V`.

## Important behavior

The paste step relies on Gemini exposing a focused `textarea`, `contenteditable`, or ARIA textbox. Google can change that page markup, and browser security may prevent paste when the prompt is not ready. If the image does not attach, click the Gemini prompt once and click the capture button again. The SolidWorks viewport export and clipboard copy are still performed automatically.

The local `SolidWorksStubs.cs` file exists only so the skeleton can be inspected without SolidWorks. It is not suitable for deployment and must not be included in the real build.