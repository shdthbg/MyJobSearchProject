using UnityEngine;
using UnityEditor;

public class BatchFixURPWhiteMaterial : EditorWindow
{
    [MenuItem("Tools/URP修复/一键修复选中材质（补Albedo）")]
    static void FixSelectedMaterials()
    {
        int count = 0;
        foreach (var obj in Selection.objects)
        {
            if (obj is Material mat)
            {
                // 1. 确保Shader是URP Lit/Unlit
                if (mat.shader.name.Contains("Universal Render Pipeline/Lit") ||
                    mat.shader.name.Contains("Universal Render Pipeline/Unlit"))
                {
                    // 2. 尝试从旧材质属性中读取主纹理（内置Shader的_MainTex对应URP的_BaseMap）
                    Texture mainTex = mat.GetTexture("_MainTex"); // 内置Shader的主纹理
                    if (mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex); // 赋值给URP的Albedo纹理
                        mat.SetColor("_BaseColor", Color.white); // 重置基础色为白色（避免偏色）
                        EditorUtility.SetDirty(mat);
                        count++;
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 修复 {count} 个材质的Albedo纹理！");
    }

    [MenuItem("Tools/URP修复/批量替换粒子材质为URP")]
    static void FixParticleMaterials()
    {
        // 创建URP粒子材质（若不存在）
        Material urpParticleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/URP_Particle.mat");
        if (urpParticleMat == null)
        {
            urpParticleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(urpParticleMat, "Assets/URP_Particle.mat");
            urpParticleMat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(urpParticleMat);
        }

        // 查找所有粒子系统并替换材质
        ParticleSystem[] particles = Object.FindObjectsOfType<ParticleSystem>();
        int count = 0;
        foreach (var ps in particles)
        {
            ps.GetComponent<ParticleSystemRenderer>().material = urpParticleMat;
            count++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 替换 {count} 个粒子系统为URP材质！");
    }
}