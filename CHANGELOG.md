# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
