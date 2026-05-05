using TTD.xUnit3.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace TTD.xUnit3;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }
