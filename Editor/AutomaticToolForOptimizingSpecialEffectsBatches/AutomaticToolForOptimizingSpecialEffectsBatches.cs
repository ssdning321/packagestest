using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Object = UnityEngine.Object;

namespace SYEfficiencyToolkit
{
    public class AutomaticToolForOptimizingSpecialEffectsBatches : EditorWindow
    {

        [MenuItem("美术工具箱/效率工具/特效优化工具")]
        public static void Open()
        {
            GetWindow<AutomaticToolForOptimizingSpecialEffectsBatches>("自动合图优化工具").Show();
        }

        private DefaultAsset doc;
        private GameObject rootOfInputObject;
        private InputGameObject[] inputGameObjects;
        private SerializedObject[] serializedObjects;
        private SerializedProperty[] serializedProperties;
        private Shader shader;
        private DefaultAsset defaultAsset;
        private DefaultAsset publicFolder;
        private int compressRatio;
        private int pixelSpace;
        private Vector2 scroll;
        private Dictionary<Material,(List<ParticleSystemRenderer> particleSystemRenderers, bool multi)> setOrder;
     
        
        private void OnEnable()
        {
            defaultAsset = CheckConfig("AutomaticToolForOptimizingSpecialEffectsBatchesOutput");
            publicFolder = CheckConfig("AutomaticToolForOptimizingSpecialEffectsBatchesPublicFolder");
            shader = CheckConfigShader("AutomaticToolForOptimizingSpecialEffectsBatchesShader");
            inputGameObjects = new InputGameObject[2];
            serializedObjects = new SerializedObject[2];
            serializedProperties = new SerializedProperty[2];
            compressRatio = 1;
            pixelSpace = 1;

            string guidToPathString = AssetDatabase.GUIDToAssetPath("5b023fb9e0ac0f34dac6ba414aa535c1");
            doc = AssetDatabase.LoadAssetAtPath<DefaultAsset>(guidToPathString);
        }

