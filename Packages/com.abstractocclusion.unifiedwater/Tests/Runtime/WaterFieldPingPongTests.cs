using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AbstractOcclusion.UnifiedWater.PlayTests
{
    /// <summary>
    /// GPU proof that the Dynamic layer's import → ping-pong → swap plumbing works: it drives the
    /// diagnostic kernel (reads previous value, writes value + increment) for several frames, then
    /// reads back the field. A correctly swapped double buffer climbs by increment*frames; a broken
    /// swap or a self-overwriting single buffer would not. This exercises the same compute + swap
    /// path the render feature uses, without needing a URP asset wired in the test.
    /// </summary>
    public sealed class WaterFieldPingPongTests
    {
        private const int Resolution = 16;
        private const int CascadeCount = 1;
        private const int FrameCount = 5;
        private const float Increment = 1f;
        private const float Tolerance = 0.001f;

        [UnityTest]
        public IEnumerator DiagnosticKernel_ClimbsByIncrement_ProvingPingPongAndSwap()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Assert.Ignore("Compute shaders are unsupported on this device.");
            }

            var compute = Resources.Load<ComputeShader>(WaterFieldShaderIds.DiagnosticComputeResourcePath);
            Assert.IsNotNull(compute, "Diagnostic compute shader missing from Resources.");
            int kernel = compute.FindKernel(WaterFieldShaderIds.DiagnosticKernelName);

            var descriptor = WaterFieldDescriptorFactory.CreateBounded(
                Resolution, new[] { WaterLayer.Dynamic });
            using var field = new WaterField(descriptor);

            ClearToZero(field.Read(WaterLayer.Dynamic));
            ClearToZero(field.Write(WaterLayer.Dynamic));

            int groupsPerAxis = Resolution / WaterFieldConstants.SimThreadGroupSize;
            for (int frame = 0; frame < FrameCount; frame++)
            {
                compute.SetTexture(kernel, WaterFieldShaderIds.ReadField, field.Read(WaterLayer.Dynamic));
                compute.SetTexture(kernel, WaterFieldShaderIds.WriteField, field.Write(WaterLayer.Dynamic));
                compute.SetFloat(WaterFieldShaderIds.Increment, Increment);
                compute.SetInt(WaterFieldShaderIds.CascadeCount, CascadeCount);
                compute.Dispatch(kernel, groupsPerAxis, groupsPerAxis, CascadeCount);
                field.Swap(WaterLayer.Dynamic);
                yield return null;
            }

            float value = ReadFirstTexelRed(field.Read(WaterLayer.Dynamic));
            Assert.AreEqual(FrameCount * Increment, value, Tolerance,
                "Dynamic layer did not climb by increment*frames — ping-pong or swap is broken.");
        }

        private static void ClearToZero(RenderTexture target)
        {
            var previous = RenderTexture.active;
            Graphics.SetRenderTarget(target, 0, CubemapFace.Unknown, 0);
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
        }

        // Copies slice 0 of the array target into a plain 2D float texture and reads its first texel.
        private static float ReadFirstTexelRed(RenderTexture arraySource)
        {
            var temp = new RenderTexture(arraySource.width, arraySource.height, 0, RenderTextureFormat.ARGBFloat);
            temp.Create();
            Graphics.CopyTexture(arraySource, 0, 0, temp, 0, 0);

            var pixels = new Texture2D(temp.width, temp.height, TextureFormat.RGBAFloat, false);
            var previous = RenderTexture.active;
            RenderTexture.active = temp;
            pixels.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;

            float red = pixels.GetPixel(0, 0).r;

            Object.DestroyImmediate(pixels);
            temp.Release();
            Object.DestroyImmediate(temp);
            return red;
        }
    }
}
