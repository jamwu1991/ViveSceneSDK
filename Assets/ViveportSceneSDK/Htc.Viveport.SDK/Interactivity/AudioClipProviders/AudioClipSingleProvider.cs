

using UnityEngine;

namespace Htc.Viveport.SDK
{
    [CreateAssetMenu(menuName = "Audio Clip Providers/Single")]
    public class AudioClipSingleProvider : AudioClipProvider
    {
        [SerializeField] private AudioClipPlayInfo _playInfo = new AudioClipPlayInfo(1.0f, 1.0f);

        public override int Count
        {
            get { return 1; }
        }

        public override AudioClipPlayInfo Next
        {
            get { return _playInfo; }
        }

        public static AudioClipProvider CreateFromClip(AudioClip clip)
        {
            var acp = ScriptableObject.CreateInstance<AudioClipSingleProvider>();

            acp._playInfo = new AudioClipPlayInfo(clip, 1.0f, 1.0f);

            return acp;
        }
    }
}