        private void OnGUI()
        {
            EmptyCheck();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            {
                GUIInput();
                EditorGUI.BeginDisabledGroup(defaultAsset == null || rootOfInputObject == null);
                {
                    if (GUILayout.Button("点我合成"))
                    {
                        ButtonClick();
                    }
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.Space(20);

                EditorGUILayout.ObjectField(doc, typeof(DefaultAsset), false);
            }
            EditorGUILayout.EndScrollView();

        }

        private void GUIInput()
        {
            GUILayout.Space(10);
            rootOfInputObject = EditorGUILayout.ObjectField("根物体",rootOfInputObject, typeof(GameObject), true) as GameObject;
            
            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            shader = EditorGUILayout.ObjectField("输入特效通用材质",shader, typeof(Shader), true) as Shader;
            if (EditorGUI.EndChangeCheck())
            {
                SetConfig("AutomaticToolForOptimizingSpecialEffectsBatchesShader",AssetDatabase.GetAssetPath(shader));
            }
            
            
            //"AutomaticToolForOptimizingSpecialEffectsBatchesShader"
            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            var defaultAssetTemp=EditorGUILayout.ObjectField("输出位置",defaultAsset,typeof(DefaultAsset),false) as DefaultAsset;
            if (EditorGUI.EndChangeCheck())
            {
                if (defaultAssetTemp == null) defaultAsset = null;
                if (Directory.Exists(AssetDatabase.GetAssetPath(defaultAssetTemp)))
                {
                    defaultAsset = defaultAssetTemp;
                    SetConfig("AutomaticToolForOptimizingSpecialEffectsBatchesOutput",AssetDatabase.GetAssetPath(defaultAssetTemp));
                }
            }

            for (int i = 0; i < 2; ++i)
            {
                GUILayout.Space(10);
                EditorGUILayout.PropertyField(serializedProperties[i],new GUIContent(((BlendMode)i).ToString()));
                serializedObjects[i].ApplyModifiedProperties();
            }
            
            GUILayout.Space(10);
            var publicFolderTemp=EditorGUILayout.ObjectField("公共文件夹",publicFolder,typeof(DefaultAsset),false) as DefaultAsset;
            if (EditorGUI.EndChangeCheck())
            {
                if (publicFolderTemp == null) publicFolder = null;
                if (Directory.Exists(AssetDatabase.GetAssetPath(publicFolderTemp)))
                {
                    publicFolder = publicFolderTemp;
                    SetConfig("AutomaticToolForOptimizingSpecialEffectsBatchesPublicFolder",AssetDatabase.GetAssetPath(publicFolderTemp));
                }
            }
            GUILayout.Space(10);
            compressRatio = EditorGUILayout.IntSlider("压缩倍数", compressRatio, 0, 3);
            pixelSpace = EditorGUILayout.IntSlider("合图像素间隔", pixelSpace, 0, 4);
        }
        
        private void ButtonClick()
        {
            //贴图-偏移
                Dictionary<Texture2D, TextureData>[] dictionary = new Dictionary<Texture2D, TextureData>[2];
                //物体-(贴图，混合模式)
                Dictionary<GameObject, (Texture2D texture, BlendMode blendMode)> gameObjectToTexture =
                    new Dictionary<GameObject, (Texture2D texture, BlendMode blendMode)>();
                //两种材质
                Material[] materials = new Material[2];

                List<Texture2D>[] allTextures = new List<Texture2D>[2];
                //找到所有贴图并且解压

                GameObject rootOfGameObjects = Object.Instantiate(rootOfInputObject);

                for (int i = 0; i < 2; ++i)
                {
                    dictionary[i] = new Dictionary<Texture2D, TextureData>();
                    
                    allTextures[i] = GetAllTextures(inputGameObjects[i].gameObjects,dictionary[i],gameObjectToTexture,(BlendMode)i);
                    //处理空的情况
                    if(allTextures[i].Count==0) continue;      
                    QuickSearchTexture2D quickSearch = new QuickSearchTexture2D();
                    quickSearch.Construct(allTextures[i],1<<compressRatio);
                    ContainBlock containBlock = new ContainBlock();
                    containBlock.Construct(quickSearch, new Vector2Int(0, 0), quickSearch.MaxSize);
                    var father = GetFather(containBlock);
                    Texture2D target = new Texture2D(father.size.x, father.size.y);
                    int all = father.size.x * father.size.y;
                    Color[] colors = new Color[all];
                    for (int xx = 0; xx < all; ++xx)
                    {
                        colors[xx] = new Color(0, 0, 0, 0);
                    }
                    target.SetPixels(colors);
                    father.Merge(target,dictionary[i],father.size,pixelSpace,1<<compressRatio);
                    materials[i]=new Material(shader);
                    SetMaterialBlend(materials[i],(BlendMode)i);
                    Save(target,materials[i],(BlendMode)i);
                }
                //合图，获取数据
                setOrder = new Dictionary<Material,(List<ParticleSystemRenderer> particleSystemRenderers, bool multi)>();
                Replace(rootOfGameObjects, rootOfInputObject, gameObjectToTexture, dictionary, materials);
                var setOrderReal = new Dictionary<int, ParticleSystemRenderer>();
                int x = 3;
                foreach (var variable in setOrder)
                {
                    if (variable.Value.multi)
                    {
                        foreach (var particleRenderer in variable.Value.particleSystemRenderers)
                        {
                            particleRenderer.sortingOrder = x;
                        }

                        ++x;
                    }
                    else
                    {
                        variable.Value.particleSystemRenderers[0].sortingOrder = 0;
                    }
                }
        }

        private void EmptyCheck()
        {
            for (int i = 0; i < 2; ++i)
            {
                if (inputGameObjects[i] == null)
                {
                    inputGameObjects[i] = ScriptableObject.CreateInstance<InputGameObject>();
                    serializedObjects[i] = new SerializedObject(inputGameObjects[i]);
                    serializedProperties[i] = serializedObjects[i].FindProperty("gameObjects");
                }
            }
        }
        
        private void Replace(GameObject root, GameObject rootOfInput,
            Dictionary<GameObject, (Texture2D texture, BlendMode blendMode)> gameObjectToTexture,
            Dictionary<Texture2D, TextureData>[] dictionary, Material[] materials)
        {
            if (gameObjectToTexture.ContainsKey(rootOfInput))
            {
                int blendMode = (int)gameObjectToTexture[rootOfInput].blendMode;
                var tex = gameObjectToTexture[rootOfInput].texture;
                var renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sortingOrder = 1 + blendMode;
                renderer.sharedMaterial = materials[blendMode];
                var particleSystem = root.GetComponent<ParticleSystem>();
                var textureSheetAnimation = particleSystem.textureSheetAnimation;
                textureSheetAnimation.enabled = true;
                
                var frameOverTime = textureSheetAnimation.frameOverTime;
                frameOverTime.mode = ParticleSystemCurveMode.Constant;
                var texData = dictionary[blendMode][tex];
                frameOverTime.constant = texData.StartFrame/(float)(texData.Tiles.x*texData.Tiles.y);
                textureSheetAnimation.frameOverTime = frameOverTime;
                textureSheetAnimation.numTilesX = texData.Tiles.x;
                textureSheetAnimation.numTilesY = texData.Tiles.y;
            }
            else
            {
                var particleSystemRenderer = root.GetComponent<ParticleSystemRenderer>();
                if (particleSystemRenderer != null)
                {
                    if (particleSystemRenderer.sharedMaterial != null)
                    {
                        if (!setOrder.ContainsKey(particleSystemRenderer.sharedMaterial))
                        {
                            setOrder.Add(particleSystemRenderer.sharedMaterial,(new List<ParticleSystemRenderer>(),false));
                            setOrder[particleSystemRenderer.sharedMaterial].particleSystemRenderers.Add(particleSystemRenderer);
                        }
                        else
                        {
                            var particleSystemRenderers=setOrder[particleSystemRenderer.sharedMaterial].particleSystemRenderers;
                            particleSystemRenderers.Add(particleSystemRenderer);
                            setOrder[particleSystemRenderer.sharedMaterial] = (particleSystemRenderers, true);
                        }
                    }
                }
            }

            int childCount = rootOfInput.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Replace(root.transform.GetChild(i).gameObject, rootOfInput.transform.GetChild(i).gameObject,
                    gameObjectToTexture, dictionary, materials);
            }
        }

