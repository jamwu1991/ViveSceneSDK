using UnityEngine;
using UnityEditor;

/// <summary>
/// Modified from this gist: https://gist.github.com/frarees/9791517
/// </summary>
[CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
class MinMaxSliderDrawer : PropertyDrawer
{
    private static readonly GUIContent[] MinMaxNames = { new GUIContent("-"), new GUIContent("+") };
    private static readonly float[] Values = new float[2];
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.Vector2)
        {
            var range = property.vector2Value;
            var min = range.x;
            var max = range.y;
            var attr = attribute as MinMaxSliderAttribute;
            //var half = position.width * 0.5f;

            EditorGUI.BeginChangeCheck();

            var sliderRect = position;
            sliderRect.width = position.width * 0.7f;
            EditorGUI.MinMaxSlider(sliderRect, label, ref min, ref max, attr.min, attr.max);

            // show values after slider
            EditorGUI.BeginChangeCheck();

            var numbersRect = position;
            numbersRect.x += position.width * 0.7f;
            numbersRect.width = position.width * 0.3f;
            Values[0] = min;
            Values[1] = max;
            EditorGUI.MultiFloatField(numbersRect, MinMaxNames, Values);

            if (EditorGUI.EndChangeCheck())
            {
                range.x = Values[0];
                range.y = Values[1];

                if (range.x > range.y) range.x = range.y;
                if (range.y < range.x) range.y = range.x;

                range.x = Mathf.Clamp(range.x, attr.min, attr.max);
                range.y = Mathf.Clamp(range.y, attr.min, attr.max);


                property.vector2Value = range;

                min = range.x;
                max = range.y;
            }

            if (EditorGUI.EndChangeCheck())
            {
                range.x = min;
                range.y = max;
                property.vector2Value = range;
            }
        }
        else
        {
            EditorGUI.LabelField(position, label, "Use only with Vector2");
        }
    }
}