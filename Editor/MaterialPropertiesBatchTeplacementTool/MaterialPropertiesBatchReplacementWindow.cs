
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using Object = UnityEngine.Object;

namespace SYEfficiencyToolkit
{
    public class MaterialPropertiesBatchReplacementWindow : EditorWindow
    {

        [MenuItem("美术工具箱/效率工具/批量创建材质内容的预制体生成工具")]
        private static void Open()
        {
            GetWindow<MaterialPropertiesBatchReplacementWindow>("批量创建材质内容的预制体生成").Show();
        }
        
        //show 
        private float startTime;
        private GameObject father;
        private List<GameObject> resultGameObjects;
        private List<bool> isShow;
        private GameObject gameObject;
        private DefaultAsset defaultAssetMat;
        private DefaultAsset defaultAssetObj;
        private ReorderableList reorderableListMain;
        private List<MaterialData> datas;
        private bool fold = true;
        private Vector2 scroll;
        private float distance;
        private DefaultAsset doc;

        private void OnEnable()
        {
            distance = 2.0f;
            datas = new List<MaterialData>();
            reorderableListMain = new ReorderableList(datas, typeof(MaterialData), true,
                true, false, false);
            reorderableListMain.elementHeightCallback = (int index) => datas[index].GetHeight();
            reorderableListMain.drawHeaderCallback = DrawHeadCallBack;
            reorderableListMain.drawElementCallback = (Rect rect, int index, bool a, bool b) => datas[index].Show(rect);
            
            resultGameObjects = new List<GameObject>();
            isShow = new List<bool>();
            gameObject = null;
            defaultAssetMat =
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                    EditorPrefs.GetString("MaterialPropertiesBatchReplacementToolMat"));
            defaultAssetObj =
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                    EditorPrefs.GetString("MaterialPropertiesBatchReplacementToolObj"));