        private void SetMaterialBlend(Material material, BlendMode blendMode)
        {
            if (blendMode == BlendMode.AlphaBlend)//0
            {
                material.SetInt("_BlendTemp",0);
                material.SetInt("_BlendModeSrc", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_BlendModeDst", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            else//1
            {
                material.SetInt("_BlendTemp",1);
                material.SetInt("_BlendModeSrc", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_BlendModeDst", (int)UnityEngine.Rendering.BlendMode.One);
            }
        }
        
        private void Save(Texture2D target,Material material,BlendMode blendMode)
        {
            string fileName = string.Format("{0}.png", rootOfInputObject.name+blendMode.ToString());
            string path = Path.Combine(AssetDatabase.GetAssetPath(defaultAsset), fileName);
            while (File.Exists(path))
            {
                path=path.Replace(".png", "0.png");
            }
            var bytes = target.EncodeToPNG();
            var file = new FileStream(path, FileMode.Create);
            var binary = new BinaryWriter(file);
            binary.Flush();
            binary.Write(bytes);
            file.Close();
            //保存material
            AssetDatabase.Refresh();
            target = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            material.SetTexture("_MainTex",target);

            path=path.Replace(".png", ".mat");
            AssetDatabase.CreateAsset(material, path);
            material = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(AssetDatabase.GetAssetPath(defaultAsset),
                fileName));
            AssetDatabase.Refresh();
        }
        
        private ContainBlock GetFather(ContainBlock containBlock)
        {
            return containBlock.father == null ? containBlock : GetFather(containBlock.father);
        }
        
        private bool FilteredTexture(Texture2D texture2D)
        {
            if (texture2D == null) return false;
            if (texture2D.width == 0 || texture2D.height == 0) return false;

            if (publicFolder != null)
            {
                var path1 = AssetDatabase.GetAssetPath(publicFolder);
                var path2 = AssetDatabase.GetAssetPath(texture2D);
                if (path2.Replace(path1, "") != path2) return false;
            }
            
            if (texture2D.width != texture2D.height)
            {
                if (texture2D.width / texture2D.height > 2) return false;
                if (texture2D.height / texture2D.width > 2) return false;
            }
            return true;
        }

        private List<Texture2D> GetAllTextures(List<GameObject> gameObjects,
            Dictionary<Texture2D, TextureData> dictionary,
            Dictionary<GameObject, (Texture2D texture, BlendMode blendMode)> gameObjectToTexture, BlendMode blendMode)
        {
            var result = new List<Texture2D>();
            foreach (var variable in gameObjects)
            {
                var particleSystemRenderer = variable.GetComponent<ParticleSystemRenderer>();
                if (particleSystemRenderer == null || particleSystemRenderer.sharedMaterial == null) continue;
                if (variable.GetComponent<ParticleSystem>().textureSheetAnimation.enabled) continue;
                var mat = particleSystemRenderer.sharedMaterial;
                Shader shader = mat.shader;
                if (shader == null) continue;
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);
                        Texture2D tex = mat.GetTexture(propertyName) as Texture2D;
                        if (FilteredTexture(tex))
                        {
                            if (!dictionary.ContainsKey(tex))
                            {
                                dictionary.Add(tex, new TextureData());
                                result.Add(tex);
                            }
                            gameObjectToTexture.Add(variable,(tex,blendMode));
                            break;
                        }
                    }
                }
            }
            return result;
        }
        
        
        private void SetConfig(string key, string value)
        {
            EditorPrefs.SetString(key, value);
        }

