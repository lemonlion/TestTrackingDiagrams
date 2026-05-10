using System;

namespace TestTrackingDiagrams.Tracking
{
    [AttributeUsage(AttributeTargets.Assembly)]
    internal sealed class TrackAssertionsAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Assembly)]
    internal sealed class TrackAssertionsBetaAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
    internal sealed class SuppressAssertionTrackingAttribute : Attribute { }
}
