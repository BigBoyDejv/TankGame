using UnityEngine;
using UnityEditor;

public class FixMaterials : MonoBehaviour
{
    [MenuItem("Tools/Fix URP Materials")]
    static void FixAllMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (!mat.shader.name.Contains("Universal Render Pipeline"))
            {
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                EditorUtility.SetDirty(mat);
            }

            // Skús znova priradiť hlavnú textúru
            Texture tex = mat.GetTexture("_MainTex");
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Hotovo!");
    }
}