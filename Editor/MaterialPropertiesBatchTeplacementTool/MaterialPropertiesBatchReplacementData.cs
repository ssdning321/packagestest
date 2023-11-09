
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SYEfficiencyToolkit
{
    public class MaterialData
    {
        //private GameObject gameObject;
        private static int count;
        private List<Material> mats;//一直存在  去处理这个
        private List<(Material mat,string name)> showMats;
        private ReorderableList reorderableList;
        private List<PropertiesGroup> propertiesGroups;
        private bool fold = true;


        public static int Count
        {
            get => count;
            set => count = value;
        }

        private ReorderableList reorderableListEmpty;
        private List<char> empty;
        
        public MaterialData(Material material,string name)
        {
            count = 1;
            mats = new List<Material>();
            showMats = new List<(Material mat, string name)>();
            mats.Add(material);
            showMats.Add((material,name));
            if (count < 1) count = 1;
            SetMaterialPropertyValuesBase();
            SetReorderableList();
        }

        public void Show(Rect rect)
         {
             if (propertiesGroups != null)
             {
                 if(fold)
                 reorderableList.DoList(rect);
                 else reorderableListEmpty.DoList(rect);
             }
             else
             {
                 GUILayout.Label("该预制体中无法查找到选定Material");
             }
         }
         public float GetHeight()=> fold ? reorderableList.GetHeight() : reorderableListEmpty.GetHeight();
         
         public void AddNewMaterial(GameObject gameObject)
         {
             Material newMaterial = Object.Instantiate(mats[0]);
             var rs = gameObject.GetComponentsInChildren<Renderer>();
             foreach (var r in rs)
             {
                 if (r.sharedMaterial == mats[0]) r.sharedMaterial = newMaterial;
             }
             mats.Add(newMaterial);
         }

         public void RemoveMaterial()
         {
             var mat = mats[mats.Count - 1];
             if (AssetDatabase.GetAssetPath(mat) != "")
             {
                 AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(mat));
             }
             Object.DestroyImmediate(mat);
             mats.RemoveAt(mats.Count - 1);
         }
         
         public void Dispose(GameObject[] gameObjects)
         {
             var valuesOfAll = new List<PropertiesValues>();

             foreach (var variable in propertiesGroups)
             {
                 valuesOfAll.AddRange(variable.GetData());
             }
             if (mats != null)
             {
                 for (int i = 0; i < count; ++i)
                 {
                     for (int j = i+1; j < count; ++j)
                     {
                         if (mats[i] == mats[j]) continue;
                         bool equal = true;
                         foreach (var variable in valuesOfAll)
                         {
                             if (!variable.EqualValue(mats[i], mats[j]))
                             {
                                 equal = false;
                                 break;
                             }
                         }
                         if (equal)
                         {
                             var rs = gameObjects[j].GetComponentsInChildren<Renderer>();
                             foreach (var r in rs)
                             {
             
                                 if (r.sharedMaterial == mats[j])
                                 {
                                     r.sharedMaterial = mats[i];
                                     Debug.Log(mats[i]);
                                 }
                             }
             
                             AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(mats[j]));
                             Object.DestroyImmediate(mats[j],false);
                             mats[j] = mats[i];
                         }
                     }
                 }
             }
             mats = null;
         }

         public void ChangePresent(List<bool> bs,List<GameObject> gameObjects)
         {
             showMats.Clear();
             for (int i = 0; i < mats.Count; ++i)
             {
                 if(bs[i]) showMats.Add((mats[i],gameObjects[i].name));
             }
             ResetFold();
         }
         
         private void DrawHeaderCallBack(Rect rect)
         {
             fold = EditorGUI.BeginFoldoutHeaderGroup(new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height),
                 fold, "材质");
             EditorGUI.BeginDisabledGroup(true);
             EditorGUI.ObjectField(new Rect(rect.x + rect.width*0.4f, rect.y, rect.width * 0.4f, rect.height), mats[0],
                 typeof(Material));
             EditorGUI.EndDisabledGroup();
             EditorGUI.EndFoldoutHeaderGroup();
             
             EditorGUI.BeginChangeCheck();
         }

         // private void InitRenderers(GameObject gameObject,Material mat)
         // {
         //     renderers = new List<Renderer>();
         //     var rs = gameObject.GetComponentsInChildren<Renderer>();
         //     foreach (var r in rs)
         //     {
         //         if (r.sharedMaterial == mat)
         //         {
         //             renderers.Add(r);
         //         }
         //     }
         //     material = Object.Instantiate(mat);
         // }
         
         
         private void SetMaterialPropertyValuesBase()
         {
             propertiesGroups = new List<PropertiesGroup>();
             Dictionary<string,(string name ,List<PropertiesValues> data)> dictionary=new Dictionary<string, (string ,List<PropertiesValues>)>();
             dictionary.Add("无分组",("无分组" ,new List<PropertiesValues>()));
            if (mats[0] == null|| mats[0].shader==null) return;
            var shader = mats[0].shader;
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
            {
                PropertiesValues valueBase;
                //GUILayout.Label(propertyName);

                if (shader.GetPropertyFlags(i) == ShaderPropertyFlags.HideInInspector) continue;
                string[] attributes = shader.GetPropertyAttributes(i);
                bool next = false;
                foreach (var attribute in attributes)
                {
                    if (attribute.IndexOf("CustomGroup") > -1 )
                    {
                        dictionary.Add(shader.GetPropertyName(i),(shader.GetPropertyDescription(i),new List<PropertiesValues>()));
                        next = true;
                        break;
                    }

                    if (attribute.IndexOf("CustomHeader") > -1)
                    {
                        next = true;
                        break;
                    }
                }
                
                if(next) continue;
                
                switch (ShaderUtil.GetPropertyType(shader, i))
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        if (shader.GetPropertyFlags(i) == ShaderPropertyFlags.HDR)
                        {
                            
                            valueBase = new PropertiesValueColorHDR(shader.GetPropertyDescription(i),
                                shader.GetPropertyNameId(i));
                        }
                        else
                        {
                            valueBase = new PropertiesValueColor(shader.GetPropertyDescription(i),
                                shader.GetPropertyNameId(i));
                        }

                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        valueBase = new PropertiesValueVector(shader.GetPropertyDescription(i),
                            shader.GetPropertyNameId(i));
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        valueBase = new PropertiesValueTexture(shader.GetPropertyDescription(i),
                            shader.GetPropertyNameId(i));
                        break;
                    case ShaderUtil.ShaderPropertyType.Range:
                        var limits = shader.GetPropertyRangeLimits(i);
                        valueBase = new PropertiesValueRange(shader.GetPropertyDescription(i),
                            shader.GetPropertyNameId(i),limits.x,limits.y);
                        break;
                    default:
                        valueBase = new PropertiesValueFloat(shader.GetPropertyDescription(i),
                            shader.GetPropertyNameId(i));
                        break;
                }

                string mainName = "";
                foreach (var attribute in attributes)
                {
                    mainName = ReadCustomGroup(attribute);
                    if (mainName != "") break;
                }

                if (dictionary.ContainsKey(mainName))
                {
                    dictionary[mainName].data.Add(valueBase);
                }
                else
                {
                    dictionary["无分组"].data.Add(valueBase);
                }
                
                
            }
            
            foreach (var variable in dictionary)
            {
                propertiesGroups.Add(new PropertiesGroup(variable.Value.name,variable.Value.data,showMats));
            }
            
         }
         private void SetReorderableList()
         {
             reorderableList= new ReorderableList(propertiesGroups, typeof(PropertiesGroup),
                 true, true, false, false);
             reorderableList.drawElementCallback = (Rect rect, int index, bool a, bool b) =>
             {
                 if(propertiesGroups.Count!=0)
                     propertiesGroups[index].ShowGUI(rect);
             };
             reorderableList.drawHeaderCallback = DrawHeaderCallBack;
             reorderableList.elementHeightCallback = (int index) => propertiesGroups[index].GetHeight();
             if (empty == null)
             {
                 empty = new List<char>();
                 reorderableListEmpty = new ReorderableList(empty, typeof(char), false, true, false, false);
                 reorderableListEmpty.drawHeaderCallback = DrawHeaderCallBack;
                 reorderableListEmpty.drawElementCallback = (Rect r, int index, bool a, bool b) => { };
                 reorderableListEmpty.footerHeight = 10;
                 reorderableListEmpty.elementHeight = 0;
             }
             
         }

         private void ResetFold()
         {
             foreach (var variable in propertiesGroups)
             {
                 variable.ResetFold();
             }
         }
         
         public void Refresh(string path,GameObject[] gameObjects,int index)
         {
             for (int i = 0; i < count; ++i)
             {
                 if (mats[i] != null&& AssetDatabase.GetAssetPath(mats[i])=="")
                 {
                     string pathTemp = String.Format("{0}/{1}_{2}.mat", path,  gameObjects[i].name,
                         index.ToString());
                     int number = 0;
                     while (File.Exists(pathTemp))
                     {
                         number++;
                         pathTemp = String.Format("{0}/{1}_{2}.mat", path,  gameObjects[i].name,
                             (index+number).ToString());
                     }
                     AssetDatabase.CreateAsset(mats[i],pathTemp);
                 }
             }
         }


         private string ReadCustomGroup(string attribute)
         {
             if (attribute.IndexOf("Custom") > -1)
             {
                 int length = attribute.Length;
                 int start = -1;
                 for (int i = 0; i < length; ++i)
                 {
                     if (start == -1)
                     {
                         if (attribute[i] == '(') start = i + 1;
                     }
                     else
                     {
                         if (attribute[i] == ')' || attribute[i] == ',')
                         {
                             return attribute.Substring(start, i - start);
                         }
                     }
                 }
             }
             return "";
         }
    }
}
