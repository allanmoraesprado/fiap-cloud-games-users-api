using Prometheus;

namespace UsersApi.Observability;

// Custom Prometheus counters (Phase 3). Exposed on /metrics next to the default HTTP metrics.
// Labels are low-cardinality only: never user ids, e-mails or other PII.
public static class FcgMetrics
{
    public static readonly Counter Registrations = Prometheus.Metrics.CreateCounter(
        "fcg_users_registrations_total",
        "Users registered successfully.");

    public static readonly Counter LoginAttempts = Prometheus.Metrics.CreateCounter(
        "fcg_users_login_attempts_total",
        "Login attempts by result (success|failure).",
        new CounterConfiguration { LabelNames = new[] { "result" } });

    public static readonly Counter EventsPublished = Prometheus.Metrics.CreateCounter(
        "fcg_events_published_total",
        "Kafka events published by topic and result (success|failure).",
        new CounterConfiguration { LabelNames = new[] { "topic", "result" } });

    static FcgMetrics()
    {
        // Pre-create the known label sets so they are exported as 0 before the first event.
        LoginAttempts.WithLabels("success");
        LoginAttempts.WithLabels("failure");
    }

    // Touching the type runs the static constructor; called once at startup.
    public static void EnsureInitialized() { }
}
