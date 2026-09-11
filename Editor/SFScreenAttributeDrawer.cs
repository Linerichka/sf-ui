using System;
using System.Linq;
using SFramework.UI.Runtime;
using SFramework.Configs.Editor;
using UnityEditor;
using UnityEngine;

namespace SFramework.UI.Editor
{
    [CustomPropertyDrawer(typeof(SFScreenAttribute), true)]
    public class SFScreenAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                GUI.backgroundColor = Color.red;
                EditorGUI.LabelField(position, "Use string field!");
                GUI.backgroundColor = Color.white;
                return;
            }

            if (attribute is not SFScreenAttribute sfTypeAttribute)
            {
                GUI.backgroundColor = Color.red;
                EditorGUI.LabelField(position, "Attribute is null!");
                GUI.backgroundColor = Color.white;
                return;
            }

            var screenTypes =  GetScreenTypes();
            if (screenTypes == null || screenTypes.Length == 0)
            {
                GUI.backgroundColor = Color.red;
                EditorGUI.LabelField(position, "Screens not found: " + property.stringValue);
                GUI.backgroundColor = Color.white;
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (string.IsNullOrWhiteSpace(property.stringValue))
            {
                property.stringValue = string.Empty;
            }
            
            var qualifiedTypeNames = screenTypes.Select(x => x.AssemblyQualifiedName).ToArray();
            if (!string.IsNullOrWhiteSpace(property.stringValue) && !qualifiedTypeNames.Contains(property.stringValue))
            {
                GUI.backgroundColor = Color.red;
                property.stringValue = EditorGUI.TextField(position, property.stringValue);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                var name = qualifiedTypeNames.Contains(property.stringValue)
                    ? property.stringValue
                    : qualifiedTypeNames[0];

                var index = Array.IndexOf(qualifiedTypeNames, name);
                
                if (index == -1)
                {
                    GUI.backgroundColor = Color.red;
                }
                
                EditorGUI.BeginChangeCheck();
                
                index = EditorGUI.Popup(position, index, screenTypes.Select(t => t.Name).ToArray());
                
                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = index == -1 ? string.Empty : screenTypes[index].AssemblyQualifiedName;
                }
                
                GUI.backgroundColor = Color.white;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }   

        private Type[] GetScreenTypes()
        {
            var screens = SFConfigServiceEditor.Instance.GetConfig<SFUIConfig>().Screens?.Select(n => n.GetScreenType()).ToArray();
            return screens;
        }
    }
}