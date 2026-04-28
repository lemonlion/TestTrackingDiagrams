# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.27.17] - 2026-04-28

### Added
- **Deterministic scenario stable IDs** (`ScenarioStableId.Compute()`): Each scenario in the JSON, XML, and YAML report output now includes a `stableId` field — a deterministic 16-character hex identifier derived from the feature name, scenario display name, and outline ID (for parameterized scenarios). Unlike the runtime `id` (which varies by test framework and can be random per run), the `stableId` is consistent across runs, making it suitable for cross-run matching (e.g., flaky test detection, trend tracking).
- **Updated report schemas**: JSON Schema and XSD both include the new `stableId` field as a required property on scenarios.

## [2.27.16] - 2026-04-28

### Added
- **New package: `TestTrackingDiagrams.Extensions.Kafka.BuildInterception`** — Automatically intercepts `ConsumerBuilder<TKey,TValue>.Build()` and `ProducerBuilder<TKey,TValue>.Build()` via [Harmony](https://github.com/pardeike/Harmony) runtime patching, wrapping the result with `TrackingKafkaConsumer` / `TrackingKafkaProducer` when tracking is enabled. This enables **zero production code changes** — no `.BuildTracked()`, no package reference in the API project, no DI changes. The Harmony dependency is isolated in the addon package.
- **`KafkaBuildInterceptor.EnableConsumerTracking<TKey,TValue>()`** — Enables consumer tracking and patches `ConsumerBuilder<TKey,TValue>.Build()` in a single call.
- **`KafkaBuildInterceptor.EnableProducerTracking<TKey,TValue>()`** — Enables producer tracking and patches `ProducerBuilder<TKey,TValue>.Build()` in a single call.
- **`KafkaBuildInterceptor.EnableTracking<TKey,TValue>()`** — Convenience method that enables both consumer and producer tracking and patches both `Build()` methods.
- **`KafkaBuildInterceptor.DisableConsumerTracking<TKey,TValue>()`** / **`DisableProducerTracking<TKey,TValue>()`** — Disables tracking for a specific type pair (Harmony patch remains but becomes a no-op).
- **`KafkaBuildInterceptor.Reset()`** — Clears all tracking state and removes all Harmony patches.

## [2.27.15] - 2026-04-28

### Fixed
- **Flaky MessageTracker test**: `TrackSendEvent_does_nothing_when_no_test_info` no longer races with parallel tests — replaced global `RequestAndResponseLogs.Length` assertion with ID-snapshot comparison.
- **Selenium StaleElementReferenceException**: `Short_note_no_up_arrow_when_expanded`, `Scenario_truncation_change_respected_by_note_buttons`, and `Reducing_truncation_makes_short_note_become_long` now use a retry-based `HoverNoteRect` helper that re-queries elements after SVG re-renders.
- **Selenium assertion failure**: `Long_note_dblclick_from_collapsed_goes_to_truncated_not_expanded` now waits for both plus buttons to appear before asserting count.

## [2.27.14] - 2026-04-28

### Added
- **Redis DI extension** (`AddRedisTestTracking()`): Decorates all `IDatabase` registrations with `RedisTrackingDatabase` via `DecorateAll<IDatabase>`, enabling zero-prod-change Redis tracking through `ConfigureTestServices`.
- **Dapper DI extension** (`AddDapperTestTracking()`): Decorates all `DbConnection` registrations with `TrackingDbConnection` via `DecorateAll<DbConnection>`, enabling zero-prod-change SQL tracking through `ConfigureTestServices`.
- **EventHubs DI extensions** (`AddEventHubsProducerTestTracking()`, `AddEventHubsConsumerTestTracking()`, `AddEventHubsTestTracking()`): Decorates `EventHubProducerClient` and `EventHubConsumerClient` registrations for zero-prod-change tracking.
- **ServiceBus `Action<>` overload** (`AddServiceBusTestTracking(Action<ServiceBusTrackingOptions>?)`): New configuration overload consistent with other extensions. The existing `ServiceBusTrackingOptions` parameter overload is preserved.

### Changed
- **ServiceBus tracking types now inherit from SDK classes**: `TrackingServiceBusClient : ServiceBusClient`, `TrackingServiceBusSender : ServiceBusSender`, `TrackingServiceBusReceiver : ServiceBusReceiver`. This enables `DecorateAll<ServiceBusClient>` to work transparently — production code typed as `ServiceBusClient` receives the tracking subclass without any code changes.
- **EventHubs tracking types now inherit from SDK classes**: `TrackingEventHubProducerClient : EventHubProducerClient`, `TrackingEventHubConsumerClient : EventHubConsumerClient`. Non-virtual properties (`EventHubName`, `FullyQualifiedNamespace`, `IsClosed`) are shadowed with `new`.
- **PubSub tracking types now inherit from SDK classes**: `TrackingPublisherClient : PublisherClient`, `TrackingSubscriberClient : SubscriberClient`. This enables `DecorateAll<PublisherClient>` / `DecorateAll<SubscriberClient>` in the updated `AddPubSubTestTracking()`.
- **ServiceBus DI extension refactored**: Uses `DecorateAll<ServiceBusClient>` instead of manual descriptor replacement. Preserves original service lifetime. The `ServiceBusClient` registration is now decorated in-place rather than replaced with a separate `TrackingServiceBusClient` registration.
- **PubSub DI extension enhanced**: `AddPubSubTestTracking()` now decorates `PublisherClient` and `SubscriberClient` registrations in addition to registering the `PubSubTracker` singleton.
- **PubSub options**: Added `IHttpContextAccessor` property to `PubSubTrackingOptions` for consistency with other extension options.

## [2.27.13] - 2026-04-28

### Changed
- **Selenium tests: Shared ChromeDriver via IClassFixture** — All 19 Selenium test classes now share a Chrome browser instance at the class level using `IClassFixture<ChromeFixture>` / `IClassFixture<ChromeFixture1280X900>`, reducing Chrome process launches from ~207 (one per test) to ~19 (one per class). This lowers memory pressure and eliminates redundant browser startup/shutdown overhead.

## [2.27.12] - 2026-04-28

### Fixed
- **CI: Release workflow "No space left on device"**: Freed additional disk space (Swift, GraalVM, PowerShell, hostedtoolcache, Docker images) and changed build/pack to target only `src/` projects instead of the full 77-project solution. Test projects are no longer built during release — only the single core test project is built implicitly by `dotnet test`.

## [2.27.11] - 2026-04-28

### Fixed
- **Eliminated all CI build warnings**: Fixed ~80 compiler and analyzer warnings across the solution, including nullability annotations (`CS8600`–`CS8625`), obsolete API usage (`CS0618`), xUnit analyzer rules (`xUnit2013`, `xUnit2017`, `xUnit2018`, `xUnit1051`), and unused field warnings (`CS0414`). No functional changes.

## [2.27.10] - 2026-04-28

### Fixed
- **`CurrentTestInfo.Fetcher` no longer throws `NullReferenceException` outside test context**: All 8 framework `CurrentTestInfo.Fetcher` implementations (xUnit3, xUnit2, NUnit4, TUnit, MSTest, LightBDD, ReqNRoll, BDDfy) now return `("Unknown", "unknown")` when the test context is unavailable (e.g. during hosted service processing, background threads, Service Bus message handlers). Previously, the delegates accessed the test context without null checks, causing `NullReferenceException` when invoked outside of test execution.
- **`MessageTracker.GetTestInfo()` now catches delegate exceptions**: `MessageTracker` was the only tracking component that invoked `CurrentTestInfoFetcher` without exception handling. All other extensions route through `TestInfoResolver.Resolve()`, which wraps the call in a try-catch. `MessageTracker.GetTestInfo()` now matches this behaviour — a throwing delegate returns `null` (tracking silently skipped) instead of propagating the exception to callers.
- **NUnit4: `TestContextEnumerableExtensions` build error on `IEnumerable<ParameterInfo>.Length`**: Fixed pre-existing compilation error caused by NUnit 4.5.1's `IMethodInfo.GetParameters()` returning `IEnumerable<ParameterInfo>` (which lacks a `Length` property). The result is now materialised to an array before pattern matching.

## [2.27.9] - 2026-04-28

### Added
- **Kafka: Static interceptor for internally-built consumers/producers** (`KafkaTrackingInterceptor`): Enables tracking for consumers and producers built via `new ConsumerBuilder<TKey,TValue>(...).Build()` inside `BackgroundService` or other non-DI code paths. Use `EnableConsumerTracking<TKey,TValue>()` / `EnableProducerTracking<TKey,TValue>()` in test setup, then replace `.Build()` with `.BuildTracked()` in production code (one-token change, no-op when not in test context). Also provides `.Tracked()` extension on existing `IConsumer` / `IProducer` instances.
- **Kafka: Consumer and producer factory interfaces**: `IKafkaConsumerFactory<TKey,TValue>` and `IKafkaProducerFactory<TKey,TValue>` with default implementations (`KafkaConsumerFactory`, `KafkaProducerFactory`) and tracking decorators (`TrackingKafkaConsumerFactory`, `TrackingKafkaProducerFactory`). Inject the factory in services that build consumers/producers internally for clean DI-based tracking.
- **`AddKafkaConsumerFactoryTestTracking<TKey,TValue>()`**: DI extension that registers a default consumer factory (if none exists) and decorates it with tracking.
- **`AddKafkaProducerFactoryTestTracking<TKey,TValue>()`**: DI extension that registers a default producer factory (if none exists) and decorates it with tracking.
- **`BuildTracked()` extension** on `ConsumerBuilder<TKey,TValue>` and `ProducerBuilder<TKey,TValue>` — builds and wraps with tracking if the static interceptor is active.
- **`Tracked()` extension** on `IConsumer<TKey,TValue>` and `IProducer<TKey,TValue>` — wraps an existing instance with tracking if the static interceptor is active. Prevents double-wrapping.

## [2.27.8] - 2026-04-28

### Added
- **Kafka: Transactional producer tracking**: All five transactional producer methods (`InitTransactions`, `BeginTransaction`, `CommitTransaction`, `AbortTransaction`, `SendOffsetsToTransaction`) are now tracked when `TrackTransactions = true`. Each has its own `KafkaOperation` enum value and classifier labels (full names in Detailed/Raw, shortened `Init Txn`/`Begin Txn`/`Commit Txn`/`Abort Txn`/`Send Offsets` in Summarised).
- **`TrackTransactions`** option on `KafkaTrackingOptions` (default `false`) — single flag to enable/disable tracking of all transactional producer operations.
- **`LogTransaction()`** method on `KafkaTracker`.
- **5 new `KafkaOperation` enum values**: `InitTransactions`, `BeginTransaction`, `CommitTransaction`, `AbortTransaction`, `SendOffsetsToTransaction`.

## [2.27.7] - 2026-04-28

### Fixed
- **Kafka: Consumer `Commit()` now tracked** (all 3 overloads): Previously, `KafkaOperation.Commit` was defined in the enum and the tracker had a `LogCommit()` method, but `TrackingKafkaConsumer` never called it. All three `Commit()` overloads now log when `TrackCommit = true`.
- **Kafka: Consumer `Unsubscribe()` now tracked**: Previously just delegated without logging. Now logs when `TrackUnsubscribe = true`.
- **Kafka: Producer `Flush()` now tracked** (both overloads): Previously just delegated without logging. Now logs when `TrackFlush = true`.
- **Kafka: Consumer now logs message Key**: Previously, `TrackingKafkaConsumer` only logged the message Value. Now uses the same `BuildContent` pattern as the producer, logging both Key and Value (controlled by `LogMessageKey` / `LogMessageValue`). Content is also correctly suppressed in Summarised mode.
- **CI: Split IKVM tests into own matrix job with disk cleanup**: The PlantUml.Ikvm test project copies hundreds of native runtime files per platform, exhausting disk space on GitHub Actions runners. IKVM tests now run in a dedicated job with pre-build cleanup of unused SDK/tooling directories.

### Added
- **`TrackFlush`** option on `KafkaTrackingOptions` (default `false`) — enables tracking of `IProducer.Flush()` calls.
- **`TrackUnsubscribe`** option on `KafkaTrackingOptions` (default `false`) — enables tracking of `IConsumer.Unsubscribe()` calls.
- **`LogFlush()`** and **`LogUnsubscribe()`** methods on `KafkaTracker`.

## [2.27.6] - 2026-04-28

### Fixed
- **Spanner: Raw and Detailed verbosity now produce different content**: Previously, both Raw and Detailed verbosity levels showed identical SQL text in the content body (and in phase variants). Raw now always includes parameter values when parameters exist, regardless of the `LogParameters` setting. The `LogParameters` option is now marked `[Obsolete]`.
- **Spanner: Phase variant content now correctly differentiates by verbosity level**: When using `SetupVerbosity`/`ActionVerbosity` overrides, the variant content now uses the appropriate content for each verbosity level (Raw includes parameters, Detailed shows plain SQL, Summarised omits content).

## [2.27.5] - 2026-04-27

### Fixed
- **Phase-aware verbosity overrides (`SetupVerbosity`/`ActionVerbosity`) now work for non-BDD test frameworks** (fixes #23): When the test phase is `Unknown` at capture time (i.e. no BDD framework sets the phase automatically) and verbosity overrides are configured, all extension trackers now pre-compute both Setup and Action rendering variants (`PhaseVariant`). The PlantUML renderer selects the correct variant based on `IsActionStart` marker position, so `SetupVerbosity = Summarised` / `ActionVerbosity = Detailed` (or any combination) works without requiring `StartSetup()`.

### Added
- **`PhaseVariant` record type** on `RequestResponseLog` — holds pre-computed `Method`, `Uri`, `Content`, `Headers`, and `Skip` for a specific verbosity level, allowing the renderer to pick the right variant per phase.
- **`PhaseVariantExtensions.AttachVariants<T>()` / `WithVariants<T>()`** — shared generic helper that all extension trackers use to attach variants when phase is `Unknown` and overrides are configured. Avoids duplicating variant logic across 24 extensions.
- **`StartSetup()` on all 8 framework `TrackingDiagramOverride` wrappers** (xUnit3, xUnit2, NUnit4, MSTest, TUnit, LightBDD, ReqNRoll, BDDfy) — delegates to `DefaultTrackingDiagramOverride.StartSetup()`. Users who prefer explicit phase boundaries can now call `StartSetup()` before setup code, though it is not required for verbosity overrides to work.

## [2.27.4] - 2026-04-27

### Fixed
- **Spanner and Bigtable services render as `participant` instead of `database` shape in diagrams**: Changed `DependencyCategory` from generic `"Database"` to `"Spanner"` and `"Bigtable"` respectively, and added both (plus generic `"Database"` fallback) to `DependencyPalette.CategoryToType`. These services now correctly render with the `database` shape and red color in sequence diagrams.

## [2.27.3] - 2026-04-27

### Fixed
- **Spanner gRPC interceptor: test identity not resolved in WebApplicationFactory scenarios**: Added `IHttpContextAccessor` overload to `SpannerConnectionExtensions.WithTestTracking()` so the interceptor can read test identity from HTTP request headers (propagated by `TestTrackingMessageHandler`) instead of relying solely on `AsyncLocal`, which does not propagate through the TestServer's request pipeline.

## [2.27.2] - 2026-04-27

### Fixed
- **Flaky `PendingRequestResponseLogsTests` under parallel execution**: Added missing `[CollectionDefinition("PendingLogs")]` so the three test classes sharing the `"PendingLogs"` collection are properly serialized by xUnit. Without this, xUnit silently ignored the `[Collection]` attribute.
- **`FlushAll_with_no_pending_entries_is_noop`**: Replaced assertion on total `RequestAndResponseLogs.Length` with testId-filtered assertion, eliminating race condition with concurrent test projects.
- **`AtlasDataApiTrackingMessageHandlerTests`**: Replaced `RequestResponseLogger.Clear()` with per-test `_testId` filtering. Tests no longer wipe the shared static log queue or assert on unfiltered total counts.
- **`TrackingDbCommandTests`**: Same fix — replaced `Clear()` and unfiltered `[0]`/`[1]` indexing with `GetLogsForTest()` filtered by unique `_testId`.
- **`MongoDbTrackingSubscriberTests`**: Replaced `Assert.Empty(RequestResponseLogger.RequestAndResponseLogs)` with testId-filtered assertion in `NoLogging_WhenCurrentTestInfoFetcherIsNull`.
- **Removed `RequestResponseLogger.Clear()` from all test constructors/teardown**: `ServiceBusTrackerTests`, `TrackingDbConnectionTests`, `TrackingDbTransactionTests`, `DbConnectionExtensionsTests`, `MongoDbTrackingSubscriberTests`. The `Clear()` call is destructive to concurrent tests and unnecessary when assertions filter by testId.

## [2.27.1] - 2026-04-27

### Fixed
- **`TestTrackingMessageHandler`** — exception from `CurrentTestInfoFetcher` (e.g. during app startup when no test is active) no longer crashes the HTTP call. The request is forwarded without tracking instead.
- **`DeferredLogFlushHandler`** — same fix: a throwing fetcher no longer prevents the HTTP response from being returned. Pending log entries remain queued for the next successful invocation.
- Partial context headers (test name present but no test ID) now gracefully skip tracking instead of throwing.

## [2.27.0] - 2025-07-25

### Added
- **`SpannerTrackingInterceptor`** — new gRPC interceptor that captures **all** Spanner operations at the transport layer, including Spanner-specific methods (`CreateInsertCommand`, `CreateSelectCommand`, `CreateInsertOrUpdateCommand`, etc.) that bypass ADO.NET wrapping. Extracts SQL text, table names, and mutation details from protobuf messages.
- **`SpannerConnectionStringBuilder.WithTestTracking()` extension** — configures gRPC interception via `SessionPoolManager.CreateWithSettings()` with `SpannerSettings.Interceptor`. Zero production code changes required.
- **`SpannerTracker.CreateServerObservers()`** — returns delegate tuple `(Action<string, IMessage, DateTimeOffset>, Action<string, IMessage, IMessage?, TimeSpan, StatusCode?, DateTimeOffset>)` for wiring to `Spanner.InMemoryEmulator`'s `FakeSpannerServer.OnRequestReceived` / `OnResponseSent` callbacks. Enables server-side observation as an alternative to client-side gRPC interception.

### Changed
- **Spanner extension now depends on `Google.Cloud.Spanner.V1` (5.\*) and `Grpc.Core.Api` (2.\*)** for protobuf type extraction in `SpannerTrackingInterceptor` and `CreateServerObservers()`.

### Documentation
- **Spanner wiki page**: Added Option D (gRPC Interception — recommended) and Option E (Server-Side Observation) setup guides with architecture diagrams, comparison tables, and "What Gets Captured" reference. Added warnings to Options A/B/C about limitations. Updated See Also with gRPC Extension and Phase-Aware Tracking links.

## [2.26.3] - 2025-07-24

### Added
- **`HttpContextAccessor` property on all extension options classes**: Every tracking extension options class now exposes `IHttpContextAccessor? HttpContextAccessor`. When set, the tracker reads it automatically via `?? options.HttpContextAccessor`, providing an alternative to the constructor parameter for dual-resolution test identity. This applies to: `TestTrackingMessageHandlerOptions`, `ServiceBusTrackingOptions`, `MassTransitTrackingOptions`, `ElasticsearchTrackingOptions`, `RedisTrackingDatabaseOptions`, `DapperTrackingOptions`, `EventHubsTrackingOptions`, `BlobTrackingMessageHandlerOptions`, `CloudStorageTrackingMessageHandlerOptions`, `CosmosTrackingMessageHandlerOptions`, `DynamoDbTrackingMessageHandlerOptions`, `EventBridgeTrackingMessageHandlerOptions`, `S3TrackingMessageHandlerOptions`, `SnsTrackingMessageHandlerOptions`, `SqsTrackingMessageHandlerOptions`, `StorageQueueTrackingMessageHandlerOptions`.
- **Auto-resolution of `IHttpContextAccessor` from DI**: DI extensions and convenience methods now auto-resolve `IHttpContextAccessor` from the service provider when available, eliminating the need for manual `httpContextAccessor: sp.GetRequiredService<IHttpContextAccessor>()` wiring:
  - `CreateTestTrackingClient()` (both overloads) — resolves from `factory.Services`
  - `AddServiceBusTestTracking()` — resolves from `IServiceProvider` in the factory lambda
  - `ReplaceWithTrackingProxy` simplified overload — accepts optional `IHttpContextAccessor?` parameter
  - All `DelegatingHandler`-based extensions (BlobStorage, CloudStorage, CosmosDB, DynamoDB, EventBridge, S3, SNS, SQS, StorageQueues) — convenience methods pass `options.HttpContextAccessor` to handler constructors
  - MassTransit, Elasticsearch, Redis, Dapper, EventHubs — convenience methods pass `options.HttpContextAccessor` to tracker constructors

### Documentation
- **All 16 extension wiki pages**: Added `HttpContextAccessor` row to options tables and updated dual-resolution notes with v2.26.3 auto-resolution info.
- **HTTP Tracking Setup wiki**: Rewrote "How to Use It" section with simplified examples showing auto-resolution. Replaced "MediatR Auto-Resolution" with broader "Auto-Resolution (v2.26.2+)" section covering all extensions. Updated extensions table with auto-resolution version info.
- **Diagnostics and Debugging wiki**: Added new "Other dependencies not appearing in per-test reports" section generalizing the gRPC troubleshooting pattern to all extensions.

## [2.26.2] - 2026-04-27

### Added
- **`CurrentTestInfo` static class in every framework package**: Each framework adapter package now provides a `static class CurrentTestInfo` with a get-only `Fetcher` property (`Func<(string Name, string Id)>`). This provides a uniform, discoverable API for setting `CurrentTestInfoFetcher` on any tracking options class — the syntax is identical regardless of framework:
  ```csharp
  CurrentTestInfoFetcher = CurrentTestInfo.Fetcher
  ```
  Available in: `TestTrackingDiagrams.xUnit3`, `TestTrackingDiagrams.xUnit2`, `TestTrackingDiagrams.NUnit4`, `TestTrackingDiagrams.MSTest`, `TestTrackingDiagrams.TUnit`, `TestTrackingDiagrams.LightBDD` (Core/xUnit2/xUnit3/TUnit), `TestTrackingDiagrams.ReqNRoll` (Core/xUnit2/xUnit3/TUnit), `TestTrackingDiagrams.BDDfy.xUnit3`.
- **`XUnit2TestTrackingMessageHandlerOptions.TestInfoFetcher`**: xUnit v2 options class now exposes a static `TestInfoFetcher` field (previously the delegate was only set inline in the constructor), aligning it with all other framework adapters.

### Documentation
- **All wiki pages**: Replaced verbose framework-specific `CurrentTestInfoFetcher` lambda examples with the new `CurrentTestInfo.Fetcher` syntax. Simplified "CurrentTestInfoFetcher by Framework" sections to a single code snippet plus a using-directive table.

## [2.26.1] - 2026-07-14

### Added
- **gRPC Extension — `AddTrackedGrpcClient<TClient>()` DI extension**: New `IServiceCollection` extension method that registers a singleton tracked gRPC client with `IHttpContextAccessor` auto-resolved from DI, matching the existing pattern used by BigQuery, Bigtable, MongoDB, Kafka, and other extensions. Eliminates the need for manual `HttpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>()` wiring.
- **gRPC Extension — auto-resolve `IHttpContextAccessor` in `CreateTestTrackingGrpcClient`**: Both `CreateTestTrackingGrpcClient` and `CreateTestTrackingGrpcClientWithChannel` now auto-resolve `IHttpContextAccessor` from `factory.Services` when not explicitly set on `GrpcTrackingOptions`. This ensures dual-resolution test identity works out of the box for the test → SUT direction.

## [2.26.0] - 2026-07-14

### Added
- **New `TestTrackingDiagrams.Extensions.AtlasDataApi` package**: MongoDB Atlas Data API extension with a `DelegatingHandler` (`AtlasDataApiTrackingMessageHandler`) that intercepts and classifies REST API operations. Supports 10 classified operations (FindOne, Find, InsertOne, InsertMany, UpdateOne, UpdateMany, DeleteOne, DeleteMany, ReplaceOne, Aggregate) with directional arrows (← read, → write, ↔ read-modify-write), three verbosity levels (Raw, Detailed, Summarised), JSON body metadata extraction (dataSource, database, collection, filter), ExcludedOperations filtering, and DI registration via `AddAtlasDataApiTestTracking()`.
- **MongoDB Extension — 11 new classified operations**: Added Change Streams (`Watch` via `$changeStream` pipeline detection), Transactions (`CommitTransaction`, `AbortTransaction`), Admin (`DropDatabase`, `ServerStatus`, `DbStats`, `CollStats`), DDL (`RenameCollection`, `ListIndexes`), and Legacy (`MapReduce`). Total classified operations: 28 (up from 17).
- **MongoDB Extension — directional arrows**: Detailed verbosity labels now include directional arrows: `←` for reads (Find, Aggregate, Count, Distinct, ListCollections, ListDatabases, ListIndexes), `→` for writes (Insert, Delete, DropIndex, DropCollection, DropDatabase, RenameCollection), `↔` for read-modify-write (FindAndModify, Update, BulkWrite). Schema and admin operations show no arrow.
- **MongoDB Extension — enriched metadata**: `MongoDbOperationInfo` now includes `DocumentCount` (from insert arrays), `DocumentId` (from filter `_id`), `PipelineStages` (from aggregate pipelines), and `IsGridFs` (from `fs.files`/`fs.chunks` collections). Detailed labels show enriched info like `Insert (×5) → users` and `Aggregate ($match, $group) ← orders`.
- **MongoDB Extension — `ExcludedOperations`**: New `HashSet<MongoDbOperation>` option to suppress specific operations from tracking (e.g. `ServerStatus`, `ListDatabases`).
- **MongoDB Extension — `LogFilterText`**: New `bool` option (default: `true`) to control whether filter document text is included in Detailed mode request content.
- **MongoDB Extension — `AddMongoDbTestTracking()` DI extension**: New service collection extension method that registers `MongoDbTrackingSubscriber` as a singleton with `IHttpContextAccessor` auto-resolved from DI.
- **MongoDB Extension — `endSessions` ignored by default**: Added `endSessions` to the default `IgnoredCommands` set to suppress session cleanup noise.
- **MongoDB Extension — response metadata extraction**: In Detailed mode, successful command replies now extract `n`, `nModified`, and `nUpserted` counts from the BSON reply and append them to response content.

## [2.25.2] - 2026-04-27

### Fixed
- **gRPC dependency tracking not resolving test identity from HTTP context**: `GrpcTrackingInterceptor` accepts an `IHttpContextAccessor` for dual-resolution test identity (HTTP headers first, delegate fallback), but none of the public API entry points (`GrpcTrackingChannel.Create`, `AsGrpcTrackingCallInvoker`, `CreateTestTrackingGrpcClient`, `WithTestTracking`) forwarded it. When a gRPC client ran inside the SUT's request pipeline (e.g. API calling a downstream gRPC service during a test HTTP request), test identity could not be resolved from the propagated HTTP context headers — causing gRPC dependency calls to be logged with "Unknown" test identity and not appearing in per-test reports. Added `IHttpContextAccessor? HttpContextAccessor` property to `GrpcTrackingOptions`; all entry points and the interceptor constructor now read from this property. Consumers set it once on the options object (typically via `sp.GetRequiredService<IHttpContextAccessor>()`) and the interceptor automatically resolves test identity from HTTP context headers, falling back to `CurrentTestInfoFetcher` when no HTTP context is available.

## [2.25.1] - 2026-04-27

### Fixed
- **Selenium tests**: Fixed `WaitForDiagramSvg` timeout in CI by explicitly calling `_renderDiagramsInContainer` before polling for SVG — `IntersectionObserver` doesn't fire reliably in headless Chrome. Applied to all 5 affected test files (`DiagramNoteTests`, `ContextMenuExtendedTests`, `ScenarioInteractionTests`, `DiagramZoomTests`, `DependencyColoringTests`).
- **`Collapsed_note_shows_plus_button_in_top_right`**: Fixed false assertion failure by scoping button selectors to the target scenario instead of the entire page — other scenarios' minus buttons (in truncated state) were being matched.

## [2.25.0] - 2026-04-27

### Added
- **`RequestResponseLogger.MaxContentLength`**: New global static property that truncates content at capture time when set. Content exceeding the limit is trimmed to the specified character count with a `…truncated (N chars total)` marker appended. Applies to all extensions (HTTP, BigQuery, Spanner, Bigtable, Cosmos, Redis, etc.) since truncation happens in the core `Log()` method. Default is `null` (no limit). Set during test setup, e.g. `RequestResponseLogger.MaxContentLength = 2000;`

## [2.24.1] - 2026-04-27

### Added
- **`BigtableTrackingOptions.ExcludedOperations`**: Added `HashSet<BigtableOperation>` property to suppress tracking of specific Bigtable operations, matching the pattern used by Spanner, Dapper, Elasticsearch, and EventBridge extensions.

## [2.24.0] - 2026-07-14

### Added
- **New `TestTrackingDiagrams.Extensions.Spanner` package**: Google Cloud Spanner extension with ADO.NET connection wrapping (`TrackingSpannerConnection`, `TrackingSpannerCommand`, `TrackingSpannerTransaction`) and a direct `SpannerTracker` for gRPC-style usage. Classifies 18 Spanner operations (Query, Read, Insert, Update, Delete, InsertOrUpdate, Replace, Commit, Rollback, BeginTransaction, BatchDml, PartitionQuery, PartitionRead, Ddl, CreateSession, DeleteSession, StreamingRead, Other) with three verbosity levels (Raw, Detailed, Summarised). Includes DI registration via `AddSpannerTestTracking()` and connection extension `WithTestTracking()`.
- **New `TestTrackingDiagrams.Extensions.Bigtable` package**: Google Cloud Bigtable extension with a direct `BigtableTracker` implementing `ITrackingComponent`. Classifies 7 Bigtable operations (ReadRows, MutateRow, MutateRows, CheckAndMutateRow, ReadModifyWriteRow, SampleRowKeys, Other) with directional diagram labels (← for reads, → for writes) and short table name extraction from full Bigtable resource paths. Includes DI registration via `AddBigtableTestTracking()`.
- **`BigQueryTracker`**: New direct tracker class for the BigQuery extension, providing `LogRequest`/`LogResponse` pair logging without HTTP interception. Useful for scenarios where BigQuery operations are tracked at a layer above or below the HTTP pipeline.
- **`BigQueryServiceCollectionExtensions.AddBigQueryTestTracking()`**: New DI extension in the BigQuery package that registers a singleton `BigQueryTracker` with `IHttpContextAccessor` auto-resolved from DI.

## [2.23.13] - 2026-04-27

### Fixed
- **SqlTrackingInterceptor**: Fixed `UriFormatException` when `Verbosity` is `Detailed` or `Raw` and the SQL Server connection uses comma-separated port notation (e.g. `127.0.0.1,33262` from Docker containers). The `DataSource` comma is now normalised to a colon for valid URI construction. ([#22](https://github.com/lemonlion/TestTrackingDiagrams/issues/22))

## [2.23.12] - 2026-04-27

### Added
- **`TestInfoResolver.CreateHttpFallbackFetcher()`**: New convenience method that creates a `Func<(string Name, string Id)>` encapsulating the dual-resolution pattern (httpContext headers first, fallback delegate second). Eliminates the ~10-line boilerplate previously needed when setting `CurrentTestInfoFetcher` on tracking options.
- **`ServiceCollectionDecoratorExtensions.DecorateAll<TService>()`**: New DI helper that wraps all existing registrations of a service type with a decorator. Removes the original registration and adds the decorated version, preserving the original service lifetime. Handles all descriptor types (factory, instance, type).
- **`ServiceCollectionDecoratorExtensions.DecorateAllOpen()`**: New DI helper that scans for all closed-generic registrations matching an open generic type and replaces each with a decorator type. Additional constructor parameters are resolved from DI via `ActivatorUtilities`.
- **`KafkaServiceCollectionExtensions.AddKafkaProducerTestTracking<TKey,TValue>()`**: New DI extension in the Kafka package that decorates all `IProducer<TKey,TValue>` registrations with `TrackingKafkaProducer` for test diagram tracking. Automatically resolves `IHttpContextAccessor` from DI.
- **`KafkaServiceCollectionExtensions.AddKafkaConsumerTestTracking<TKey,TValue>()`**: Same pattern for `IConsumer<TKey,TValue>` with `TrackingKafkaConsumer`.
- **`PubSubServiceCollectionExtensions.AddPubSubTestTracking()`**: New DI extension in the PubSub package that registers a singleton `PubSubTracker` with `IHttpContextAccessor` auto-resolved from DI.

## [2.23.11] - 2026-04-27

### Added
- **GraphQL query formatting in sequence diagram notes**: GraphQL request bodies are now automatically detected and formatted with proper indentation in diagram notes, replacing the previous single-line JSON string representation. The GraphQL query is parsed and pretty-printed with brace-depth indentation, while arguments inside parentheses (e.g. HotChocolate filtering/sorting) stay inline. Four configurable display modes via `GraphQlBodyFormat` on `ReportConfigurationOptions`:
  - `Json` — Previous behaviour: JSON pretty-print with query as a single-line string value.
  - `FormattedQueryOnly` — Formatted GraphQL query only; HTTP headers and metadata are suppressed.
  - `Formatted` — Formatted GraphQL query with HTTP headers shown above.
  - `FormattedWithMetadata` (default) — Formatted GraphQL query with HTTP headers, plus `variables` and `extensions` sections rendered below.
- When `FocusFields` are in use, GraphQL formatting automatically falls back to `Json` mode so JSON field highlighting works correctly.

## [2.23.10] - 2026-04-27

### Fixed
- **gRPC Activity spans not appearing in Activity Diagrams / Flamecharts**: Fixed three issues preventing gRPC calls from producing activity spans:
  1. `InternalFlowSpanCollector.FilterByAutoInstrumentation()` excluded `"TestTrackingDiagrams.Grpc"` spans because the source was not in `WellKnownAutoInstrumentationSources`. Added `"TestTrackingDiagrams.Grpc"` and `"Grpc.Net.Client"` to the well-known list.
  2. `AsyncUnaryCall` disposed its `Activity` immediately on method return (before the async response arrived), producing near-zero-duration spans. The Activity is now kept alive and stopped in `WrapUnaryResponse` after the response is logged, so spans cover the full call duration.
  3. No trace context was propagated to the server. A `traceparent` metadata header is now injected into all outgoing gRPC calls, allowing server-side ASP.NET Core spans to share the same TraceId.

## [2.23.9] - 2026-04-27

### Fixed
- **Hover buttons not appearing on notes with Creole separator markup**: Fixed a bug where `findNoteGroups()` failed to detect note groups in SVG when PlantUML's Creole `..text..` separator syntax was used inside notes (e.g. `..Continued From Previous Diagram..`). The Creole syntax causes PlantUML to insert `<line>` elements between the note's background `<path>` and `<text>` elements. The algorithm now uses bounding-box containment to skip `<line>`, `<rect>`, and `<circle>` elements that are visually inside the note, while correctly stopping at lifeline/arrow elements between different note groups.
- **Pre-existing Selenium test fix**: Fixed `Partition_long_note_expand_click_works` test that was failing due to SVG note fold path intercepting the button click — now uses JS-dispatched click.

### Added
- **Comprehensive Selenium regression tests for split-diagram hover buttons**: Added 6 new Selenium tests for 3-diagram split scenarios with Creole continuation notes, plus 6 tests for 2-diagram split initial render. Tests cover hover rects, toggle icons, hover visibility, double-click state cycling, and state change preservation across all diagram parts within a single scenario.

## [2.23.8] - 2026-04-27

### Added
- **Activity Diagram & Flamechart support for gRPC calls**: `GrpcTrackingInterceptor` now creates `System.Diagnostics.Activity` spans around each gRPC call, populates `ActivityTraceId`, `ActivitySpanId`, and `Timestamp` on all `RequestResponseLog` entries, and lazily starts the `InternalFlowActivityListener`. This enables gRPC calls to appear in Activity Diagrams and Flamecharts alongside HTTP calls — previously they were invisible because the interceptor never created activities or set trace context on log entries.

## [2.23.7] - 2026-04-27

### Added
- **`CreateTestTrackingGrpcClient` convenience extension on `WebApplicationFactory`**: New `factory.CreateTestTrackingGrpcClient<TEntryPoint, TClient>(options)` extension method mirrors the HTTP `CreateTestTrackingClient` API for gRPC. A single call handles the `GrpcResponseVersionHandler` (HTTP/2 fix for TestServer), `GrpcChannel` creation, `GrpcTrackingInterceptor` installation, and typed gRPC client construction — making it impossible to accidentally forget tracking. Also provides a `CreateTestTrackingGrpcClientWithChannel` variant that returns the underlying `GrpcChannel` for disposal.
- **`GrpcResponseVersionHandler`**: Built-in `DelegatingHandler` that fixes the HTTP response version mismatch when testing gRPC services in-process via `TestServer`. `TestServer` returns HTTP/1.1, but gRPC requires HTTP/2 — this handler copies the request version onto the response. Previously, every test project had to implement their own `ResponseVersionHandler`; now it's included in the `TestTrackingDiagrams.Extensions.Grpc` package.

## [2.23.6] - 2026-04-26

### Added
- **`GrpcTrackingChannel` factory for incoming gRPC tracking**: New `GrpcTrackingChannel.Create()` and `CreateWithChannel()` static methods that create a tracked `CallInvoker` from an `HttpMessageHandler` and base address. This enables rich gRPC-aware diagrams for test-to-SUT gRPC calls — with protobuf JSON deserialization, operation classification (`UnaryCall`, `ServerStreamingCall`, etc.), `grpc://` URIs, and gRPC status code mapping — instead of raw HTTP/2 `POST` requests with binary bodies. Also provides `HttpMessageHandler.AsGrpcTrackingCallInvoker()` extension method for terser syntax.

## [2.23.5] - 2026-04-26

### Added
- **Automatic GraphQL operation detection in diagram arrows**: HTTP POST requests containing GraphQL request bodies are now automatically detected and the diagram arrow label is enriched with the operation type and name (e.g. `POST: /graphql\n(query GetUser)`, `POST: /api/data\n(mutation CreateOrder)`). Detection is purely body-based (no URL assumption) using a regex that identifies the GraphQL `"query"` JSON key and parses the operation type (`query`/`mutation`/`subscription`) and optional operation name. Anonymous shorthand queries (`{ user { name } }`) are labelled as `(query)`. The `operationName` JSON field is respected when present. No configuration or extra packages required — works automatically for all HTTP-tracked GraphQL traffic.

## [2.23.4] - 2026-04-26

### Added
- **Static `TestInfoFetcher` property on all framework adapter options classes**: `XUnitTestTrackingMessageHandlerOptions`, `NUnitTestTrackingMessageHandlerOptions`, `TUnitTestTrackingMessageHandlerOptions`, `MSTestTestTrackingMessageHandlerOptions`, `BDDfyTestTrackingMessageHandlerOptions`, `LightBddTestTrackingMessageHandlerOptions`, and `ReqNRollTestTrackingMessageHandlerOptions` now expose a `public static readonly Func<(string Name, string Id)> TestInfoFetcher` property. Extension options (e.g. `SqlTrackingInterceptorOptions`, `CosmosTrackingMessageHandlerOptions`) can reference this directly instead of writing verbose inline lambdas with null guards.

## [2.23.3] - 2026-04-26

### Fixed
- **Failure cluster links not scrolling to second scenario**: Clicking a failure cluster link after previously clicking another would change the URL hash but not scroll to the target scenario. The onclick handler now explicitly calls `scrollIntoView` and `preventDefault` instead of relying on native anchor navigation, which fails when `<details>` elements are dynamically opened.

## [2.23.2] - 2026-04-26

### Fixed
- **Aligned scenario-steps and example-diagrams borders in parameterized groups**: The steps panel and diagrams panel now have matching left borders in parameterized/multi-parameter scenarios. Previously, the steps panel was indented 1em further right due to its `.param-detail-panels` wrapper having `margin-left: 1em`.

## [2.23.1] - 2026-04-26

### Fixed
- **Search now includes feature names, descriptions, labels, and tags**: The report search bar now indexes feature display names, feature descriptions, feature labels, scenario categories, and scenario labels as plain text in the `data-search` attribute. Previously, searching for a feature name like "Pancakes Creation" would return no results. This applies to both regular scenarios and parameterized groups.

## [2.23.0] - 2026-04-26

### Added
- **Dual-resolution test identity across all extensions**: All 22 tracking extensions now support resolving test identity from HTTP request headers (propagated by `TestTrackingMessageHandler`) in addition to the existing `CurrentTestInfoFetcher` delegate. This fixes a systemic issue where extensions running inside the SUT's request pipeline (e.g. via `WebApplicationFactory`) could not resolve the test context because the test framework's `AsyncLocal` does not flow from the test thread to the server thread.
  - New `TestInfoResolver` static helper in the core package centralises the dual-resolution logic: try HTTP headers first, fall back to delegate.
  - Every tracker/handler constructor now accepts an optional `IHttpContextAccessor? httpContextAccessor` parameter (backward-compatible; defaults to `null`).
  - `MediatorTrackingExtensions` auto-resolves `IHttpContextAccessor` from DI when creating the tracking proxy.
  - `TrackingProxyOptions` gains an `HttpContextAccessor` property for extensions using `TrackingProxy<T>` (MediatR, DispatchProxy).
  - `SqlTrackingInterceptor` refactored to use the shared `TestInfoResolver`.
  - 13 new unit tests for `TestInfoResolver` covering header precedence, delegate fallback, null/exception handling, and the nullable-tuple overload.
- **Affected extensions**: Kafka, CosmosDB, ServiceBus, EventHubs, EventBridge, SQS, SNS, MediatR, MassTransit, Redis, MongoDB, BlobStorage, S3, BigQuery, CloudStorage, StorageQueues, DynamoDB, Elasticsearch, Dapper, gRPC, DispatchProxy (via TrackingProxy), and EfCore.Relational (refactored).

## [2.22.32] - 2026-04-26

### Fixed
- **Dependency filter**: `ExtractDependencies` now matches all PlantUML participant types (`actor`, `boundary`, `control`, `entity`, `database`, `collections`, `queue`, `participant`). Previously only `entity` and `participant` were matched, so dependencies rendered as `database`, `collections`, or `queue` (e.g. Cosmos DB, Redis, ServiceBus) were silently excluded from the dependency filter buttons in the HTML report.

## [2.22.31] - 2026-04-26

### Fixed
- **DiagramNoteTests**: Fixed `StaleElementReferenceException` in `Short_note_no_up_arrow_when_expanded` by re-querying hover rects after `SetScenarioState` re-renders the SVG.

## [2.22.30] - 2026-04-26

### Fixed
- **Wiki Feature 8 (CI Summary)**: Removed `!pragma teoz true` from failure diagram source so the PlantUML server renders a compact, readable diagram inside the CI summary panel.
- **Wiki Feature 11 (DiagramFocus)**: Replaced hand-crafted PlantUML source with real TTD-generated diagram via `PlantUmlCreator.GetPlantUmlImageTagsPerTestId()` using `RequestResponseLog` entries and `FocusFields`. Response note now correctly appears on the right side (matching real TTD output).
- **Wiki Feature 12 (Failure Diagnostics)**: Fixed failure cluster expansion using `setAttribute('open', '')` instead of unreliable `click()` on `<details>` element in headless Chrome. Cluster is now visibly expanded from the start of the GIF.

## [2.22.29] - 2026-04-26

### Fixed
- **Wiki Feature 8 (CI Summary)**: Expand all `<details>` and scroll to show embedded PlantUML diagram image.
- **Wiki Feature 9 (JSON Report)**: Rewrote JSON viewer using inline style toggling; CSS sibling selectors were unreliable in headless Chrome.
- **Wiki Feature 11 (DiagramFocus)**: Changed highlighted fields from `<color:blue><b>` to `<back:#FFEB3B>` (yellow background) for much more visible highlighting.
- **Wiki Feature 12 (Failure Diagnostics)**: Removed initial overview hold so GIF starts directly at the failure cluster section.

## [2.22.28] - 2026-04-26

### Fixed
- **Wiki Feature 8 (CI Summary)**: Replaced fake header overlay with real `CiSummaryGenerator.GenerateMarkdown()` output rendered in GitHub Actions-styled page.
- **Wiki Feature 9 (JSON Report)**: Changed JSON viewer from dark theme (mostly black) to GitHub-styled light theme with visible syntax highlighting and toolbar.
- **Wiki Feature 11 (DiagramFocus)**: Set diagram Details to "Expanded" before screenshot so response notes with highlighted fields are visible.
- **Wiki Feature 12 (Failure Clustering)**: Changed test data so 4 failures share the same error message (`Connection refused (Stock Service:5001)`), enabling the `FailureClusterer` to produce a visible cluster. GIF now leads with the cluster section.

## [2.22.27] - 2026-04-25

### Fixed
- **Race condition in ParameterGrouper.Analyze**: R2 flattening mutated shared `Scenario` objects' `ExampleValues`/`ExampleRawValues` dictionaries. When `Parallel.Invoke` in `CreateStandardReportsWithDiagrams` generated multiple reports concurrently, thread B could encounter a `KeyNotFoundException` reading a key that thread A's flattening had already replaced. `Analyze` now deep-clones all scenario dictionaries at entry, so each parallel call works on independent copies.

### Added
- **TUnit end-to-end integration tests**: Added TUnit to the integration test matrix (`TestProjects.All`) with full C# attribute → TUnit adapter → report HTML pipeline verification. 8 new tests in `TUnitParameterizedRenderingTests.cs` covering R1 (scalar `[Arguments]`), R2 (flattened `[MethodDataSource]` records), R3 (sub-table for complex params), and R4 (expandable nested objects).
- **TUnit parameterized example tests**: Added `Parameterized_Feature.cs` to the TUnit example project with 10 test cases covering all 4 rendering rules using real `[Arguments]` and `[MethodDataSource]` attributes with `OrderScenario`, `ShippingAddress`, and `CustomerOrder` records.
- **TUnit `dotnet run` support**: `TestProjectRunner` now detects Microsoft.Testing.Platform projects (TUnit) and uses `dotnet run` instead of `dotnet test`, which is required on .NET 10+.
- **ReportParser `ExtractParameterizedGroupsAsync`**: New helper for asserting parameterized rendering in HTML reports, extracting scenario names, column headers, row counts, sub-table and expandable presence.

## [2.22.26] - 2026-04-25

### Added
- **Realistic PlantUML diagrams in integration tests**: `GenerateReport` helper now embeds per-scenario PlantUML sequence diagrams instead of empty strings, eliminating "Decompression error" in generated HTML reports and making them viewable in a browser.
- **TUnit R3/R4 integration tests**: Added `TUnit_scalar_plus_small_complex_object_renders_R3_subtable` and `TUnit_scalar_plus_deeply_nested_object_renders_R4_expandable` covering sub-table and expandable rendering for TUnit adapter patterns.

## [2.22.25] - 2026-04-24

### Fixed
- **Truncated record ToString() parsing**: `TryParseRecordToString` now handles records truncated by xUnit/MSTest display name limits (ending in `··...` or `...` instead of ` }`), extracting all fully-parsed properties. Previously, truncated records fell back to R0 (raw string in a single cell) instead of R2 (flattened columns).

### Added
- **Comprehensive parameterized rendering integration tests**: 29 new tests in `ParameterizedRenderingIntegrationTests.cs` covering all 8 framework adapters (xUnit2, xUnit3, TUnit, NUnit4, MSTest, LightBDD, ReqNRoll, BDDfy) with scalar, complex record, truncated record, nullable, nested, and edge case scenarios. Tests generate full HTML reports and verify correct R0/R1/R2/R3/R4 rendering.
- **5 new parser unit tests** for truncated record handling (mid-property-name, mid-value, mid-quoted-string, plain ellipsis).

## [2.22.24] - 2026-04-24

### Added
- **String-based record `ToString()` parsing for parameterized groups**: When test parameters are complex objects without raw .NET object references (e.g. display-name-only parsing), the report now parses C# record `ToString()` representations (e.g. `TypeName { Prop = Val, ... }`) and decomposes them into individual table columns (R2 flattening), sub-tables (R3), or expandable details (R4).
- **Defensive error handling**: All string-based record parsing is wrapped in try-catch with `Debug.WriteLine` warnings, ensuring malformed values gracefully fall back to plain text rendering with no cascading failures.

## [2.22.23] - 2026-04-24

### Fixed
- **DependencyCategory defaults for all trackers**: `MessageTracker` and all 15 extension trackers now set the correct `DependencyCategory` on `RequestResponseLog` entries, so PlantUML diagrams render the correct participant shapes (e.g. `queue` for message brokers, `database` for databases, `collections` for storage) instead of defaulting to `entity`.
- **MessageTrackerOptions.DependencyCategory**: New property (default `"MessageQueue"`) allowing callers to override the category used by `MessageTracker`.
- **DependencyPalette**: Added 7 new category mappings: `MessageQueue`, `MongoDB`, `DynamoDB`, `Elasticsearch`, `S3`, `CloudStorage`, `gRPC`.

## [2.22.22] - 2026-04-24

### Fixed
- **Wiki GIF tests**: Rewrote all `WikiGifTests` with correct CSS selectors, rich test data (50+ scenarios across 8 features), and proper PlantUML rendering (force-call `_renderDiagramsInContainer` for headless Chrome where `IntersectionObserver` doesn't fire).
- **GIF file sizes**: Added `-resize 960x -fuzz 2% -layers optimize` to ImageMagick stitching, reducing GIF sizes from ~280MB total to ~7.3MB total. Increased `WaitForExit` timeout from 120s to 600s to prevent truncated GIFs.
- **Feature01 (Interactive Report)**: Reduced Hold() durations to target ~34s (was ~47s). Shows 6+ filter combinations (Failed, Passed, Dependency, P95, Happy Paths, Categories).
- **Feature09 (CI Summary)**: Rewritten to use real report with JS-injected GitHub Actions header instead of broken custom HTML page.

### Changed
- **What's New In 2.0 wiki**: Removed former Feature 13 (Category Filtering) and renumbered remaining features.

## [2.22.21] - 2026-04-24

### Added
- **`MessageTrackerOptions.UseHttpContextCorrelation`**: When `true`, the `MessageTracker` reads test info from `IHttpContextAccessor` request headers first (the same dual-layer correlation used by the legacy constructor), falling back to `CurrentTestInfoFetcher` when `HttpContext` is null. This enables safe migration from the legacy constructor without losing interactions tracked via HTTP-propagated headers.
- **`TestTrackingMessageHandlerOptions.ClientNamesToServiceNames`**: Maps `IHttpClientFactory` client names to human-readable service names for diagrams. Useful when HTTP mocking makes port-based mapping via `PortsToServiceNames` unreliable. Pass the client name via `new TestTrackingMessageHandler(options, clientName: builder.Name)`. Resolution order: `FixedNameForReceivingService` > `ClientNamesToServiceNames` > `PortsToServiceNames` > `localhost:port`.

### Documentation
- **Event-Annotations wiki**: Added migration warning for `UseHttpContextCorrelation`, fixed all `CurrentTestInfoFetcher` examples to use null-safe patterns (replaced `TestContext.Current.Test!.TestDisplayName` with null-check), added `UseHttpContextCorrelation` to the options properties table.
- **HTTP-Tracking-Setup wiki**: Added `ClientNamesToServiceNames` to the `TestTrackingMessageHandlerOptions` reference table.
- **Tracking-Dependencies wiki**: Added simplified `ClientNamesToServiceNames` example alongside existing Pattern 8.
- **Generated-Reports wiki**: Added "Validating Tracking Configuration Changes" section with gold standard comparison workflow.

## [2.22.20] - 2026-04-24

### Changed
- **TTD Version row in Test Execution Summary is now hidden**: The TTD Version row in the HTML report summary table is now `display:none` so it doesn't clutter the visible report. The version is still present in the HTML (and in the `<meta name="generator">` tag) for diagnostic purposes.

## [2.22.19] - 2026-04-23

### Fixed
- **Failure cluster links broken for parameterized test scenarios**: Links in the failure clusters section did not navigate to parameterized test rows because individual `<tr>` rows only had `data-scenario-id` attributes, not `id` attributes. Added `id` to parameterized `<tr>` rows and updated the onclick handler to walk up ancestor `<details>` elements (opening them) and trigger row selection via `click()`.

## [2.22.18] - 2026-04-23

### Fixed
- **Flaky `No_unused_component_warning_when_no_components_registered` test**: `MessageTrackerTests` and `TestTrackingMessageHandlerTests` construct `MessageTracker`/`TestTrackingMessageHandler` instances whose constructors call `TrackingComponentRegistry.Register()`, but these test classes were not in the `"TrackingComponentRegistry"` xUnit collection. This allowed them to run in parallel with `ReportDiagnosticsTests`, polluting the static registry between `Clear()` and `Analyse()`. Added `[Collection("TrackingComponentRegistry")]` to both classes.

## [2.22.17] - 2026-04-23

### Fixed
- **Scenario-steps `<details>` not aligned with example-diagrams `<details>`**: `.scenario-steps` had `padding: 0.5em 1em` on the container, pushing its content inward compared to `.example-diagrams` which has no container padding. Removed container padding and moved it to `.scenario-steps > summary` (`padding: 1em`, matching `.example-diagrams > summary`) and child steps (`margin-left/right: 1em`), so both sections are now left-aligned.

## [2.22.16] - 2026-04-23

### Fixed
- **Component diagram not embedded in TestRunReport when ComponentDiagramOptions is null**: The embed decision in `CreateStandardReportsWithDiagrams` used `options.ComponentDiagramOptions?.EmbedInTestRunReport == true` which evaluates to `false` when `ComponentDiagramOptions` is `null` (the default). This contradicted the PlantUML generation path on the same method which already falls back to `new ComponentDiagramOptions()` (where `EmbedInTestRunReport` defaults to `true`). Extracted the decision into `ShouldEmbedComponentDiagram()` using the same `?? new ComponentDiagramOptions()` null-coalesce pattern — no mutation of the original options object, avoiding the WAF NRE that the v2.22.6 fix caused.

## [2.22.15] - 2026-04-23

### Added
- **Phase-aware tracking configuration (Setup vs Action)**: All tracking extensions now support configuring behavior differently for the Setup phase (Given/Arrange) vs the Action phase (When/Act). This allows reducing noise from setup operations while keeping full detail for the actions under test.
  - **`TestPhase` enum**: New `Unknown`, `Setup`, `Action` values representing the current test phase.
  - **`TestPhaseContext`**: AsyncLocal-based ambient context (similar to `TrackingTraceContext`) that holds the current `TestPhase`. BDD frameworks (BDDfy, LightBDD, ReqNRoll) set this automatically based on the step type keyword. Non-BDD tests use `TrackingDiagramOverride.StartSetup()` and `TrackingDiagramOverride.StartAction()`.
  - **`PhaseConfiguration`**: Utility class providing `ShouldTrack()` (phase-aware enable/disable), `GetEffectiveVerbosity<T>()` (phase-aware verbosity override), and `ResolvePhaseFromStepType()` (keyword-to-phase mapping).
  - **Phase on `RequestResponseLog`**: Each log entry now carries a `Phase` property indicating which test phase produced it.
  - **`TrackingDiagramOverride.StartSetup()`**: New method (with `Action` delegate overload) to explicitly mark the Setup phase.
- **Phase properties on all extension options**: Every tracking extension options class now has `SetupVerbosity?`, `ActionVerbosity?`, `TrackDuringSetup` (default `true`), and `TrackDuringAction` (default `true`) properties. When a phase-specific verbosity is set, it overrides the default `Verbosity` during that phase. Setting `TrackDuringSetup = false` suppresses all tracking during setup.
- **Phase-aware tracker implementations**: All 21 tracker implementations (EF Core, Redis, Kafka, gRPC, MassTransit, MongoDB, CosmosDB, Elasticsearch, Dapper, BigQuery, BlobStorage, CloudStorage, DynamoDB, EventBridge, S3, SNS, SQS, StorageQueues, EventHubs, PubSub, ServiceBus) now check `PhaseConfiguration.ShouldTrack()` before recording and use `PhaseConfiguration.GetEffectiveVerbosity()` to resolve the active verbosity level.
- **Phase-aware core handlers**: `TestTrackingMessageHandler`, `MessageTracker`, `TrackingProxy`, and `RequestResponseLogger` all support phase-based filtering and verbosity.
- **30 new unit tests** for `TestPhaseContext` (5 tests) and `PhaseConfiguration` (25 tests) covering all phase combinations, verbosity override precedence, and step-type keyword resolution.

## [2.22.14] - 2026-04-23

### Changed
- **Scenario-steps sections now have a rounded border**: `.scenario-steps` `<details>` elements are now surrounded by a slightly rounded `1px solid` border matching the `.example-diagrams` styling (`border-radius: 1em`, `border-color: rgb(224, 224, 224)`). The summary also received `background-color: white` and `border-radius: 1em` for visual consistency.
- **Parameterized detail-panel steps now use collapsible `<details>/<summary>`**: Steps within `param-detail-panel` were previously rendered as a plain `<div>`. They now use the same `<details class="scenario-steps" open><summary class="h4">Steps</summary>` pattern as normal scenario steps, making them collapsible and visually consistent.
- **Violet theme border override updated**: The violet theme's `.scenario-steps` override changed from `border-left-color` to `border-color` to match the new full-border design.

## [2.22.13] - 2026-04-23

### Fixed
- **Flaky TrackingComponentRegistry tests under parallel execution**: `TrackingComponentRegistry.Clear()` used a `while (TryTake)` drain loop on `ConcurrentBag` which is not atomic — items added from other threads could survive the clear due to thread-local storage stealing delays. Replaced with `Interlocked.Exchange` to atomically swap in a fresh empty bag. Also added `[CollectionDefinition]` for the `"TrackingComponentRegistry"` xUnit collection to ensure proper test serialisation.

## [2.22.12] - 2026-04-23

### Fixed
- **Note toggle buttons not working with SeparateSetup/partition diagrams**: `findNoteGroups()` incorrectly detected participant boxes and partition labels (fill `#E2E2F0`) as note groups, causing a mismatch between SVG note groups and source note blocks. Hover rects and button click handlers were attached to the wrong elements, making note collapse/expand/cycle actions appear broken. Fixed by excluding the standard PlantUML participant/partition fill (`#E2E2F0`) from `hasNoteFill()` and adding a safety-net fill-frequency filter in `makeNotesCollapsible()` that reconciles group counts with note block counts.
- **WebApplicationFactory NRE on teardown when ComponentDiagramOptions is null**: The v2.22.6 fix for `ComponentDiagramOptions` null handling changed the semantic behavior — `null` options now defaulted to embedding the component diagram (via `new ComponentDiagramOptions()` where `EmbedInTestRunReport = true`). This caused unexpected component diagram embedding for consumers who never configured `ComponentDiagramOptions`, adding work during report generation that could trigger a `NullReferenceException` in `WebApplicationFactory<T>.DisposeAsync()`. Reverted to the v2.22.5 null-conditional behavior: `null` `ComponentDiagramOptions` now means "don't embed".

### Added
- **TTD version embedded in all reports**: The TestTrackingDiagrams version is now included in HTML reports (as a `<meta name="generator">` tag and in the Test Execution Summary table), JSON data (`ttdVersion` field), YAML data (`TtdVersion` field), XML data (`<TtdVersion>` element), and the JSON schema.

## [2.22.11] - 2026-04-23

### Added
- **R2 (FlattenedObject) parameter display rule**: When a parameterized test has a single complex object parameter with all scalar properties (≤ max columns), its properties are automatically flattened into individual columns in the parameter table. This provides a clear, readable view of complex test case objects instead of showing a single wall-of-text column. Only applies when structured extraction is available (xUnit3, TUnit, NUnit4).
- **R3 (SubTable) cell rendering**: Within R1/R2 parameter tables, individual cell values that are small complex objects (≤5 scalar properties, no nesting) are rendered as a mini sub-table inside the cell, with property names as row headers and values as row data.
- **R4 (ExpandableComplex) cell rendering**: Within R1/R2 parameter tables, individual cell values that are deeply complex (nested objects, arrays, or >5 properties) are rendered as a collapsible `<details>/<summary>` element with a type-name preview and syntax-highlighted JSON expansion body.
- **`ExampleRawValues` on Scenario**: New `Dictionary<string, object?>?` property that preserves the raw parameter objects (not just `ToString()` strings) for reflection-based R2/R3/R4 rendering.
- **`ParameterParser.ExtractStructuredParametersWithRaw()`**: New method that returns both string values and raw object references, used by xUnit3, TUnit, and NUnit4 adapters.
- **`ParameterValueRenderer` helper class**: Internal static class providing object introspection (`IsScalarType`, `IsSmallComplexObject`, `IsComplexValue`, `TryGetFlattenableProperties`), property flattening (`FlattenToStringValues`, `FlattenToRawValues`), and HTML rendering (`RenderSubTable`, `RenderExpandable`, `GenerateHighlightedJson`).
- **CSS for R3/R4**: `.cell-subtable` styles for sub-table cells and `details.param-expand` / `.expand-body` / `.prop-key` / `.prop-val` styles for expandable complex parameter cells.
- **JavaScript for R4 interaction**: Expanding a `<details class="param-expand">` element auto-selects the containing row (switching diagrams/detail panels); sub-table clicks don't bubble to row selection.
- **40 new unit tests** for `ParameterValueRenderer` covering type classification, property introspection, flattening, sub-table rendering, expandable rendering, JSON highlighting, and preview generation.
- **5 new R2 detection tests** in `ParameterGrouperTests` covering single complex param flattening, nested objects not flattening, no raw values fallback, multiple params not triggering R2, and max columns exceeded.
- **10+ new R2/R3/R4 rendering integration tests** in `ParameterizedGroupRenderTests` verifying flattened property columns, sub-table HTML, expandable details HTML, CSS/JS presence, and correct scalar fallback for single primitives.

### Changed
- **xUnit3, TUnit, NUnit4 adapters**: Updated to use `ExtractStructuredParametersWithRaw()` and populate `ExampleRawValues` alongside `ExampleValues`, enabling R2/R3/R4 cell rendering when structured extraction is available.
- **`ParameterGrouper.DetermineParamsAndRule()`**: Now detects R2 (FlattenedObject) when all scenarios have a single complex parameter with all-scalar properties ≤ max columns, flattening the property names into columns and updating `ExampleValues`/`ExampleRawValues` on each scenario.
- **`ReportGenerator.RenderParameterizedGroup()`**: Header and cell rendering now handles `FlattenedObject` rule identically to `ScalarColumns`, and applies per-cell R3/R4 rendering based on `ExampleRawValues` type inspection.

## [2.22.10] - 2026-04-23

### Added
- **Structured parameter extraction for TUnit, NUnit4, and MSTest adapters**: Parameterized test tables now use real parameter names instead of positional `arg0`, `arg1` keys when the framework provides access to method arguments and parameter metadata.
  - **TUnit**: Uses `TestDetails.TestMethodArguments` and `MethodMetadata.Parameters` to extract named parameter values directly, bypassing string parsing entirely.
  - **NUnit4**: Uses `TestContext.Test.Arguments` and `TestContext.Test.Method.GetParameters()` for the same structured extraction.
  - **MSTest**: Captures parameter names via `MethodInfo.GetParameters()` in `DiagrammedComponentTest.TestTrackingCleanup()` and rebinds positional keys from display-name parsing to real parameter names. Added `ParameterNames` property to `MSTestScenarioInfo`.
  - **xUnit3**: Refactored to use shared `ParameterParser.ExtractStructuredParameters()` method (no behavioral change — xUnit3 already had structured extraction).
- **`ParameterParser.ExtractStructuredParameters()` shared method**: New public method on `ParameterParser` that maps raw argument values to parameter names, used by all adapters that support structured extraction.

## [2.22.9] - 2026-04-23

### Changed
- **Steps section is now collapsible**: Test steps within each scenario are wrapped in a `<details>` element with a "Steps" summary heading, matching the pattern used by the Diagrams section. Steps are expanded by default but can be collapsed by clicking the heading to reduce visual noise, especially for scenarios with many or long parameterized steps.
- **Removed left border from top-level steps**: The 3px vertical border on the left of the top-level steps container has been removed for a cleaner look. Sub-step borders are preserved.

## [2.22.8] - 2026-04-23

### Fixed
- **Parameter parser now handles curly-brace nesting in C# record `ToString()` output**: `SplitParams()` and `FindColon()` previously only tracked parenthesis depth, so commas inside `TypeName { Prop = val, ... }` structures (produced by C# record auto-generated `ToString()`) were incorrectly treated as top-level parameter separators. This caused parameterized test tables to split a single complex parameter into multiple mangled columns. Both methods now track `braceDepth` alongside `parenDepth`.

## [2.22.7] - 2026-04-23

### Fixed
- **Removed redundant "Loading component diagram..." text**: The placeholder text was shown alongside the "Rendering Diagram..." indicator from the PlantUML renderer, creating duplicate loading messages.
- **Component Diagram and Scenario Timeline toggles now act as radio buttons**: Activating one automatically deactivates the other and removes its active styling, preventing both panels from being visible simultaneously.

## [2.22.6] - 2026-04-23

### Fixed
- **Component diagram not embedded in TestRunReport when ComponentDiagramOptions is null**: When users did not explicitly set `ComponentDiagramOptions` on `ReportConfigurationOptions` (the common case), the null-conditional `?.EmbedInTestRunReport == true` evaluated to `null == true` → `false`, silently suppressing the embedded component diagram despite `EmbedInTestRunReport` defaulting to `true`. Now falls back to `new ComponentDiagramOptions()` before checking the flag.

## [2.22.5] - 2026-04-22

### Fixed
- **Fixed flaky Selenium note toggle tests under concurrent load**: `WaitForReRender()` now waits for both the SVG re-render AND `makeNotesCollapsible()` to finish adding `.note-toggle-icon` elements. Previously it returned as soon as the SVG innerHTML changed, but under CPU contention from parallel Chrome instances, there was a timing gap before the JS callback created toggle icons — causing assertions on button counts/types to see stale or missing state. `SetScenarioState()` also now waits for toggle icons alongside hover rects.

## [2.22.4] - 2026-04-22

### Changed
- **Component diagram is now a toolbar toggle instead of a collapsible `<details>` section**: The embedded component diagram in the TestRunReport is hidden by default and revealed via a "Component Diagram" toggle button in the toolbar (matching the existing "Scenario Timeline" button style). This avoids the large diagram dominating the report on load. The PlantUML renderer is triggered on first show via `_renderDiagramsInContainer`, ensuring the diagram renders correctly despite being initially hidden from the IntersectionObserver.

## [2.22.3] - 2026-04-22

### Fixed
- **Restored activity diagram and flame chart span capture**: The v2.21.1 fix for Application Insights dependency tracking over-corrected by excluding *all* well-known auto-instrumentation `ActivitySource`s (`Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `Npgsql`, `StackExchange.Redis`, etc.) from the `InternalFlowActivityListener`. Since `FilterByAutoInstrumentation` requires at least one well-known source span to anchor traces, this caused activity diagrams and flame charts to be completely empty for projects not using `AddTestTrackingExporter()`. The listener now only excludes `System.Net.Http` — the sole source where `ActivitySource.HasListeners()` triggers a mutually exclusive code path in `DiagnosticsHandler` that breaks Application Insights dependency telemetry.

## [2.22.2] - 2026-04-22

### Fixed
- **Note collapse/expand 3-state cycle for long notes**: Fixed three bugs in the note truncation/collapse/expand system:
  1. The ▼ (expand) button and + button from collapsed state now correctly go to **truncated** (step 1) for long notes, instead of skipping directly to fully expanded (step 2).
  2. The double-click cycle from collapsed state now correctly goes to **truncated** for long notes, instead of fully expanded.
  3. `isLongNote()` checks now use the container's per-scenario `_truncateLines` value instead of falling back to the global `window._truncateLines`. This fixes the ▲ button not appearing after scenario-level truncation changes.
- **Tooltip truncation for collapsed notes**: Collapsed note tooltips now respect the container's per-scenario truncation level instead of always using the global default.

### Added
- **Comprehensive Selenium tests for note state transitions**: Added 16 new Selenium tests covering all note collapse/expand/truncate state transitions including:
  - Long note 3-state double-click cycle (expanded → truncated → collapsed → truncated)
  - ▼ button from collapsed → truncated (not expanded) for long notes
  - ▼ button from truncated → expanded
  - ▲ button visibility and click behavior
  - Short note 2-state cycle (expanded ↔ collapsed)
  - Truncation level changes affecting note "long" classification
  - Minus button state transitions

## [2.22.1] - 2026-04-22

### Fixed
- **Deferred `InternalFlowActivityListener` startup**: The `TestTrackingMessageHandler` constructor no longer calls `InternalFlowActivityListener.EnsureStarted()`. The listener is now started lazily on the first `SendAsync()` call. Registering an `ActivityListener` during DI resolution could alter `ActivitySource.HasListeners()` state before Application Insights' `DependencyTrackingTelemetryModule` and the host had fully initialised, preventing `DiagnosticsHandler` from being added to the HTTP pipeline and silently breaking HTTP dependency telemetry.
- **Traceparent injection limited to TestServer scenarios**: `TestTrackingMessageHandler.SendAsync()` now only injects the `traceparent` header when `Activity.Current` is null (i.e. in-process TestServer calls). When an ambient Activity already exists, framework handlers (e.g. `DiagnosticsHandler` inside `SocketsHttpHandler`) create proper child Activities and inject `traceparent` themselves — pre-empting this by injecting the parent’s span ID broke Application Insights dependency correlation.
- **Fixed flaky `No_tracking_component_section_when_none_registered` test**: The diagnostic report test now clears `TrackingComponentRegistry` before asserting, preventing pollution from parallel test runs that create `TestTrackingMessageHandler` instances.

## [2.22.0] - 2026-04-22

### Added
- **Dependency-Type Colored Arrows & Typed Shapes**: Sequence and component diagrams now color-code arrows and use typed shapes based on the target service's dependency type (Issue #21).
  - New `DependencyCategory` parameter on `RequestResponseLog` — set automatically by all extension packages (CosmosDB, Redis, EF Core, ServiceBus, BigQuery, BlobStorage).
  - New `DependencyType` enum: `HttpApi`, `Database`, `Cache`, `MessageQueue`, `Storage`, `Unknown`.
  - New `DependencyPalette` static class with default vivid color palette and category-to-type resolution.
  - **Sequence diagrams**: Services render with typed PlantUML shapes (`database` for DB/Storage, `collections` for Cache, `queue` for MessageQueue, `entity` for HTTP APIs). Request/response arrows are colored by target service type.
  - **Component diagrams**: C4 mode uses `SystemDb()`/`SystemQueue()` for appropriate types. Plain PlantUML mode uses `database`/`collections`/`queue` shapes with matching skinparams. Arrows colored by dependency type (default) or P95 latency (opt-in via `ArrowColorMode.Performance`).
  - New `ArrowColorMode` enum to select between `DependencyType` (default) and `Performance` arrow coloring.
  - New config options on `ReportConfigurationOptions`: `SequenceDiagramArrowColors`, `SequenceDiagramParticipantColors`, `DependencyColors`, `ServiceTypeOverrides`.
  - New `DependencyColors` property on `ComponentDiagramOptions` for per-diagram color overrides.
- **Embedded Component Diagram in TestRunReport**: When `ComponentDiagramOptions.EmbedInTestRunReport` is `true` (default), the component diagram is rendered inline in the TestRunReport as a collapsible section before the scenario list, using the same BrowserJs PlantUML renderer. The standalone `ComponentDiagram.html` file continues to be generated as well.

### Changed
- Default arrow style in both sequence and component diagrams is now dependency-type colored. Previous behavior (plain arrows or P95-based coloring) is available via `SequenceDiagramArrowColors = false` or `ArrowColorMode.Performance`.
- `GetProtocol` in `ComponentDiagramGenerator` now prefers `DependencyCategory` over HTTP method when available, producing labels like "CosmosDB: Query" instead of "HTTP: POST".

## [2.21.2] - 2026-04-22

### Fixed
- **`TestTrackingMessageHandler.SendAsync` no longer sets `Activity.Current`**: The handler was creating a `new Activity("TestTrackingDiagrams.Request").Start()` when no ambient Activity existed, which set `Activity.Current` and interfered with Application Insights' telemetry correlation — `DependencyTelemetry` items received the wrong `Context.Operation.Name` (or none at all) instead of the server request's operation name (e.g. `"GET /health"`). The handler now generates trace/span IDs directly via `ActivityTraceId.CreateRandom()` / `ActivitySpanId.CreateRandom()` and injects the `traceparent` header without creating an Activity. This preserves W3C trace context propagation for InternalFlow span correlation while leaving `Activity.Current` untouched.

## [2.21.1] - 2026-04-22

### Fixed
- **`InternalFlowActivityListener` no longer breaks Application Insights HTTP dependency tracking**: The listener was subscribing to ALL `ActivitySource`s (`ShouldListenTo = _ => true`), which caused .NET's `System.Net.Http.DiagnosticsHandler` to take the `ActivitySource`-based code path instead of the `DiagnosticListener`-based path. Application Insights SDK 2.x only creates `DependencyTelemetry` from the latter, so HTTP dependency tracking was silently broken. The listener now excludes well-known auto-instrumentation sources (e.g. `System.Net.Http`, `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`) via `InternalFlowSpanCollector.WellKnownAutoInstrumentationSources`. Custom application `ActivitySource` spans are still captured for internal flow diagrams.

## [2.21.0] - 2026-04-22

### Added
- **`MessageTracker` upgraded to first-class tracking component**: The core `MessageTracker` class (used for tracking custom messaging abstractions) now implements `ITrackingComponent` with auto-registration in `TrackingComponentRegistry`, enabling unused-component diagnostic warnings.
  - New `MessageTrackerOptions` record with `ServiceName`, `CallingServiceName`, `Verbosity`, `CurrentTestInfoFetcher`, and `SerializerOptions` — aligns `MessageTracker` with the same options pattern used by all extension packages.
  - New `MessageTracker(MessageTrackerOptions)` constructor — recommended for new code. The legacy `IHttpContextAccessor`-based constructor is preserved for backward compatibility.
  - New `MessageTrackerVerbosity` enum (Raw, Detailed, Summarised) — `Summarised` omits message payloads from diagrams.
  - New `TrackSendEvent()` one-shot method — logs a complete fire-and-forget request/response pair in a single call, reducing boilerplate for event-driven patterns.
  - New `TrackMessagesForDiagrams(MessageTrackerOptions)` DI overload.

### Fixed
- Fixed `ReportDiagnosticsTests.Unused_component_warning_lists_count` flaky test — now clears `TrackingComponentRegistry` before asserting component counts, preventing pollution from parallel test runs.

## [2.20.0] - 2026-04-22

### Added
- **New `TestTrackingDiagrams.Extensions.EventBridge` package**: Track Amazon EventBridge operations in test diagrams via the AWS SDK HTTP pipeline. Intercepts `X-Amz-Target` JSON-RPC calls using the same `DelegatingHandler` pattern as the S3, DynamoDB, SQS, and SNS extensions. Includes:
  - `EventBridgeTrackingMessageHandler` — DelegatingHandler implementing `ITrackingComponent` with auto-registration. Classifies and logs PutEvents, rule management, target management, event bus lifecycle, archive, replay, and tagging operations.
  - `EventBridgeOperationClassifier` — Dictionary-based classifier mapping 28 `X-Amz-Target` headers to `EventBridgeOperation` enum values, with JSON body parsing for PutEvents (DetailType, Source, EntryCount, EventBusName) and rule operations (Name, EventBusName).
  - `AmazonEventBridgeConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonEventBridgeConfig` that injects the tracking handler via `HttpClientFactory`.
  - URI scheme: `eventbridge://{busName}/` (Detailed/Summarised), original AWS URL (Raw).
  - Three verbosity levels: Raw (full HTTP details), Detailed (classified labels with context like `PutEvents [OrderCreated] x5`), Summarised (grouped labels like `ManageRule`, `ManageTargets`, `ManageBus`).
  - Default excluded operations: TagResource, UntagResource, ListTagsForResource, ListEventBuses.

## [2.19.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.Dapper` package**: Track Dapper and ADO.NET SQL operations in test diagrams. Wraps `DbConnection` to intercept all query execution with zero Dapper-specific dependencies — works with any ADO.NET provider. Includes:
  - `TrackingDbConnection` — Decorator wrapping `DbConnection` that implements `ITrackingComponent` with auto-registration. Creates `TrackingDbCommand` instances that intercept `ExecuteReader`, `ExecuteNonQuery`, `ExecuteScalar` (sync + async).
  - `TrackingDbCommand` — Intercepts all execution methods, classifies the SQL, and logs request/response pairs to `RequestResponseLogger`.
  - `TrackingDbTransaction` — Transparent wrapper that logs `BEGIN`, `COMMIT`, and `ROLLBACK` operations.
  - `DapperOperationClassifier` — Regex-based classifier recognising 15 SQL operation types (Query, Insert, Update, Delete, Merge, StoredProcedure, CreateTable, AlterTable, DropTable, CreateIndex, Truncate, BeginTransaction, Commit, Rollback, Other) with table name extraction.
  - `DbConnectionExtensions.WithTestTracking()` — Fluent extension on any `DbConnection` that wraps it in a `TrackingDbConnection`.
  - URI scheme: `sql://dataSource/database/table` (Detailed), `sql://dataSource/database` (Raw), `sql:///database/table` (Summarised).
  - Three verbosity levels with configurable SQL text logging, parameter logging, and operation exclusions.

## [2.18.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.Elasticsearch` package**: Track Elasticsearch operations in test diagrams via the Elastic .NET client's `OnRequestCompleted` callback. Intercepts and classifies REST API operations across indices. Includes:
  - `ElasticsearchTrackingCallbackHandler` — Callback handler implementing `ITrackingComponent`. Classifies and logs index, search, document, bulk, and cluster operations.
  - `ElasticsearchOperationClassifier` — Classifies 24 Elasticsearch REST API operations (IndexDocument, GetDocument, Search, Bulk, CreateIndex, DeleteIndex, etc.) from URL path patterns and HTTP methods.
  - `ElasticsearchClientSettingsExtensions.WithTestTracking()` — Fluent extension on `ElasticsearchClientSettings` that enables `DisableDirectStreaming` and registers the tracking callback.
  - URI scheme: `elasticsearch:///indexName` (Detailed), full request URI (Raw), `elasticsearch:///` (Summarised).
  - Configurable operation exclusions (ClusterHealth and CatApis excluded by default), request/response body capture, and three verbosity levels.

## [2.17.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.Kafka` package**: Track Apache Kafka produce and consume operations in test diagrams using wrapper classes around Confluent.Kafka's `IProducer<TKey,TValue>` and `IConsumer<TKey,TValue>`. Includes:
  - `KafkaTracker` — Central logging component implementing `ITrackingComponent`. Logs produce, consume, subscribe, and commit operations with Event MetaType. Consume operations swap caller/service names to reflect incoming message direction.
  - `TrackingKafkaProducer<TKey,TValue>` — Wrapper implementing `IProducer<TKey,TValue>` that intercepts `Produce` and `ProduceAsync` calls with topic, partition, and offset tracking.
  - `TrackingKafkaConsumer<TKey,TValue>` — Wrapper implementing `IConsumer<TKey,TValue>` that intercepts `Consume` and `Subscribe` calls, skipping EOF/null results.
  - `KafkaOperationClassifier` — Classifies operations (Produce, ProduceAsync, Consume, Subscribe, Unsubscribe, Commit, Flush) with topic, partition, and offset details.
  - URI scheme: `kafka:///topic` (Detailed), `kafka:///topic/partition@offset` (Raw), `kafka:///` (Summarised).
  - Supports configurable produce/consume/subscribe/commit tracking, message key/value logging, and three verbosity levels.

## [2.16.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.MassTransit` package**: Track MassTransit message operations (RabbitMQ, Azure Service Bus, Amazon SQS, and other transports) in test diagrams using MassTransit observer interfaces. Includes:
  - `MassTransitTracker` — Central logging component implementing `ITrackingComponent`. Logs send, publish, consume, and fault operations with Event MetaType.
  - `TrackingSendObserver`, `TrackingPublishObserver`, `TrackingConsumeObserver` — MassTransit observer implementations that delegate to the tracker.
  - `MassTransitOperationClassifier` — Classifies operations (Send, Publish, Consume, SendFault, PublishFault, ConsumeFault) with message type and URI extraction.
  - `BusConfigurationExtensions.WithTestTracking()` — Fluent extension on `IBusFactoryConfigurator`.
  - URI scheme: `masstransit:///queue-name` (Detailed) or transport URI (Raw).
  - Supports configurable send/publish/consume tracking, message body logging, and fault logging.

## [2.15.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.StorageQueues` package**: Track Azure Storage Queue operations in test diagrams using the Azure.Core Transport pattern (same as BlobStorage). Includes:
  - `StorageQueueOperationClassifier` — Classifies Storage Queue REST API operations (SendMessage, ReceiveMessages, PeekMessages, DeleteMessage, UpdateMessage, ClearMessages, CreateQueue, DeleteQueue, GetProperties, SetMetadata, ListQueues) from URL path patterns and query parameters.
  - `StorageQueueTrackingMessageHandler` — `DelegatingHandler` + `ITrackingComponent` that intercepts HTTP requests, classifies operations, and logs request/response pairs.
  - `QueueClientOptionsExtensions.WithTestTracking()` — Fluent extension on `QueueClientOptions` that sets `Transport` to `HttpClientTransport` with tracking handler.
  - URI scheme: `storagequeue:///queueName`.
  - Supports three verbosity levels: Raw, Detailed (with queue name in labels), and Summarised.

## [2.14.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.EventHubs` package**: Track Azure Event Hubs operations in test diagrams using the wrapper/decorator pattern around `EventHubProducerClient` and `EventHubConsumerClient`. Includes:
  - `EventHubsOperationClassifier` — Classifies Event Hubs operations (Send, SendBatch, CreateBatch, ReadEvents, ReadEventsFromPartition, GetPartitionIds, GetEventHubProperties, GetPartitionProperties, StartProcessing, StopProcessing, ProcessEvent) by method name with event count awareness.
  - `EventHubsTracker` — Central logging helper implementing `ITrackingComponent`. Logs request/response pairs with Event MetaType. Supports three verbosity levels with partition ID in URI.
  - `TrackingEventHubProducerClient` — Wrapper around `EventHubProducerClient` tracking single and batch send operations with event body serialization.
  - `TrackingEventHubConsumerClient` — Wrapper around `EventHubConsumerClient` tracking `ReadEventsAsync` and `ReadEventsFromPartitionAsync` via `IAsyncEnumerable`.
  - URI scheme: `eventhubs:///hub-name[/partition-id]`.

## [2.13.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.CloudStorage` package**: Track Google Cloud Storage operations in test diagrams using the `DelegatingHandler` pattern via Google APIs `HttpClientFactory`. Includes:
  - `CloudStorageOperationClassifier` — Classifies GCS REST API operations (Upload, Download, Delete, ListObjects, GetMetadata, UpdateMetadata, Copy, Compose, CreateBucket, DeleteBucket, GetBucket, ListBuckets) from URL path patterns. Distinguishes Download vs GetMetadata via `alt=media` query parameter.
  - `CloudStorageTrackingMessageHandler` — `DelegatingHandler` + `ITrackingComponent` that intercepts HTTP requests, classifies operations, and logs request/response pairs.
  - `TrackingCloudStorageHttpClientFactory` — Google APIs `HttpClientFactory` wrapper that injects the tracking handler.
  - `StorageClientBuilderExtensions.WithTestTracking()` — Fluent extension on `StorageClientBuilder`.
  - URI scheme: `gcs:///bucket/object` (Detailed) or original Google API URL (Raw).
  - Handles URL-encoded object names, Copy/Compose paths, and bucket-level operations.

## [2.12.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.SNS` package**: Track Amazon SNS operations in test diagrams using the `DelegatingHandler` pattern via `AmazonSimpleNotificationServiceConfig.HttpClientFactory`. Includes:
  - `SnsOperationClassifier` — Classifies SNS operations (Publish, PublishBatch, Subscribe, Unsubscribe, CreateTopic, DeleteTopic, ListTopics, ListSubscriptions, ListSubscriptionsByTopic, GetTopicAttributes, SetTopicAttributes, ConfirmSubscription) from `X-Amz-Target: AmazonSimpleNotificationService.{Op}` header or legacy `Action` query/form parameter. Extracts topic name from `TopicArn`/`TargetArn` ARN fields.
  - `SnsTrackingMessageHandler` — `DelegatingHandler` + `ITrackingComponent` that intercepts HTTP requests, classifies operations, and logs request/response pairs with configurable verbosity. Reconstructs request body after classification for downstream handlers.
  - `AmazonSNSConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonSimpleNotificationServiceConfig` that installs the tracking handler via `HttpClientFactory`.
  - URI scheme: `sns:///topic-name` (Detailed/Summarised) or original AWS URL (Raw).
  - Supports FIFO topics (`.fifo` suffix preserved), direct publish via `TargetArn`, and full ARN extraction.

## [2.11.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.PubSub` package**: Track Google Cloud Pub/Sub operations in test diagrams using wrapper/decorator pattern around `PublisherClient` and `SubscriberClient`. Includes:
  - `PubSubOperationClassifier` — Classifies Pub/Sub operations (Publish, PublishBatch, Pull, Acknowledge, ModifyAckDeadline, Receive, StartSubscriber, StopSubscriber) with short name extraction from full GCP resource paths (`projects/p/topics/t` → `t`).
  - `PubSubTracker` — Central logging helper implementing `ITrackingComponent`. Logs request/response pairs with Event MetaType for publish/receive operations. Supports three verbosity levels.
  - `TrackingPublisherClient` — Wrapper around `PublisherClient` tracking single and batch publish operations with message content at Raw/Detailed verbosity.
  - `TrackingSubscriberClient` — Wrapper around `SubscriberClient` that wraps the message handler callback to track received messages and Ack/Nack replies.
  - URI scheme: `pubsub:///topic-name` (Detailed) or `pubsub:///projects/p/topics/t` (Raw).

## [2.10.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.Grpc` package**: Track gRPC client calls in test diagrams using the `Grpc.Core.Interceptors.Interceptor` API. Includes:
  - `GrpcOperationClassifier` — Classifies gRPC calls by method type (Unary, ServerStreaming, ClientStreaming, DuplexStreaming) with service name, method name, and full method path extraction from `ClientInterceptorContext`.
  - `GrpcTrackingInterceptor` — Client-side interceptor that intercepts all gRPC call types (AsyncUnaryCall, BlockingUnaryCall, AsyncServerStreamingCall, AsyncClientStreamingCall, AsyncDuplexStreamingCall). Logs request/response pairs with protobuf message serialization. Implements `ITrackingComponent` with auto-registration. Maps gRPC `StatusCode` to HTTP status codes for consistent error logging.
  - `GrpcChannelExtensions.WithTestTracking()` — Fluent extension on `GrpcChannel` that wraps the channel's `CallInvoker` with the tracking interceptor.
  - Three verbosity levels: Raw (full method path + call type annotation + headers), Detailed (method name with streaming annotations + grpc URI + message content), Summarised (method name only, no content).
  - URI scheme: `grpc:///ServiceName/MethodName` (path-based to preserve casing).
  - Optional `UseProtoServiceNameInDiagram` to use the proto service name instead of the configured `ServiceName`.
  - Streaming calls tracked at initiation level (not per-message) for clean diagrams.

## [2.9.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.SQS` package**: Track Amazon SQS operations in test diagrams. Includes:
  - `SqsOperationClassifier` — Classifies SQS operations from both the JSON protocol (`X-Amz-Target: AmazonSQS.{Op}`) and legacy query protocol (`Action=` parameter in query string or form body). Extracts queue names from URL path (`/account-id/queue-name`), body `QueueUrl` field, or body `QueueName` field. Supports 14 operations: messaging (SendMessage, SendMessageBatch, ReceiveMessage, DeleteMessage, DeleteMessageBatch), visibility (ChangeMessageVisibility, ChangeMessageVisibilityBatch), queue management (CreateQueue, DeleteQueue, GetQueueUrl, GetQueueAttributes, SetQueueAttributes, PurgeQueue, ListQueues).
  - `SqsTrackingMessageHandler` — `DelegatingHandler` that intercepts all SQS HTTP traffic, reads and reconstructs request bodies for classification, and logs request/response pairs for diagram generation. Implements `ITrackingComponent` with auto-registration.
  - `AmazonSQSConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonSQSConfig` that injects a tracking `HttpClientFactory` into the AWS SDK pipeline. Zero production code changes required.
  - Three verbosity levels: Raw (HTTP method + full URI), Detailed (classified operation name + `sqs:///QueueName` URI + request/response bodies), Summarised (operation name only, `sqs:///QueueName` URI, no content/headers, skips unrecognised operations).
  - URI scheme: `sqs:///QueueName` (path-based to preserve casing, supports FIFO queue `.fifo` suffix).
  - Default excluded headers: `Authorization`, `x-amz-date`, `x-amz-security-token`, `x-amz-content-sha256`, `User-Agent`, `amz-sdk-invocation-id`, `amz-sdk-request`.

## [2.8.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.MongoDB` package**: Track MongoDB operations in test diagrams using the driver's built-in command monitoring events. Includes:
  - `MongoDbOperationClassifier` — Classifies MongoDB commands by name (find, insert, update, delete, aggregate, count, findAndModify, distinct, bulkWrite, createIndexes, dropIndexes, create, drop, listCollections, listDatabases, getMore), extracts collection name from the command BsonDocument, and optionally extracts filter text.
  - `MongoDbTrackingSubscriber` — Event-driven subscriber that hooks into `CommandStartedEvent`, `CommandSucceededEvent`, and `CommandFailedEvent` via `ClusterBuilder.Subscribe<T>()`. Uses `ConcurrentDictionary<int, PendingOperation>` to correlate request/response pairs by `RequestId`. Implements `ITrackingComponent` with auto-registration.
  - `MongoClientSettingsExtensions.WithTestTracking()` — Fluent extension on `MongoClientSettings` that chains a tracking subscriber into `ClusterConfigurator` without replacing existing configurators.
  - Three verbosity levels: Raw (full BSON command/reply + database.collection + filter), Detailed (operation → collection label + filter as request content), Summarised (operation name only, no content).
  - Default ignored commands: `isMaster`, `hello`, `saslStart`, `saslContinue`, `ping`, `buildInfo`, `getLastError`, `killCursors`. Optional `getMore` tracking (disabled by default).
  - URI scheme: `mongodb:///database/collection` (path-based to preserve casing).

## [2.7.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.DynamoDB` package**: Track Amazon DynamoDB operations in test diagrams. Includes:
  - `DynamoDbOperationClassifier` — Classifies DynamoDB operations from the `X-Amz-Target` header, extracting table names from JSON request bodies (including batch operations via `RequestItems` keys) and PartiQL statements. Supports 19 distinct operations: CRUD (PutItem, GetItem, UpdateItem, DeleteItem), queries (Query, Scan), batch (BatchWriteItem, BatchGetItem), transactions (TransactWriteItems, TransactGetItems), table management (CreateTable, DeleteTable, DescribeTable, ListTables, UpdateTable), and PartiQL (ExecuteStatement, BatchExecuteStatement, ExecuteTransaction).
  - `DynamoDbTrackingMessageHandler` — `DelegatingHandler` that intercepts all DynamoDB HTTP traffic, reads and reconstructs request bodies for classification, and logs request/response pairs for diagram generation. Implements `ITrackingComponent` with auto-registration.
  - `AmazonDynamoDBConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonDynamoDBConfig` that injects a tracking `HttpClientFactory` into the AWS SDK pipeline. Zero production code changes required.
  - Three verbosity levels: Raw (HTTP method + full URI), Detailed (classified operation name + `dynamodb:///TableName` URI + request/response bodies), Summarised (operation name only, `dynamodb:///TableName` URI, no content/headers, skips unrecognised operations).
  - Default excluded headers: `Authorization`, `x-amz-date`, `x-amz-security-token`, `x-amz-content-sha256`, `User-Agent`, `amz-sdk-invocation-id`, `amz-sdk-request`.

### Fixed
- **Flaky `TrackingComponentRegistryTests` when run in parallel**: Tests now use `Assert.Contains` instead of exact count assertions, preventing false failures when handler auto-registration from parallel test projects pollutes the static registry.

## [2.6.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.S3` package**: Track Amazon S3 operations in test diagrams. Includes:
  - `S3OperationClassifier` — Regex-based classifier that identifies S3 operations from HTTP requests, supporting both **path-style** (`s3.region.amazonaws.com/bucket/key`) and **virtual-hosted-style** (`bucket.s3.region.amazonaws.com/key`) URL formats. Classifies 20 distinct operations including PutObject, GetObject, DeleteObject, CopyObject, multipart uploads, tagging, and bucket management.
  - `S3TrackingMessageHandler` — `DelegatingHandler` that intercepts all S3 HTTP traffic, classifies operations, and logs request/response pairs for diagram generation. Implements `ITrackingComponent` with auto-registration.
  - `AmazonS3ConfigExtensions.WithTestTracking()` — Fluent extension on `AmazonS3Config` that injects a tracking `HttpClientFactory` into the AWS SDK pipeline. Zero production code changes required.
  - Three verbosity levels: Raw (HTTP method + full URI), Detailed (classified operation name + `s3://bucket/key` URI), Summarised (operation name only, `s3://bucket/` URI, no content/headers, skips unrecognised operations).
  - Default excluded headers: `Authorization`, `x-amz-date`, `x-amz-security-token`, `x-amz-content-sha256`, `User-Agent`, `amz-sdk-invocation-id`, `amz-sdk-request`.

## [2.5.2] - 2026-04-21

### Changed
- **Increased PlantUML browser-rendering size limit by 50%**: The hosted `plantuml.js` CDN URL now points to a new patched build (`plantuml-js-plantuml_limit_size_98304`) that raises the maximum diagram pixel dimensions from 65,536px to 98,304px.

## [2.5.1] - 2026-04-21

### Fixed
- **EF Core extension now uses TFM-conditional package references**: `Microsoft.EntityFrameworkCore.Relational` is now referenced as `8.*` for net8.0, `9.*` for net9.0, and `10.*` for net10.0. Previously it unconditionally referenced `9.*`, which forced .NET 8 consumers to pull in EF Core 9 — a major version upgrade just to use the tracking package.

## [2.5.0] - 2026-04-21

### Added
- **HttpContext-based test identity for SQL tracking**: `SqlTrackingInterceptor` now accepts an optional `IHttpContextAccessor` and reads TTD's test identity headers (`test-tracking-current-test-name`, `test-tracking-current-test-id`) from the current `HttpContext` before falling back to `CurrentTestInfoFetcher`. This enables SQL tracking inside server-side HTTP request pipelines in `WebApplicationFactory`-based tests without custom fetcher wrappers.
- **Automatic `IHttpContextAccessor` resolution**: `AddSqlTestTracking(options)` now uses factory-based DI registration that auto-resolves `IHttpContextAccessor` when available. No code changes needed for existing consumers — SQL tracking in server-side pipelines works out of the box.

### Fixed
- **Exception safety in `SqlTrackingInterceptor`**: `CurrentTestInfoFetcher?.Invoke()` is now wrapped in a try-catch. An exception from a diagnostic fetcher delegate (e.g. `ScenarioExecutionContext.CurrentScenario` called outside a LightBDD scenario context) will never propagate into the EF Core command pipeline. If the fetcher throws and no `HttpContext` headers are available, the SQL command executes normally and is simply not logged.

## [2.4.1] - 2026-04-21

### Fixed
- **Corrected PostConfigure guidance for Duende IdentityServer**: The diagnostic report hint and wiki troubleshooting section previously recommended using `PostConfigure<ConfigurationStoreOptions>` to wire the SQL tracking interceptor into Duende's EF Core pipeline. This does not work because Duende registers its store options as a direct singleton (not via `IOptions<T>`) and captures the `ResolveDbContextOptions` delegate by value at service-registration time. The diagnostic hint now correctly advises adding `WithSqlTestTracking(sp)` inside the `ResolveDbContextOptions` implementation that runs at resolution time, and explicitly warns that `PostConfigure` does not work with Duende IdentityServer.
- **Fixed flaky ServiceBus test**: `Constructor_RegistersTrackerWithRegistry` failed intermittently due to xUnit parallel execution racing on the static `TrackingComponentRegistry`. Added `[Collection("TrackingComponentRegistry")]` to all test classes that share this static state.

## [2.4.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.ServiceBus` package**: Track Azure Service Bus messaging operations in test diagrams. Includes:
  - `ServiceBusTracker` — Central logging helper that logs send/receive/management operations as request/response pairs for diagram generation. Implements `ITrackingComponent` with auto-registration.
  - `TrackingServiceBusClient` — Wrapper around `ServiceBusClient` that creates tracked senders and receivers.
  - `TrackingServiceBusSender` — Wrapper around `ServiceBusSender` that intercepts `SendMessageAsync`, `SendMessagesAsync`, `ScheduleMessageAsync`, and `CancelScheduledMessageAsync`.
  - `TrackingServiceBusReceiver` — Wrapper around `ServiceBusReceiver` that intercepts `ReceiveMessageAsync`, `ReceiveMessagesAsync`, `PeekMessageAsync`, `CompleteMessageAsync`, `AbandonMessageAsync`, `DeadLetterMessageAsync`, `DeferMessageAsync`, and `RenewMessageLockAsync`.
  - `ServiceBusOperationClassifier` — Classifies Service Bus method calls into operations (Send, SendBatch, Receive, Complete, Abandon, DeadLetter, Defer, Schedule, etc.) with diagram labels.
  - `ServiceBusServiceCollectionExtensions.AddServiceBusTestTracking()` — DI extension that wraps existing `ServiceBusClient` registrations with tracking.
  - Three verbosity levels: Raw (enum names), Detailed (operation with queue/topic arrows), Summarised (simple operation names, no content).
  - Messaging operations (Send, Receive, Schedule, Peek) use `MetaType.Event` for blue async-messaging rendering in PlantUML diagrams.

## [2.3.2] - 2026-04-21

### Fixed
- **Failure cluster links now navigate correctly**: Clicking a scenario link in the failure clusters section of the HTML report now scrolls to the correct scenario. Previously, anchor IDs were generated from the test runtime ID instead of the scenario display name, causing a mismatch with the target element's ID.

## [2.3.1] - 2026-04-21

### Changed
- **TrackingComponentRegistry no longer throws**: Removed `ValidateAllComponentsWereInvoked()` and `ValidateComponentsWereInvoked<T>()` methods. Unused component detection is now fully passive — warnings appear in console output automatically and in the HTML diagnostic report when `DiagnosticMode=true`. TTD should never cause test failures in its default configuration.

## [2.3.0] - 2026-04-21

### Added
- **`ITrackingComponent` interface**: All tracking handlers and interceptors now implement `ITrackingComponent`, providing `ComponentName`, `WasInvoked`, and `InvocationCount` properties.
- **`TrackingComponentRegistry`**: Central static registry that auto-registers all tracking components on construction.
  - `GetUnusedComponents()` / `GetRegisteredComponents()` — Programmatic inspection of component state.
  - `Clear()` — Reset alongside `RequestResponseLogger.Clear()` in test setup.
- **Unused component warnings in diagnostic report**: `ReportDiagnostics.Analyse()` now warns when registered tracking components were never invoked — a strong indicator of misconfiguration (e.g. EF Core `DbContextOptions<T>` type mismatch). Warnings appear in console output automatically and in the HTML diagnostic report when `DiagnosticMode=true`. This never throws or fails tests.
- **Diagnostic report "Tracking Components" section**: When `DiagnosticMode=true`, the HTML diagnostic report now includes a table of all registered tracking components with their invocation counts and active/unused status, plus troubleshooting hints for common causes.
- **Invocation tracking on all extensions**: `SqlTrackingInterceptor`, `CosmosTrackingMessageHandler`, `BlobTrackingMessageHandler`, `BigQueryTrackingMessageHandler`, `RedisTracker`, and core `TestTrackingMessageHandler` all track invocation counts and auto-register with the registry.

### Documentation
- Added troubleshooting guide to EF Core Relational wiki page covering the `DbContextOptions<TBase>` vs `DbContextOptions<TDerived>` pitfall (Duende IdentityServer, ASP.NET Identity, ABP Framework).
- Added `TrackingComponentRegistry` documentation to Diagnostics and Debugging wiki page.
- Added Invocation Validation sections to all extension wiki pages (CosmosDB, BlobStorage, BigQuery, Redis, EF Core Relational).

## [2.2.1] - 2026-04-21

### Fixed
- **SqlTrackingInterceptor request/response pairing**: `LogCommandExecuting` and `LogCommandExecuted` now share the same `TraceId` and `RequestResponseId` via a `ConcurrentDictionary<DbCommand, (Guid, Guid)>` lookup. Previously each method generated its own IDs, making it impossible for the report generator to pair request and response entries — breaking internal flow popups, component diagram stats, and diagnostic report pairing warnings.

## [2.2.0] - 2026-04-21

### Added
- **New `TestTrackingDiagrams.Extensions.BigQuery` package**: Track Google BigQuery REST API operations in test diagrams. Includes:
  - `BigQueryTrackingMessageHandler` — A `DelegatingHandler` that intercepts all BigQuery REST calls and logs them as request/response pairs for diagram generation.
  - `BigQueryOperationClassifier` — Classifies BigQuery REST API URLs into operations (Query, Insert, Read, List, Create, Delete, Update, Cancel) with resource type extraction (table, dataset, job, model, routine, query, tabledata).
  - `BigQueryClientBuilderExtensions.WithTestTracking()` — Extension method on `BigQueryClientBuilder` for one-line integration.
  - Three verbosity levels: Raw (full HTTP), Detailed (classified labels with content), Summarised (operation names only, no content/headers).
  - Default header filtering for noisy Google API headers (Authorization, x-goog-api-client, etc.).

## [2.1.0] - 2026-04-21

### Added
- **`WithTestInfoFrom()` extension** on `SqlTrackingInterceptorOptions`: Copies `CurrentTestInfoFetcher`, `CurrentStepTypeFetcher`, and `CallingServiceName` from an existing `TestTrackingMessageHandlerOptions` instance. Works with all framework adapters (LightBDD, xUnit3, TUnit, MSTest, BDDfy, ReqNRoll) — no framework-specific subclass needed.
- **`services.AddSqlTestTracking(options)`**: DI extension that registers `SqlTrackingInterceptor` as a singleton in `IServiceCollection`.
- **`builder.WithSqlTestTracking(serviceProvider)` overload**: Resolves the interceptor from DI instead of requiring options to be passed directly. Use with `AddSqlTestTracking()` for cleaner `AddDbContext` callbacks.

## [2.0.0] - 2026-04-21

### Release
- **Official 2.0.0 release** — all beta features stabilized.

## [2.0.175-beta] - 2026-04-21

### Fixed
- **ExpectedTestCount guard was blocking all reports**: The partial-run guard introduced in v2.0.174-beta returned early from `CreateStandardReportsWithDiagrams`, preventing all report generation (TestRunReport, ComponentDiagram, etc.) during partial runs. Now it only disables Specifications (HTML + data) generation — TestRunReport and all other reports still generate normally during filtered/partial test runs.

## [2.0.174-beta] - 2026-04-20

### Changed
- **ExpectedTestCount guard moved to core pipeline**: The partial-run guard that prevents Specifications reports from being overwritten during filtered test runs is now a property on `ReportConfigurationOptions.ExpectedTestCount` and enforced in the core `ReportGenerator.CreateStandardReportsWithDiagrams()`. Previously this was LightBDD-specific (`StandardPipelineFormatter.ExpectedTestCount`). All frameworks (xUnit2/3, NUnit4, TUnit, MSTest, BDDfy, ReqNRoll) can now opt in by setting `options.ExpectedTestCount = () => count`. LightBDD adapters continue to wire this automatically via assembly reflection.

## [2.0.173-beta] - 2026-04-20

### Fixed
- **Per-call activity diagrams showing wrong spans**: `InternalFlowSegmentBuilder.BuildSegments()` now filters spans by the specific request log's `ActivityTraceId` in addition to timestamp windowing. Previously, all spans from all trace IDs within a test were pooled and separated only by timestamps — causing spans from one HTTP call to bleed into another call's popup when timing overlapped or was coarse (Windows ~15.6ms timer resolution). Each call's internal flow popup now correctly shows only spans belonging to that call's W3C trace.
- **Root span excluded from per-call diagrams**: Added a 50ms tolerance before the segment start timestamp to capture the `TestTrackingDiagrams.Request` root span, whose `Activity.Start()` fires before the log's `Timestamp = DateTimeOffset.UtcNow` is recorded. Combined with per-call TraceId filtering, this ensures the tolerance doesn't accidentally include unrelated spans.

## [2.0.172-beta] - 2026-04-20

### Fixed
- **LightBDD specs generation guard**: `StandardPipelineFormatter` now correctly prevents Specifications report generation when fewer tests ran than expected (partial run). The comparison operator was inverted (`>` instead of `<`), allowing partial runs to generate the report.
- **Loading message on sequence diagrams**: Unrendered diagrams inside collapsed features no longer show "Waiting for page load to complete..." after the page has loaded. A `body.plantuml-ready` CSS class now switches the message to "Rendering diagram..." once DOMContentLoaded fires, regardless of IntersectionObserver timing.
- **Note collapse breaking zoom toggle**: Collapsing or expanding notes via the radio buttons re-renders the diagram SVG (destroying the zoom button). The zoom button is now re-added via `requestAnimationFrame` after every note-state re-render.
- **Jump-to-failure scroll position**: The "Next Failure" button now scrolls to the scenario's `<summary>` (title) rather than the `<details>` element, and uses `block: 'start'` so the title is visible at the top of the viewport.
- **Showcase test not skipped**: `ShowcaseReportTests.Showcase_drives_through_report_features_capturing_frames` was running as a regular `[Fact]` on every test run (~60s). Now marked with `Skip` like all other GIF/screenshot generation tests.

## [2.0.171-beta] - 2026-04-20

### Fixed
- Activity diagram popups (internal flow) now correctly set `data-queued` and `data-rendered` attributes, preventing the "Waiting for page load to complete..." loading text from showing indefinitely after the diagram has rendered.
- Zoom toggle button reliability improved: the render callback now triggers `addZoomButton` via `requestAnimationFrame` after SVG insertion, and the IntersectionObserver path also defers to `requestAnimationFrame` to ensure layout is complete before checking diagram dimensions.

## [2.0.170-beta] - 2026-04-20

### Added
- Integration tests verifying specifications reports are blanked when any test fails, are not blanked for skipped/bypassed-only runs, and that the test run report is unaffected by failures.

## [2.0.169-beta] - 2026-04-20

### Changed
- **Breaking:** `ReportConfigurationOptions.FeaturesReportShowStepNumbers` renamed to `TestRunReportShowStepNumbers`.
- **Breaking:** `ComponentDiagramOptions.EmbedInFeaturesReport` renamed to `EmbedInTestRunReport`.
- Renamed all remaining "FeaturesReport" references (comments, test data, test class name) to "TestRunReport" for consistency with the actual report output filename.

## [2.0.168-beta] - 2026-04-20

### Added
- Diagram loading placeholders ("Waiting for page load to complete\u2026" and "Rendering diagram\u2026") now pulse with a gentle fade-in/fade-out animation so they feel alive rather than static.

## [2.0.167-beta] - 2026-04-20

### Changed
- First-state loading placeholder changed from "Waiting for page\u2026" to "Waiting for page load to complete\u2026" for clarity.

## [2.0.166-beta] - 2026-04-20

### Changed
- Diagram loading now shows two distinct states: "Waiting for page load to complete\u2026" while CDN scripts download, then "Rendering diagram\u2026" once the diagram enters the render queue. Previously a single "Loading diagram\u2026" message covered both phases. The `data-queued` attribute is now set when a diagram enters the render queue, while `data-rendered` is set only after the SVG has been fully rendered.

## [2.0.165-beta] - 2026-04-20

### Changed
- PlantUML CDN scripts (`viz-global.js`, `plantuml.js`) now load with the `defer` attribute, allowing the browser to parse and render the HTML report while the scripts download in parallel. Previously these blocking scripts stalled all page rendering. Unrendered diagram containers show a "Loading diagram\u2026" placeholder until the WASM engine is ready.
- Diagram zoom buttons are now added lazily via `IntersectionObserver` instead of eagerly scanning all diagram containers on page load. This eliminates hundreds of forced layout reflows (`getBoundingClientRect`) on large reports. A per-container `MutationObserver` waits for the SVG to render before checking whether the diagram needs a zoom toggle.

## [2.0.164-beta] - 2026-04-20

### Reverted
- Removed search data compression (data-search-z / data-row-search-z) introduced in v2.0.162-beta. The gzip+base64 approach only achieved ~22% reduction on real-world reports (not the projected 80%) and added a 30-second decompression delay on page load for large reports. Search attributes are back to plain text (data-search / data-row-search).

## [2.0.161-beta] - 2026-04-19

### Fixed
- Parameterized group row click no longer hides content when multiple features have parameterized groups — the `pgrp` prefix counter was resetting per feature, causing duplicate HTML element IDs across features. `selectRow()` uses global `document.querySelectorAll`, so clicking a row in one feature’s group would hide/show panels from a different feature’s identically-prefixed group.

## [2.0.160-beta] - 2026-04-19

### Added
- Collapsed diagram notes now show a plus (+) button in the top-right corner that expands the note — mirrors the existing bottom-center ▼ expand button. The minus (−) button remains when notes are expanded or truncated. Clicking either the plus button or the ▼ button returns the note to expanded state and restores the minus button.

## [2.0.159-beta] - 2026-04-19

### Fixed
- Parameterized test groups now correctly display the "Happy Path" badge and CSS class — previously the `happy-path` class was only applied to non-parameterized scenarios, so the "Happy Paths Only" filter button and happy-path styling were broken for parameterized groups

## [2.0.158-beta] - 2026-04-19

### Changed
- **BREAKING**: LightBDD adapter now delegates to the standard `ReportGenerator.CreateStandardReportsWithDiagrams()` pipeline — the same pipeline used by every other framework adapter (xUnit, NUnit, MSTest, TUnit, BDDfy, ReqNRoll). This eliminates a chronic source of feature-parity drift where new options and features had to be wired into both the main pipeline and a parallel LightBDD-specific pipeline.
- Removed `UnifiedReportFormatter`, `UnifiedSpecificationsDataFormatter`, `UnifiedTestRunDataFormatter`, and `PostReportActionsFormatter` — replaced by a single `StandardPipelineFormatter` that calls the shared pipeline once
- `ReportWritersConfigurationExtensions.CreateStandardReportsWithDiagramsInternal` reduced from ~180 lines to ~15 lines
- LightBDD now automatically gets component diagram generation, diagnostics, CI summary/artifacts — features that were previously unavailable in the LightBDD path

## [2.0.157-beta] - 2026-04-19

### Fixed
- LightBDD report generation now respects `GenerateSpecificationsReport`, `GenerateTestRunReport`, `GenerateSpecificationsData`, and `GenerateTestRunReportData` option flags — previously all reports were always generated regardless of these settings
- LightBDD adapter now passes `GroupParameterizedTests`, `MaxParameterColumns`, and `TitleizeParameterNames` options through to HTML report generation — previously these options were silently ignored
- LightBDD test run report title now incorporates `FixedNameForReceivingService` / `ComponentDiagramOptions.Title` via `GetTestRunReportTitle()` — previously always defaulted to "Test Run Report"
- LightBDD schema generation was unreachable dead code (placed after `return` statement in v2.0.156-beta) — moved before the return so it actually executes

### Changed
- `ReportGenerator.GetTestRunReportTitle()` visibility changed from `internal` to `public` so LightBDD adapter can use it

## [2.0.156-beta] - 2026-04-19

### Fixed
- LightBDD adapter now extracts all scenario parameters from `INameInfo.NameFormat`, including parameters substituted inline into the scenario name (e.g. `NewPasscode "{0}"`) — previously only bracket-appended parameters were detected
- LightBDD report formatters now generate reports when running a subset of tests (e.g. single test filtering) — previously the `ExpectedTestCount` guard prevented any output when the count didn't match the full assembly total
- LightBDD adapter now generates `TestRunReport.schema.json` (was missing because the schema writer was not registered in the LightBDD report configuration)

## [2.0.155-beta] - 2026-04-19

### Fixed
- ParameterParser now correctly extracts parameters from multiple separate bracket groups (e.g. `[version: "V1"] [claimName: "Sdes"]` as produced by LightBDD for unmatched inline data parameters)
- ExtractBaseName now strips all trailing bracket groups, not just the last one — fixes parameterized group titles retaining leftover brackets
- "All diagrams identical across test cases" badge no longer displays when there is only one test case in a parameterized group

## [2.0.154-beta] - 2026-04-18

### Fixed
- Three broken documentation links in README (CosmosDB, EF Core Relational, Redis) now point to wiki pages instead of non-existent docs/ files
- .NET targeting statement corrected from ".NET 10.0" to multi-target ".NET 8.0, .NET 9.0, and .NET 10.0"

### Added
- Four missing extension packages added to README Extensions table: Blob Storage, DispatchProxy, MediatR, OpenTelemetry
- Wiki links for the four new extensions added to README Documentation section

## [2.0.153-beta] - 2026-04-18

### Added
- GitHub issue templates (bug report and feature request) with structured forms
- Pull request template with checklist (TDD, tests, docs, changelog, version)
- CodeQL security scanning workflow (runs on push, PR, and weekly schedule)

### Changed
- Added explicit least-privilege `permissions: contents: read` to CI and CI Summary Preview workflows

## [2.0.152-beta] - 2026-04-18

### Changed
- Renamed README title from "Test Tracking Diagrams" to "TestTrackingDiagrams" (PascalCase, matching .NET package naming convention)
- Added TTD icon prefix to README title
- Updated nuget-readme.md title to match

## [2.0.151-beta] - 2026-04-18

### Added
- Default TTD favicon for all HTML reports — reports now show the TTD icon in the browser tab without any configuration
- `DefaultFavicon.DataUri` constant containing the base64-encoded SVG icon
- Favicon added to component diagram reports (previously had no favicon support)

### Changed
- `CustomFaviconBase64` now overrides the default TTD favicon instead of switching between a custom favicon and no favicon

## [2.0.150-beta] - 2026-04-18

### Changed
- Updated NuGet package descriptions across all 15 packages to reflect broader tracking capabilities (HTTP, database, cache, events, and more) instead of just "request-responses"
- Core package description now highlights all supported dependency types (Cosmos DB, SQL via EF Core, Redis, events/messages, arbitrary method calls)

## [2.0.149-beta] - 2026-04-18

### Removed
- Removed incorrect Mermaid references from nuget-readme.md (Mermaid is not currently supported)

## [2.0.148-beta] - 2026-04-18

### Changed
- Updated README.md to reflect all tracking capabilities beyond HTTP — now covers Cosmos DB, EF Core SQL, Redis, TrackingProxy, and events/messages
- Updated nuget-readme.md with the same broader language
- Revised ASCII architecture diagram to show CosmosDB, SQL DB, Redis, and proxy dependencies alongside HTTP and events
- Replaced HTTP-specific wording in How It Works, Use Cases, and Deterministic vs AI-Generated Diagrams sections with inclusive language covering all tracked interaction types
- Step 1 (Intercept) now uses a table summarising all six tracking mechanisms

## [2.0.139-beta] - 2026-04-18

### Added
- `.editorconfig` for consistent code style enforcement
- `.gitattributes` for cross-platform line ending normalisation
- `global.json` to pin .NET SDK version (replaces inline CI hack)
- `CHANGELOG.md` following Keep a Changelog format
- `CONTRIBUTING.md` with development workflow and PR guidelines
- `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1)
- `SECURITY.md` with vulnerability reporting guidance
- NuGet package icon (sequence diagram motif with checkmark)
- XML doc comments on all core public API types: `ReportConfigurationOptions`, `TestTrackingMessageHandlerOptions`, `ComponentDiagramOptions`, `ServiceCollectionExtensions`, `WebApplicationFactoryExtensions`

### Fixed
- Removed copy-pasted `.gitignore` entry referencing unrelated project
- Removed 5 orphaned `Example.Api/` files with unresolved git merge conflict markers
- Removed untracked prototype/PoC files from repository root (`generate-poc.ps1`, `prototype-parameterized-grouping.html`)
- Moved `themes/` directory under `examples/` to match reorganised structure
- Removed internal design documents from tracked `docs/` directory
- Updated copyright year in LICENSE to 2023-2026

### Changed
- CI workflows now use committed `global.json` instead of inline SDK pinning

## [2.0.138-beta] - 2026-04-18

### Fixed
- LightBDD.xUnit3 example: added missing `using TestTrackingDiagrams.LightBDD` for `HappyPathAttribute`, `LightBddTestTrackingMessageHandlerOptions`, and `TrackingDiagramOverride`
- ReqNRoll xUnit2/xUnit3 examples: added `TestTrackingDiagrams.ReqNRoll.Core` to `reqnroll.json` binding assemblies so ReqNRoll discovers `[Binding]` hooks

## [2.0.137-beta] - 2026-04-18

### Changed
- Reorganised repository from flat structure into `src/`, `tests/`, `examples/` directories
- Updated all project references and solution file for new structure

## [2.0.0-beta] - 2026

### Added
- Complete rewrite with multi-framework support (xUnit v2/v3, NUnit 4, MSTest, TUnit)
- BDD framework integrations (BDDfy, LightBDD, ReqNRoll)
- Extension packages for CosmosDB, EF Core Relational, Redis, OpenTelemetry, MediatR, Blob Storage, DispatchProxy
- C4-style component diagram generation
- Interactive HTML reports with search, filtering, and zoom
- PlantUML IKVM package for offline rendering
- CI summary integration (GitHub Actions job summaries)
- Inline SVG rendering option
- Internal flow tracking
- Event annotations
- Custom theme support

## [1.x] - 2023–2025

### Notes
- Initial release series. See [GitHub Releases](https://github.com/lemonlion/TestTrackingDiagrams/releases) for detailed history.

[Unreleased]: https://github.com/lemonlion/TestTrackingDiagrams/compare/v2.0.139-beta...HEAD
[2.0.139-beta]: https://github.com/lemonlion/TestTrackingDiagrams/compare/v2.0.138-beta...v2.0.139-beta
[2.0.138-beta]: https://github.com/lemonlion/TestTrackingDiagrams/compare/v2.0.137-beta...v2.0.138-beta
[2.0.137-beta]: https://github.com/lemonlion/TestTrackingDiagrams/releases/tag/v2.0.137-beta
