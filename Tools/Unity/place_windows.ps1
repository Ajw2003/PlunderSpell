# Puts the client build's window (process Plunderspell) beside the Editor so neither covers the
# other, then prints the primary monitor's rectangle as "x y width height" for ffmpeg to record.
# Used by Tools/Unity/coop_carry_check.sh. Prints "none" if either window is missing.
# "place_windows.ps1 beside" only moves the client beside the Editor, nothing on top (Tools/Unity/coop_local.sh).
Add-Type @"
using System; using System.Runtime.InteropServices;
public class Win
{
    public struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
"@

$mode = if ($args.Count -gt 0) { $args[0] } else { "place" }
# This checkout's Editor: its title starts with the project folder's name (PlunderSpell-lair in a worktree).
$project = Split-Path -Leaf (Resolve-Path (Join-Path $PSScriptRoot "..\.."))
$editor = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like "$project - *" } | Select-Object -First 1
if ($mode -eq "release")
{
    # -2 = no longer topmost.
    if ($editor) { [Win]::SetWindowPos($editor.MainWindowHandle, [IntPtr]-2, 0, 0, 0, 0, 0x3) | Out-Null }
    "released"; exit 0
}
$client = Get-Process Plunderspell -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $editor -or -not $client -or $client.MainWindowHandle -eq 0) { "none"; exit 0 }

$e = New-Object Win+Rect; [Win]::GetWindowRect($editor.MainWindowHandle, [ref]$e) | Out-Null
$c = New-Object Win+Rect; [Win]::GetWindowRect($client.MainWindowHandle, [ref]$c) | Out-Null
$width = $c.Right - $c.Left; $height = $c.Bottom - $c.Top

# Left of the Editor if it fits there. Otherwise (a maximized Editor) the bottom-left corner of the
# primary monitor, over the Editor's project and console panels rather than its game view.
Add-Type -AssemblyName System.Windows.Forms
$work = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
if ($e.Left - $work.Left -ge $width) { $x = $e.Left - $width; $y = $e.Top }
else { $x = $work.Left; $y = $work.Bottom - $height }
if ($mode -eq "beside")
{
    # For playing by hand (coop_local.sh): move the client beside the Editor and bring it forward, but leave
    # both as normal windows (0 = top of the ordinary windows, 0x1 = keep the size). Prints where it went.
    [Win]::SetWindowPos($client.MainWindowHandle, [IntPtr]0, $x, $y, 0, 0, 0x1) | Out-Null
    "client at $x,$y"; exit 0
}
# Both above every other window (-1 = topmost, 0x1 = keep the size, 0x2 = keep the place), so the
# recording shows the games rather than whatever else is open. The client closes at the end of the
# check; "place_windows.ps1 release" puts the Editor back to normal.
[Win]::SetWindowPos($client.MainWindowHandle, [IntPtr]-1, $x, $y, 0, 0, 0x1) | Out-Null
[Win]::SetWindowPos($editor.MainWindowHandle, [IntPtr]-1, 0, 0, 0, 0, 0x3) | Out-Null

Add-Type -AssemblyName System.Windows.Forms
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
# Even sizes: the H.264 encoder needs them.
"{0} {1} {2} {3}" -f $screen.X, $screen.Y, ($screen.Width -band -2), ($screen.Height -band -2)