            string guidToPathString = AssetDatabase.GUIDToAssetPath("e89aa762612a1c64bb473a1e6a592d21");
            doc = AssetDatabase.LoadAssetAtPath<DefaultAsset>(guidToPathString);
        }

        private void OnDisable()
        {
            Refresh(gameObject);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUI.BeginDisabledGroup(defaultAssetObj==null || defaultAssetMat==null);
            var newGameObject = EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true) as GameObject;
            if (newGameObject != gameObject)
            {
                Refresh(gameObject);
                gameObject = newGameObject;
                GetAllMaterial();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            fold = EditorGUILayout.BeginFoldoutHeaderGroup(fold, "输出选项");
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUI.BeginChangeCheck();
            MaterialData.Count = EditorGUILayout.DelayedIntField("处理预制体数量", MaterialData.Count);
            if (EditorGUI.EndChangeCheck())
            {
                if (MaterialData.Count < 1) MaterialData.Count = 1;
                else if (MaterialData.Count >= 16) MaterialData.Count = 16;
            }

            GUILayout.EndHorizontal();

            if (fold)
            {
                EditorGUI.BeginDisabledGroup(gameObject!=null);
                var defaultAssetTemp =
                    EditorGUILayout.ObjectField("预制体输出文件夹", defaultAssetObj, typeof(DefaultAsset)) as DefaultAsset;
                if (defaultAssetTemp != defaultAssetObj)
                {
                    if (defaultAssetTemp == null) defaultAssetObj = null;
                    else if (Directory.Exists(AssetDatabase.GetAssetPath(defaultAssetTemp)))
                    {
                        defaultAssetObj = defaultAssetTemp;
                        EditorPrefs.SetString("MaterialPropertiesBatchReplacementToolObj",
                            AssetDatabase.GetAssetPath(defaultAssetTemp));
                    }
                }

                defaultAssetTemp =
                    EditorGUILayout.ObjectField("材质球输出文件夹", defaultAssetMat, typeof(DefaultAsset)) as DefaultAsset;
                if (defaultAssetTemp != defaultAssetMat)
                {
                    if (defaultAssetTemp == null) defaultAssetMat = null;
                    else if (Directory.Exists(AssetDatabase.GetAssetPath(defaultAssetTemp)))
                    {
                        defaultAssetMat = defaultAssetTemp;
                        EditorPrefs.SetString("MaterialPropertiesBatchReplacementToolMat",
                            AssetDatabase.GetAssetPath(defaultAssetTemp));
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            
            GUILayout.BeginHorizontal();
            distance = EditorGUILayout.Slider( "生成间隔",distance, 0, 20);

            EditorGUILayout.ObjectField(doc, typeof(DefaultAsset), false);
            GUILayout.EndHorizontal();

            PreviewRealTime();

            GUILayout.Space(10);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            reorderableListMain.DoLayoutList();
            EditorGUILayout.EndScrollView();
            
            RefreshMaterial();
            
        }

        
        
        private void GetAllMaterial()
        {
            datas.Clear();
            resultGameObjects.Clear();
            isShow.Clear();
            List<Material> materials = new List<Material>();
            if (gameObject == null) return;
            var rs = gameObject.GetComponentsInChildren<Renderer>();
            foreach (var r in rs)
            {
                var mat = r.sharedMaterial;
                if (mat && !materials.Contains(mat))
                {
                    materials.Add(mat);
                }
            }

            resultGameObjects.Add(gameObject);
            isShow.Add(true);
            foreach (var mat in materials)
            {
                datas.Add(new MaterialData(mat,gameObject.name));
            }
        }

        private void RefreshMaterial()
        {
            if (defaultAssetMat == null)
            {
                return;
            }
            string pathMat = AssetDatabase.GetAssetPath(defaultAssetMat);
            int index = 0;
            foreach (var variable in datas)
            {
                variable.Refresh(pathMat, resultGameObjects.ToArray(), index++);
            }
        }

        private void Refresh(GameObject priGameObject)
        {
            RefreshMaterial();

            string pathObj = AssetDatabase.GetAssetPath(defaultAssetObj);
            foreach (var variable in resultGameObjects)
            {
                if (variable == null || variable == priGameObject) continue;
                string objPath = pathObj + "/" + variable.name + ".prefab";
                variable.gameObject.name = variable.name;
                PrefabUtility.SaveAsPrefabAsset(variable.gameObject, objPath);
                Debug.Log(objPath + "-保存成功\n");
            }


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        

        private void DrawHeadCallBack(Rect rect)
        {
            float width = rect.width * 0.2f;
            if (EditorGUI.DropdownButton(new Rect(rect.x + rect.width - 2 * width, rect.y, width, rect.height),
                    new GUIContent("筛选"), FocusType.Passive))
            {
                GenericMenu _menu = new GenericMenu();
                for (int i = 0; i < resultGameObjects.Count; ++i)
                {
                    //添加菜单
                    _menu.AddItem(new GUIContent(resultGameObjects[i].name), isShow[i], OnValueSelected, i);
                }

                _menu.ShowAsContext(); //显示菜单
            }

            if (GUI.Button(new Rect(rect.x + rect.width -  width, rect.y, width, rect.height), "更新资源"))
            {
                Refresh(gameObject);
            }

            // if (GUI.Button(new Rect(rect.x , rect.y, width, rect.height), "保存资源"))
            // {
            //     
            //     Disable(gameObject);
            //     gameObject = null;
            //     GetAllMaterial();
            //     GUIUtility.ExitGUI();
            // }

        }

        private void OnValueSelected(object value)
        {
            isShow[(int)value] = !isShow[(int)value];
            foreach (var variable in datas)
            {
                variable.ChangePresent(isShow,resultGameObjects);
            }
        }

        private string StringEndTwoToInt(string s, int sum)
        {
            if (s == null) return "";
            int numStart = s.Length;
            if (numStart == 0) return "";
            for (int i = numStart - 1; i >= 0; --i)
            {
                if (s[i] >= 48 && s[i] <= 57) numStart = i;
                else break;
            }

            if (numStart == s.Length)
            {
                return s + sum.ToString();
            }
            else
            {
                int length = s.Length - numStart;
                string leftString = s.Substring(0, numStart);
                string rightString = s.Substring(numStart, s.Length - numStart);
                int value = Int32.Parse(rightString);
                value += sum;
                rightString = value.ToString();
                while (rightString.Length < length)
                {
                    rightString += '0';
                }

                return leftString + rightString;
            }
        }

        private void PreviewRealTime() //变为每帧都更新
        {
            if (gameObject == null) return;
            if (father == null)
            {
                father = new GameObject();
            }

            int count = MaterialData.Count;

            if (count == resultGameObjects.Count) return;
            if (count < resultGameObjects.Count)
            {
                while (count != resultGameObjects.Count)
                {
                    var objTemp = resultGameObjects[resultGameObjects.Count - 1];
                    AssetDatabase.DeleteAsset(string.Format("{0}/{1}.prefab",
                        AssetDatabase.GetAssetPath(defaultAssetObj), objTemp.name));
                    Object.DestroyImmediate(objTemp);
                    resultGameObjects.RemoveAt(resultGameObjects.Count - 1);
                    isShow.RemoveAt(resultGameObjects.Count - 1);
                    foreach (var variable in datas)
                    {
                        variable.RemoveMaterial();
                    }
                }
            }
            else if (count > resultGameObjects.Count)
            {
                while (count != resultGameObjects.Count)
                {
                    GameObject gameObjectTemp = Instantiate(gameObject, father.transform);
                    gameObjectTemp.name = gameObjectTemp.name.Replace("(Clone)", "");
                    int index = 0;
                    string name = gameObjectTemp.name;
                    gameObjectTemp.name = StringEndTwoToInt(name, resultGameObjects.Count+index);
                    string pathObj = AssetDatabase.GetAssetPath(defaultAssetObj);
                    while (File.Exists(string.Format("{0}/{1}.prefab",pathObj,gameObjectTemp.name)))
                    {
                        ++index;
                        gameObjectTemp.name = StringEndTwoToInt(name, resultGameObjects.Count+index);
                    }
                    PrefabUtility.SaveAsPrefabAsset(gameObjectTemp, string.Format("{0}/{1}.prefab",pathObj,gameObjectTemp.name));
                    Debug.Log(string.Format("{0}/{1}.prefab",pathObj,gameObjectTemp.name) + "-保存成功\n");

                    Vector3 pos;
                    if (resultGameObjects.Count == 0)
                    {
                        var v3 = gameObject.transform.position;
                        pos = new Vector3(v3.x + distance, v3.y, v3.z);
                    }
                    else
                    {
                        var v3 = resultGameObjects[resultGameObjects.Count - 1].transform.position;
                        pos = new Vector3(v3.x + distance, v3.y, v3.z);
                    }

                    gameObjectTemp.transform.position = pos;
                    resultGameObjects.Add(gameObjectTemp);
                    isShow.Add(true);
                    foreach (var variable in datas)
                    {
                        variable.AddNewMaterial(gameObjectTemp);
                    }
                }
            }
            
            foreach (var variable in datas)
            {
                variable.ChangePresent(isShow,resultGameObjects);
            }

        }

    }
}