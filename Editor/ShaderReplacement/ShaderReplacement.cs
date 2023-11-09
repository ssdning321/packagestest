using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SYEfficiencyToolkit
{
    public class ShaderReplacement : ScriptableWizard
    {
        [MenuItem("����������/Ч�ʹ���/shader�滻")]
        public static void Open()
        {
            DisplayWizard<ShaderReplacement>("Shader�滻", "", "�滻");
        }

        public Shader originShader;
        public Shader replacementShader;
        public DefaultAsset defaultAsset;
        public string lod = "�滻�����ԭʼ��Դ,��ɲ�����ĺ��";
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
                lod = "���������Ƿ���Ч";
                return;
            }
            lod = "����ͨ�����";
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
                Debug.Log(item.path + "�滻�ɹ�");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

    }
}
