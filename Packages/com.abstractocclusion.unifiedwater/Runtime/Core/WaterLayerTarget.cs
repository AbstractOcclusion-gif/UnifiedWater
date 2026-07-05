using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Owns the GPU render texture(s) backing one water layer. A single-buffered layer
    /// reads and writes the same texture; a double-buffered layer keeps a read copy and a
    /// write copy and flips them via <see cref="Swap"/>. Storage only — no simulation logic.
    /// Each texture is also wrapped once in a persistent <see cref="RTHandle"/> so the render
    /// graph can import it by handle every frame without allocating a wrapper per frame; the
    /// wrapper is a view, this type still owns the underlying texture's lifetime.
    /// </summary>
    internal sealed class WaterLayerTarget : IDisposable
    {
        private readonly RenderTexture _primary;
        private readonly RenderTexture _secondary; // null when single-buffered
        private readonly RTHandle _primaryHandle;
        private readonly RTHandle _secondaryHandle; // null when single-buffered
        private bool _flipped;

        internal WaterLayerTarget(string baseName, RenderTextureDescriptor descriptor, bool doubleBuffered)
        {
            _primary = Create(baseName + WaterFieldConstants.PrimaryBufferSuffix, descriptor);
            _primaryHandle = Wrap(_primary);

            if (doubleBuffered)
            {
                _secondary = Create(baseName + WaterFieldConstants.SecondaryBufferSuffix, descriptor);
                _secondaryHandle = Wrap(_secondary);
            }
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

        /// <summary>Importable view of the current read texture. Tracks <see cref="Swap"/>.</summary>
        internal RTHandle ReadHandle => _flipped && _secondaryHandle != null ? _secondaryHandle : _primaryHandle;

        /// <summary>Importable view of the current write texture. Tracks <see cref="Swap"/>.</summary>
        internal RTHandle WriteHandle
        {
            get
            {
                if (_secondaryHandle == null) return _primaryHandle;
                return _flipped ? _primaryHandle : _secondaryHandle;
            }
        }

        internal void Swap()
        {
            if (_secondary == null) return;
            _flipped = !_flipped;
        }

        public void Dispose()
        {
            // Release the wrapper views first (they do not own the texture), then the textures.
            _primaryHandle?.Release();
            _secondaryHandle?.Release();
            Release(_primary);
            Release(_secondary);
        }

        // transferOwnership: false keeps texture destruction this type's responsibility, so a
        // wrapper release never double-frees a texture we also release below.
        private static RTHandle Wrap(RenderTexture texture) => RTHandles.Alloc(texture, transferOwnership: false);

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
