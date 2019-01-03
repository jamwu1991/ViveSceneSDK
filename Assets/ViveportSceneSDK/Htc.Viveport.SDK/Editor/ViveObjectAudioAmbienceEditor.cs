using UnityEngine;
using UnityEditor;

namespace Htc.Viveport.SDK
{
	[CustomEditor(typeof(ViveObjectAudioAmbience))]
	public class ViveObjectAudioAmbienceEditor : Editor 
	{
		private SerializedProperty _soundsSpeakerProp;
		private SerializedProperty _distanceProp;
		private SerializedProperty _volumeProp;
		private SerializedProperty _timeMinProp;
		private SerializedProperty _timeMaxProp;
		private SerializedProperty _soundsProp;

		private bool _isAmbienceShowing = false;

		private static readonly GUIContent AudioSourceContent = new GUIContent("Audio Source", "An audio source is requried for an ambient sound array.");
		private static readonly GUIContent AmbientSoundsContent = new GUIContent("Ambient sounds", "Place ambient sounds here.");
		private static readonly GUIContent DistanceContent = new GUIContent("Sound Distance", "Sets the farthest point a sound can play and affects Audio Source falloff minimum distance.");
		private static readonly GUIContent VolumeContent = new GUIContent("Sound Volume", "Sets audio source volume. Recommended range is 0.1 - 0.4 for ambience.");
		private static readonly GUIContent TimeMinContent = new GUIContent("Random Time Minimum", "The shortest time value in seconds for an ambient sound to play.");
		private static readonly GUIContent TimeMaxContent = new GUIContent("Random Time Maximum", "The longest time value in seconds for an ambient sound to play.");

		private static readonly GUIContent AddAmbientSounds = new GUIContent("Add ambient sounds", "Add audio clips of ambient sounds to cycle through.");
		private static readonly GUIContent RemoveAmbientSounds = new GUIContent("Remove ambient sounds", "Remove audio clips of ambient sounds to cycle through.");

		private void OnEnable()
		{
			_soundsSpeakerProp = serializedObject.FindProperty ("ambientSpeaker");
			_soundsProp = serializedObject.FindProperty ("ambientSounds");
			_distanceProp = serializedObject.FindProperty ("soundDistance");
			_volumeProp = serializedObject.FindProperty ("soundVolume");
			_timeMinProp = serializedObject.FindProperty ("timeMinimum");
			_timeMaxProp = serializedObject.FindProperty ("timeMaximum");
			_isAmbienceShowing = ShouldBeShowing (_soundsSpeakerProp, _soundsProp, _distanceProp, _volumeProp, _timeMinProp, _timeMaxProp);
		}

		private static bool ShouldBeShowing(SerializedProperty a, SerializedProperty b, SerializedProperty c, SerializedProperty d, SerializedProperty e, SerializedProperty f)
		{
			return a.objectReferenceValue != null || b.objectReferenceValue != null || c.objectReferenceValue != null || d.objectReferenceValue != null || e.objectReferenceValue != null || f.objectReferenceValue != null;	
		}

		private static void DrawTriggerGui(ref bool isShowing, GUIContent addContent, GUIContent removeContent, SerializedProperty speakerProp, SerializedProperty soundsProp, SerializedProperty distanceProp, SerializedProperty volumeProp, SerializedProperty timeMinProp, SerializedProperty timeMaxProp, bool hasSpace = true)
		{
			if (!isShowing && GUILayout.Button(addContent) ||
				isShowing && GUILayout.Button(removeContent))
			{
				isShowing = !isShowing;
				if (!isShowing) 
				{
					speakerProp = null;
					soundsProp = null;
					distanceProp = null;
					volumeProp = null;
					timeMinProp = null;
					timeMaxProp = null;
				}
				
			}

			if (isShowing) 
			{
				EditorGUILayout.PropertyField (speakerProp, AudioSourceContent);
				EditorGUILayout.PropertyField (soundsProp, AmbientSoundsContent, true);
				EditorGUILayout.PropertyField (distanceProp, DistanceContent);
				EditorGUILayout.PropertyField (volumeProp, VolumeContent);
				EditorGUILayout.PropertyField (timeMinProp, TimeMinContent);
				EditorGUILayout.PropertyField (timeMaxProp, TimeMaxContent);
			}

			if (hasSpace) EditorGUILayout.Space ();

		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space ();
			DrawTriggerGui (ref _isAmbienceShowing, AddAmbientSounds, RemoveAmbientSounds, _soundsSpeakerProp, _soundsProp, _distanceProp, _volumeProp, _timeMinProp, _timeMaxProp);
			EditorGUILayout.Space ();
			serializedObject.ApplyModifiedProperties();
		}

	}
}
