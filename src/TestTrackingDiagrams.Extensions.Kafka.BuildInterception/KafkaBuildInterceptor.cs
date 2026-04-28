using System.Collections.Concurrent;
using System.Reflection;
using Confluent.Kafka;
using HarmonyLib;
using Microsoft.AspNetCore.Http;

namespace TestTrackingDiagrams.Extensions.Kafka.BuildInterception;

/// <summary>
/// Intercepts <see cref="ConsumerBuilder{TKey,TValue}.Build()"/> and
/// <see cref="ProducerBuilder{TKey,TValue}.Build()"/> at runtime via Harmony,
/// automatically wrapping the result with tracking when enabled.
/// This enables zero-production-code-change Kafka tracking in tests.
/// </summary>
public static class KafkaBuildInterceptor
{
    private const string HarmonyId = "TestTrackingDiagrams.Kafka.BuildInterception";

    private static readonly Harmony HarmonyInstance = new(HarmonyId);
    private static readonly ConcurrentDictionary<Type, bool> PatchedConsumerTypes = new();
    private static readonly ConcurrentDictionary<Type, bool> PatchedProducerTypes = new();

    private static readonly MethodInfo ConsumerPostfixDefinition =
        typeof(KafkaBuildInterceptor).GetMethod(nameof(ConsumerBuildPostfix),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo ProducerPostfixDefinition =
        typeof(KafkaBuildInterceptor).GetMethod(nameof(ProducerBuildPostfix),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Enables consumer tracking and patches <see cref="ConsumerBuilder{TKey,TValue}.Build()"/>
    /// to automatically return a <see cref="TrackingKafkaConsumer{TKey,TValue}"/>.
    /// </summary>
    public static void EnableConsumerTracking<TKey, TValue>(
        KafkaTrackingOptions options,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        KafkaTrackingInterceptor.EnableConsumerTracking<TKey, TValue>(options, httpContextAccessor);
        PatchConsumerBuild<TKey, TValue>();
    }

    /// <summary>
    /// Enables consumer tracking with an options configuration action and patches Build().
    /// </summary>
    public static void EnableConsumerTracking<TKey, TValue>(
        Action<KafkaTrackingOptions>? configure = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        var options = new KafkaTrackingOptions();
        configure?.Invoke(options);
        EnableConsumerTracking<TKey, TValue>(options, httpContextAccessor);
    }

    /// <summary>
    /// Enables producer tracking and patches <see cref="ProducerBuilder{TKey,TValue}.Build()"/>
    /// to automatically return a <see cref="TrackingKafkaProducer{TKey,TValue}"/>.
    /// </summary>
    public static void EnableProducerTracking<TKey, TValue>(
        KafkaTrackingOptions options,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        KafkaTrackingInterceptor.EnableProducerTracking<TKey, TValue>(options, httpContextAccessor);
        PatchProducerBuild<TKey, TValue>();
    }

    /// <summary>
    /// Enables producer tracking with an options configuration action and patches Build().
    /// </summary>
    public static void EnableProducerTracking<TKey, TValue>(
        Action<KafkaTrackingOptions>? configure = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        var options = new KafkaTrackingOptions();
        configure?.Invoke(options);
        EnableProducerTracking<TKey, TValue>(options, httpContextAccessor);
    }

    /// <summary>
    /// Enables both consumer and producer tracking and patches both Build() methods.
    /// </summary>
    public static void EnableTracking<TKey, TValue>(
        KafkaTrackingOptions options,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        EnableConsumerTracking<TKey, TValue>(options, httpContextAccessor);
        EnableProducerTracking<TKey, TValue>(options, httpContextAccessor);
    }

    /// <summary>
    /// Enables both consumer and producer tracking with an options configuration action.
    /// </summary>
    public static void EnableTracking<TKey, TValue>(
        Action<KafkaTrackingOptions>? configure = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        var options = new KafkaTrackingOptions();
        configure?.Invoke(options);
        EnableTracking<TKey, TValue>(options, httpContextAccessor);
    }

    /// <summary>Disables consumer tracking. The Harmony patch remains but becomes a no-op.</summary>
    public static void DisableConsumerTracking<TKey, TValue>()
    {
        KafkaTrackingInterceptor.DisableConsumerTracking<TKey, TValue>();
    }

    /// <summary>Disables producer tracking. The Harmony patch remains but becomes a no-op.</summary>
    public static void DisableProducerTracking<TKey, TValue>()
    {
        KafkaTrackingInterceptor.DisableProducerTracking<TKey, TValue>();
    }

    /// <summary>Clears all tracking state and removes all Build() patches.</summary>
    public static void Reset()
    {
        KafkaTrackingInterceptor.Reset();
        HarmonyInstance.UnpatchAll(HarmonyId);
        PatchedConsumerTypes.Clear();
        PatchedProducerTypes.Clear();
    }

    private static void PatchConsumerBuild<TKey, TValue>()
    {
        var builderType = typeof(ConsumerBuilder<TKey, TValue>);
        if (!PatchedConsumerTypes.TryAdd(builderType, true))
            return;

        var original = builderType.GetMethod(nameof(ConsumerBuilder<TKey, TValue>.Build))!;
        var postfix = ConsumerPostfixDefinition.MakeGenericMethod(typeof(TKey), typeof(TValue));
        HarmonyInstance.Patch(original, postfix: new HarmonyMethod(postfix));
    }

    private static void PatchProducerBuild<TKey, TValue>()
    {
        var builderType = typeof(ProducerBuilder<TKey, TValue>);
        if (!PatchedProducerTypes.TryAdd(builderType, true))
            return;

        var original = builderType.GetMethod(nameof(ProducerBuilder<TKey, TValue>.Build))!;
        var postfix = ProducerPostfixDefinition.MakeGenericMethod(typeof(TKey), typeof(TValue));
        HarmonyInstance.Patch(original, postfix: new HarmonyMethod(postfix));
    }

    private static void ConsumerBuildPostfix<TKey, TValue>(ref IConsumer<TKey, TValue> __result)
    {
        __result = KafkaTrackingInterceptor.WrapConsumer(__result);
    }

    private static void ProducerBuildPostfix<TKey, TValue>(ref IProducer<TKey, TValue> __result)
    {
        __result = KafkaTrackingInterceptor.WrapProducer(__result);
    }
}
