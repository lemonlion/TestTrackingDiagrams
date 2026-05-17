@endpoint:/
Feature: Sample
    As a SERVICE_NAME consumer
    I want to call the root endpoint
    So that I get a successful response

@happy-path
Scenario: Get root endpoint returns success
    Given the service is running
    When a GET request is sent to the root endpoint
    Then the response should be successful
