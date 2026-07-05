using System;
using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Owns the GPU render texture(s) backing one water layer. A single-buffered layer
    /// reads and writes the same texture; a double-buffered layer keeps a read copy and a
    /// write copy and flips them via <see cref="Swap"/>. Storage only — no simulation logic.
    /// </summary>
    internal sealed class WaterLayerTarget : IDisposable
    {
        private readonly RenderTexture _primary;
        private readonly RenderTexture _secondary; // null when single-buffered
        private bool _flipped;

        internal WaterLayerTarget(string baseName, RenderTextureDescriptor descriptor, bool doubleBuffered)
        {
            _primary = Create(baseName + WaterFieldConstants.PrimaryBufferSuffix, descriptor);
            _secondary = doubleBuffered
                ? Create(baseName + WaterFieldConstants.SecondaryBufferSuffix, descriptor)
                : null;
        }

        internal RenderTexture Read => _flipped && _secondary != null ? _secondary : _primary;

        internal RenderTexture Write
        {
            get
            {
                if (_secondary == null) return _primary;
                return _flipped ? _primary : _secondary;
            }
        }

        internal void Swap()
        {
            if (_secondary == null) return;
            _flipped = !_flipped;
        }

        public void Dispose()
        {
            Release(_primary);
            Release(_secondary);
        }

        private static RenderTexture Create(string name, RenderTextureDescriptor descriptor)
        {
            var texture = new RenderTexture(descriptor) { name = name };
            if (!texture.Create())
            {
                throw new InvalidOperationException($"Failed to allocate water render texture '{name}'.");
            }

            return texture;
        }

        private static void Release(RenderTexture texture)
        {
            if (texture == null) return;

            texture.Release();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
