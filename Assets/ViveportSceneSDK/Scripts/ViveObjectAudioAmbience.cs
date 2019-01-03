using System;
using UnityEngine;
using UnityEngine.Audio;
using Htc.Viveport;
using Htc.Viveport.Store;
using UniRx;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PreviewAudioContext))]

[System.Serializable]
public class ViveObjectAudioAmbience : MonoBehaviour
{
	public enum Distance
	{
		Near = 1,
		Medium = 2,
		Far = 3
	}

	public AudioClip[] ambientSounds;

	public float timeMinimum = 5f;
	public float timeMaximum = 12f;

	[Range(0f, 1f)]
	public float soundVolume = 0.4f;

	public Distance soundDistance = Distance.Far;

	public AudioSource ambientSpeaker;

	private float minDistance = -5f;
	private float maxDistance = 5f;
	private float waitTime;
	private int lastSound;
	private int clipId = -1;

	private PreviewAudioContext output;

	private void Cache()
	{
		if(ambientSpeaker == null)
			ambientSpeaker = GetComponent<AudioSource>();
	}

	private void Reset()
	{
		Cache();
	}

	private void OnValidate()
	{
		Cache();
	}

	private void Awake ()
    {		
		Cache ();

		//Initialize the speaker.
		ambientSpeaker.playOnAwake = false;
		ambientSpeaker.spatialBlend = 1.0f;
		ambientSpeaker.volume = soundVolume;

		//Set the sound rolloff distance
		var distanceMultiplier = (int)soundDistance;
		minDistance = minDistance * distanceMultiplier;
		maxDistance = maxDistance * distanceMultiplier;

		var dm = (float)distanceMultiplier;
		ambientSpeaker.minDistance = 1.0f - dm * 0.2f;
		waitTime = Random.Range (timeMinimum, timeMaximum);
        
        ScheduleRandomSound();
	}

	private void RandomSound()
	{
		//Find a new sound to play
		clipId = GetNextClipId ();
		var clip = ambientSounds [clipId];

		//Find a new position
		gameObject.transform.position = new Vector3(
            Random.Range(minDistance,maxDistance), 
            Random.Range(minDistance,maxDistance), 
            Random.Range(minDistance,maxDistance));

		//Play random sound from position
		ambientSpeaker.PlayOneShot(clip, 1.0f);

		//Find a randomized delay time
		waitTime = Random.Range (timeMinimum, timeMaximum);

	    ScheduleRandomSound();
	}

    private void ScheduleRandomSound()
    {
        Observable
            .Timer(TimeSpan.FromSeconds(waitTime))
            .Subscribe(_ => RandomSound());
    }

	private int GetNextClipId()
	{
		var size = ambientSounds.Length;

		if (size <= 1)
			return 0;

		var nextClipId = clipId;

		while (nextClipId == clipId) 
		{
			nextClipId = Random.Range (0, size);
		}

		return nextClipId;
	}
}
