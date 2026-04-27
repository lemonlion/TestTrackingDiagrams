using TestTrackingDiagrams.PlantUml;

namespace TestTrackingDiagrams.Tests.PlantUml;

public class GraphQlBodyFormatterTests
{
    // ════════════════════════════════════════════════════════════
    //  NON-GRAPHQL → null (caller falls through to JSON formatter)
    // ════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Null_or_empty_returns_null(string? content)
    {
        Assert.Null(GraphQlBodyFormatter.TryFormat(content, GraphQlBodyFormat.FormattedWithMetadata));
    }

    [Fact]
    public void Non_json_returns_null()
    {
        Assert.Null(GraphQlBodyFormatter.TryFormat("name=Alice&age=30", GraphQlBodyFormat.FormattedWithMetadata));
    }

    [Fact]
    public void Json_without_query_key_returns_null()
    {
        Assert.Null(GraphQlBodyFormatter.TryFormat("""{"name":"Alice"}""", GraphQlBodyFormat.FormattedWithMetadata));
    }

    [Fact]
    public void Json_mode_always_returns_null()
    {
        // Json mode means "don't touch it, let the normal JSON formatter handle it"
        var body = """{"query":"{ user { name } }"}""";
        Assert.Null(GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.Json));
    }

    // ════════════════════════════════════════════════════════════
    //  FormattedQueryOnly — query only, no headers, no metadata
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void FormattedQueryOnly_simple_anonymous_query()
    {
        var body = """{"query":"{ user { name } }"}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedQueryOnly);

        Assert.Equal("""
            {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void FormattedQueryOnly_named_query_with_variables_in_body()
    {
        var body = """{"query":"query GetUser($id: ID!) { user(id: $id) { name } }","variables":{"id":"123"}}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedQueryOnly);

        // FormattedQueryOnly shows ONLY the formatted query — no variables section
        Assert.Equal("""
            query GetUser($id: ID!) {
              user(id: $id) {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void FormattedQueryOnly_strips_json_escapes_from_query()
    {
        // Query value contains \n (JSON-escaped newlines) — should be unescaped before formatting
        var body = """{"query":"query GetUser {\n  user {\n    name\n  }\n}"}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedQueryOnly);

        Assert.Equal("""
            query GetUser {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  Formatted — query + headers kept (returned content; headers added by pipeline)
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Formatted_simple_query()
    {
        var body = """{"query":"{ user { name } }"}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.Formatted);

        // Same output as FormattedQueryOnly for body content — headers are added by PlantUmlCreator
        Assert.Equal("""
            {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void Formatted_ignores_variables_in_output()
    {
        var body = """{"query":"query GetUser($id: ID!) { user(id: $id) { name } }","variables":{"id":"123"}}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.Formatted);

        // Formatted shows only query, no variables section
        Assert.Equal("""
            query GetUser($id: ID!) {
              user(id: $id) {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  FormattedWithMetadata — query + variables/extensions
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void FormattedWithMetadata_query_only_no_variables()
    {
        var body = """{"query":"{ user { name } }"}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        // No variables → just formatted query
        Assert.Equal("""
            {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void FormattedWithMetadata_with_simple_variables()
    {
        var body = """{"query":"query GetUser($id: ID!) { user(id: $id) { name } }","variables":{"id":"123"}}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        var expected = """
            query GetUser($id: ID!) {
              user(id: $id) {
                name
              }
            }

            variables:
            {
              "id": "123"
            }
            """.ReplaceLineEndings("\n").Trim();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormattedWithMetadata_with_complex_nested_variables()
    {
        var body = """{"query":"mutation CreateOrder($input: CreateOrderInput!) { createOrder(input: $input) { orderId status } }","variables":{"input":{"customerName":"Alice","items":3,"tableNumber":5}}}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        var expected = """
            mutation CreateOrder($input: CreateOrderInput!) {
              createOrder(input: $input) {
                orderId
                status
              }
            }

            variables:
            {
              "input": {
                "customerName": "Alice",
                "items": 3,
                "tableNumber": 5
              }
            }
            """.ReplaceLineEndings("\n").Trim();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormattedWithMetadata_with_extensions()
    {
        var body = """{"query":"{ user { name } }","extensions":{"persistedQuery":{"sha256Hash":"abc123","version":1}}}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        var expected = """
            {
              user {
                name
              }
            }

            extensions:
            {
              "persistedQuery": {
                "sha256Hash": "abc123",
                "version": 1
              }
            }
            """.ReplaceLineEndings("\n").Trim();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormattedWithMetadata_with_both_variables_and_extensions()
    {
        var body = """{"query":"query GetUser($id: ID!) { user(id: $id) { name } }","variables":{"id":"123"},"extensions":{"tracing":true}}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        var expected = """
            query GetUser($id: ID!) {
              user(id: $id) {
                name
              }
            }

            variables:
            {
              "id": "123"
            }

            extensions:
            {
              "tracing": true
            }
            """.ReplaceLineEndings("\n").Trim();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormattedWithMetadata_null_variables_are_omitted()
    {
        var body = """{"query":"{ user { name } }","variables":null}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        Assert.Equal("""
            {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void FormattedWithMetadata_empty_variables_object_is_omitted()
    {
        var body = """{"query":"{ user { name } }","variables":{}}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        Assert.Equal("""
            {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void FormattedWithMetadata_operationName_is_not_shown()
    {
        var body = """{"query":"query GetUser { user { name } }","operationName":"GetUser","variables":{"id":"123"}}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        // operationName is informational only — not shown in formatted output
        Assert.Contains("query GetUser {", result);
        Assert.DoesNotContain("operationName", result);
        Assert.Contains("variables:", result);
    }

    // ════════════════════════════════════════════════════════════
    //  JSON-escaped query values (real-world pattern)
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Handles_json_escaped_newlines_in_query()
    {
        var body = """{"query":"{\n  user {\n    name\n  }\n}"}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        Assert.Equal("""
            {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void Handles_json_escaped_tabs_in_query()
    {
        var body = """{"query":"{\t\tuser { name } }"}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedQueryOnly);

        Assert.Equal("""
            {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  Pretty-printed JSON bodies (real-world pattern)
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Pretty_printed_json_body_with_variables()
    {
        var body = """
{
  "operationName": "GetOrders",
  "variables": {
    "status": "Completed"
  },
  "query": "query GetOrders($status: String!) { orderSummaries(where: { status: { eq: $status } }) { orderId customerName status } }"
}
""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedWithMetadata);

        Assert.Contains("query GetOrders($status: String!) {", result);
        Assert.Contains("variables:", result);
        Assert.Contains("\"status\": \"Completed\"", result);
        Assert.DoesNotContain("operationName", result);
    }

    // ════════════════════════════════════════════════════════════
    //  HotChocolate filtering/sorting pattern
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void HotChocolate_query_with_where_and_order()
    {
        var body = """{"query":"{ orderSummaries(where: { status: { eq: \"Completed\" } }, order: { createdAt: DESC }) { orderId customerName status createdAt } }"}""";
        var result = GraphQlBodyFormatter.TryFormat(body, GraphQlBodyFormat.FormattedQueryOnly);

        Assert.Contains("orderSummaries(where: { status: { eq: \"Completed\" } }, order: { createdAt: DESC })", result);
        Assert.Contains("  orderId", result);
    }

    // ════════════════════════════════════════════════════════════
    //  Non-GraphQL JSON with "query" key at wrong level → null
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Nested_query_key_returns_null()
    {
        Assert.Null(GraphQlBodyFormatter.TryFormat(
            """{"data":{"query":"query GetUser { user { name } }"}}""",
            GraphQlBodyFormat.FormattedWithMetadata));
    }

    [Fact]
    public void Non_graphql_query_value_returns_null()
    {
        Assert.Null(GraphQlBodyFormatter.TryFormat(
            """{"query":"SELECT * FROM users"}""",
            GraphQlBodyFormat.FormattedWithMetadata));
    }
}
