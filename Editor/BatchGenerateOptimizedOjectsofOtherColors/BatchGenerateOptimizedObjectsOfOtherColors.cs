
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace SYEfficiencyToolkit {
    public class BatchGenerateOptimizedObjectsOfOtherColors : EditorWindow
    {
        [MenuItem("美术工具箱/效率工具/自动生成优化后染色工具")]
        public static void Open()
        {
            GetWindow<BatchGenerateOptimizedObjectsOfOtherColors>("自动生成优化后染色工具").Show();
        }

        private GameObject optimizedObject;
        private GameObject primalGameObject;
        private InputGameObject inputGameObject;
        private SerializedObject serializedObject;
        private SerializedProperty serializedProperty;
        private Vector2 scroll, scrollLog;
        private List<OptimizedObject> optimizedObjects;
        private ReorderableList reorderableList;
        private DefaultAsset defaultAsset;
        private string log;
        private DefaultAsset doc;
        private void OnEnable()
        {
            log = "";
            optimizedObjects = new List<OptimizedObject>();
            reorderableList = new ReorderableList(optimizedObjects, typeof(OptimizedObject), false, true, false, false);
            reorderableList.drawElementCallback = (rect, index, a, b) =>
            {
                EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width / 2, rect.height), optimizedObjects[index].gameObject, typeof(GameObject), true);

                GUI.Label(new Rect(rect.x + rect.width / 2 + 20, rect.y, 40, rect.height), "命名：");

                optimizedObjects[index].name = EditorGUI.TextField(
                    new Rect(rect.x + rect.width / 2 + 60, rect.y, rect.width / 2 - 60, rect.height),
                    optimizedObjects[index].name);
            };

            string guidToPathString = AssetDatabase.GUIDToAssetPath("4bcbade6196db5e4dafa2d9b94e9e6d9");
            doc = AssetDatabase.LoadAssetAtPath<DefaultAsset>(guidToPathString);
            reorderableList.drawHeaderCallback = (Rect rect) => { GUI.Label(rect, "优化后染色物体"); };
        }
        private void OnGUI()
        {
            GUILayout.Label("此处请拖入优化前的物体");
            primalGameObject = EditorGUILayout.ObjectField(primalGameObject, typeof(GameObject), true) as GameObject;
            GUILayout.Label("此处请拖入优化后的物体");
            optimizedObject = EditorGUILayout.ObjectField(optimizedObject, typeof(GameObject), true) as GameObject;
            GUILayout.Space(10);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (inputGameObject == null || serializedProperty == null || serializedObject == null)
            {
                inputGameObject = ScriptableObject.CreateInstance<InputGameObject>();
                serializedObject = new SerializedObject(inputGameObject);
                serializedProperty = serializedObject.FindProperty("gameObjects");
            }

            EditorGUILayout.PropertyField(serializedProperty, new GUIContent("请输入优化前的染色物体", "！"));
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button(new GUIContent("生成", "会清除Log信息")))
            {
                log = "";
                optimizedObjects.Clear();
                foreach (var variable in inputGameObject.gameObjects)
                {
                    if (variable == null) continue;
                    var gameObject = Object.Instantiate(optimizedObject);
                    SetColorAndMaterial(gameObject, variable);
                    //
                    AddDifferentNodes(gameObject, variable, primalGameObject);
                    optimizedObjects.Add(new OptimizedObject(gameObject, variable.name));
                }
            }
            GUILayout.Space(10);
            reorderableList.DoLayoutList();

            var defaultAssetTemp = EditorGUILayout.ObjectField(defaultAsset, typeof(DefaultAsset), true) as DefaultAsset;
            if (Directory.Exists(AssetDatabase.GetAssetPath(defaultAssetTemp)))
            {
                defaultAsset = defaultAssetTemp;
            }
            EditorGUI.BeginDisabledGroup(defaultAsset == null || optimizedObjects.Count == 0);

            if (GUILayout.Button("保存"))
            {
                GenerateGameObject(AssetDatabase.GetAssetPath(defaultAsset));
            }
            EditorGUI.EndDisabledGroup();


            //if (GUILayout.Button("使用说明"))
            //{
            //    string guidToPathString = AssetDatabase.GUIDToAssetPath("4bcbade6196db5e4dafa2d9b94e9e6d9");
            //    Debug.Log(guidToPathString);
            //    Application.OpenURL(Application.dataPath.Substring(0, Application.dataPath.Length - 6) +
            //                        guidToPathString);
            //}

            GUILayout.Space(20);
            EditorGUILayout.ObjectField(doc, typeof(DefaultAsset), false);

            EditorGUILayout.EndScrollView();
            if (log != "")
            {
                GUILayout.Space(50);
                GUILayout.BeginArea(new Rect(0, position.height - 50, position.width, 50));
                scrollLog = EditorGUILayout.BeginScrollView(scrollLog);
                GUILayout.Label(log);
                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
            }



        }


        private void GenerateGameObject(string path)
        {
            log = "";
            foreach (var variable in optimizedObjects)
            {
                if (variable.gameObject == null) continue;
                variable.gameObject.name = variable.name;
                if (File.Exists(path + "/" + variable.name + ".prefab"))
                {
                    log += path + "/" + variable.name + ".prefab" + "-已经存在\n";
                }
                else
                {
                    variable.gameObject.name = variable.name;
                    PrefabUtility.SaveAsPrefabAsset(variable.gameObject, path + "/" + variable.name + ".prefab");
                    log += path + "/" + variable.name + ".prefab" + "-保存成功\n";
                }
            }
        }

        private void SetColorAndMaterial(GameObject comparison, GameObject gameObject)
        {
            var particleSystemOfComparison = comparison.GetComponent<ParticleSystem>();
            if (particleSystemOfComparison)
            {
                var particleSystemOfGameObject = gameObject.GetComponent<ParticleSystem>();
                if (particleSystemOfGameObject)
                {
                    //粒子的startColor
                    var main = particleSystemOfComparison.main;
                    main.startColor = particleSystemOfGameObject.main.startColor;

                    //粒子的colorOverlifeTime
                    var particleSystemOfComparisonColorOverLifetime = particleSystemOfComparison.colorOverLifetime;
                    particleSystemOfComparisonColorOverLifetime.color = particleSystemOfGameObject.colorOverLifetime.color;

                    //粒子的ColorBySpeed
                    var particleSystemOfComparisonColorBySpeed = particleSystemOfComparison.colorBySpeed;
                    particleSystemOfComparisonColorBySpeed.color = particleSystemOfGameObject.colorBySpeed.color;

                    var particleSystemOfComparisonTrail = particleSystemOfComparison.trails;
                    var particleSystemOfGameObjectTrail = particleSystemOfGameObject.trails;
                    //条带的colorOverLifetime
                    particleSystemOfComparisonTrail.colorOverLifetime = particleSystemOfGameObjectTrail.colorOverLifetime;
                    //条带的colorOverTrail
                    particleSystemOfComparisonTrail.colorOverTrail = particleSystemOfGameObjectTrail.colorOverTrail;
                }
            }


            var particleSystemRenderer = comparison.GetComponent<ParticleSystemRenderer>();
            if (particleSystemRenderer)
            {
                //粒子的Material
                if (particleSystemRenderer.sharedMaterial)
                {
                    var p2 = gameObject.GetComponent<ParticleSystemRenderer>();
                    if (p2)
                    {
                        var mat = p2.sharedMaterial;
                        if (mat != particleSystemRenderer.sharedMaterial)
                        {
                            particleSystemRenderer.sharedMaterial = mat;
                        }
                    }
                }
                //粒子条带的Material
                if (particleSystemRenderer.trailMaterial)
                {
                    var p2 = gameObject.GetComponent<ParticleSystemRenderer>();
                    if (p2)
                    {
                        var mat = p2.trailMaterial;
                        if (mat != particleSystemRenderer.trailMaterial)
                        {
                            particleSystemRenderer.trailMaterial = mat;
                        }
                    }
                }
            }

            //MeshRenderer的Material
            var meshRenderer = comparison.GetComponent<MeshRenderer>();
            if (meshRenderer)
            {
                if (meshRenderer.sharedMaterial != null)
                {
                    var p2 = gameObject.GetComponent<MeshRenderer>();
                    if (p2)
                    {
                        var mat = p2.sharedMaterial;
                        if (mat != meshRenderer.sharedMaterial)
                        {
                            meshRenderer.sharedMaterial = mat;
                        }
                    }
                }
            }

            //SkinMeshRenderer的Material
            var skinMeshRenderer = comparison.GetComponent<SkinnedMeshRenderer>();
            if (skinMeshRenderer)
            {
                if (skinMeshRenderer.sharedMaterial != null)
                {
                    var p2 = gameObject.GetComponent<SkinnedMeshRenderer>();
                    if (p2)
                    {
                        var mat = p2.sharedMaterial;
                        if (mat != skinMeshRenderer.sharedMaterial)
                        {
                            skinMeshRenderer.sharedMaterial = mat;
                        }
                    }
                }

            }

            //TrailRenderer的Material
            var trailRenderer = comparison.GetComponent<TrailRenderer>();
            if (trailRenderer)
            {
                var p2 = gameObject.GetComponent<TrailRenderer>();
                if (p2 != null)
                {
                    if (trailRenderer.sharedMaterial)
                    {
                        if (p2)
                        {
                            var mat = p2.sharedMaterial;
                            if (mat != skinMeshRenderer.sharedMaterial)
                            {
                                skinMeshRenderer.sharedMaterial = mat;
                            }
                        }
                    }

                    trailRenderer.startColor = p2.startColor;
                    trailRenderer.endColor = p2.endColor;
                    trailRenderer.colorGradient = p2.colorGradient;
                }
            }

            Dictionary<string, Transform> hash = new Dictionary<string, Transform>();

            for (int i = 0; i < gameObject.transform.childCount; ++i)
            {
                Transform child = gameObject.transform.GetChild(i);
                hash.Add(child.name, child);
            }

            for (int i = 0; i < comparison.transform.childCount; ++i)
            {
                Transform child = comparison.transform.GetChild(i);
                if (hash.ContainsKey(child.name))
                {
                    SetColorAndMaterial(child.gameObject, hash[child.name].gameObject);
                }
            }

        }

        private void AddDifferentNodes(GameObject target, GameObject contrast, GameObject primal)//在父节点处理子节点
        {
            if (primal == null) return;
            Dictionary<string, Transform> hashString0 = new Dictionary<string, Transform>();
            Dictionary<string, Transform> hashString1 = new Dictionary<string, Transform>();

            int childCountPrimal = primal.transform.childCount;
            for (int i = 0; i < childCountPrimal; ++i)
            {
                var transformTemporary = primal.transform.GetChild(i);
                hashString0.Add(transformTemporary.name, transformTemporary);
            }

            int childCountTarget = target.transform.childCount;
            for (int i = 0; i < childCountTarget; i++)
            {
                var transformTemporary = target.transform.GetChild(i);
                hashString1.Add(transformTemporary.name, transformTemporary);
            }

            int childCountContrast = contrast.transform.childCount;
            for (int i = 0; i < childCountContrast; i++)
            {
                var transformTemporary = contrast.transform.GetChild(i);
                bool primalContain = hashString0.ContainsKey(transformTemporary.name);
                bool targetContain = hashString1.ContainsKey(transformTemporary.name);
                if (!primalContain && !targetContain)
                {
                    Object.Instantiate(transformTemporary.gameObject, target.transform);
                }
                else if (primalContain && targetContain)
                {
                    AddDifferentNodes(hashString1[transformTemporary.name].gameObject, transformTemporary.gameObject, hashString0[transformTemporary.name].gameObject);
                }
            }


        }
        private class OptimizedObject
        {
            public GameObject gameObject;
            public string name;

            public OptimizedObject(GameObject gameObject, string name)
            {
                this.gameObject = gameObject;
                this.name = name;
            }
        }

        private class InputGameObject : ScriptableObject
        {
            public List<GameObject> gameObjects;
            public InputGameObject()
            {
                gameObjects = new List<GameObject>();
                gameObjects.Add(null);
            }
        }

    }
}
