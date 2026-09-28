# Puts the client build's window (process Plunderspell) beside the Editor so neither covers the
# other, then prints the primary monitor's rectangle as "x y width height" for ffmpeg to record.
# Used by Tools/Unity/coop_carry_check.sh. Prints "none" if either window is missing.
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
$editor = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like "PlunderSpell - *" } | Select-Object -First 1
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

# Left of the Editor if it fits there, otherwise below it. 0x4 = keep the z-order, 0x1 = keep the size.
if ($e.Left -ge $width) { $x = $e.Left - $width; $y = $e.Top } else { $x = $e.Left; $y = $e.Bottom }
# Both above every other window (-1 = topmost, 0x1 = keep the size, 0x2 = keep the place), so the
# recording shows the games rather than whatever else is open. The client closes at the end of the
# check; "place_windows.ps1 release" puts the Editor back to normal.
[Win]::SetWindowPos($client.MainWindowHandle, [IntPtr]-1, $x, $y, 0, 0, 0x1) | Out-Null
[Win]::SetWindowPos($editor.MainWindowHandle, [IntPtr]-1, 0, 0, 0, 0, 0x3) | Out-Null

Add-Type -AssemblyName System.Windows.Forms
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
# Even sizes: the H.264 encoder needs them.
"{0} {1} {2} {3}" -f $screen.X, $screen.Y, ($screen.Width -band -2), ($screen.Height -band -2)
