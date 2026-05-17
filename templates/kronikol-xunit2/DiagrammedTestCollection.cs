using Kronikol.xUnit2.Infrastructure;
using Kronikol.xUnit2;

namespace Kronikol.xUnit2;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }
