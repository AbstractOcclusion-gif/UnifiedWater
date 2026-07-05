using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Cached shader property IDs, kernel names, and resource paths for the water field compute
    /// work. Centralised so no shader-binding string literal appears anywhere else, and each id is
    /// hashed once at load rather than every frame.
    /// </summary>
    internal static class WaterFieldShaderIds
    {
        // Resources-relative path (no extension) to the diagnostic compute shader.
        internal const string DiagnosticComputeResourcePath = "AbstractOcclusion/UnifiedWater/WaterFieldDiagnostics";
        internal const string DiagnosticKernelName = "CsAdvanceDiagnostic";

        // Ripple simulation compute shader and its two kernels.
        internal const string RippleComputeResourcePath = "AbstractOcclusion/UnifiedWater/WaterRipple";
        internal const string IntegrateKernelName = "CsIntegrate";
        internal const string InjectKernelName = "CsInject";

        // Wave compose compute shader: derives SurfaceNormalFoam from the Dynamic height field.
        internal const string ComposeComputeResourcePath = "AbstractOcclusion/UnifiedWater/WaterCompose";
        internal const string ComposeKernelName = "CsCompose";

        // Debug-view shader, sampled to blit one field layer slice to the screen.
        internal const string DebugViewShaderName = "Hidden/AbstractOcclusion/UnifiedWater/WaterFieldDebug";

        internal static readonly int ReadField = Shader.PropertyToID("_ReadField");
        internal static readonly int WriteField = Shader.PropertyToID("_WriteField");
        internal static readonly int Increment = Shader.PropertyToID("_Increment");
        internal static readonly int CascadeCount = Shader.PropertyToID("_CascadeCount");

        internal static readonly int RippleDamping = Shader.PropertyToID("_RippleDamping");
        internal static readonly int PropagationSpeed = Shader.PropertyToID("_PropagationSpeed");
        internal static readonly int Resolution = Shader.PropertyToID("_Resolution");
        internal static readonly int Impulses = Shader.PropertyToID("_Impulses");
        internal static readonly int ImpulseCount = Shader.PropertyToID("_ImpulseCount");

        internal static readonly int DynamicField = Shader.PropertyToID("_DynamicField");
        internal static readonly int NormalFoamField = Shader.PropertyToID("_NormalFoamField");

        internal static readonly int DebugField = Shader.PropertyToID("_DebugField");
        internal static readonly int DebugValueScale = Shader.PropertyToID("_DebugValueScale");
    }
}
