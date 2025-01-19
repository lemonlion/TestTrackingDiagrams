using Example.Api.Tests.Component.XUnit.Infrastructure;
using TestTrackingDiagrams.XUnit;

namespace Example.Api.Tests.Component.XUnit;

[CollectionDefinition(DiagrammedComponentTest.DiagrammedTestCollectionName)]
// ReSharper disable once ClassNeverInstantiated.Global : This is specifically not instantiated, it's just here for required grouping
public class DiagrammedTestCollection : ICollectionFixture<TestRun> { }