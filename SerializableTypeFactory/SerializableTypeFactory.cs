//
// Cratesmith 2017
//

using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cratesmith.InspectorTypes
{

    #region example_usage
// Example usage:
//
//public class ActorHudMinimapIcon : Actor
//{
//    [Serializable]
//    public class Factory : SerializableTypeFactory<MinimapIcon, ActorHudMinimapIcon, HudPawnShipMinimap>
//    {
//        protected override ActorHudMinimapIcon CreateInstance(MinimapIcon key, ActorHudMinimapIcon prefab,
//            HudPawnShipMinimap owner)
//        {
//            var pool = ManagerContainer.GetManager<Pool>(owner.gameObject.scene);
//            var hud = pool.Spawn(prefab);
//            hud.Ref.Setup(key, owner);
//            return hud;
//        }
//
//        public override bool CheckPrefabCompatibility(Type keyType, ActorHudMinimapIcon prefab)
//        {
//            return prefab.CheckCompatibility(keyType);
//        }
//    }
//
//    protected virtual bool CheckCompatibility(Type keyType)
//    {
//        return true;
//    }
//}
//
//public class ActorHudMinimap : Actor
//{
//    public ActorHudMinimapIcon.Factory iconFactory = new ActorHudMinimapIcon.Factory();
//
//    private void AddIcon(MinimapIcon icon)
//    {
//        RemoveIcon(icon);
//        var newIcon = iconFactory.Create(icon, this);
//        if (newIcon != null)
//        {
//            newIcon.transform.SetParent(transform);
//        }
//    }
//}
    #endregion

    public abstract class SerializableTypeFactoryBase
    {
        public abstract System.Type KeyType { get; }
        public abstract System.Type ObjectType { get; }
        public abstract bool CheckPrefabCompatibility(System.Type keyType, Object prefab);

        protected static System.Type LookupType(string name)
        { 
            System.Type type = null;
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(name);
                if (type != null) break;
            }
            return type;
        }

        #region drawer
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SerializableTypeFactoryBase), true)]
        public class SerializableTypeFactoryDrawer : UnityEditor.PropertyDrawer
        {
            const int HEADER_HEIGHT = 17;
            const int LINE_HEIGHT = 17;
            const int ERROR_LINE_HEIGHT = 51;

            string[] GetKeyTypes(System.Type keyType)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                return assemblies.SelectMany(x=>x.GetTypes())
                    .Where(x=>keyType.IsAssignableFrom(x) && ((!x.IsInterface && !x.IsGenericTypeDefinition) || x==keyType))
                    .Select(x=>x.FullName)
                    .ToArray();
            }
    
            public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
            {
                var target = property.serializedObject.targetObject;
                if (target == null)
                {
                    return base.GetPropertyHeight(property,label);
                }
                var factory = fieldInfo.GetValue(target) as SerializableTypeFactoryBase;
                if (factory == null) 
                {
                    return base.GetPropertyHeight(property,label);
                }

                var foldout = EditorPrefs.GetBool(property.propertyPath+"_foldout");
                if (foldout)
                {
                    var yHeight = HEADER_HEIGHT;

                    var table = GetTable(property);
                    var keyTypes = GetKeyTypes(factory.KeyType);
                    foreach (var keyTypeName in keyTypes)
                    {
                        Object prefab = null;
                        var keyType = LookupType(keyTypeName);
                        yHeight += (table.TryGetValue(keyTypeName, out prefab) && !factory.CheckPrefabCompatibility(keyType, prefab))
                            ? ERROR_LINE_HEIGHT
                            : LINE_HEIGHT;                   
                    }
                    return yHeight;
                }
                else
                {
                    return HEADER_HEIGHT;
                }
            }

            public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
            {
                var target = property.serializedObject.targetObject;
                if (target == null) return;
                var factory = fieldInfo.GetValue(target) as SerializableTypeFactoryBase;
                if (factory == null) return;

                var factoryKeyType = factory.KeyType;
                var factoryObjectType = factory.ObjectType;

                var foldout = EditorPrefs.GetBool(property.propertyPath+"_foldout");

                EditorGUI.BeginChangeCheck();
                var foldoutRect = new Rect(position.position, new Vector2(position.width,LINE_HEIGHT));
                foldout = EditorGUI.Foldout(foldoutRect, foldout, label, true);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(property.propertyPath+"_foldout", foldout);
                }
            
                if (!foldout)
                    return;

                position.yMin += LINE_HEIGHT;

                var table = GetTable(property);


                EditorGUI.BeginChangeCheck();
                var keyTypes = GetKeyTypes(factoryKeyType);           
            
                ++UnityEditor.EditorGUI.indentLevel;
                float currentY = 0;
                for (int i = 0; i < keyTypes.Length; i++)
                {
                    Object prevValue = null;
                    table.TryGetValue(keyTypes[i], out prevValue);

                    var rLabelField  = new Rect(position.position + new Vector2(0, currentY), new Vector2(UnityEditor.EditorGUIUtility.labelWidth, LINE_HEIGHT));
                    var rObjectField = new Rect(position.position + new Vector2(UnityEditor.EditorGUIUtility.labelWidth, currentY), new Vector2(position.width-UnityEditor.EditorGUIUtility.labelWidth, LINE_HEIGHT));
                    var rHelpBox     = new Rect(position.position + new Vector2(UnityEditor.EditorGUIUtility.labelWidth, currentY+LINE_HEIGHT), new Vector2(position.width-UnityEditor.EditorGUIUtility.labelWidth, ERROR_LINE_HEIGHT-LINE_HEIGHT));
            
                    var keyName = keyTypes[i];
                    var keyType = LookupType(keyTypes[i]);
                    if(keyType==null) continue;

                    if (keyName == factoryKeyType.FullName)
                    {
                        keyName = "<default>";
                    }
                    else
                    {
                        var lastPoint = keyName.LastIndexOf(".");
                        if (lastPoint != -1 && lastPoint+1 < keyName.Length)
                        {
                            keyName = keyName.Substring(lastPoint+1);
                        }

                        if (!keyType.IsInterface
                            && !factoryKeyType.IsInterface
                            && keyName.StartsWith(factoryKeyType.Name) 
                            && keyName.Length > factoryKeyType.Name.Length)
                        {
                            keyName = keyName.Substring(factoryKeyType.Name.Length).TrimStart(new char[] {' ','_','-'});
                        }
                    }

                    UnityEditor.EditorGUI.LabelField(rLabelField, keyName);   
                    var newValue = UnityEditor.EditorGUI.ObjectField(rObjectField, prevValue, factoryObjectType, true);
                    if (newValue != prevValue)
                    {
                        if (newValue)
                        {
                            table[keyTypes[i]] = newValue;
                        }
                        else
                        {
                            table.Remove(keyTypes[i]);
                        }               
                    }

                    if (prevValue!= null && !factory.CheckPrefabCompatibility(keyType, newValue))
                    {
                        var errorString = string.Format("{0} is not compatible with {1}", 
                            prevValue.GetType().Name, 
                            keyType.Name);

                        UnityEditor.EditorGUI.HelpBox(rHelpBox, errorString, MessageType.Error);
                        currentY += ERROR_LINE_HEIGHT;
                    }
                    else
                    {
                        currentY += LINE_HEIGHT;
                    }
                }
                --UnityEditor.EditorGUI.indentLevel;

                if (EditorGUI.EndChangeCheck())
                {
                    var typeIds = property.FindPropertyRelative("_typeIds");
                    var prefabs = property.FindPropertyRelative("_prefabs");

                    var kvp = table.ToArray();
                    prefabs.arraySize = typeIds.arraySize = kvp.Length;
                    for (int i = 0; i < kvp.Length; i++)
                    {
                        typeIds.GetArrayElementAtIndex(i).stringValue = kvp[i].Key;
                        prefabs.GetArrayElementAtIndex(i).objectReferenceValue = kvp[i].Value;
                    }          
                }
            }

            Dictionary<string, Object> GetTable(SerializedProperty property)
            {
                var table = new Dictionary<string, Object>();
                var target = property.serializedObject.targetObject;
                if (target != null)
                {
                    var factory = fieldInfo.GetValue(target) as SerializableTypeFactoryBase;
                    if (factory != null)
                    {
                        var objectType = factory.ObjectType;
                        var typeIds = property.FindPropertyRelative("_typeIds");
                        var prefabs = property.FindPropertyRelative("_prefabs");
                        for (int i = 0; i < Mathf.Min(typeIds.arraySize, prefabs.arraySize); i++)
                        {
                            var typeId = typeIds.GetArrayElementAtIndex(i).stringValue;
                            var prefab = prefabs.GetArrayElementAtIndex(i).objectReferenceValue;
                            if (prefab != null && objectType.IsInstanceOfType(prefab))
                                table[typeId] = prefab;
                        }
                    }
                }
                return table;
            }
        }
