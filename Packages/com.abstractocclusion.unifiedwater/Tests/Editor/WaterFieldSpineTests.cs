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
        public void Descriptor_Rejects_ResolutionNotMultipleOfThreadGroup()
        {
            // 100 is within the allowed range but not a whole multiple of the 8-texel compute tile.
            Assert.Throws<ArgumentException>(() =>
                new WaterFieldDescriptor(100, 1, Layers(WaterLayer.Dynamic)));
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
                new WaterDomain(
                    null,
                    new IWaterFieldProvider[] { new FakeProvider(0, WaterLayer.Dynamic) },
                    default));
        }

        [Test]
        public void Extent_MapsCenterToUvCentre()
        {
            var extent = new BoundedDomainExtent(new Vector2(5f, 5f), 10f, 256);

            var uv = extent.WorldToUv(new Vector3(5f, 99f, 5f));

            Assert.AreEqual(0.5f, uv.x, 1e-5f);
            Assert.AreEqual(0.5f, uv.y, 1e-5f);
        }

        [Test]
        public void Extent_RoundTripsWorldAndUv()
        {
            var extent = new BoundedDomainExtent(new Vector2(2f, -3f), 8f, 128);
            var world = new Vector3(3.5f, 0f, -1.25f);

            var back = extent.UvToWorld(extent.WorldToUv(world));

            Assert.AreEqual(world.x, back.x, 1e-4f);
            Assert.AreEqual(world.z, back.y, 1e-4f);
        }

        [Test]
        public void Extent_TexelSizeIsSizeOverResolution()
        {
            var extent = new BoundedDomainExtent(Vector2.zero, 16f, 256);

            Assert.AreEqual(16f / 256f, extent.TexelSizeMeters, 1e-6f);
        }

        [Test]
        public void Extent_Rejects_NonPositiveSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BoundedDomainExtent(Vector2.zero, 0f, 256));
        }

        [Test]
        public void OrderProviders_Rejects_NullSet()
        {
            Assert.Throws<ArgumentNullException>(() => WaterDomain.OrderProviders(null));
        }

        [Test]
        public void OrderProviders_Rejects_EmptySet()
        {
            Assert.Throws<ArgumentException>(() =>
                WaterDomain.OrderProviders(Array.Empty<IWaterFieldProvider>()));
        }

        [Test]
        public void OrderProviders_Rejects_NullEntry()
        {
            Assert.Throws<ArgumentException>(() =>
                WaterDomain.OrderProviders(new IWaterFieldProvider[] { null }));
        }

        [Test]
        public void OrderProviders_Rejects_ProviderWithNoLayers()
        {
            Assert.Throws<ArgumentException>(() =>
                WaterDomain.OrderProviders(new IWaterFieldProvider[] { new FakeProvider(0) }));
        }

        [Test]
        public void OrderProviders_SortsByWriteOrder()
        {
            var later = new FakeProvider(5, WaterLayer.Depth);
            var earlier = new FakeProvider(1, WaterLayer.Dynamic);

            var ordered = WaterDomain.OrderProviders(new IWaterFieldProvider[] { later, earlier });

            Assert.AreSame(earlier, ordered[0]);
            Assert.AreSame(later, ordered[1]);
        }

        [Test]
        public void DeriveFieldLayers_UnionsAndDedupesSharedLayer()
        {
            // Ripple integration and obstacle injection both write Dynamic (an ordered write-chain);
            // the field still allocates Dynamic once, then Depth.
            var providers = new IWaterFieldProvider[]
            {
                new FakeProvider(0, WaterLayer.Dynamic),
                new FakeProvider(1, WaterLayer.Dynamic),
                new FakeProvider(2, WaterLayer.Depth)
            };

            var layers = WaterDomain.DeriveFieldLayers(providers);

            CollectionAssert.AreEqual(new[] { WaterLayer.Dynamic, WaterLayer.Depth }, layers);
        }

        /// <summary>
        /// Minimal <see cref="IWaterFieldProvider"/> stand-in for pure-logic tests: declares layers
        /// and a write position without allocating any GPU resource.
        /// </summary>
        private sealed class FakeProvider : IWaterFieldProvider
        {
            private readonly IReadOnlyList<WaterLayer> _writtenLayers;

            internal FakeProvider(int writeOrder, params WaterLayer[] writtenLayers)
            {
                WriteOrder = writeOrder;
                _writtenLayers = writtenLayers;
            }

            public IReadOnlyList<WaterLayer> WrittenLayers => _writtenLayers;
            public int WriteOrder { get; }

            public void Setup(WaterFieldDescriptor descriptor)
            {
            }

            public void RecordPasses(in WaterFieldFrame frame)
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
