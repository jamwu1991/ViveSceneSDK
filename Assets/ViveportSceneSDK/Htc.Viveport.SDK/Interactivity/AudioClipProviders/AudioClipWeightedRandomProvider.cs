

using System;
using System.Linq;
using UnityEngine;

namespace Htc.Viveport.SDK
{
    [CreateAssetMenu(menuName = "Audio Clip Providers/Weighted Random")]
    public class AudioClipWeightedRandomProvider : AudioClipProvider
    {
        [Serializable]
        private struct WeightedEntry
        {
            public AudioClipPlayInfo PlayInfo;
            [Range(0.0f, 1.0f)] public float Weight;

            public WeightedEntry(AudioClipPlayInfo playInfo, float weight = 1.0f)
            {
                PlayInfo = playInfo;
                Weight = weight;
            }

            public WeightedEntry(float weight = 1.0f)
            {
                PlayInfo = new AudioClipPlayInfo(1.0f, 1.0f);
                Weight = weight;
            }
        }

        [SerializeField] private WeightedEntry[] _entries = { new WeightedEntry(1.0f), };

        private AudioClipPlayInfo _next;

        public override int Count
        {
            get { return _entries.Length; }
        }

        public override AudioClipPlayInfo Next
        {
            get
            {
                var idx = GetIndex();
                return _entries[idx].PlayInfo;
            }
        }

        private int GetIndex()
        {
            var totalWeight = _entries.Sum(e => e.Weight);
            var choice = UnityEngine.Random.value * totalWeight;

            for (var idx = 0; idx < _entries.Length; ++idx)
            {
                var e = _entries[idx];
                if (choice < e.Weight) return idx;
                choice -= e.Weight;
            }

            return _entries.Length - 1;
        }
    }
}