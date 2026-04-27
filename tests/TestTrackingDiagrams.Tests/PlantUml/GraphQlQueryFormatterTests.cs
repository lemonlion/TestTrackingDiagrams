using TestTrackingDiagrams.PlantUml;

namespace TestTrackingDiagrams.Tests.PlantUml;

public class GraphQlQueryFormatterTests
{
    // ════════════════════════════════════════════════════════════
    //  NULL / EMPTY / WHITESPACE → returned as-is
    // ════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("\n\t", "")]
    public void Null_empty_or_whitespace_returns_empty(string? input, string expected)
    {
        Assert.Equal(expected, GraphQlQueryFormatter.FormatQuery(input));
    }

    // ════════════════════════════════════════════════════════════
    //  ANONYMOUS / SHORTHAND QUERIES
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Anonymous_query_with_single_field()
    {
        var result = GraphQlQueryFormatter.FormatQuery("{ user { name } }");

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
    public void Anonymous_query_with_multiple_fields()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ orderSummaries { orderId customerName itemCount tableNumber status createdAt } }");

        Assert.Equal("""
            {
              orderSummaries {
                orderId
                customerName
                itemCount
                tableNumber
                status
                createdAt
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  NAMED OPERATIONS
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Named_query()
    {
        var result = GraphQlQueryFormatter.FormatQuery("query GetUser { user { name } }");

        Assert.Equal("""
            query GetUser {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void Named_mutation()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "mutation CreateOrder { createOrder { id } }");

        Assert.Equal("""
            mutation CreateOrder {
              createOrder {
                id
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void Named_subscription()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "subscription OnMessage { messageAdded { text } }");

        Assert.Equal("""
            subscription OnMessage {
              messageAdded {
                text
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  VARIABLES IN OPERATION SIGNATURE
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Named_query_with_variable_declaration()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "query GetUser($id: ID!) { user(id: $id) { name } }");

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
    public void Named_mutation_with_complex_variable()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "mutation CreateOrder($input: CreateOrderInput!) { createOrder(input: $input) { orderId status createdAt } }");

        Assert.Equal("""
            mutation CreateOrder($input: CreateOrderInput!) {
              createOrder(input: $input) {
                orderId
                status
                createdAt
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void Named_query_with_multiple_variables()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "query Search($term: String!, $limit: Int) { search(term: $term, limit: $limit) { id title } }");

        Assert.Equal("""
            query Search($term: String!, $limit: Int) {
              search(term: $term, limit: $limit) {
                id
                title
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  INLINE ARGUMENTS (HotChocolate filtering/sorting)
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Arguments_with_nested_braces_stay_inline()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            """{ orderSummaries(where: { status: { eq: "Completed" } }, order: { createdAt: DESC }) { orderId customerName status } }""");

        Assert.Equal(""""
            {
              orderSummaries(where: { status: { eq: "Completed" } }, order: { createdAt: DESC }) {
                orderId
                customerName
                status
              }
            }
            """".ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void Paging_arguments_stay_inline()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ orderSummaries(first: 10, after: \"cursor123\") { orderId status } }");

        Assert.Equal("""
            {
              orderSummaries(first: 10, after: "cursor123") {
                orderId
                status
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  FRAGMENTS
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Fragment_spread_without_space()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ orderSummaries { ...OrderFields } } fragment OrderFields on OrderSummary { orderId customerName }");

        Assert.Equal("""
            {
              orderSummaries {
                ...OrderFields
              }
            }

            fragment OrderFields on OrderSummary {
              orderId
              customerName
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void Inline_fragment()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ orderSummaries { orderId ... on OrderSummary { customerName status } } }");

        Assert.Equal("""
            {
              orderSummaries {
                orderId
                ... on OrderSummary {
                  customerName
                  status
                }
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    [Fact]
    public void Multiple_fragment_spreads()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ user { ...UserBasic ...UserContact } } fragment UserBasic on User { name age } fragment UserContact on User { email phone }");

        Assert.Equal("""
            {
              user {
                ...UserBasic
                ...UserContact
              }
            }

            fragment UserBasic on User {
              name
              age
            }

            fragment UserContact on User {
              email
              phone
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  DEEP NESTING
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Deeply_nested_selection_sets()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ a { b { c { d } } } }");

        Assert.Equal("""
            {
              a {
                b {
                  c {
                    d
                  }
                }
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  DIRECTIVES
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Field_with_include_directive()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "query GetOrders($includeCustomer: Boolean!) { orderSummaries { orderId status customerName @include(if: $includeCustomer) } }");

        Assert.Equal("""
            query GetOrders($includeCustomer: Boolean!) {
              orderSummaries {
                orderId
                status
                customerName @include(if: $includeCustomer)
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  ALIASES
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Field_aliases()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ completed: orderSummaries(where: { status: { eq: \"Completed\" } }) { orderId } pending: orderSummaries(where: { status: { eq: \"Created\" } }) { orderId } }");

        Assert.Equal("""
            {
              completed: orderSummaries(where: { status: { eq: "Completed" } }) {
                orderId
              }
              pending: orderSummaries(where: { status: { eq: "Created" } }) {
                orderId
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  INTROSPECTION
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Introspection_schema_query()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ __schema { queryType { name } mutationType { name } subscriptionType { name } types { name kind } } }");

        Assert.Equal("""
            {
              __schema {
                queryType {
                  name
                }
                mutationType {
                  name
                }
                subscriptionType {
                  name
                }
                types {
                  name
                  kind
                }
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }

    // ════════════════════════════════════════════════════════════
    //  ALREADY FORMATTED (IDEMPOTENT)
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Already_formatted_query_is_idempotent()
    {
        var formatted = """
            query GetUser {
              user {
                name
              }
            }
            """.ReplaceLineEndings("\n").Trim();

        Assert.Equal(formatted, GraphQlQueryFormatter.FormatQuery(formatted));
    }

    [Fact]
    public void Already_formatted_complex_query_is_idempotent()
    {
        var formatted = """
            query GetOrders($status: String!) {
              orderSummaries(where: { status: { eq: $status } }) {
                orderId
                customerName
                status
              }
            }
            """.ReplaceLineEndings("\n").Trim();

        Assert.Equal(formatted, GraphQlQueryFormatter.FormatQuery(formatted));
    }

    // ════════════════════════════════════════════════════════════
    //  REAL-WORLD QUERY PATTERNS (unescaped)
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void BreakfastProvider_order_summaries_query()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ orderSummaries { orderId customerName itemCount tableNumber status createdAt } }");

        var lines = result.Split('\n');
        Assert.Equal("{", lines[0]);
        Assert.Equal("  orderSummaries {", lines[1]);
        Assert.Equal("    orderId", lines[2]);
        Assert.Equal("    customerName", lines[3]);
        Assert.Equal("    itemCount", lines[4]);
        Assert.Equal("    tableNumber", lines[5]);
        Assert.Equal("    status", lines[6]);
        Assert.Equal("    createdAt", lines[7]);
        Assert.Equal("  }", lines[8]);
        Assert.Equal("}", lines[9]);
    }

    [Fact]
    public void Query_with_escaped_string_arguments()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            """{ orderSummaries(where: { customerName: { eq: "Alice \"Bob\" Smith" } }) { orderId } }""");

        Assert.Contains("""customerName: { eq: "Alice \"Bob\" Smith" }""", result);
    }

    // ════════════════════════════════════════════════════════════
    //  COMMAS BETWEEN FIELDS
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Fields_separated_by_commas()
    {
        var result = GraphQlQueryFormatter.FormatQuery(
            "{ user { name, email, age } }");

        Assert.Equal("""
            {
              user {
                name,
                email,
                age
              }
            }
            """.ReplaceLineEndings("\n").Trim(),
            result);
    }
}
