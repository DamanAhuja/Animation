namespace TextureSource
{
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Scripting;

    /// <summary>
    /// Invokes texture update event from the provided texture source ScriptableObject asset.
    /// </summary>
    public class VirtualTextureSource : MonoBehaviour
    {
        [System.Serializable]
        public class TextureEvent : UnityEvent<Texture> { }
        [System.Serializable]
        public class AspectChangeEvent : UnityEvent<float> { }

        [SerializeField]
        [Tooltip("A texture source scriptable object")]
        private BaseTextureSource source = default;

        [SerializeField]
        [Tooltip("A texture source scriptable object for Editor. If it is null, used source in Editor")]
        private BaseTextureSource sourceForEditor = null;

        [SerializeField]
        [Tooltip("If true, the texture is trimmed to the screen aspect ratio. Use this to show in full screen")]
        private bool trimToScreenAspect = false;

        [Tooltip("Event called when texture updated")]
        public TextureEvent OnTexture = new TextureEvent();

        [Tooltip("Event called when the aspect ratio changed")]
        public AspectChangeEvent OnAspectChange = new AspectChangeEvent();

        private ITextureSource activeSource;
        private float aspect = float.NegativeInfinity;
        private TextureTransformer transformer;

        public bool DidUpdateThisFrame => activeSource.DidUpdateThisFrame;
        public Texture Texture => activeSource.Texture;

        public BaseTextureSource Source
        {
            get => source;
            set => source = value;
        }
        public BaseTextureSource SourceForEditor
        {
            get => sourceForEditor;
            set => sourceForEditor = value;
        }

        private void OnEnable()
        {
            activeSource = sourceForEditor != null && Application.isEditor
                ? sourceForEditor
                : source;

            if (activeSource == null)
            {
                Debug.LogError("Source is not set.", this);
                enabled = false;
                return;
            }
            activeSource.Start();
        }

        private void OnDisable()
        {
            activeSource?.Stop();
            transformer?.Dispose();
            transformer = null;
        }

        private void Update()
        {
            if (!activeSource.DidUpdateThisFrame)
            {
                return;
            }

            Texture tex = trimToScreenAspect
                ? TrimToScreen(Texture)
                : Texture;

            // Convert to RGBA32 before invoking event
            tex = ConvertToRGBA32(tex);
            OnTexture?.Invoke(tex);

            float aspect = (float)tex.width / tex.height;
            if (aspect != this.aspect)
            {
                OnAspectChange?.Invoke(aspect);
                this.aspect = aspect;
            }
        }

        // Converts any texture to RGBA32
        private Texture2D ConvertToRGBA32(Texture texture)
        {
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(texture, rt);

            Texture2D tex2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            tex2D.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return tex2D;
        }

        // Invoked by UI Events
        [Preserve]
        public void NextSource()
        {
            activeSource?.Next();
        }

        private Texture TrimToScreen(Texture texture)
        {
            float srcAspect = (float)texture.width / texture.height;
            float dstAspect = (float)Screen.width / Screen.height;

            // Allow 1% mismatch
            if (Mathf.Abs(srcAspect - dstAspect) < 0.01f)
            {
                return texture;
            }

            Utils.GetTargetSizeScale(
                new Vector2Int(texture.width, texture.height), dstAspect,
                out Vector2Int dstSize, out Vector2 scale);

            bool needInitialize = transformer == null || dstSize.x != transformer.width || dstSize.y != transformer.height;
            if (needInitialize)
            {
                transformer?.Dispose();

                // Setting ARGB32 as the closest supported format
                RenderTextureFormat format = RenderTextureFormat.ARGB32;

                transformer = new TextureTransformer(dstSize.x, dstSize.y, format);
            }

            return transformer.Transform(texture, Vector2.zero, 0, scale);
        }
    }
}
