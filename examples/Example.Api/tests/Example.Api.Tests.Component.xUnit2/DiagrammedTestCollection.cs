using Example.Api.Tests.Component.xUnit2.Infrastructure;
using TestTrackingDiagrams.xUnit2;

namespace Example.Api.Tests.Component.xUnit2;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
// ReSharper disable once ClassNeverInstantiated.Global : This is specifically not instantiated, it's just here for required grouping
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }
