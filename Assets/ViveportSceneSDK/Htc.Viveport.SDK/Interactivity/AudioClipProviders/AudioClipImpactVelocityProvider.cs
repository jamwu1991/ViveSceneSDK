

using UnityEngine;

namespace Htc.Viveport.SDK
{
    [CreateAssetMenu(menuName = "Audio Clip Providers/Velocity")]
    public class AudioClipImpactVelocityProvider : AudioClipProvider
    {
        [SerializeField] private AudioClipPlayInfo[] _playInfos = { new AudioClipPlayInfo(1.0f, 1.0f) };
        [SerializeField, MinMaxSlider(0.0f, 100.0f)] private Vector2 _velocityRange = new Vector2(0.0f, 10.0f);
        private float _relativeVelocity = 0.0f;

        public override int Count
        {
            get { return _playInfos.Length; }
        }

        public override AudioClipPlayInfo Next
        {
            get
            {
                float range = _velocityRange.y - _velocityRange.x;
                float pct = Mathf.Clamp( ( _relativeVelocity - _velocityRange.x ) / range, 0, 1 );
                int index = (int)Mathf.Clamp( Mathf.Floor(pct * Count-1), 0, Count-1 );

                //Debug.Log( "_relativeVelocity: " + _relativeVelocity );
                //Debug.Log( "index: " + index );

                return _playInfos[index];
            }
        }

        public float ContactVelocity
        {
            get { return _relativeVelocity; }
            set { _relativeVelocity = Mathf.Clamp(value, 0.0f, float.PositiveInfinity); }
        }
    }
}