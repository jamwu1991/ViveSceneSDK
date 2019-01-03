

using System;
using System.Linq;
using UnityEngine;

namespace Htc.Viveport.SDK
{
    [Serializable]
    public struct AudioClipPlayInfo
    {
        public AudioClip Clip;

        [MinMaxSlider(-3.0f, 3.0f)] public Vector2 PitchRange;

        public float MinPitch
        {
            get { return PitchRange.x; }
        }

        public float MaxPitch
        {
            get { return PitchRange.y; }
        }

        public AudioClipPlayInfo(AudioClip clip, float min, float max)
        {
            Clip = clip;
            PitchRange = new Vector2(min, max);
        }

        public AudioClipPlayInfo(float min, float max)
        {
            Clip = null;
            PitchRange = new Vector2(min, max);
        }
    }

    public abstract class AudioClipProvider : ScriptableObject
    {
        public abstract int Count { get; }
        public abstract AudioClipPlayInfo Next { get; }
    }
}

