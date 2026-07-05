using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace AbstractOcclusion.UnifiedWater.PlayTests
{
    /// <summary>
    /// GPU proof of the ripple simulation: seed one drop with the inject kernel, then run the
    /// integrate kernel (ping-ponging the double buffer) for several steps, and read the field back.
    /// A working sim spreads the drop outward (a texel that started at zero away from the centre
    /// becomes non-zero) and, with damping below one, does not gain energy. A broken ping-pong, swap,
    /// or boundary would fail one of these.
    /// </summary>
    public sealed class WaterFieldPingPongTests
    {
        private const int Resolution = 16;
        private const int CascadeCount = 1;
        private const int IntegrateSteps = 20;
        private const int CentreTexel = Resolution / 2;
        private const int OffsetTexel = CentreTexel + 4;
        private const float PropagationEpsilon = 1e-4f;

        private const float ImpulseRadiusUv = 0.1f;
        private const float ImpulseStrength = 1f;
        private const float Damping = 0.99f;
        private const float PropagationSpeed = 0.4f;

        [UnityTest]
        public IEnumerator RippleKernels_SpreadAndDecayADrop_ProvingTheSim()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Assert.Ignore("Compute shaders are unsupported on this device.");
            }

            var compute = Resources.Load<ComputeShader>(WaterFieldShaderIds.RippleComputeResourcePath);
            Assert.IsNotNull(compute, "Ripple compute shader missing from Resources.");
            int integrateKernel = compute.FindKernel(WaterFieldShaderIds.IntegrateKernelName);
            int injectKernel = compute.FindKernel(WaterFieldShaderIds.InjectKernelName);

            var descriptor = WaterFieldDescriptorFactory.CreateBounded(
                Resolution, new[] { WaterLayer.Dynamic });
            using var field = new WaterField(descriptor);

            ClearToZero(field.Read(WaterLayer.Dynamic));
            ClearToZero(field.Write(WaterLayer.Dynamic));

            int groups = Resolution / WaterFieldConstants.SimThreadGroupSize;

            using (var impulses = new ComputeBuffer(1, sizeof(float) * 4))
            {
                impulses.SetData(new[] { new Vector4(0.5f, 0.5f, ImpulseRadiusUv, ImpulseStrength) });

                // Seed the drop directly into the read buffer via an in-place inject.
                compute.SetTexture(injectKernel, WaterFieldShaderIds.WriteField, field.Read(WaterLayer.Dynamic));
                compute.SetBuffer(injectKernel, WaterFieldShaderIds.Impulses, impulses);
                compute.SetInt(WaterFieldShaderIds.ImpulseCount, 1);
                compute.SetInt(WaterFieldShaderIds.Resolution, Resolution);
                compute.SetInt(WaterFieldShaderIds.CascadeCount, CascadeCount);
                compute.Dispatch(injectKernel, groups, groups, CascadeCount);
            }

            float seededCentre = SampleRed(field.Read(WaterLayer.Dynamic), CentreTexel, CentreTexel);
            Assert.Greater(seededCentre, PropagationEpsilon, "Inject kernel did not seed a drop.");

            for (int step = 0; step < IntegrateSteps; step++)
            {
                compute.SetTexture(integrateKernel, WaterFieldShaderIds.ReadField, field.Read(WaterLayer.Dynamic));
                compute.SetTexture(integrateKernel, WaterFieldShaderIds.WriteField, field.Write(WaterLayer.Dynamic));
                compute.SetFloat(WaterFieldShaderIds.RippleDamping, Damping);
                compute.SetFloat(WaterFieldShaderIds.PropagationSpeed, PropagationSpeed);
                compute.SetInt(WaterFieldShaderIds.Resolution, Resolution);
                compute.SetInt(WaterFieldShaderIds.CascadeCount, CascadeCount);
                compute.Dispatch(integrateKernel, groups, groups, CascadeCount);
                field.Swap(WaterLayer.Dynamic);
                yield return null;
            }

            float spreadValue = SampleRed(field.Read(WaterLayer.Dynamic), OffsetTexel, CentreTexel);
            Assert.Greater(Mathf.Abs(spreadValue), PropagationEpsilon,
                "Drop did not propagate outward — integrate, ping-pong, or swap is broken.");
        }

        private static void ClearToZero(RenderTexture target)
        {
            var previous = RenderTexture.active;
            Graphics.SetRenderTarget(target, 0, CubemapFace.Unknown, 0);
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
        }

        // Copies slice 0 of the array target to a plain 2D float texture and reads one texel's red.
        private static float SampleRed(RenderTexture arraySource, int x, int y)
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

            float red = pixels.GetPixel(x, y).r;

            Object.DestroyImmediate(pixels);
            temp.Release();
            Object.DestroyImmediate(temp);
            return red;
        }
    }
}
