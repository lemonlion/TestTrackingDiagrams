using Kronikol.xUnit3.Infrastructure;
using Kronikol.xUnit3;

namespace Kronikol.xUnit3;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }
