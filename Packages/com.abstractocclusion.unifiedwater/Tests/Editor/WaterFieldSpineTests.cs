using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace AbstractOcclusion.UnifiedWater.Tests
{
    /// <summary>
    /// Pure-logic coverage of the Water Field spine (no GPU allocation), so the descriptor,
    /// factory, catalog, tier, and domain guards can't silently regress as passes are built on top.
    /// </summary>
    public sealed class WaterFieldSpineTests
    {
        private static IReadOnlyList<WaterLayer> Layers(params WaterLayer[] layers) => layers;

        [Test]
        public void Descriptor_StoresValues_AndReportsMembership()
        {
            var descriptor = new WaterFieldDescriptor(256, 1, Layers(WaterLayer.Dynamic, WaterLayer.Depth));

            Assert.AreEqual(256, descriptor.Resolution);
            Assert.AreEqual(1, descriptor.CascadeCount);
            Assert.IsTrue(descriptor.Contains(WaterLayer.Dynamic));
            Assert.IsFalse(descriptor.Contains(WaterLayer.Flow));
        }

        [Test]
        public void Descriptor_Rejects_OutOfRangeResolution()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new WaterFieldDescriptor(WaterFieldConstants.MaxResolution * 2, 1, Layers(WaterLayer.Dynamic)));
        }

        [Test]
        public void Descriptor_Rejects_EmptyLayers()
        {
            Assert.Throws<ArgumentException>(() =>
                new WaterFieldDescriptor(256, 1, new List<WaterLayer>()));
        }

        [Test]
        public void Descriptor_Rejects_DuplicateLayers()
        {
            Assert.Throws<ArgumentException>(() =>
                new WaterFieldDescriptor(256, 1, Layers(WaterLayer.Dynamic, WaterLayer.Dynamic)));
        }

        [Test]
        public void Factory_Bounded_UsesDepthOneCascade()
        {
            var descriptor = WaterFieldDescriptorFactory.CreateBounded(128, Layers(WaterLayer.Dynamic));

            Assert.AreEqual(WaterFieldConstants.BoundedCascadeCount, descriptor.CascadeCount);
            Assert.AreEqual(128, descriptor.Resolution);
        }

        [Test]
        public void Catalog_DynamicLayer_IsFullFloat_AndDoubleBuffered()
        {
            Assert.AreEqual(GraphicsFormat.R32G32B32A32_SFloat, WaterLayerCatalog.FormatOf(WaterLayer.Dynamic));
            Assert.IsTrue(WaterLayerCatalog.IsDoubleBuffered(WaterLayer.Dynamic));
            Assert.IsFalse(WaterLayerCatalog.IsDoubleBuffered(WaterLayer.Depth));
        }

        [Test]
        public void Tier_DefaultsToBalancedResolution()
        {
            var tier = ScriptableObject.CreateInstance<WaterQualityTier>();
            try
            {
                Assert.AreEqual(WaterFieldConstants.DefaultResolution, tier.FieldResolution);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tier);
            }
        }

        [Test]
        public void Domain_Rejects_NullTier()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new WaterDomain(null, Layers(WaterLayer.Dynamic)));
        }
    }
}
