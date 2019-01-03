


using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Htc.Viveport.Store
{
    public class PreviewAudioContext : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroupBindings _mixerGroupKey = AudioMixerGroupBindings.AllPreviewSceneSFX;
        
        public void Bind(Func<AudioMixerGroupBindings, AudioMixerGroup> getMixer)
        {
            var mixer = getMixer(_mixerGroupKey);

            if (mixer == null) return;

            var sources = GetComponentsInChildren<AudioSource>(true)
                .Where(src => HasNoSubContext(src) 
                && src.outputAudioMixerGroup == null);

            foreach (var source in sources)
                source.outputAudioMixerGroup = mixer;
        }

        private bool HasNoSubContext(AudioSource src)
        {
            var pac = src.GetComponent<PreviewAudioContext>();
            return pac == this || pac == null;
        }
    }

}