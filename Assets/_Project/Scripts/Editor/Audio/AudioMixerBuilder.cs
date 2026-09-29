using System;
using System.Reflection;
using Plunderspell.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Creates <c>Assets/_Project/Audio/Plunderspell.mixer</c> with the group tree from docs/plans/audio.md
    /// section 2, four exposed volume parameters and the Default and Casting snapshots. Unity has no public
    /// API for authoring a mixer, so this drives the editor's own <c>AudioMixerController</c> through
    /// reflection. It never overwrites an existing mixer. See docs/4-systems/audio.md.
    /// </summary>
    public static class AudioMixerBuilder
    {
        public const string MixerPath = "Assets/_Project/Audio/Plunderspell.mixer";
        public const float CastingDipDb = -9f;

        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        [MenuItem("Plunderspell/Audio/Build Mixer")]
        public static void BuildMenu() => LoadOrBuild();

        /// <summary>Returns the mixer, building it first if the asset is missing.</summary>
        public static AudioMixer LoadOrBuild()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null)
                return existing;

            Assembly editorAssembly = typeof(Editor).Assembly;
            Type controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            Type pathType = editorAssembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            if (controllerType == null || pathType == null)
                throw new InvalidOperationException("This Unity version has no UnityEditor.Audio.AudioMixerController; the mixer cannot be authored from a script.");

            object controller = controllerType.GetMethod("CreateMixerControllerAtPath", Any).Invoke(null, new object[] { MixerPath });
            object master = controllerType.GetProperty("masterGroup", Any).GetValue(controller);

            object music = NewGroup(controller, controllerType, master, "Music");
            object sfx = NewGroup(controller, controllerType, master, "SFX");
            foreach (string name in new[] { "Spells", "Weapons", "World", "Foley", "Creatures", "Ambience" })
                NewGroup(controller, controllerType, sfx, name);
            object ui = NewGroup(controller, controllerType, master, "UI");

            Expose(controller, controllerType, pathType, master, AudioLevels.MasterParameter);
            Expose(controller, controllerType, pathType, music, AudioLevels.MusicParameter);
            Expose(controller, controllerType, pathType, sfx, AudioLevels.SfxParameter);
            Expose(controller, controllerType, pathType, ui, AudioLevels.UiParameter);

            // The mixer starts with one snapshot; make it Default, then clone it into Casting and dip Music and SFX.
            object[] snapshots = ((Array)controllerType.GetProperty("snapshots", Any).GetValue(controller)).Cast();
            Rename(snapshots[0], "Default");
            controllerType.GetMethod("CloneNewSnapshotFromTarget", Any).Invoke(controller, new object[] { false });
            snapshots = ((Array)controllerType.GetProperty("snapshots", Any).GetValue(controller)).Cast();
            object casting = snapshots[snapshots.Length - 1];
            Rename(casting, "Casting");
            SetSnapshotVolume(casting, music, CastingDipDb);
            SetSnapshotVolume(casting, sfx, CastingDipDb);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceUpdate);
            var built = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Debug.Log("[Audio] Built " + MixerPath);
            return built;
        }

        private static object[] Cast(this Array array)
        {
            var result = new object[array.Length];
            array.CopyTo(result, 0);
            return result;
        }

        private static object NewGroup(object controller, Type controllerType, object parent, string name)
        {
            object group = controllerType.GetMethod("CreateNewGroup", Any).Invoke(controller, new object[] { name, false });
            controllerType.GetMethod("AddChildToParent", Any).Invoke(controller, new[] { group, parent });
            return group;
        }

        private static void Expose(object controller, Type controllerType, Type pathType, object group, string parameterName)
        {
            object guid = group.GetType().GetMethod("GetGUIDForVolume", Any).Invoke(group, null);
            object path = Activator.CreateInstance(pathType, Any, null, new[] { group, guid }, null);
            controllerType.GetMethod("AddExposedParameter", Any).Invoke(controller, new[] { path });

            PropertyInfo parametersProperty = controllerType.GetProperty("exposedParameters", Any);
            Array parameters = (Array)parametersProperty.GetValue(controller);
            for (int i = 0; i < parameters.Length; i++)
            {
                object parameter = parameters.GetValue(i);
                FieldInfo guidField = parameter.GetType().GetField("guid", Any);
                if (!guidField.GetValue(parameter).Equals(guid))
                    continue;
                parameter.GetType().GetField("name", Any).SetValue(parameter, parameterName);
                parameters.SetValue(parameter, i);
                parametersProperty.SetValue(controller, parameters);
                return;
            }

            throw new InvalidOperationException("Exposed parameter for " + parameterName + " was not found after adding it.");
        }

        private static void Rename(object snapshot, string name) => ((UnityEngine.Object)snapshot).name = name;

        private static void SetSnapshotVolume(object snapshot, object group, float db)
        {
            object guid = group.GetType().GetMethod("GetGUIDForVolume", Any).Invoke(group, null);
            snapshot.GetType().GetMethod("SetValue", Any).Invoke(snapshot, new[] { guid, (object)db });
        }
    }
}
