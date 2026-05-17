using Example.Api.Tests.CiPreview.AllFailing.Infrastructure;
using Kronikol.xUnit3;

namespace Example.Api.Tests.CiPreview.AllFailing;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }
