namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// A provider that accepts ripple impulses to inject on its next recorded frame. The domain
    /// forwards authoring and gameplay impulses to whichever of its providers implements this, so a
    /// caller never needs to hold a concrete provider reference.
    /// </summary>
    internal interface IImpulseReceiver
    {
        void Enqueue(WaterImpulse impulse);
    }
}
