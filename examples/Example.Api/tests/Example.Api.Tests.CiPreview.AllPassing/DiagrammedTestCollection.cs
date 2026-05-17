using Example.Api.Tests.CiPreview.AllPassing.Infrastructure;
using Kronikol.xUnit3;

namespace Example.Api.Tests.CiPreview.AllPassing;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }
