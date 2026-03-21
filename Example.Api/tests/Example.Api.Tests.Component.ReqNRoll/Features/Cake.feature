@endpoint:/cake
Feature: Cake
    As a dessert provider
    I want to create cakes from ingredients
    So that customers can enjoy delicious cakes

@happy-path
Scenario: Calling Create Cake Endpoint Successfully
    Given a valid post request for the Cake endpoint
    When the request is sent to the cake post endpoint
    Then the response should be successful

Scenario: Calling Create Cake Endpoint Without Eggs
    Given a valid post request for the Cake endpoint
    But the request body is missing eggs
    When the request is sent to the cake post endpoint
    Then the response http status should be bad request