#endif
        #endregion 
    }

    public abstract class SerializableTypeFactory<TKeyType, TObjectType, TOwner> 
        : SerializableTypeFactoryBase<TKeyType, TObjectType>
        where TObjectType : Object
    {
        protected abstract TObjectType CreateInstance(TKeyType key, TObjectType prefab, TOwner owner);

        public TObjectType Create<T>(T key, TOwner owner) where T:TKeyType
        {
            TObjectType prefab = GetPrefab(key);
            return prefab!=null 
                ? CreateInstance(key, prefab, owner)
                : null;
        }    
    }

    public abstract class SerializableTypeFactory<TKeyType, TObjectType> 
        : SerializableTypeFactoryBase<TKeyType, TObjectType> 
        where TObjectType : Object
    {
        protected abstract TObjectType CreateInstance(TKeyType key, TObjectType prefab);

        public TObjectType Create<T>(T key) where T:TKeyType
        {
            TObjectType prefab = GetPrefab(key);
            return prefab!=null 
                ? CreateInstance(key, prefab)
                : null;
        }
    }

    public abstract class SerializableTypeFactoryBase<TKeyType, TObjectType> 
        : SerializableTypeFactoryBase
            , ISerializationCallbackReceiver
            , IEnumerable<TObjectType>
        where TObjectType : Object
    {
        [SerializeField] List<string> _typeIds = new List<string>();
        [SerializeField] List<TObjectType> _prefabs = new List<TObjectType>();

        public override System.Type KeyType       { get { return typeof(TKeyType); } }
        public override System.Type ObjectType    { get { return typeof(TObjectType); } }

        private Dictionary<System.Type, TObjectType> table = new Dictionary<System.Type, TObjectType>();
       
        protected TObjectType GetPrefab(TKeyType key)
        {
            if (key == null)
            {
                return null;
            }

            var keyType = key.GetType();
            var prefab = default(TObjectType);

            do
            {
                if (table.TryGetValue(keyType, out prefab) && CheckPrefabCompatibility(keyType, prefab))
                {
                    return prefab;
                }
                keyType = keyType.BaseType;
            } while (keyType != null && typeof(TKeyType).IsAssignableFrom(keyType));

            if (!typeof(TKeyType).IsInterface) return null;

            if (table.TryGetValue(typeof(TKeyType), out prefab) && CheckPrefabCompatibility(typeof(TKeyType), prefab))
            {
                return prefab;
            }

            return null;
        }

        public override bool CheckPrefabCompatibility(System.Type keyType, Object prefab)
        {
            var obj = prefab as TObjectType;
            if (obj == null)
            {
                return false;
            }
            return CheckPrefabCompatibility(keyType, obj);
        }

        public virtual bool CheckPrefabCompatibility(System.Type keyType, TObjectType prefab)
        {
            return true;
        }
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            table.Clear();
            for (int i = 0; i < Mathf.Min(_typeIds.Count, _prefabs.Count); i++)
            {
                var type = LookupType(_typeIds[i]);
                if (type != null)
                {
                    table[type] = _prefabs[i];
                }
            }
        }

        public List<TObjectType>.Enumerator GetEnumerator()
        {
            return _prefabs.GetEnumerator();
        }

        IEnumerator<TObjectType> IEnumerable<TObjectType>.GetEnumerator()
        {
            return _prefabs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _prefabs).GetEnumerator();
        }
    }
}