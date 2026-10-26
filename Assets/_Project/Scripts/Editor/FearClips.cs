using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>The fear clips by name, read from the file the import rules (#413) produce.</summary>
    public static class FearClips
    {
        public static IReadOnlyDictionary<string, AnimationClip> Load()
        {
            return AssetDatabase.LoadAllAssetsAtPath(ArtBibleModelImporter.FearClipsPath)
                .OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__"))
                .ToDictionary(clip => clip.name);
        }
    }
}
