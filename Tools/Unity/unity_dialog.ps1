# Prints each dialog this checkout's Unity Editor has open, one per line: 'Title' [Button, Button].
# Prints nothing when there is none. Never clicks anything. (#371)
# A modal dialog (e.g. "Scene(s) Have Been Modified") holds the Editor's main thread, so every
# `unity command` answers "Main thread operation timed out" until someone clicks.
# Usage: powershell -NoProfile -File Tools/Unity/unity_dialog.ps1 -Project PlunderSpell-lair
param([Parameter(Mandatory = $true)][string]$Project)

Add-Type @'
using System; using System.Text; using System.Runtime.InteropServices; using System.Collections.Generic;
public static class UnityDialogs
{
    delegate bool Visit(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(Visit visit, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, Visit visit, IntPtr data);
    [DllImport("user32.dll")] static extern int GetWindowThreadProcessId(IntPtr window, out int processId);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr window, StringBuilder text, int max);

    static string Text(IntPtr window) { var s = new StringBuilder(512); GetWindowText(window, s, 512); return s.ToString(); }
    static string ClassOf(IntPtr window) { var s = new StringBuilder(128); GetClassName(window, s, 128); return s.ToString(); }

    // "#32770" is the Windows class of a standard dialog box, which is what Unity's modal prompts are.
    public static List<string> OpenBy(int processId)
    {
        var found = new List<string>();
        EnumWindows((window, _) =>
        {
            int owner;
            GetWindowThreadProcessId(window, out owner);
            if (owner != processId || !IsWindowVisible(window) || ClassOf(window) != "#32770")
                return true;
            var buttons = new List<string>();
            EnumChildWindows(window, (child, __) =>
            {
                if (ClassOf(child) == "Button" && Text(child).Length > 0) buttons.Add(Text(child));
                return true;
            }, IntPtr.Zero);
            found.Add("'" + Text(window) + "' [" + string.Join(", ", buttons) + "]");
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
'@

# This checkout's Editor is the one whose main window title starts with the project folder's name.
$editors = Get-Process -Name Unity -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowTitle -like "$Project - *" }
foreach ($editor in $editors) {
    [UnityDialogs]::OpenBy($editor.Id) | ForEach-Object { Write-Output $_ }
}