        private Shader CheckConfigShader(string key)
        {
            string value = EditorPrefs.GetString(key);
            if (!string.IsNullOrEmpty(value))
            {
                var checkAsset = AssetDatabase.LoadAssetAtPath<Shader>(value);
                if (checkAsset != null)
                {
                    return checkAsset;
                }
            }
            return null;
        }
        private DefaultAsset CheckConfig(string key)
        {
            string value = EditorPrefs.GetString(key);
            if (!string.IsNullOrEmpty(value))
            {
                var checkAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(value);
                if (checkAsset != null)
                {
                    return checkAsset;
                }
            }

            return null;
        }
        
        

        private class InputGameObject: ScriptableObject
        {
            public List<GameObject> gameObjects;
            public InputGameObject()
            {
                gameObjects = new List<GameObject>();
            }
        }

        private class TextureData
        {
            public Vector2Int Tiles;
            public int StartFrame;

            public TextureData()
            {
                Tiles = new Vector2Int();
                StartFrame = 0;
            }
        }

        private class ContainBlock
        {
            public ContainBlock father;
            public ContainBlock child0;
            public ContainBlock child1;
            public Texture2D texture2D;
            public Vector2Int start;
            public Vector2Int size;
            
            public ContainBlock(ContainBlock c0=null)
            {
                father = null;
                child0 = c0;
                child1 = null;
                texture2D = null;
                //int main return 0 
            }


