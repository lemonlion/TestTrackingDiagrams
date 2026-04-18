using Example.Api.Tests.CiPreview.Mixed.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace Example.Api.Tests.CiPreview.Mixed;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }
