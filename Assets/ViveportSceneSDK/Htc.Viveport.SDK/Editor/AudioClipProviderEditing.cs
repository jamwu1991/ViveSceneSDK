
using UnityEditor;
using UnityEngine;

namespace Htc.Viveport.SDK
{
    //[CustomPropertyDrawer(typeof(AudioClipPlayInfo))]
    //public class AudioClipPlayInfoDrawer : PropertyDrawer
    //{
    //    private static readonly GUIContent SliderContent = new GUIContent("Pitch Range", "Range for random pitch variation.");

    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        property.serializedObject.UpdateIfDirtyOrScript();

    //        EditorGUI.BeginProperty(position, label, property);

    //        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

    //        // override indent
    //        var indent = EditorGUI.indentLevel;
    //        EditorGUI.indentLevel = 0;

    //        // setup rects
    //        var clipRect = new Rect(position.x, position.y, 100.0f, position.height);
    //        var minMaxRect = new Rect(position.x + 100.0f, position.y, 100.0f, position.height);

    //        EditorGUI.PropertyField(clipRect, property.FindPropertyRelative("Clip"), GUIContent.none);

    //        // min/max slider
    //        var minProp = property.FindPropertyRelative("MinPitch");
    //        var maxProp = property.FindPropertyRelative("MinPitch");

    //        var min = minProp.floatValue;
    //        var max = maxProp.floatValue;

    //        EditorGUI.MinMaxSlider(minMaxRect, SliderContent, ref min, ref max, -3.0f, 3.0f);

    //        minProp.floatValue = min;
    //        maxProp.floatValue = max;

    //        property.serializedObject.ApplyModifiedProperties();

    //        // reset indent
    //        EditorGUI.indentLevel = indent;

    //        EditorGUI.EndProperty();
    //    }
    //}

    //[CustomEditor(typeof(AudioClipVelocityProvider))]
    //public sealed class AudioClipVelocityProviderEditor : Editor
    //{
    //    private const string MinPropName = "_minVelocity";
    //    private const string MaxPropName = "_maxVelocity";

    //    private static readonly GUIContent SliderContent = new GUIContent("Velocity Range", "The range for the velocity, used to pick how far into the Play Infos array we pick.");

    //    public override void OnInspectorGUI()
    //    {
    //        serializedObject.UpdateIfDirtyOrScript();

    //        DrawPropertiesExcluding(serializedObject, MinPropName, MaxPropName);
    //        var isDirty = serializedObject.ApplyModifiedProperties();
            
    //        var minProp = serializedObject.FindProperty(MinPropName);
    //        var maxProp = serializedObject.FindProperty(MaxPropName);

    //        var min = minProp.floatValue;
    //        var max = maxProp.floatValue;

    //        EditorGUILayout.MinMaxSlider(SliderContent, ref min, ref max, 0.0f, 100.0f);

    //        minProp.floatValue = min;
    //        maxProp.floatValue = max;

    //        isDirty |= serializedObject.ApplyModifiedProperties();
            
    //        if(isDirty)
    //            EditorUtility.SetDirty(serializedObject.targetObject);
    //    }
    //}
}