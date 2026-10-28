using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.EditorTools;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    public sealed class RoomPartSplitterTests
    {
        private static readonly int[][] Faces =
        {
            new[] {0,2,1}, new[] {0,3,2}, new[] {4,5,6}, new[] {4,6,7}, new[] {0,1,5}, new[] {0,5,4},
            new[] {1,2,6}, new[] {1,6,5}, new[] {2,3,7}, new[] {2,7,6}, new[] {3,0,4}, new[] {3,4,7},
        };

        private static void AddBox(List<Vector3> v, List<int> t, Vector3 min, Vector3 size)
        {
            int b = v.Count;
            foreach (float y in new[] { 0f, 1f })
                foreach (var xz in new[] { (0f, 0f), (1f, 0f), (1f, 1f), (0f, 1f) })
                    v.Add(min + new Vector3(xz.Item1 * size.x, y * size.y, xz.Item2 * size.z));
            // Corner order is bottom 0-3 then top 4-7, so Faces indexes straight into it; reversed so faces point out, as
            // the imported rooms' do (the splitter tells a floor's top from a wall's foot by which way a face points).
            foreach (int[] f in Faces) { t.Add(b + f[0]); t.Add(b + f[2]); t.Add(b + f[1]); }
        }

        [Test]
        public void Classify_SyntheticRoom_AssignsEachBoxItsPart()
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            AddBox(v, t, new Vector3(0, 0, 0), new Vector3(12, 0.3f, 12));        // floor
            // Walls stand on the floor slab (room_kit.room_shell: wall_base = base_z + FLOOR_T) and meet the roof slab.
            AddBox(v, t, new Vector3(0, 0.3f, 0), new Vector3(12, 3.7f, 0.5f));    // walls
            AddBox(v, t, new Vector3(0, 0.3f, 11.5f), new Vector3(12, 3.7f, 0.5f));
            AddBox(v, t, new Vector3(0, 0.3f, 0), new Vector3(0.5f, 3.7f, 12));
            AddBox(v, t, new Vector3(11.5f, 0.3f, 0), new Vector3(0.5f, 3.7f, 12));
            AddBox(v, t, new Vector3(5.5f, 0.3f, 5.5f), Vector3.one);             // prop
            AddBox(v, t, new Vector3(0, 4, 0), new Vector3(12, 0.3f, 12));        // roof

            int[] part = RoomPartSplitter.Classify(v.ToArray(), t.ToArray());
            int[] expected = { RoomPartSplitter.Floor, RoomPartSplitter.Wall, RoomPartSplitter.Wall, RoomPartSplitter.Wall,
                               RoomPartSplitter.Wall, RoomPartSplitter.Props, RoomPartSplitter.Roof };
            Assert.AreEqual(expected.Length * 12, part.Length);
            for (int i = 0; i < part.Length; i++)
                Assert.AreEqual(expected[i / 12], part[i], $"box {i / 12}");
        }
    }
}
