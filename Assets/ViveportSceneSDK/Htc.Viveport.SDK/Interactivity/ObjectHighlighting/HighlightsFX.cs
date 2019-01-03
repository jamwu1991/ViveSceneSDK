using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Htc.Viveport.SDK
{
    public class HighlightsFX : MonoBehaviour
    {
        #region enums
        public enum HighlightType
        {
            Glow = 0,
            Solid = 1
        }
        public enum SortingType
        {
            Overlay = 3,
            DepthFilter = 4
        }
        #endregion

        #region public vars

        public HighlightType _selectionType = HighlightType.Glow;
        public SortingType _sortingType = SortingType.DepthFilter;
        public Color _highlightColor = new Color( 1f, 0f, 0f, 1f );

        #endregion

        #region public accessors

        public List<Renderer> ObjectRenderers { get { return _objectRenderers; } }

        #endregion

        #region private field

        private List<Renderer> _objectRenderers;
        private BlurOptimizedHTC _blur;
        private Material _highlightMaterial;
        private CommandBuffer _renderBuffer;
        private int _RTWidth = 512;
        private int _RTHeight = 512;

        #endregion

        private void Awake()
        {
            CreateBuffers();
            CreateMaterials();

            _blur = gameObject.AddComponent<BlurOptimizedHTC>();
            _blur.blurShader = Shader.Find( "HtcViveportSDK/FastBlur" );
            _blur.enabled = false;

            _RTWidth = Screen.width;
            _RTHeight = Screen.height;

            _objectRenderers = new List<Renderer>();
        }

        private void CreateBuffers()
        {
            _renderBuffer = new CommandBuffer();
        }

        private void ClearCommandBuffers()
        {
            _renderBuffer.Clear();
        }

        private void CreateMaterials()
        {
            _highlightMaterial = new Material( Shader.Find( "HtcViveportSDK/Highlight" ) );
        }

        private void RenderHighlights( RenderTexture rt )
        {
            RenderTargetIdentifier rtid = new RenderTargetIdentifier( rt );
            _renderBuffer.SetRenderTarget( rtid );

            foreach( Renderer mr in _objectRenderers )
            {
                if( mr != null )
                    _renderBuffer.DrawRenderer( mr, _highlightMaterial, 0, (int)_sortingType );
            }

            RenderTexture.active = rt;
            Graphics.ExecuteCommandBuffer( _renderBuffer );
            RenderTexture.active = null;
        }

        /// Final image composing.
        /// 1. Renders all the highlight objects either with Overlay shader or DepthFilter
        /// 2. Downsamples and blurs the result image using standard BlurOptimized image effect
        /// 3. Substracts the occlusion map from the blurred image, leaving the highlight area
        /// 4. Renders the result image over the main camera's G-Buffer
        private void OnRenderImage( RenderTexture source, RenderTexture destination )
        {
            RenderTexture highlightRT;

            RenderTexture.active = highlightRT = RenderTexture.GetTemporary( _RTWidth, _RTHeight, 0, RenderTextureFormat.R8 );

            GL.Clear( true, true, Color.clear );
            RenderTexture.active = null;

            ClearCommandBuffers();

            RenderHighlights( highlightRT );

            RenderTexture blurred = RenderTexture.GetTemporary( _RTWidth, _RTHeight, 0, RenderTextureFormat.R8 );

            _blur.OnRenderImage( highlightRT, blurred );

            RenderTexture occluded = RenderTexture.GetTemporary( _RTWidth, _RTHeight, 0, RenderTextureFormat.R8 );

            // Excluding the original image from the blurred image, leaving out the areal alone
            _highlightMaterial.SetTexture( "_OccludeMap", highlightRT );
            Graphics.Blit( blurred, occluded, _highlightMaterial, 2 );

            _highlightMaterial.SetTexture( "_OccludeMap", occluded );

            RenderTexture.ReleaseTemporary( occluded );

            _highlightMaterial.SetColor( "_Color", _highlightColor );
            Graphics.Blit( source, destination, _highlightMaterial, (int)_selectionType );


            RenderTexture.ReleaseTemporary( blurred );
            RenderTexture.ReleaseTemporary( highlightRT );
        }
    }
}