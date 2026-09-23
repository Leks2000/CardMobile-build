#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [V] Editor-only: bakes <see cref="ProcSprites"/> into Assets/Resources/Generated/*.png (9-sliced sprites)
/// and the outlined Chewy material. Re-run any time: Tools/Visual/Bake Generated UI Assets.
/// </summary>
public static class VisualAssetBuilder
{
    public const string Dir = "Assets/Resources/Generated";

    [MenuItem("Tools/Visual/Bake Generated UI Assets")]
    public static string BakeAll()
    {
        Directory.CreateDirectory(Dir);
        var sb = new System.Text.StringBuilder();
        foreach (var spec in ProcSprites.All())
        {
            var tex = ProcSprites.Generate(spec);
            var path = Dir + "/" + spec.name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = spec.name == ProcSprites.Fog ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spriteBorder = spec.border;
            imp.SaveAndReimport();
            sb.AppendLine(path);
        }

        var font = UiTheme.Font;
        if (font != null)
        {
            var matPath = Dir + "/Chewy-Outline.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(font.material) { name = "Chewy-Outline" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = font.material.shader;
                mat.CopyPropertiesFromMaterial(font.material);
            }
            VisualTheme.ConfigureOutline(mat);
            EditorUtility.SetDirty(mat);
            sb.AppendLine(matPath);
        }
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }
}
#endif
