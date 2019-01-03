
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Htc.Viveport.SDK
{
    public class PlaySelectorPlayable : PlayableBehaviour
    {
        public int CurrentClipIndex = -1;
        public AnimationMixerPlayable Mixer;

        private struct ClipData
        {
            public readonly int MixerIndex;
            public readonly AnimationClipPlayable ClipPlayable;
            public readonly AnimationClip Clip;

            public ClipData(int mixerIndex, AnimationClipPlayable clipPlayable)
            {
                MixerIndex = mixerIndex;
                ClipPlayable = clipPlayable;
                // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                Clip = ClipPlayable.GetAnimationClip();
            }
        }

        private readonly List<ClipData> _clipData = new List<ClipData>(3);

        public void Initialize(Playable owner, PlayableGraph graph)
        {
            Mixer = AnimationMixerPlayable.Create(graph);
            owner.AddInput(Mixer, 0, 1.0f);
        }

        public override void PrepareFrame(Playable owner, FrameData info)
        {
            if (CurrentClipIndex == -1) return;

            var curClip = (AnimationClipPlayable)Mixer.GetInput(CurrentClipIndex);
            if (curClip.GetTime() > curClip.GetDuration()) owner.Play();
        }

        public void AddClip(PlayableGraph graph, AnimationClip clip)
        {
            if (_clipData.Any(cd => cd.Clip == clip)) return;

            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            var mixerIndex = Mixer.AddInput(clipPlayable, 0);
            _clipData.Add(new ClipData(mixerIndex, clipPlayable));
        }

        public void PlayAnimation(Playable owner, AnimationClip clip)
        {
            var bindIndex = _clipData.FindIndex(cd => cd.Clip == clip);
            if (bindIndex == -1)
            {
                owner.Pause();
                return;
            }

            var data = _clipData[bindIndex];

            CurrentClipIndex = data.MixerIndex;

            var clipPlayable = (AnimationClipPlayable)Mixer.GetInput(CurrentClipIndex);
            clipPlayable.SetTime(0.0);

            var inputCount = Mixer.GetInputCount();
            for (var idx = 0; idx < inputCount; idx++)
            {
                Mixer.SetInputWeight(idx, idx != CurrentClipIndex ? 0.0f : 1.0f);
            }

            owner.Play();
        }
    }

    [RequireComponent(typeof(Animator))]
    public class PlayObjectAnimations : MonoBehaviour
    {
        private PlayableGraph _graph;
        private ScriptPlayable<PlaySelectorPlayable> _playSelectorOwner;
        private PlaySelectorPlayable _playSelector;
        private Animator _animator;

        public void AddClip(AnimationClip clip)
        {
            if (_playSelector != null)
                _playSelector.AddClip(_graph, clip);
        }

        public void Play(AnimationClip clip)
        {
            if (_playSelector != null)
                _playSelector.PlayAnimation(_playSelectorOwner, clip);
        }

        public bool IsPlaying
        {
            get { return _playSelectorOwner.GetPlayState() == PlayState.Playing; }
        }

        private void Awake()
        {
            this.SafeGetComponent(ref _animator);
            
            // for objects in bundles, it'll be pre-scrubbed out
            #if UNITY_EDITOR
            _animator.runtimeAnimatorController = null;
            #endif

            _graph = PlayableGraph.Create();

            _playSelectorOwner = ScriptPlayable<PlaySelectorPlayable>.Create(_graph);
            _playSelector = _playSelectorOwner.GetBehaviour();
            _playSelector.Initialize(_playSelectorOwner, _graph);

            var playableOutput = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
            playableOutput.SetSourcePlayable(_playSelectorOwner);
            playableOutput.SetSourceInputPort(0);
            
            #if UNITY_EDITOR
            GraphVisualizerClient.Show(_graph, string.Format("{0}.PlayObjectAnimations", gameObject.name));
            #endif

            _graph.Play();
            _playSelectorOwner.Pause();
        }

        private void OnDestroy()
        {
            #if UNITY_EDITOR
            GraphVisualizerClient.Hide(_graph);
            #endif
            
            _graph.Destroy();
        }
    }
}
