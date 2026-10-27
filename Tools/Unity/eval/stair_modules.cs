// Play mode only. Prints each stair module's id, root position and yaw, one per line: "id x y z yaw".
var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>();
var sb = new System.Text.StringBuilder();
foreach (var m in d.Castle.PlacedModules)
    if (m.Storeys > 1)
        sb.Append(m.RoomId + " " + m.Position.x.ToString("F2") + " " + m.Position.y.ToString("F2") + " " + m.Position.z.ToString("F2")
            + " " + m.Rotation.eulerAngles.y.ToString("F0") + "\n");
return sb.ToString().TrimEnd();