            public void Construct(QuickSearchTexture2D quickSearch, Vector2Int start, Vector2Int size)
            {
                if (quickSearch.Empty() || size.x < quickSearch.MinSize.x || size.y < quickSearch.MinSize.y) return;
                this.start = start;
                this.size = size;
                Texture2D tex = quickSearch.GetTexture(size);
                if (tex != null)
                {
                    texture2D = tex;
                }
                else
                {
                    if (child0 != null)
                    {
                        child1 = new ContainBlock();
                        child1.father = this;
                        child1.Construct(quickSearch,
                            child0.size.x > child0.size.y
                                ? new Vector2Int(start.x, start.y + child0.size.y)
                                : new Vector2Int(start.x + child0.size.x, start.y), child0.size);
            
                    }
                    else
                    {
                        bool left = false;
                        if (size.x == size.y)
                        {
                            if (quickSearch.ExistTexture(new Vector2Int(size.x / 2, size.y)))
                            {
                                left = true;
                            }
                        }
                        else if (size.x > size.y)
                        {
                            //数分
                            left = true;
                        }
                        child0 = new ContainBlock();
                        child1 = new ContainBlock();
                        child0.father = this;
                        child1.father = this;
                        if (left)
                        {
                            child0.Construct(quickSearch,start,new Vector2Int(size.x/2,size.y));
                            child1.Construct(quickSearch,new Vector2Int(start.x+size.x/2,start.y),new Vector2Int(size.x/2,size.y));
                        }
                        else
                        {
                            child0.Construct(quickSearch,start,new Vector2Int(size.x,size.y/2));
                            child1.Construct(quickSearch,new Vector2Int(start.x,start.y+size.y/2),new Vector2Int(size.x,size.y/2));
                        }
                    }

                }
                if (!quickSearch.Empty()&& father==null)
                {
                    father = new ContainBlock(this);
                    father.Construct(quickSearch,start,size.x>size.y? new Vector2Int(size.x,size.y*2): new Vector2Int(size.x*2,size.y));
                }
            }
            
            
            public void Merge(Texture2D outTex,Dictionary<Texture2D, TextureData> dictionary,Vector2Int maxSize,int empty=1,int compressRatio=1)
            {
                if (texture2D!=null)
                {
                    int w = maxSize.x / size.x;
                    int h = maxSize.y / size.y;
                    dictionary[texture2D].Tiles = new Vector2Int(w, h);
                    dictionary[texture2D].StartFrame = start.x / size.x + start.y / size.y * w;
                    var tex = DeCompress(texture2D,compressRatio);
                    for (int i = 0+empty; i < size.x-empty; ++i)
                    {
                        for (int j =0+empty; j < size.y-empty; ++j)
                        {
                            outTex.SetPixel(start.x+i, maxSize.y-start.y-j-1,tex.GetPixel(i,size.y-j-1));
                        }
                    }
                }
                else
                {
                    if(child0!=null) child0.Merge(outTex,dictionary,maxSize,empty,compressRatio);   
                    if(child1!=null) child1.Merge(outTex,dictionary,maxSize,empty,compressRatio);
                }
                
            }
            
                    
            private Texture2D DeCompress(Texture2D source,int compressRatio=1)
            {
                if (source == null) return null;
                RenderTexture renderTex = RenderTexture.GetTemporary(
                    source.width/compressRatio,
                    source.height/compressRatio,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.Default);

                Graphics.Blit(source, renderTex);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTex;
                Texture2D readableText = new Texture2D(source.width/compressRatio, source.height/compressRatio);
                readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                readableText.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTex);
                return readableText;
            }
            
        }
        
        private class QuickSearchTexture2D
        {
            private Vector2Int maxSize;
            private Vector2Int minSize;
            private Dictionary<(int width, int height), Stack<Texture2D>> elements; //元组
            public Vector2Int MaxSize
            {
                get { return maxSize; }
            }
            public Vector2Int MinSize
            {
                get { return minSize; }
            }
            public QuickSearchTexture2D()
            {
                maxSize = new Vector2Int(0, 0);
                minSize = new Vector2Int(Int32.MaxValue, Int32.MaxValue);
                elements = new Dictionary<(int, int), Stack<Texture2D>>();
            }

            public void Construct(List<Texture2D> _listTexture2D = null,int compressRatio=1)
            {
                if (_listTexture2D == null)
                {
                    Debug.LogWarning("输入为空");
                    return;
                }

                maxSize = new Vector2Int(0, 0);
                minSize = new Vector2Int(Int32.MaxValue, Int32.MaxValue);
                elements = new Dictionary<(int, int), Stack<Texture2D>>();
                foreach (var texture2D in _listTexture2D)
                {
                    if (texture2D == null) continue;
                    Vector2Int size = new Vector2Int(texture2D.width/compressRatio, texture2D.height/compressRatio);
                    maxSize = Vector2Int.Max(size, maxSize);
                    minSize = Vector2Int.Min(size, minSize);
                    var key = (texture2D.width/compressRatio, texture2D.height/compressRatio);
                    if (!elements.ContainsKey(key))
                        elements[key] = new Stack<Texture2D>();
                    elements[key].Push(texture2D);
                }
            }

            public bool Empty()
            {
                return elements.Count <= 0;
            }

            public Texture2D GetTexture(Vector2Int size)
            {
                Texture2D res = null;
                var key = (size.x, size.y);
                if (elements.ContainsKey(key))
                {
                    var que = elements[key];
                    res = que.Pop();
                    if (que.Count <= 0) elements.Remove(key);
                }
                return res;
            }

            public bool ExistTexture(Vector2Int size)
            {
                var key = (size.x, size.y);
                return elements.ContainsKey(key);
            }
            
        }
        
        private enum BlendMode
        {
            AlphaBlend = 0,
            Add = 1
        }

    }
}