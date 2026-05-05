using TTD.xUnit2.Infrastructure;
using TestTrackingDiagrams.xUnit2;

namespace TTD.xUnit2;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }
