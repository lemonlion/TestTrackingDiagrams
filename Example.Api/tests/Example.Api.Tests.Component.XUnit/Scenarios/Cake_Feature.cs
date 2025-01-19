using TestTrackingDiagrams.XUnit;

namespace Example.Api.Tests.Component.XUnit.Scenarios;

[Endpoint("/cake")]
public partial class Cake_Feature
{
    [Fact]
    [HappyPath]
    public async Task Calling_Create_Cake_Endpoint_Returns_Cake()
    {
        await Given_a_valid_post_request_for_the_Cake_endpoint();
        await When_the_request_is_sent_to_the_cake_post_endpoint();
        await Then_the_response_should_be_successful();
    }

    [Fact]
    public async Task Calling_Create_Cake_Endpoint_Without_Eggs_Returns_Bad_Request()
    {
        await Given_a_valid_post_request_for_the_Cake_endpoint();
        await But_the_request_body_is_missing_eggs();
        await When_the_request_is_sent_to_the_cake_post_endpoint();
        await Then_the_response_http_status_should_be_bad_request();
    }
}