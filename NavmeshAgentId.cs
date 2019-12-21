using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR

#endif


namespace Cratesmith.InspectorTypes
{
    [System.Serializable]
    public class NavMeshAgentId
    {
        public int value;

        public static implicit operator int(NavMeshAgentId source)
        {
            return source.value;
        }

        public static implicit operator NavMeshAgentId(int source)
        {
            return new NavMeshAgentId() { value = source };
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(NavMeshAgentId))]
        public class Drawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, prop);


                var valueProp = prop.FindPropertyRelative("value");
                var values = Enumerable.Range(0, NavMesh.GetSettingsCount()).Select(x => NavMesh.GetSettingsByIndex(x).agentTypeID).ToArray();
                var names = values.Select(x => new GUIContent(NavMesh.GetSettingsNameFromID(x))).ToArray();
                var currentValue = valueProp.intValue;

                if(valueProp.hasMultipleDifferentValues)
                {
                    currentValue = -1;
                }

                EditorGUI.BeginChangeCheck();
                var result = EditorGUI.IntPopup(position, label, currentValue, names, values);
                if(EditorGUI.EndChangeCheck())
                {
                    valueProp.intValue = result;
                }

                EditorGUI.EndProperty();
            }
        }
#endif
    }
}

