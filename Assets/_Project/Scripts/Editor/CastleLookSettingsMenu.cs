using Plunderspell.Atmosphere;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>Menu shortcut to the one asset that holds the castle's look (#277).</summary>
    public static class CastleLookSettingsMenu
    {
        private const string k_Path = "Assets/_Project/Settings/Atmosphere/NightAtmosphere.asset";

        [MenuItem("Tools/Plunderspell/Castle Look Settings")]
        public static void Select()
        {
            var asset = AssetDatabase.LoadAssetAtPath<NightAtmosphereProfile>(k_Path);
            if (asset == null)
            {
                Debug.LogError($"Castle look settings not found at {k_Path}");
                return;
            }
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
