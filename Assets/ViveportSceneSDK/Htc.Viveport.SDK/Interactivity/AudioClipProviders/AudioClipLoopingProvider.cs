

using UnityEngine;

namespace Htc.Viveport.SDK
{
    [CreateAssetMenu(menuName = "Audio Clip Providers/Looping Array")]
    public class AudioClipLoopingProvider : AudioClipProvider
    {
        [SerializeField] private AudioClipPlayInfo[] _clips = { new AudioClipPlayInfo(1.0f, 1.0f) };
        private int _clipIndex = 0;

        public override int Count
        {
            get { return _clips.Length; }
        }

        private void MoveNext()
        {
            _clipIndex = (_clipIndex + 1) % Count;
            _clipIndex = Mathf.Clamp(_clipIndex, 0, Count - 1);
        }

        public override AudioClipPlayInfo Next
        {
            get
            {
                var nextClip = _clips[_clipIndex];

                MoveNext();

                return nextClip;
            }
        }
    }

}