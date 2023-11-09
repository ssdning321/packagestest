using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SYEfficiencyToolkit
{
    public class ShaderReplacement : ScriptableWizard
    {
        [MenuItem("美术工具箱/效率工具/shader替换")]
        public static void Open()
        {
            DisplayWizard<ShaderReplacement>("Shader替换", "", "替换");
        }

        public Shader originShader;
        public Shader replacementShader;
        public DefaultAsset defaultAsset;
        public string lod = "替换会更新原始资源,造成不可逆的后果";
        public DefaultAsset doc;

        private void OnWizardUpdate()
        {
            string guidToPathString = AssetDatabase.GUIDToAssetPath("d99e11d9cfe636144810f0611f8aff3c");
            doc = AssetDatabase.LoadAssetAtPath<DefaultAsset>(guidToPathString);
        }
        private void OnWizardOtherButton()
        {
            if (!CheckVaild())
            {
                lod = "请检查输入是否有效";
                return;
            }
            lod = "输入通过检查";
            DO();

        }

        private bool CheckVaild()
        {
            if (originShader == null) return false;
            if (replacementShader == null) return false;
            if (defaultAsset == null) return false;
            if (Directory.Exists(AssetDatabase.GetAssetPath(defaultAsset)))
            {
                return true;
            }
            return false;
        }

        private void DO()
        {
            string foldPath = AssetDatabase.GetAssetPath(defaultAsset);
            var files = Directory.GetFiles(foldPath, "*.mat", SearchOption.AllDirectories);
            List<(Material mat, string path)> materials = new List<(Material mat, string path)>();
            foreach (var item in files)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(item);
                if (mat == null || mat.shader != originShader) continue;
                materials.Add((mat, item));
            }
            foreach (var item in materials)
            {
                item.mat.shader = replacementShader;
                Debug.Log(item.path + "替换成功");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

    }
}
