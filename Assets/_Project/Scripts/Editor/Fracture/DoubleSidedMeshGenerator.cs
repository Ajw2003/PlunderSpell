// Ported from ufo-3-rag-dahl FractureSystem (aj); see docs/plans/ophan-redo-plan.html.
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Plunderspell.Fracture.Editor
{
    public class DoubleSidedMeshGenerator
    {
        [MenuItem("Window/Fracture System/Generate Double-Sided Mesh")]
        public static void GenerateDoubleSidedMesh()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a GameObject with a MeshFilter.", "OK");
                return;
            }

            MeshFilter filter = selected.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected GameObject does not have a MeshFilter with a valid mesh.", "OK");
                return;
            }

            Mesh originalMesh = filter.sharedMesh;
            Mesh doubleSidedMesh = CreateDoubleSidedMesh(originalMesh);

            string directory = "Assets/_Project/Art/Models/Castle/Parts/Fracture";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                string[] parts = directory.Split('/');
                string currentPath = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(currentPath + "/" + parts[i]))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath += "/" + parts[i];
                }
            }

            string assetPath = $"{directory}/{originalMesh.name}_DoubleSided.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            AssetDatabase.CreateAsset(doubleSidedMesh, assetPath);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(filter, "Assign Double-Sided Mesh");
            filter.sharedMesh = doubleSidedMesh;
            
            MeshCollider collider = selected.GetComponent<MeshCollider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Assign Double-Sided Mesh");
                collider.sharedMesh = doubleSidedMesh;
            }

            EditorUtility.DisplayDialog("Success", $"Double-sided mesh generated and saved to {assetPath}.\nAssigned to MeshFilter and MeshCollider.", "OK");
        }

        private static Mesh CreateDoubleSidedMesh(Mesh original)
        {
            Vector3[] origVerts = original.vertices;
            Vector3[] origNorms = original.normals;
            Vector2[] origUvs = original.uv;
            
            int vertCount = origVerts.Length;
            
            Vector3[] newVerts = new Vector3[vertCount * 2];
            Vector3[] newNorms = new Vector3[vertCount * 2];
            Vector2[] newUvs = new Vector2[vertCount * 2];

            for (int i = 0; i < vertCount; i++)
            {
                // Front
                newVerts[i] = origVerts[i];
                newNorms[i] = origNorms[i];
                newUvs[i] = origUvs[i];
                
                // Back
                newVerts[i + vertCount] = origVerts[i];
                newNorms[i + vertCount] = -origNorms[i]; // Flip normal
                newUvs[i + vertCount] = origUvs[i];
            }

            Mesh newMesh = new Mesh();
            newMesh.name = original.name + "_DoubleSided";
            newMesh.vertices = newVerts;
            newMesh.normals = newNorms;
            newMesh.uv = newUvs;
            
            newMesh.subMeshCount = original.subMeshCount;
            for (int s = 0; s < original.subMeshCount; s++)
            {
                int[] origTris = original.GetTriangles(s);
                int triCount = origTris.Length;
                
                int[] newTris = new int[triCount * 2];
                for (int i = 0; i < triCount; i += 3)
                {
                    // Front face
                    newTris[i] = origTris[i];
                    newTris[i + 1] = origTris[i + 1];
                    newTris[i + 2] = origTris[i + 2];
                    
                    // Back face (inverted winding order, using the second set of vertices)
                    newTris[triCount + i] = origTris[i + 2] + vertCount;
                    newTris[triCount + i + 1] = origTris[i + 1] + vertCount;
                    newTris[triCount + i + 2] = origTris[i] + vertCount;
                }
                newMesh.SetTriangles(newTris, s);
            }

            newMesh.RecalculateBounds();
            
            return newMesh;
        }
    }
}
