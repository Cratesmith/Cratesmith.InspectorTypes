// TypeId.cs
// 
// 

using System;
using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif
namespace Cratesmith.InspectorTypes
{
    public abstract class TypeId : ISerializationCallbackReceiver
    {
#pragma warning disable 649
        [SerializeField] string m_TypeName;
#pragma warning restore 649
        public System.Type value { get; private set; }
        protected abstract System.Type baseType { get; }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            value = LookupType(m_TypeName);
        }

        public static Type LookupType(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName))
            {
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = a.GetType(typeName);
                    if (type != null) return type;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(TypeId), true)]
        [CanEditMultipleObjects]
        public class Drawer : UnityEditor.PropertyDrawer
        {
            Type[] GetKeyTypes(System.Type keyType)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                return assemblies.SelectMany(x=>x.GetTypes())
                    .Where(x=>keyType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract && !x.IsGenericTypeDefinition)
                    .ToArray();
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var target = property.serializedObject.targetObject;
                var typeId = fieldInfo.GetValue(target) as TypeId;
                var typeNameProp = property.FindPropertyRelative(nameof(m_TypeName));
                var currentType = TypeId.LookupType(typeNameProp.stringValue);
                var types = GetKeyTypes(typeId.baseType);
                var currentIndex = System.Array.IndexOf(types, currentType);
                var typeNames = types.Select(x => new GUIContent(x.Name)).ToArray();

                var newIndex = EditorGUI.Popup(position, label, currentIndex, typeNames);
                typeNameProp.stringValue = (newIndex>=0 && newIndex<types.Length) ? types[newIndex].FullName: "";
            }
        }
#endif
    }

    public abstract class TypeId<T> : TypeId, IEquatable<System.Type>
    {
        protected override Type baseType => typeof(T);

        public static implicit operator Type(TypeId<T> @this)
        {
            return @this.value;
        }

        public bool Equals(Type other)
        {
            return value == other;
        }
    }
}