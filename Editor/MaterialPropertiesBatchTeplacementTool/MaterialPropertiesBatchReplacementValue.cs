
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace SYEfficiencyToolkit
{

    public class PropertiesGroup//
    {
        public bool fold;
        public string name;
        private Material material;
        private List<PropertiesValues> valuesOfAll;
        private List<PropertiesValues> valuesOfSelect;
        private List<char> chars;
        private ReorderableList reorderableList;
        private ReorderableList reorderableListEmpty;

        private CustomShaderPropertyType type;
        
        public PropertiesGroup(string name,List<PropertiesValues> values,List<(Material mat,string name)> showMats)
        {
            this.name = name;
            fold = false;
            valuesOfAll = values;
            valuesOfSelect = new List<PropertiesValues>();
            type =(CustomShaderPropertyType)15;
            foreach (var pro in valuesOfAll)
            {
                valuesOfSelect.Add(pro);
            }
            SetReorderableList(showMats,null);
        }

        public void ResetFold()
        {
            foreach (var variable in valuesOfAll)
            {
                variable.Fold = false;
            }
        }
        
        public void ShowGUI(Rect rect)
        {
            if(fold) reorderableList.DoList(rect);
            else reorderableListEmpty.DoList(rect);
        }

        public float GetHeight()
        {
            return fold? reorderableList.GetHeight():reorderableListEmpty.GetHeight();
        }

        public List<PropertiesValues> GetData()
        {
            return valuesOfAll;
        }

        private void SetReorderableList(List<(Material mat,string name)> showMats,Material mat)
        {
            chars = new List<char>();
            reorderableList = new ReorderableList(valuesOfSelect, typeof(PropertiesValues),
                true, true, false, false);
            reorderableList.drawElementCallback = (Rect rect, int index, bool a, bool b) =>
            {
                if (valuesOfSelect.Count != 0)
                    valuesOfSelect[index].ShowGUI(rect, showMats.ToArray());
            };
            reorderableList.drawHeaderCallback = DrawHeaderCallBack;
            reorderableList.elementHeightCallback = (int index) =>
                valuesOfSelect[index].Fold ? valuesOfSelect[index].SingleHeight * (showMats.Count) + 20 : 20;
            chars = new List<char>();
            reorderableListEmpty = new ReorderableList(chars, typeof(char), false, true, false, false);
            reorderableListEmpty.drawHeaderCallback = DrawHeaderCallBack;
            reorderableListEmpty.drawElementCallback = (Rect r, int index, bool a, bool b) => { };
            reorderableListEmpty.footerHeight = 10;
            reorderableListEmpty.elementHeight = 0;
        }

        private void DrawHeaderCallBack(Rect rect)
        {
            fold = EditorGUI.BeginFoldoutHeaderGroup(new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height),
                fold, name);
            EditorGUI.EndFoldoutHeaderGroup();
             
            EditorGUI.BeginChangeCheck();
            type=(CustomShaderPropertyType)EditorGUI.EnumFlagsField(
                new Rect(rect.x + rect.width * 0.8f, rect.y, rect.width * 0.2f, rect.height), type);
            if ((int)type == -1) type = (CustomShaderPropertyType)15;
            if (EditorGUI.EndChangeCheck())
            {
                valuesOfSelect.Clear();
                foreach (var value in valuesOfAll)
                {
                    if ((value.Type & type) != 0)
                    {
                        valuesOfSelect.Add(value);
                    }
                }
            }
        }

    }
    
    
    
    
    
    
    
    
    
    
    public interface PropertiesValues
    {
        public bool Fold { set; get; } 
        public float SingleHeight { get; }
        public CustomShaderPropertyType Type { get; }
        public void ShowGUI(Rect rect, (Material mat,string name)[] materials);
        // public List<int> GetChange();
        public bool EqualValue(Material i, Material j);
    }
    //
    public abstract class PropertiesValueBase<T> : PropertiesValues
    {
        private bool fold;
        protected float singleHeight;
        protected string propertyName;
        protected int propertyId;
        protected CustomShaderPropertyType type;
        
        // private T originalValue;//物体的初始值
        // public Dictionary<int, T> dictionary; //Key-Value  索引-值
        
        public bool Fold
        {
            get => fold;
            set => fold = value;
        }
        public float SingleHeight
        {
            get => singleHeight;
            set => singleHeight = value;
        }
        
        public CustomShaderPropertyType Type
        {
            get => type; }

        protected PropertiesValueBase(string name,int id)
        {
            fold = false;
            propertyName = name;
            propertyId = id;
            // this.originalValue = originValue;
            // dictionary = new Dictionary<int, T>();
        }
        
        public void ShowGUI(Rect rect, (Material mat,string name)[] materials)
        {
            float ratio = 0.7f;
            fold = EditorGUI.BeginFoldoutHeaderGroup(new Rect(rect.x,rect.y,rect.width,20),fold, propertyName);
            EditorGUI.EndFoldoutHeaderGroup();
            ShowType(new Rect(rect.x+rect.width*ratio,rect.y,rect.width* (1-ratio),20));
            if (fold)
            {
                for (int i = 0; i <materials.Length; ++i)
                {
                    T value = GetValue(materials[i].mat);
                    T getValue = GetValue(new Rect(rect.x+20,rect.y+i*singleHeight+20,rect.width-30,singleHeight),materials[i].name, value);
                    // if (dictionary.ContainsKey(i))
                    // {
                    //     if (Equal(getValue, originalValue))
                    //     {
                    //         if (dictionary.ContainsKey(i)) dictionary.Remove(i);
                    //     }
                    //     else if (!Equal(getValue, dictionary[i]))
                    //     {
                    //         dictionary[i] = getValue;
                    //     }
                    // }
                    // else
                    // {
                    //     if (!Equal(getValue, originalValue))
                    //     {
                    //         dictionary.Add(i,getValue);
                    //     }
                    // }
                    if (!Equal(getValue, value))
                    {
                        Undo.RecordObject(materials[i].mat, "撤回");
                        if (materials[i].mat != null) SetValue(materials[i].mat, getValue);
                    }
                    //Undo.postprocessModifications += MyPostprocessModificationsCallback;
                }
            }
        }

        // UndoPropertyModification[] MyPostprocessModificationsCallback(UndoPropertyModification[] modifications)
        // {
        //     // here, you can perform processing of the recorded modifications before returning them
        //     int n = modifications.Length - 1;
        //     Debug.Log( modifications[n].currentValue.objectReference.name );
        //     Debug.Log( modifications[n].previousValue.objectReference.name );
        //     return modifications;
        // }
        protected abstract void SetValue(Material material,T value);
        protected abstract T GetValue(Rect rect,string name,T value);

        protected abstract T GetValue(Material material);
        protected abstract void ShowType(Rect rect);
        protected abstract bool Equal(T a,T b);

        // public List<int> GetChange()
        // {
        //     List<int> res = new List<int>();
        //     int count = MaterialData.Count;
        //     foreach (var variable in dictionary)
        //     {
        //         if(variable.Key>=count) continue;
        //         res.Add(variable.Key);
        //     }
        //     return res;
        // }

        public bool EqualValue(Material i, Material j)
        {
            return Equal(GetValue(i), GetValue(j));
        }
    }
    
    public class PropertiesValueTexture : PropertiesValueBase<Texture>
    {
        public PropertiesValueTexture(string name, int id) : base(name,id)
        {
            singleHeight = 80;
            type = CustomShaderPropertyType.Texture;
        }
        
        protected override Texture GetValue(Rect rect, string name, Texture value)
        {
            var temp = EditorGUI.ObjectField(rect, name+": ", value, typeof(Texture)) as Texture;
            return temp;
        }

        protected override bool Equal(Texture a, Texture b)
        {
            return a == b;
        }

        protected override void ShowType(Rect rect)
        {
            GUI.Label(rect,"Texture");
        }

        protected override void SetValue(Material material,Texture value)
        {
            material.SetTexture(propertyId,value);
        }

        protected override Texture GetValue(Material material)
        {
            return material.GetTexture(propertyId);
        }
    }
    
    public class PropertiesValueVector : PropertiesValueBase<Vector4>
    {
        public PropertiesValueVector(string name, int id) : base(name,id)
        {
            singleHeight = 40;
            type = CustomShaderPropertyType.Vector;
        }
        
        protected override Vector4 GetValue(Rect rect, string name, Vector4 value)
        {
            var temp = EditorGUI.Vector4Field(rect, name+": ", value) ;
            return temp;
        }

        protected override bool Equal(Vector4 a, Vector4 b)
        {
            return a == b;
        }
        protected override void ShowType(Rect rect)
        {
            GUI.Label(rect,"Vector");
        }

        protected override void SetValue(Material material,Vector4 value)
        {
            material.SetVector(propertyId,value);
        }
        protected override Vector4 GetValue(Material material)
        {
            return material.GetVector(propertyId);
        }
    }
    
    public class PropertiesValueColor : PropertiesValueBase<Color>
    {
        public PropertiesValueColor(string name,int id) : base(name,id)
        {
            singleHeight = 20;
            type = CustomShaderPropertyType.Color;
        }
        
        protected override Color GetValue(Rect rect, string name, Color value)
        {
            var temp = EditorGUI.ColorField(rect, name+": ", value) ;
            return temp;
        }

        protected override bool Equal(Color a, Color b)
        {
            return a == b;
        }
        protected override void ShowType(Rect rect)
        {
            GUI.Label(rect,"Color");
        }

        protected override void SetValue(Material material,Color value)
        {
            material.SetColor(propertyId,value);
        }
        
        protected override Color GetValue(Material material)
        {
            return material.GetColor(propertyId);
        }
    }

    public class PropertiesValueColorHDR : PropertiesValueColor
    {
        public PropertiesValueColorHDR(string name,int id) : base(name,id)
        {
        }
        
        protected override Color GetValue(Rect rect, string name, Color value)
        {
            var temp = EditorGUI.ColorField(rect, new GUIContent(name+": "), value,true,true,true) ;
            return temp;
        }
    }
    

    public class PropertiesValueFloat : PropertiesValueBase<float>
    {
        public PropertiesValueFloat(string name,int id) : base(name,id)
        {
            singleHeight = 20;
            type = CustomShaderPropertyType.Float;
        }

        protected override float GetValue(Rect rect, string name, float value)
        {
            var temp = EditorGUI.FloatField(rect, name+": ", value);
            return temp;
        }

        protected override bool Equal(float a, float b)
        {
            return a == b;
        }

        protected override void ShowType(Rect rect)
        {
            GUI.Label(rect, "Float");
        }

        protected override void SetValue(Material material, float value)
        {
            material.SetFloat(propertyId, value);
        }

        protected override float GetValue(Material material)
        {
            return material.GetFloat(propertyId);
        }
    }

    public class PropertiesValueRange : PropertiesValueFloat
    {
        private float limitsMin = 0;
        private float limitsMax = 1;
        public PropertiesValueRange(string name,int id,float min,float max) : base(name,id)
        {
            limitsMax = max;
            limitsMin = min;
        }

        protected override float GetValue(Rect rect, string name, float value)
        {
            var temp = EditorGUI.Slider(rect, name+": ", value,limitsMin,limitsMax);
            return temp;
        }

    }


    public enum CustomShaderPropertyType
    {
        Texture=1,
        Float=2,
        Color=4,
        Vector=8
    }


    
    
}