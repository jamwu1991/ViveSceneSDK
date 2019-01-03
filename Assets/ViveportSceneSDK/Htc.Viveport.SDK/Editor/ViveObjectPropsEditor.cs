using UnityEngine;
using UnityEditor;

namespace Htc.Viveport.SDK
{
    [CustomEditor( typeof( ViveObjectProps ) )]
    public class ViveObjectPropsEditor : Editor
    {
        private SerializedProperty _colliderProp;
        private SerializedProperty _rigidbodyProp;
        private SerializedProperty _pickupProp;

        private SerializedProperty _touchAudioProviderProp;
        private SerializedProperty _touchAnimProp;

        private SerializedProperty _clickAudioProviderProp;
        private SerializedProperty _clickAnimProp;

        private SerializedProperty _enterAudioProviderProp;
        private SerializedProperty _enterAnimProp;

        private SerializedProperty _impactAudioProviderProp;
        private SerializedProperty _impactAnimProp;

        private SerializedProperty _objectObjectProp;
        private SerializedProperty _objectAudioProviderProp;
        private SerializedProperty _objectAnimProp;
        private SerializedProperty _objectIsConsumedProp;

        private bool _isTouchShowing = false;
        private bool _isClickShowing = false;
        private bool _isEnterShowing = false;
        private bool _isImpactShowing = false;
        private bool _isObjectShowing = false;

        private static readonly GUIContent PhysicsSettingsContent = new GUIContent( "Physics Settings" );

        private static readonly GUIContent ColliderContent = new GUIContent( "Collider", "A collider is required for any interactions." );
        private static readonly GUIContent RigidbodyContent = new GUIContent( "Rigidbody", " A rigidbody is required for pickup." );
        private static readonly GUIContent PickupItemContent = new GUIContent( "Pick Up Item" );
        private static readonly GUIContent AudioContent = new GUIContent( "Audio", "Audio clip provider to play on this interaction." );
        private static readonly GUIContent AnimationContent = new GUIContent( "Animation", "Animation to play on this interaction." );
        private static readonly GUIContent ObjectContent = new GUIContent( "Interaction Object", "Object which triggers the interaction. Must be a Vive Object Props component." );
        private static readonly GUIContent ObjectIsConsumed = new GUIContent( "Is Object Consumed", "Should the trigger object be destroyed upon interaction?" );

        private static readonly GUIContent AddTouchContent = new GUIContent( "Add Touch Interaction", "Add data for a touch interaction, which fires when your right hand touches an object." );
        private static readonly GUIContent RemoveTouchContent = new GUIContent( "Remove Touch Interaction", "Clears touch interaction data from this object." );
        private static readonly GUIContent AddClickContent = new GUIContent( "Add Click Interaction", "Add data for a click interaction, which fires when you point at this object and pull the controller's trigger" );
        private static readonly GUIContent RemoveClickContent = new GUIContent( "Remove Click Interaction", "Clears click interaction data from this object" );
        private static readonly GUIContent AddPointerEnterContent = new GUIContent( "Add Pointer Enter Interaction", "Add data for a pointer enter/hover interaction, for when the user points at the objects." );
        private static readonly GUIContent RemovePointerEnterContent = new GUIContent( "Remove Pointer Enter Interaction", "Clears pointer enter/hover interaction data from this object." );
        private static readonly GUIContent AddImpactContent = new GUIContent( "Add Impact Interaction", "Add physics-based impact interaction from non-controller surfaces." );
        private static readonly GUIContent RemoveImpactContent = new GUIContent( "Remove Impact Interaction", "Removes physics-based impact interaction from non-controller surfaces." );
        private static readonly GUIContent AddObjectContent = new GUIContent( "Add Object Interaction", "Add object-to-object interaction." );
        private static readonly GUIContent RemoveObjectContent = new GUIContent( "Remove Object Interaction", "Removes object-to-object interaction.." );


