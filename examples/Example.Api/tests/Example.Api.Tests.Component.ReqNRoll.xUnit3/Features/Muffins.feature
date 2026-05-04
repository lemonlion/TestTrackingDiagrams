@endpoint:/muffins
Feature: Muffins Creation
    /muffins - Creating apple cinnamon muffins with baking profiles and toppings

    @happy-path
    Scenario: A valid apple cinnamon muffin request should return a fresh batch
        Given a valid apple cinnamon muffin recipe with all ingredients
        When the muffins are prepared
        Then the muffin response should contain a valid batch with all ingredients
        And the cow service should have received a milk request for the muffins

    Scenario Outline: Different muffin recipes should produce the expected batch
        Given a muffin recipe "<RecipeName>" with the following ingredients:
            | Flour   | Apples         | Cinnamon       |
            | <Flour> | <AppleVariety> | <CinnamonType> |
        And with baking at <Temperature> degrees for <Duration> minutes in a "<PanType>" pan
        And the following muffin toppings:
            | Name       |
            | <Topping1> |
            | <Topping2> |
        When the muffins are prepared
        Then the muffin batch should have 5 ingredients
        And the muffin response should include 2 toppings
        And the muffin response should include baking information

        Examples:
            | RecipeName       | Flour       | AppleVariety | CinnamonType | Temperature | Duration | PanType   | Topping1          | Topping2           |
            | Classic          | Plain Flour | Granny Smith | Ceylon       | 180         | 25       | Standard  | Streusel          | Icing Glaze        |
            | Rustic Wholesome | Whole Wheat | Honeycrisp   | Cassia       | 175         | 30       | Cast Iron | Brown Sugar Crumb | Maple Drizzle      |
            | Spiced Deluxe    | Almond      | Pink Lady    | Saigon       | 190         | 20       | Silicone  | Cinnamon Sugar    | Cream Cheese Swirl |
