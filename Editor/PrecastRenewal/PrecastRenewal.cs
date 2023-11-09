using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SYEfficiencyToolkit
{
    public class PrecastRenewal : ScriptableWizard
    {
        [MenuItem("美术工具箱/效率工具/预制体更新节点工具")]
        private static void Open()
        {
            DisplayWizard<PrecastRenewal>("预制体更新节点", "替换", "生成");
        }

        public GameObject gameObjectFBX;
        [Range(-10, 10)] public float space = 2.0f;
        public List<GameObject> inputGameObjects;
        private List<(string, GameObject)> gameObjectsInput = new List<(string, GameObject)>();
        private GameObject father;

        public DefaultAsset doc;

        private void OnWizardUpdate()
        {
            string guidToPathString = AssetDatabase.GUIDToAssetPath("d3156a10d15c5e04c81e8e1cc1a0ea2d");
            doc = AssetDatabase.LoadAssetAtPath<DefaultAsset>(guidToPathString);
        }

        private void OnWizardOtherButton()
        {
            Do();
        }

        private void OnWizardCreate()
        {

            foreach (var g in gameObjectsInput)
            {
                PrefabUtility.SaveAsPrefabAsset(g.Item2, g.Item1);
            }
            DisplayWizard<PrecastRenewal>("工具名字", "替换", "生成");
        }

        private void Do()
        {
            gameObjectsInput.Clear();
            var dealGameObject = CheckInput();
            if (dealGameObject == null) return;
            List<string> names = new List<string>();
            foreach (var item in dealGameObject)
            {
                names.Add(item.name);
            }
            var dictionary = GetRendererDictionary(dealGameObject);
            var results = GetFBXStringDictionaty(dictionary, names);
            int count = results.Count;
            for (int i = 0; i < count; ++i)
            {
                gameObjectsInput.Add((AssetDatabase.GetAssetPath(dealGameObject[i]), results[i]));
            }
        }

        private List<GameObject> CheckInput()
        {
            if (gameObjectFBX == null || inputGameObjects == null || inputGameObjects.Count == 0) return null;
            string FBXPath = AssetDatabase.GetAssetPath(gameObjectFBX);
            List<GameObject> dealGameObject = new List<GameObject>();
            foreach (GameObject gameObject in inputGameObjects)
            {
                if (gameObject == null) continue;
                if (AssetDatabase.GetAssetPath(gameObject) == "") continue;
                dealGameObject.Add(gameObject);
            }
            if (dealGameObject.Count == 0) return null;
            return dealGameObject;
        }

        private Dictionary<string, Queue<Material>> GetRendererDictionary(List<GameObject> gameObjects)
        {
            Dictionary<string, Queue<Material>> result = new Dictionary<string, Queue<Material>>();
            foreach (GameObject gameObject in gameObjects)
            {
                var rs = gameObject.GetComponentsInChildren<Renderer>();
                foreach (var variable in rs)
                {
                    if (!result.ContainsKey(variable.gameObject.name))
                    {
                        result.Add(variable.gameObject.name, new Queue<Material>());
                    }
                    result[variable.gameObject.name].Enqueue(variable.sharedMaterial);
                }
            }
            return result;
        }

        private List<GameObject> GetFBXStringDictionaty(Dictionary<string, Queue<Material>> dictionary, List<string> names)
        {
            Queue<Transform> queue = new Queue<Transform>();
            List<GameObject> result = new List<GameObject>();
            foreach (var name in names)
            {
                queue.Clear();
                if (father == null) father = new GameObject();
                GameObject newGameObject = Instantiate(gameObjectFBX, father.transform);
                if (result.Count > 0)
                {
                    var v3 = result[result.Count - 1].transform.position;
                    newGameObject.transform.position = new Vector3(v3.x + space, v3.y, v3.z);
                }
                newGameObject.name = name;
                result.Add(newGameObject);
                queue.Enqueue(newGameObject.transform);
                while (queue.Count > 0)
                {
                    Transform t = queue.Dequeue();
                    if (t == null) continue;
                    if (dictionary.ContainsKey(t.gameObject.name))
                    {
                        t.gameObject.GetComponent<Renderer>().sharedMaterial = dictionary[t.gameObject.name].Dequeue();
                        if (dictionary[t.gameObject.name].Count <= 0) dictionary.Remove(t.gameObject.name);
                    }

                    for (int j = 0; j < t.childCount; ++j)
                    {
                        queue.Enqueue(t.GetChild(j));
                    }
                }
            }
            return result;
        }



    }
}