        private void OnEnable()
        {
            _colliderProp = serializedObject.FindProperty( "objectCollider" );
            _rigidbodyProp = serializedObject.FindProperty( "body" );

            _pickupProp = serializedObject.FindProperty( "pickUpItem" );

            _touchAudioProviderProp = serializedObject.FindProperty( "touchAudioProvider" );
            _touchAnimProp = serializedObject.FindProperty( "touchAnimation" );

            _clickAudioProviderProp = serializedObject.FindProperty( "clickAudioProvider" );
            _clickAnimProp = serializedObject.FindProperty( "clickAnimation" );

            _enterAudioProviderProp = serializedObject.FindProperty( "enterAudioProvider" );
            _enterAnimProp = serializedObject.FindProperty( "enterAnimation" );

            _impactAudioProviderProp = serializedObject.FindProperty( "impactAudioProvider" );
            _impactAnimProp = serializedObject.FindProperty( "impactAnim" );

            _objectAudioProviderProp = serializedObject.FindProperty( "objectAudioProvider" );
            _objectAnimProp = serializedObject.FindProperty( "objectAnim" );
            _objectObjectProp = serializedObject.FindProperty( "objectObject" );
            _objectIsConsumedProp = serializedObject.FindProperty( "objectIsConsumed" );

            _isTouchShowing = ShouldBeShowing( _touchAudioProviderProp, _touchAnimProp );
            _isClickShowing = ShouldBeShowing( _clickAudioProviderProp, _clickAnimProp );
            _isEnterShowing = ShouldBeShowing( _enterAudioProviderProp, _enterAnimProp );
            _isImpactShowing = ShouldBeShowing( _impactAudioProviderProp, _impactAnimProp );
            _isObjectShowing = ShouldBeShowing( _objectAudioProviderProp, _objectAnimProp );
        }

        private static bool ShouldBeShowing( SerializedProperty a, SerializedProperty b )
        {
            return a.objectReferenceValue != null || b.objectReferenceValue != null;
        }

        private static void DrawTriggerGui( ref bool isShowing, GUIContent addContent, GUIContent removeContent, SerializedProperty audioProviderProp, SerializedProperty animProp, SerializedProperty objectProp = null, SerializedProperty objectIsConsumedProp = null, bool hasSpace = true )
        {
            if( !isShowing && GUILayout.Button( addContent ) ||
                isShowing && GUILayout.Button( removeContent ) )
            {
                isShowing = !isShowing;
                if( !isShowing )
                {
                    audioProviderProp.objectReferenceValue = null;
                    animProp.objectReferenceValue = null;
                }
            }

            if( isShowing )
            {
                EditorGUILayout.PropertyField( audioProviderProp, AudioContent );
                EditorGUILayout.PropertyField( animProp, AnimationContent );
                if( objectProp != null )
                {
                    EditorGUILayout.PropertyField( objectProp, ObjectContent );
                    EditorGUILayout.PropertyField( objectIsConsumedProp, ObjectIsConsumed );
                }
            }

            if( hasSpace ) EditorGUILayout.Space();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField( PhysicsSettingsContent );
            EditorGUILayout.PropertyField( _colliderProp, ColliderContent );
            EditorGUILayout.PropertyField( _rigidbodyProp, RigidbodyContent );
            EditorGUILayout.Space();

            using( new EditorGUI.DisabledScope(
                _colliderProp.objectReferenceValue == null
                || _rigidbodyProp.objectReferenceValue == null ) )
            {
                EditorGUILayout.PropertyField( _pickupProp, PickupItemContent );
            }
            EditorGUILayout.Space();

            // TOUCH
            DrawTriggerGui( ref _isTouchShowing, AddTouchContent, RemoveTouchContent, _touchAudioProviderProp, _touchAnimProp );

            // POINT & CLICK
            DrawTriggerGui( ref _isClickShowing, AddClickContent, RemoveClickContent, _clickAudioProviderProp, _clickAnimProp );

            // POINTER ENTER
            DrawTriggerGui( ref _isEnterShowing, AddPointerEnterContent, RemovePointerEnterContent, _enterAudioProviderProp, _enterAnimProp );

            // PHYSICS IMPACT
            DrawTriggerGui( ref _isImpactShowing, AddImpactContent, RemoveImpactContent, _impactAudioProviderProp, _impactAnimProp );

            // OBJECT-TO-OBJECT
            DrawTriggerGui( ref _isObjectShowing, AddObjectContent, RemoveObjectContent, _objectAudioProviderProp, _objectAnimProp, _objectObjectProp, _objectIsConsumedProp, false );

            serializedObject.ApplyModifiedProperties();
        }
    }
}
