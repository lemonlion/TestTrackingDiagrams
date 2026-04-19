using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;
using static TestTrackingDiagrams.DefaultDiagramsFetcher;

namespace TestTrackingDiagrams.Tests.Selenium;

/// <summary>
/// Generates animated GIFs and screenshots for the "What's New in 2.0" wiki page.
/// Run:  dotnet test TestTrackingDiagrams.Tests.Selenium --filter "WikiGifTests"
/// Output goes to bin/SeleniumOutput/wiki-gifs/
///
/// Requirements: Chrome, ImageMagick (magick on PATH)
/// </summary>
public class WikiGifTests : IDisposable
{
    private readonly ChromeDriver _driver;
    private readonly string _tempDir;
    private readonly string _wikiGifDir;
    private int _frameNumber;
    private string _currentFrameDir = "";

    private static readonly string OutputDir = Path.Combine(
        Path.GetDirectoryName(typeof(WikiGifTests).Assembly.Location)!,
        "SeleniumOutput");

    public WikiGifTests()
    {
        _driver = ChromeDriverFactory.Create(1280, 900);
        _tempDir = Path.Combine(Path.GetTempPath(), "ttd-wiki-" + Guid.NewGuid().ToString("N")[..8]);
        _wikiGifDir = Path.Combine(OutputDir, "wiki-gifs");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_wikiGifDir);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { /* best effort */ }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FRAME CAPTURE & GIF STITCHING
    // ═══════════════════════════════════════════════════════════════════

    private string BeginFrameCapture(string name)
    {
        _frameNumber = 0;
        _currentFrameDir = Path.Combine(_wikiGifDir, $"_frames_{name}");
        if (Directory.Exists(_currentFrameDir))
            foreach (var f in Directory.GetFiles(_currentFrameDir, "*.png")) File.Delete(f);
        Directory.CreateDirectory(_currentFrameDir);
        return _currentFrameDir;
    }

    private void CaptureFrame()
    {
        _frameNumber++;
        var screenshot = _driver.GetScreenshot();
        screenshot.SaveAsFile(Path.Combine(_currentFrameDir, $"{_frameNumber:D4}.png"));
    }

    /// <summary>Capture N frames with ~33ms delay (30fps).</summary>
    private void CaptureFrames(int count, int delayMs = 33)
    {
        for (var i = 0; i < count; i++)
        {
            CaptureFrame();
            Pause(delayMs);
        }
    }

    /// <summary>Hold current view for a duration at 30fps.</summary>
    private void Hold(double seconds) => CaptureFrames((int)(seconds * 30));

    /// <summary>Stitch frames into a GIF using ImageMagick.</summary>
    private void StitchGif(string outputName)
    {
        var outputPath = Path.Combine(_wikiGifDir, outputName);
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "magick",
            // -delay 3 = 3/100s per frame = 33ms ≈ 30fps
            Arguments = $"-delay 3 -loop 0 \"{_currentFrameDir}\\*.png\" \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        process.WaitForExit(120_000);
        Assert.True(File.Exists(outputPath), $"GIF not created: {outputPath}");
    }

    private void SaveScreenshot(string name)
    {
        var screenshot = _driver.GetScreenshot();
        screenshot.SaveAsFile(Path.Combine(_wikiGifDir, name));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SELENIUM HELPERS (adapted from ShowcaseReportTests)
    // ═══════════════════════════════════════════════════════════════════

    private static void Pause(int ms) => Thread.Sleep(ms);

    private IWebElement WaitFor(By by, int timeoutSeconds = 10)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private void WaitForDiagramSvg(int timeoutSeconds = 30)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        wait.Until(d =>
        {
            try
            {
                var svg = d.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                return svg.Displayed ? svg : null;
            }
            catch (NoSuchElementException) { return null; }
        });
    }

    private void TypeSlowly(IWebElement element, string text, int charDelayMs = 60)
    {
        foreach (var ch in text)
        {
            element.SendKeys(ch.ToString());
            CaptureFrame(); // capture each keystroke
            Pause(charDelayMs);
        }
    }

    private void InjectFakeCursor()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            if (document.getElementById('_fakeCursor')) return;
            var c = document.createElement('div');
            c.id = '_fakeCursor';
            c.innerHTML = `<svg width='24' height='24' viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>
                <path d='M5 3 L5 20 L10 15 L16 21 L19 18 L13 12 L19 12 Z'
                      fill='white' stroke='black' stroke-width='1.5' stroke-linejoin='round'/>
            </svg>`;
            c.style.cssText = 'position:fixed;top:-50px;left:-50px;z-index:99999;pointer-events:none;filter:drop-shadow(1px 2px 2px rgba(0,0,0,0.3));transition:none;';
            document.body.appendChild(c);
        ");
    }

    private void MoveCursorTo(IWebElement element)
    {
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(@"
            var el = arguments[0];
            var c = document.getElementById('_fakeCursor');
            if (!c) return;
            var rect = el.getBoundingClientRect();
            var tx = rect.left + Math.min(rect.width * 0.3, 60);
            var ty = rect.top + rect.height / 2;
            var cx = parseFloat(c.style.left) || -50;
            var cy = parseFloat(c.style.top) || -50;
            var dist = Math.sqrt((tx-cx)*(tx-cx) + (ty-cy)*(ty-cy));
            var duration = Math.min(500, Math.max(150, dist * 0.8));
            var start = performance.now();
            function ease(t) { return t < 0.5 ? 2*t*t : 1 - Math.pow(-2*t+2, 2)/2; }
            function step(now) {
                var t = Math.min((now - start) / duration, 1);
                var p = ease(t);
                c.style.left = (cx + (tx - cx) * p) + 'px';
                c.style.top = (cy + (ty - cy) * p) + 'px';
                if (t < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        ", element);
        // Capture frames during cursor movement
        CaptureFrames(10, 33);
    }

    private void MoveCursorToCoords(int x, int y)
    {
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript($@"
            var c = document.getElementById('_fakeCursor');
            if (!c) return;
            var cx = parseFloat(c.style.left) || -50;
            var cy = parseFloat(c.style.top) || -50;
            var tx = {x}; var ty = {y};
            var dist = Math.sqrt((tx-cx)*(tx-cx) + (ty-cy)*(ty-cy));
            var duration = Math.min(500, Math.max(150, dist * 0.8));
            var start = performance.now();
            function ease(t) {{ return t < 0.5 ? 2*t*t : 1 - Math.pow(-2*t+2, 2)/2; }}
            function step(now) {{
                var t = Math.min((now - start) / duration, 1);
                var p = ease(t);
                c.style.left = (cx + (tx - cx) * p) + 'px';
                c.style.top = (cy + (ty - cy) * p) + 'px';
                if (t < 1) requestAnimationFrame(step);
            }}
            requestAnimationFrame(step);
        ");
        CaptureFrames(10, 33);
    }

    private void HideCursor()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "var c = document.getElementById('_fakeCursor'); if (c) c.style.display = 'none';");
    }

    private void ShowCursor()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "var c = document.getElementById('_fakeCursor'); if (c) c.style.display = '';");
    }

    private void JsClick(IWebElement element)
    {
        MoveCursorTo(element);
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
    }

    private void ScrollTo(IWebElement element)
    {
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(@"
            var el = arguments[0];
            var rect = el.getBoundingClientRect();
            var targetY = window.scrollY + rect.top - (window.innerHeight / 2) + (rect.height / 2);
            var startY = window.scrollY;
            var distance = targetY - startY;
            var duration = Math.min(1200, Math.max(400, Math.abs(distance) * 0.6));
            var startTime = performance.now();
            function ease(t) { return t < 0.5 ? 4*t*t*t : 1 - Math.pow(-2*t+2, 3)/2; }
            function step(now) {
                var elapsed = now - startTime;
                var t = Math.min(elapsed / duration, 1);
                window.scrollTo(0, startY + distance * ease(t));
                if (t < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        ", element);
        // Capture during scroll
        CaptureFrames(20, 33);
        WaitForScrollIdle();
    }

    private void ScrollToTop()
    {
        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(@"
            var startY = window.scrollY;
            var duration = Math.min(1200, Math.max(400, startY * 0.6));
            var startTime = performance.now();
            function ease(t) { return t < 0.5 ? 4*t*t*t : 1 - Math.pow(-2*t+2, 3)/2; }
            function step(now) {
                var elapsed = now - startTime;
                var t = Math.min(elapsed / duration, 1);
                window.scrollTo(0, startY * (1 - ease(t)));
                if (t < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        ");
        CaptureFrames(20, 33);
        WaitForScrollIdle();
    }

    private void WaitForScrollIdle(int maxMs = 3000)
    {
        var js = (IJavaScriptExecutor)_driver;
        var lastY = Convert.ToDouble(js.ExecuteScript("return window.scrollY;"));
        var stableCount = 0;
        var elapsed = 0;
        while (elapsed < maxMs)
        {
            Pause(25);
            elapsed += 25;
            var y = Convert.ToDouble(js.ExecuteScript("return window.scrollY;"));
            if (Math.Abs(y - lastY) < 0.5) { stableCount++; if (stableCount >= 6) return; }
            else stableCount = 0;
            lastY = y;
        }
    }

    private void OpenUrl(string url)
    {
        _driver.Navigate().GoToUrl(url);
        Pause(500);
    }

    private void OpenFile(string path)
    {
        OpenUrl(new Uri(Path.GetFullPath(path)).AbsoluteUri);
    }

    private void ExpandAll()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.querySelectorAll('details').forEach(d => d.open = true);");
        Pause(300);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  RICH TEST DATA GENERATION (50+ scenarios across 8 features)
    // ═══════════════════════════════════════════════════════════════════

    private static readonly string OrderDiagramSource = """
        @startuml
        !pragma teoz true
        skinparam wrapWidth 800
        autonumber 1

        actor "Test" as test
        participant "Orders API" as orders
        participant "Stock Service" as stock
        database "Cosmos DB" as cosmos
        queue "EventGrid" as events

        test -> orders : POST /api/orders
        note left
        Content-Type: application/json
        {
          "productId": "SKU-42",
          "quantity": 3,
          "customerId": "cust-1234"
        }
        end note

        orders -> stock : PUT /stock/reserve
        note left
        {"sku":"SKU-42","qty":3}
        end note
        stock --> orders : 200 OK

        orders -> cosmos : Create Item
        note left
        Container: orders
        {"id":"ord-7f3a","status":"created"}
        end note
        cosmos --> orders : 201

        orders -> events : OrderCreated
        note left #LightBlue
        Topic: order-events
        {"orderId":"ord-7f3a","type":"OrderCreated"}
        end note

        orders --> test : 201 Created
        note left
        {
          "orderId": "ord-7f3a",
          **"status": "Confirmed"**,
          **"totalAmount": 250.50**,
          "currency": "GBP"
        }
        end note
        @enduml
        """;

    private static readonly string PaymentDiagramSource = """
        @startuml
        !pragma teoz true
        skinparam wrapWidth 800
        autonumber 1

        actor "Test" as test
        participant "Payment API" as pay
        participant "Stripe Gateway" as stripe
        database "SQL Server" as sql

        test -> pay : POST /api/payments
        note left
        {"orderId":"ord-7f3a","amount":250.50,"currency":"GBP"}
        end note

        pay -> stripe : POST /v1/charges
        note left
        {"amount":25050,"currency":"gbp"}
        end note
        stripe --> pay : 200 OK

        pay -> sql : INSERT INTO Payments
        sql --> pay : OK

        pay --> test : 200 OK
        note left
        {"paymentId":"pay-001","status":"completed"}
        end note
        @enduml
        """;

    private static readonly string FailureDiagramSource = """
        @startuml
        !pragma teoz true
        skinparam wrapWidth 800
        autonumber 1

        actor "Test" as test
        participant "Orders API" as orders
        participant "Stock Service" as stock

        test -> orders : POST /api/orders
        note left
        {"productId":"SKU-99","quantity":1000}
        end note

        orders -> stock : PUT /stock/reserve
        stock --> orders : 409 Conflict
        note right #Pink
        {"error":"Insufficient stock","available":12}
        end note

        orders --> test : 500 Internal Server Error
        note left #Pink
        {"error":"Internal Server Error"}
        end note
        @enduml
        """;

    private static readonly string MediatRDiagramSource = """
        @startuml
        !pragma teoz true
        skinparam wrapWidth 800
        autonumber 1

        actor "Test" as test
        participant "Orders API" as api
        participant "Application" as app #LightBlue
        database "Cosmos DB" as cosmos
        queue "EventGrid" as events

        test -> api : POST /api/orders
        note left
        Content-Type: application/json
        {
          "productId": "SKU-42",
          "quantity": 3
        }
        end note

        api -> app : **Send: CreateOrderCommand**
        note left #LightYellow
        {
          "productId": "SKU-42",
          "quantity": 3,
          "customerId": "cust-1234"
        }
        end note

        app -> cosmos : Create Item
        note left
        Container: orders
        {"id":"ord-7f3a","status":"created"}
        end note
        cosmos --> app : 201

        app -> events : **Publish: OrderCreatedEvent**
        note left #LightBlue
        {"orderId":"ord-7f3a","type":"OrderCreated"}
        end note

        app --> api : OrderResult
        note right #LightYellow
        {
          "orderId": "ord-7f3a",
          "status": "Confirmed"
        }
        end note

        api --> test : 201 Created
        note left
        {
          "orderId": "ord-7f3a",
          "status": "Confirmed",
          "totalAmount": 250.50
        }
        end note
        @enduml
        """;

    private static readonly string FocusDiagramSource = """
        @startuml
        !pragma teoz true
        skinparam wrapWidth 800
        autonumber 1

        actor "Test" as test
        participant "Orders API" as orders
        participant "Stock Service" as stock
        database "Cosmos DB" as cosmos

        test -> orders : POST /api/orders
        note left
        Content-Type: application/json
        {
          "productId": "SKU-42",
          "quantity": 3,
          "customerId": "cust-1234",
          "shippingAddress": "123 High St",
          "billingAddress": "123 High St",
          "paymentMethod": "card_visa_4242",
          "couponCode": null,
          "giftWrap": false,
          "deliveryNotes": "Leave at door"
        }
        end note

        orders -> stock : PUT /stock/reserve
        stock --> orders : 200 OK

        orders -> cosmos : Create Item
        cosmos --> orders : 201

        orders --> test : 201 Created
        note left
        {
          "orderId": "ord-7f3a",
          <color:blue><b>"status": "Confirmed"</b></color>,
          <color:blue><b>"totalAmount": 250.50</b></color>,
          <color:gray><size:10>"currency": "GBP"</size></color>,
          <color:gray><size:10>"createdAt": "2026-04-17T19:35:06Z"</size></color>,
          <color:gray><size:10>"updatedAt": "2026-04-17T19:35:06Z"</size></color>,
          <color:gray><size:10>"customerId": "cust-1234"</size></color>,
          <color:gray><size:10>"shippingAddress": "123 High St"</size></color>,
          <color:gray><size:10>"paymentRef": "pay-001"</size></color>,
          <color:gray><size:10>"trackingNumber": null</size></color>
        }
        end note
        @enduml
        """;

    private (Feature[] Features, DiagramAsCode[] Diagrams) CreateRichShowcaseData()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Order Management",
                Description = "End-to-end order lifecycle from creation to fulfilment",
                Scenarios = [
                    Sc("order-1", "Creating an order reserves stock and publishes event", true, ExecutionResult.Passed, 342, ["Smoke","Orders"],
                        Steps("Given", "the Orders API is running", "And", "Stock Service has 50 units of SKU-42",
                              "When", "I create an order for 3 units of SKU-42",
                              "Then", "the order is confirmed with status Created",
                              "And", "an OrderCreated event is published")),
                    Sc("order-2", "Insufficient stock returns 409 Conflict", false, ExecutionResult.Failed, 1204, ["Orders","ErrorHandling"],
                        Steps("Given", "the Orders API is running", "And", "Stock Service has 12 units of SKU-99",
                              "When", "I create an order for 1000 units of SKU-99", "Then", "a 409 Conflict is returned"),
                        "Expected status code 409, but got 500.\nResponse body: {\"error\":\"Internal Server Error\"}",
                        "   at FluentAssertions.Execution.LateBoundTestFramework.Throw(String message)\n   at OrderTests.Insufficient_stock_returns_409() in /src/Tests/OrderTests.cs:line 87"),
                    Sc("order-3", "Listing orders returns paginated results", true, ExecutionResult.Passed, 189, ["Smoke","Orders"]),
                    Sc("order-4", "Updating order status sends notification", true, ExecutionResult.Passed, 267, ["Orders"]),
                    Sc("order-5", "Cancelling order within grace period succeeds", true, ExecutionResult.Passed, 198, ["Orders"]),
                    Sc("order-6", "Cancelling order after cut-off returns 409", false, ExecutionResult.Passed, 156, ["Orders","ErrorHandling"]),
                    // Parameterized tests
                    ScParam("order-p1", "Order validation", "Amount: 100, Currency: GBP, Region: EU", ExecutionResult.Passed, 34, ["Orders","Validation"], new(){["Amount"]="100.00",["Currency"]="GBP",["Region"]="EU"}),
                    ScParam("order-p2", "Order validation", "Amount: 250.50, Currency: USD, Region: US", ExecutionResult.Passed, 28, ["Orders","Validation"], new(){["Amount"]="250.50",["Currency"]="USD",["Region"]="US"}),
                    ScParam("order-p3", "Order validation", "Amount: 999.99, Currency: EUR, Region: APAC", ExecutionResult.Failed, 1102, ["Orders","Validation"], new(){["Amount"]="999.99",["Currency"]="EUR",["Region"]="APAC"}, "Expected 200 OK but got 502 Bad Gateway"),
                    ScParam("order-p4", "Order validation", "Amount: 0.01, Currency: JPY, Region: APAC", ExecutionResult.Passed, 31, ["Orders","Validation"], new(){["Amount"]="0.01",["Currency"]="JPY",["Region"]="APAC"}),
                    ScParam("order-p5", "Order validation", "Amount: 50, Currency: AUD, Region: APAC", ExecutionResult.Passed, 29, ["Orders","Validation"], new(){["Amount"]="50.00",["Currency"]="AUD",["Region"]="APAC"}),
                    ScParam("order-p6", "Order validation", "Amount: 1500, Currency: CAD, Region: NA", ExecutionResult.Passed, 35, ["Orders","Validation"], new(){["Amount"]="1500.00",["Currency"]="CAD",["Region"]="NA"}),
                ]
            },
            new Feature
            {
                DisplayName = "Payment Processing",
                Description = "Stripe integration for payment capture and refunds",
                Scenarios = [
                    Sc("pay-1", "Processing a payment charges Stripe and records transaction", true, ExecutionResult.Passed, 456, ["Smoke","Payments"],
                        Steps("Given", "the Payment API is running", "When", "I process a payment of £250.50",
                              "Then", "Stripe is charged successfully", "And", "the transaction is recorded in SQL Server")),
                    Sc("pay-2", "Refund reverses the charge", true, ExecutionResult.Passed, 312, ["Payments"]),
                    Sc("pay-3", "Payment timeout retries three times", false, ExecutionResult.Skipped, 50, ["Payments","ErrorHandling"]),
                    Sc("pay-4", "Partial refund calculates correct amount", true, ExecutionResult.Passed, 278, ["Payments"]),
                    Sc("pay-5", "Duplicate payment detection blocks charge", true, ExecutionResult.Passed, 189, ["Payments","ErrorHandling"]),
                    Sc("pay-6", "Currency conversion applies correct rate", true, ExecutionResult.Passed, 234, ["Payments"]),
                ]
            },
            new Feature
            {
                DisplayName = "User Authentication",
                Description = "OAuth2 and JWT-based authentication flows",
                Scenarios = [
                    Sc("auth-1", "Login with valid credentials returns JWT", true, ExecutionResult.Passed, 95, ["Smoke","Auth"]),
                    Sc("auth-2", "Login with invalid credentials returns 401", false, ExecutionResult.Passed, 78, ["Auth","ErrorHandling"]),
                    Sc("auth-3", "Expired token is rejected with 403", false, ExecutionResult.Passed, 62, ["Auth"]),
                    Sc("auth-4", "Refresh token extends session", true, ExecutionResult.Passed, 112, ["Auth"]),
                    Sc("auth-5", "Multi-factor authentication challenge", true, ExecutionResult.Passed, 245, ["Auth","Smoke"]),
                    Sc("auth-6", "Rate limiting blocks brute force attempts", false, ExecutionResult.Passed, 89, ["Auth","ErrorHandling"]),
                ]
            },
            new Feature
            {
                DisplayName = "Inventory Tracking",
                Description = "Real-time stock level management across warehouses",
                Scenarios = [
                    Sc("inv-1", "Stock level decreases after order", true, ExecutionResult.Passed, 220, ["Inventory"]),
                    Sc("inv-2", "Restock event increases available units", true, ExecutionResult.Passed, 145, ["Inventory"]),
                    Sc("inv-3", "Low stock triggers reorder alert", false, ExecutionResult.Failed, 890, ["Inventory","Alerting"],
                        error: "Expected alert to be triggered but notification service returned 503",
                        stack: "   at InventoryTests.Low_stock_triggers_alert() in /src/Tests/InventoryTests.cs:line 134"),
                    Sc("inv-4", "Cross-warehouse transfer updates both locations", true, ExecutionResult.Passed, 334, ["Inventory"]),
                ]
            },
            new Feature
            {
                DisplayName = "Shipping & Fulfilment",
                Description = "Carrier integration and delivery tracking",
                Scenarios = [
                    Sc("ship-1", "Creating shipment allocates tracking number", true, ExecutionResult.Passed, 178, ["Smoke","Shipping"]),
                    Sc("ship-2", "Delivery confirmation updates order status", true, ExecutionResult.Passed, 267, ["Shipping"]),
                    Sc("ship-3", "Failed delivery triggers return process", false, ExecutionResult.Passed, 445, ["Shipping","ErrorHandling"]),
                    Sc("ship-4", "Express shipping selects priority carrier", true, ExecutionResult.Passed, 156, ["Shipping"]),
                    Sc("ship-5", "International shipping calculates customs duty", true, ExecutionResult.Passed, 312, ["Shipping"]),
                ]
            },
            new Feature
            {
                DisplayName = "Notification Service",
                Description = "Email, SMS, and push notification delivery",
                Scenarios = [
                    Sc("notif-1", "Order confirmation email is sent", true, ExecutionResult.Passed, 134, ["Smoke","Notifications"]),
                    Sc("notif-2", "SMS notification for delivery update", true, ExecutionResult.Passed, 89, ["Notifications"]),
                    Sc("notif-3", "Push notification for price drop alert", true, ExecutionResult.Passed, 67, ["Notifications"]),
                    Sc("notif-4", "Unsubscribed user does not receive marketing email", false, ExecutionResult.Passed, 45, ["Notifications","Privacy"]),
                ]
            },
            new Feature
            {
                DisplayName = "Analytics & Reporting",
                Description = "Business intelligence and dashboard data",
                Scenarios = [
                    Sc("analytics-1", "Daily sales summary aggregation", true, ExecutionResult.Passed, 567, ["Analytics"]),
                    Sc("analytics-2", "Customer segmentation pipeline", true, ExecutionResult.Passed, 890, ["Analytics"]),
                    Sc("analytics-3", "Revenue forecast model accuracy", true, ExecutionResult.Passed, 1234, ["Analytics"]),
                    Sc("analytics-4", "Real-time dashboard WebSocket feed", true, ExecutionResult.Passed, 445, ["Smoke","Analytics"]),
                ]
            },
            new Feature
            {
                DisplayName = "Admin Portal",
                Description = "Back-office administration tools",
                Scenarios = [
                    Sc("admin-1", "Admin can search orders by customer", true, ExecutionResult.Passed, 123, ["Smoke","Admin"]),
                    Sc("admin-2", "Admin can manually trigger refund", true, ExecutionResult.Passed, 234, ["Admin","Payments"]),
                    Sc("admin-3", "Admin can view audit trail", true, ExecutionResult.Passed, 167, ["Admin"]),
                    Sc("admin-4", "Admin can suspend user account", true, ExecutionResult.Passed, 89, ["Admin","Auth"]),
                ]
            },
        };

        var diagrams = new List<DiagramAsCode>
        {
            new("order-1", "", OrderDiagramSource),
            new("order-2", "", FailureDiagramSource),
            new("order-p1", "", OrderDiagramSource),
            new("order-p2", "", OrderDiagramSource),
            new("order-p3", "", FailureDiagramSource),
            new("order-p4", "", OrderDiagramSource),
            new("order-p5", "", OrderDiagramSource),
            new("order-p6", "", OrderDiagramSource),
            new("pay-1", "", PaymentDiagramSource),
            new("inv-3", "", FailureDiagramSource),
        };

        return (features, diagrams.ToArray());
    }

    private static Scenario Sc(string id, string name, bool happy, ExecutionResult result, int ms, string[] cats,
        ScenarioStep[]? steps = null, string? error = null, string? stack = null)
        => new() { Id = id, DisplayName = name, IsHappyPath = happy, Result = result,
            Duration = TimeSpan.FromMilliseconds(ms), Categories = cats, Steps = steps,
            ErrorMessage = error, ErrorStackTrace = stack };

    private static Scenario ScParam(string id, string outlineId, string suffix, ExecutionResult result, int ms, string[] cats,
        Dictionary<string, string> values, string? error = null)
        => new() { Id = id, DisplayName = $"{outlineId}({suffix})", OutlineId = outlineId,
            IsHappyPath = result == ExecutionResult.Passed, Result = result,
            Duration = TimeSpan.FromMilliseconds(ms), Categories = cats, ExampleValues = values,
            ErrorMessage = error };

    private static ScenarioStep[] Steps(params string[] kwText)
    {
        var steps = new List<ScenarioStep>();
        for (var i = 0; i < kwText.Length; i += 2)
            steps.Add(new ScenarioStep { Keyword = kwText[i], Text = kwText[i + 1],
                Status = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(Random.Shared.Next(5, 300)) });
        return steps.ToArray();
    }

    private string GenerateReport()
    {
        var (features, diagrams) = CreateRichShowcaseData();
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow.AddMinutes(-3), DateTime.UtcNow,
            null, Path.Combine(_tempDir, "WikiReport.html"),
            "E‑Commerce Service Specifications",
            includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            groupParameterizedTests: true, maxParameterColumns: 10, showStepNumbers: true);
        return new Uri(path).AbsoluteUri;
    }

    private void NavigateToReport()
    {
        var url = GenerateReport();
        _driver.Navigate().GoToUrl(url);
        WaitFor(By.CssSelector("details.feature"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("document.documentElement.style.scrollBehavior='smooth';");
        InjectFakeCursor();
        Pause(500);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 1: Interactive HTML Report GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature01_Interactive_Report_GIF()
    {
        BeginFrameCapture("feature01");
        NavigateToReport();

        // Scene: Overview of the full report
        ScrollToTop(); Hold(2);

        // Click "Expand All Features"
        var expandBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Expand All')]"));
        JsClick(expandBtn); Hold(1.5);

        // Scroll down through expanded features
        var firstScenario = _driver.FindElement(By.CssSelector("details.scenario"));
        ScrollTo(firstScenario); Hold(1.5);

        // Click status filter "Failed"
        ScrollToTop(); Hold(0.5);
        try
        {
            var failedBtn = _driver.FindElement(By.CssSelector("[data-status-filter='Failed']"));
            JsClick(failedBtn); Hold(2);
            // Clear filter
            var clearBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Clear All')]"));
            JsClick(clearBtn); Hold(1);
        }
        catch { /* filter not found */ }

        // Click dependency filter
        try
        {
            var depBtns = _driver.FindElements(By.CssSelector("[data-dep]"));
            if (depBtns.Count > 0) { JsClick(depBtns[0]); Hold(1.5); }
        }
        catch { }

        // Click P95 duration filter
        try
        {
            var p95Btn = _driver.FindElement(By.XPath("//button[contains(text(),'P95')]"));
            JsClick(p95Btn); Hold(1.5);
        }
        catch { }

        // Happy Paths Only
        try
        {
            var happyBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Happy Paths Only')]"));
            JsClick(happyBtn); Hold(1.5);
        }
        catch { }

        // Clear and return to overview
        try
        {
            var clearBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Clear All')]"));
            JsClick(clearBtn); Hold(0.5);
        }
        catch { }
        ScrollToTop(); Hold(2);

        StitchGif("whats-new-feature01-report.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 2: Advanced Search GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature02_Search_GIF()
    {
        BeginFrameCapture("feature02");
        NavigateToReport();

        // Focus on search bar
        ScrollToTop(); Hold(1);
        var searchInput = _driver.FindElement(By.CssSelector("input[type='text'], input[type='search'], input[placeholder*='Search']"));
        HideCursor();
        TypeSlowly(searchInput, "order"); Hold(1.5);
        ShowCursor();

        // Clear and type a tag search
        searchInput.Clear(); Pause(100); Hold(0.5);
        HideCursor();
        TypeSlowly(searchInput, "@tag:Smoke"); Hold(1.5);
        ShowCursor();

        // Clear and type a status search
        searchInput.Clear(); Pause(100); Hold(0.5);
        HideCursor();
        TypeSlowly(searchInput, "$status:failed"); Hold(1.5);
        ShowCursor();

        // Clear and type a boolean search
        searchInput.Clear(); Pause(100); Hold(0.5);
        HideCursor();
        TypeSlowly(searchInput, "payment && stripe"); Hold(1.5);
        ShowCursor();

        // Show search help
        try
        {
            var helpBtn = _driver.FindElement(By.CssSelector("[aria-label*='help'], .search-help-btn, button[title*='Help']"));
            JsClick(helpBtn); Hold(2);
        }
        catch
        {
            // Try the ? button
            try
            {
                var qBtn = _driver.FindElement(By.XPath("//button[text()='?']"));
                JsClick(qBtn); Hold(2);
            }
            catch { }
        }

        // Clear
        searchInput.Clear(); Pause(100); Hold(1);

        StitchGif("whats-new-feature02-search.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 3: Parameterized Test Grouping GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature03_Parameterized_Tests_GIF()
    {
        BeginFrameCapture("feature03");
        NavigateToReport();

        // Expand all to find the param table
        var expandBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Expand All Features')]"));
        JsClick(expandBtn); Pause(300);
        var expandScenarios = _driver.FindElement(By.XPath("//button[contains(text(),'Expand All Scenarios')]"));
        JsClick(expandScenarios); Pause(300);

        // Scroll to the param table (Order validation)
        try
        {
            var paramTable = _driver.FindElement(By.CssSelector("table.param-table, .param-group table, table"));
            ScrollTo(paramTable); Hold(2);

            // Click on a passed row
            var rows = _driver.FindElements(By.CssSelector("table tr[data-scenario-id], table tbody tr"));
            if (rows.Count > 1)
            {
                JsClick(rows[1]); Hold(2);
            }

            // Click on a failed row if exists
            try
            {
                var failedRow = _driver.FindElement(By.CssSelector("tr.failed, tr[data-status='Failed']"));
                JsClick(failedRow); Hold(2);
            }
            catch { }

            // Scroll down to see expanded diagram
            try
            {
                var diagram = _driver.FindElement(By.CssSelector("[data-diagram-type]"));
                ScrollTo(diagram); Hold(2);
            }
            catch { }
        }
        catch { }

        Hold(1);
        StitchGif("whats-new-feature03-params.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 5: Sequence Diagrams GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature05_Sequence_Diagrams_GIF()
    {
        BeginFrameCapture("feature05");
        NavigateToReport();

        // Navigate to first scenario with diagram
        ExpandAll(); Pause(300);

        try
        {
            WaitForDiagramSvg(30);

            // Scroll to a diagram
            var diagramContainer = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));
            ScrollTo(diagramContainer); Hold(2);

            // Click "Expanded" details button
            try
            {
                var expandedBtn = _driver.FindElement(By.XPath("//button[text()='Expanded']"));
                JsClick(expandedBtn); Hold(2);
            }
            catch { }

            // Click "Collapsed" details button
            try
            {
                var collapsedBtn = _driver.FindElement(By.XPath("//button[text()='Collapsed']"));
                JsClick(collapsedBtn); Hold(1.5);
            }
            catch { }

            // Click "Truncated" back
            try
            {
                var truncBtn = _driver.FindElement(By.XPath("//button[text()='Truncated']"));
                JsClick(truncBtn); Hold(1);
            }
            catch { }

            // Toggle Headers
            try
            {
                var hiddenBtn = _driver.FindElement(By.XPath("//button[text()='Hidden']"));
                JsClick(hiddenBtn); Hold(1.5);
                var shownBtn = _driver.FindElement(By.XPath("//button[text()='Shown']"));
                JsClick(shownBtn); Hold(1);
            }
            catch { }

            // Double-click to zoom
            try
            {
                var svg = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml'] svg"));
                new Actions(_driver).DoubleClick(svg).Perform();
                Hold(2);
                // Close zoom
                try
                {
                    var closeZoom = _driver.FindElement(By.CssSelector(".diagram-zoom-close, .zoom-close"));
                    JsClick(closeZoom); Hold(1);
                }
                catch
                {
                    new Actions(_driver).DoubleClick(svg).Perform();
                    Hold(1);
                }
            }
            catch { }
        }
        catch { Hold(3); }

        StitchGif("whats-new-feature05-diagrams.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 6: Database Tracking (static screenshot with zoom + arrow)
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature06_Database_Tracking_Screenshot()
    {
        NavigateToReport();
        ExpandAll(); Pause(300);

        try
        {
            WaitForDiagramSvg(30);

            // Find and scroll to a diagram that shows Cosmos DB
            var diagramContainer = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));
            ScrollTo(diagramContainer); Pause(300);

            // Zoom into the diagram area by scrolling it to center
            ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                var el = arguments[0];
                el.scrollIntoView({block:'start', behavior:'instant'});
                window.scrollBy(0, -20);
            ", diagramContainer);
            Pause(300);

            // Inject a hand-drawn red arrow pointing at Cosmos DB
            ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                var svgs = document.querySelectorAll('[data-diagram-type=""plantuml""] svg');
                if (svgs.length === 0) return;
                var svg = svgs[0];
                var container = svg.closest('[data-diagram-type]');
                var rect = container.getBoundingClientRect();

                // Create arrow overlay
                var arrow = document.createElement('div');
                arrow.id = 'cosmos-arrow';
                arrow.style.cssText = 'position:absolute;z-index:9999;pointer-events:none;';

                // Find approximate Cosmos DB position (database icon is typically 60-70% from left)
                var arrowSvg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
                arrowSvg.setAttribute('width', '200');
                arrowSvg.setAttribute('height', '120');
                arrowSvg.setAttribute('viewBox', '0 0 200 120');
                arrowSvg.style.cssText = 'overflow:visible;';

                var path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
                path.setAttribute('d', 'M 180 10 C 150 15, 100 20, 50 60 C 30 75, 20 85, 15 100');
                path.setAttribute('stroke', '#e53e3e');
                path.setAttribute('stroke-width', '3');
                path.setAttribute('fill', 'none');
                path.setAttribute('stroke-linecap', 'round');
                path.setAttribute('stroke-dasharray', '8,4');

                var arrowhead = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
                arrowhead.setAttribute('points', '5,95 20,105 10,110');
                arrowhead.setAttribute('fill', '#e53e3e');

                arrowSvg.appendChild(path);
                arrowSvg.appendChild(arrowhead);
                arrow.appendChild(arrowSvg);

                container.style.position = 'relative';
                arrow.style.right = '10px';
                arrow.style.top = '10px';
                arrow.style.position = 'absolute';
                container.appendChild(arrow);
            ");
            Pause(200);
        }
        catch { /* proceed without arrow */ }

        SaveScreenshot("whats-new-database-tracking.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 7: Component Diagram Screenshot
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature07_Component_Diagram_Screenshot()
    {
        // Use the real ComponentDiagram.html from Example.Api build output
        var componentHtml = Path.Combine(
            Path.GetDirectoryName(typeof(WikiGifTests).Assembly.Location)!,
            "..", "..", "..", "..", "..", "examples", "Example.Api", "tests",
            "Example.Api.Tests.Component.BDDfy.xUnit3", "bin", "Debug", "net10.0",
            "Reports", "ComponentDiagram.html");
        componentHtml = Path.GetFullPath(componentHtml);

        Assert.True(File.Exists(componentHtml), $"ComponentDiagram.html not found at: {componentHtml}");
        OpenFile(componentHtml);

        WaitFor(By.TagName("body"));
        Pause(500);
        ExpandAll();
        Pause(500);

        // Wait for PlantUML to render
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
            wait.Until(d =>
            {
                try { return d.FindElement(By.CssSelector("svg, img[src*='plantuml']")).Displayed; }
                catch { return false; }
            });
            Pause(1000);
        }
        catch { Pause(2000); /* wait for rendering */ }

        SaveScreenshot("whats-new-component-diagram.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 8: Internal Flow GIF (10 spans, Activity → Flame)
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature08_Internal_Flow_GIF()
    {
        // Generate a test page with ~10 spans
        var html = GenerateInternalFlowPage();
        var path = Path.Combine(_tempDir, "InternalFlow.html");
        File.WriteAllText(path, html);

        BeginFrameCapture("feature08");
        OpenFile(path);
        WaitFor(By.TagName("body"));
        Pause(500);

        // Open the flow details
        ExpandAll(); Hold(1);

        // Wait for activity diagram to render
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
            wait.Until(d =>
            {
                try { return d.FindElement(By.CssSelector("svg, .activity-diagram")).Displayed; }
                catch { return false; }
            });
        }
        catch { }

        InjectFakeCursor();

        // Show the Activity diagram
        Hold(2);

        // Click "Flame Chart" toggle
        try
        {
            var flameBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Flame')]"));
            JsClick(flameBtn); Hold(2);

            // Show the flame chart bars
            try
            {
                var flameBars = _driver.FindElement(By.CssSelector(".flame-chart, .flame-bar, canvas"));
                ScrollTo(flameBars); Hold(2);
            }
            catch { Hold(2); }

            // Click "Activity" toggle to go back
            var actBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Activity')]"));
            JsClick(actBtn); Hold(2);
        }
        catch { Hold(3); }

        Hold(1);
        StitchGif("whats-new-feature08-internal-flow.gif");
    }

    private string GenerateInternalFlowPage()
    {
        using var activitySource = new ActivitySource("TestTrackingDiagrams.Wiki.InternalFlow");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        Activity.Current = null;
        var baseTime = DateTime.UtcNow;

        // Root span: HTTP request
        var root = activitySource.StartActivity("HTTP POST /api/orders", ActivityKind.Server)!;
        root.SetStartTime(baseTime);
        root.SetEndTime(baseTime.AddMilliseconds(850));
        var rootCtx = new ActivityContext(root.TraceId, root.SpanId, ActivityTraceFlags.Recorded);

        // Child spans simulating a realistic flow
        var spans = new List<Activity> { root };
        Activity MakeSpan(string name, int startMs, int endMs)
        {
            var s = activitySource.StartActivity(name, ActivityKind.Internal, rootCtx)!;
            s.SetStartTime(baseTime.AddMilliseconds(startMs));
            s.SetEndTime(baseTime.AddMilliseconds(endMs));
            spans.Add(s);
            return s;
        }

        MakeSpan("Middleware: Authentication", 5, 25);
        MakeSpan("Middleware: Authorization", 25, 35);
        MakeSpan("MediatR: Send CreateOrderCommand", 40, 780);
        MakeSpan("FluentValidation: Validate", 45, 65);
        MakeSpan("EF Core: SELECT Products", 70, 180);
        MakeSpan("HttpClient: PUT /stock/reserve", 185, 420);
        MakeSpan("EF Core: INSERT Orders", 425, 560);
        MakeSpan("CosmosDB: Create Item (orders)", 565, 680);
        MakeSpan("EventGrid: Publish OrderCreated", 685, 750);
        MakeSpan("Serialization: JsonSerializer", 755, 775);

        var testId = "test-flow-1";
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            [$"iflow-test-{testId}"] = new(
                Guid.Empty, RequestResponseType.Request, testId,
                root.StartTimeUtc, root.StartTimeUtc + root.Duration,
                spans.ToArray())
        };

        var boundaryLogs = new[]
        {
            ("POST: /api/orders", new DateTimeOffset(baseTime.AddMilliseconds(5), TimeSpan.Zero))
        };

        var flowHtml = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, testId, boundaryLogs, WholeTestFlowVisualization.Both);

        var styles = DiagramContextMenu.GetInternalFlowPopupStyles();
        var toggleScript = DiagramContextMenu.GetToggleScript();
        var flameChartScript = DiagramContextMenu.GetFlameChartRenderScript();
        var plantUmlScript = DiagramContextMenu.GetPlantUmlBrowserRenderScript();

        foreach (var s in spans) s.Dispose();

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>Internal Flow Tracking</title>
                <style>
                    {{styles}}
                    body { font-family: sans-serif; padding: 20px; }
                    h1 { color: #333; }
                </style>
                {{plantUmlScript}}
                {{flameChartScript}}
                {{toggleScript}}
            </head>
            <body>
                <h2>▼ Creating an order reserves stock and publishes event</h2>
                <p style="color:#666;">Scenario duration: 850ms &bull; 10 internal spans captured</p>
                {{flowHtml}}
            </body>
            </html>
            """;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 9: CI Summary (screenshot of rendered markdown)
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature09_CI_Summary_Screenshot()
    {
        // Generate a rich CI summary markdown
        var (features, diagrams) = CreateRichShowcaseData();
        var markdown = CiSummaryGenerator.GenerateMarkdown(
            features, diagrams, diagrams,
            DateTime.UtcNow.AddMinutes(-3), DateTime.UtcNow,
            maxDiagrams: 5,
            diagramFormat: DiagramFormat.PlantUml);

        // Write markdown to file and render it as styled HTML
        var htmlPath = Path.Combine(_tempDir, "CiSummary.html");
        var styledHtml = $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif; max-width: 900px; margin: 40px auto; padding: 0 20px; line-height: 1.5; color: #24292f; background: #fff; }
                    h1 { border-bottom: 1px solid #d0d7de; padding-bottom: 0.3em; }
                    h2 { border-bottom: 1px solid #d0d7de; padding-bottom: 0.3em; margin-top: 24px; }
                    table { border-collapse: collapse; width: auto; margin: 16px 0; }
                    th, td { border: 1px solid #d0d7de; padding: 6px 13px; }
                    th { background: #f6f8fa; font-weight: 600; }
                    details { margin: 8px 0; }
                    summary { cursor: pointer; font-weight: 600; padding: 4px 0; }
                    details summary strong { color: #cf222e; }
                    pre { background: #f6f8fa; border: 1px solid #d0d7de; border-radius: 6px; padding: 16px; overflow: auto; font-size: 13px; }
                    code { background: #f6f8fa; padding: 2px 4px; border-radius: 3px; font-size: 85%; }
                    img { max-width: 100%; border: 1px solid #d0d7de; border-radius: 6px; margin: 8px 0; }
                    .markdown-body { padding: 15px; }
                    /* GitHub Actions summary header styling */
                    .gh-header { background: #f6f8fa; border: 1px solid #d0d7de; border-radius: 6px; padding: 16px; margin-bottom: 20px; display: flex; align-items: center; gap: 10px; }
                    .gh-header svg { width: 24px; height: 24px; }
                    .gh-header .run-info { color: #57606a; font-size: 14px; }
                </style>
            </head>
            <body>
                <div class="gh-header">
                    <svg viewBox="0 0 24 24" fill="#57606a"><path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z"/></svg>
                    <div>
                        <strong>GitHub Actions</strong> &bull; <span class="run-info">E-Commerce CI &bull; Run #847 &bull; main &bull; 3m 12s</span>
                    </div>
                </div>
                <div class="markdown-body">{{ConvertMarkdownToHtml(markdown)}}</div>
            </body>
            </html>
            """;
        File.WriteAllText(htmlPath, styledHtml);

        OpenFile(htmlPath);
        WaitFor(By.TagName("body")); Pause(500);
        SaveScreenshot("whats-new-ci-summary.png");
    }

    private static string ConvertMarkdownToHtml(string md)
    {
        // Simple markdown to HTML converter for tables, headers, details, code blocks
        var sb = new StringBuilder();
        var lines = md.Split('\n');
        var inTable = false;
        var inCodeBlock = false;
        var headerRow = true;

        foreach (var line in lines)
        {
            var l = line.TrimEnd('\r');
            if (l.StartsWith("```"))
            {
                if (inCodeBlock) { sb.AppendLine("</code></pre>"); inCodeBlock = false; }
                else { sb.AppendLine("<pre><code>"); inCodeBlock = true; }
                continue;
            }
            if (inCodeBlock) { sb.AppendLine(System.Net.WebUtility.HtmlEncode(l)); continue; }

            if (l.StartsWith("# ")) { sb.AppendLine($"<h1>{l[2..]}</h1>"); continue; }
            if (l.StartsWith("## ")) { sb.AppendLine($"<h2>{l[3..]}</h2>"); continue; }
            if (l.StartsWith("### ")) { sb.AppendLine($"<h3>{l[4..]}</h3>"); continue; }
            if (l.StartsWith("<details")) { sb.AppendLine(l); continue; }
            if (l.StartsWith("</details")) { sb.AppendLine(l); continue; }
            if (l.StartsWith("<summary")) { sb.AppendLine(l); continue; }
            if (l.StartsWith("</summary")) { sb.AppendLine(l); continue; }
            if (l.StartsWith("**Error:**")) { sb.AppendLine($"<p><strong>Error:</strong> {l[10..]}</p>"); continue; }
            if (l.StartsWith("*") && l.EndsWith("*")) { sb.AppendLine($"<p><em>{l.Trim('*')}</em></p>"); continue; }
            if (l.StartsWith("!["))
            {
                var altEnd = l.IndexOf(']');
                var urlStart = l.IndexOf('(', altEnd);
                var urlEnd = l.IndexOf(')', urlStart);
                if (altEnd > 0 && urlStart > 0 && urlEnd > 0)
                {
                    var alt = l[2..altEnd];
                    var url = l[(urlStart+1)..urlEnd];
                    sb.AppendLine($"<img src=\"{url}\" alt=\"{alt}\" />");
                }
                continue;
            }
            if (l.StartsWith("| "))
            {
                if (!inTable) { sb.AppendLine("<table>"); inTable = true; headerRow = true; }
                if (l.Contains("---|")) continue; // separator row
                var cells = l.Split('|', StringSplitOptions.RemoveEmptyEntries);
                sb.Append("<tr>");
                foreach (var cell in cells)
                {
                    var tag = headerRow ? "th" : "td";
                    sb.Append($"<{tag}>{cell.Trim()}</{tag}>");
                }
                sb.AppendLine("</tr>");
                headerRow = false;
                continue;
            }
            if (inTable && !l.StartsWith("| ")) { sb.AppendLine("</table>"); inTable = false; }
            if (string.IsNullOrWhiteSpace(l)) { sb.AppendLine("<br/>"); continue; }
            sb.AppendLine($"<p>{l}</p>");
        }
        if (inTable) sb.AppendLine("</table>");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 10: TestRunReport.json GIF (browser JSON viewer)
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature10_JSON_Report_GIF()
    {
        // Generate a rich JSON report
        var (features, diagrams) = CreateRichShowcaseData();
        var jsonPath = Path.Combine(_tempDir, "TestRunReport.json");
        ReportGenerator.GenerateTestRunReportData(
            features, DateTime.UtcNow.AddMinutes(-3), DateTime.UtcNow,
            jsonPath, DataFormat.Json, diagrams);

        BeginFrameCapture("feature10");
        OpenFile(jsonPath);
        WaitFor(By.TagName("body"));
        Pause(1000);

        InjectFakeCursor();
        Hold(2);

        // Chrome renders JSON with collapsible tree — try clicking on expanders
        var js = (IJavaScriptExecutor)_driver;

        // Try to interact with Chrome's built-in JSON viewer toggle elements
        try
        {
            var toggles = _driver.FindElements(By.CssSelector(".json-formatter-toggler, .json-formatter-open, .json-formatter-key, span.json-formatter-key"));
            if (toggles.Count > 0)
            {
                // Click first few toggles to expand
                for (var i = 0; i < Math.Min(3, toggles.Count); i++)
                {
                    JsClick(toggles[i]); Hold(1);
                }
            }
            else
            {
                // Chrome might not format JSON — inject a formatter
                InjectJsonFormatter(jsonPath);
                Hold(2);
            }
        }
        catch
        {
            InjectJsonFormatter(jsonPath);
            Hold(2);
        }

        Hold(2);
        StitchGif("whats-new-feature10-json.gif");
    }

    private void InjectJsonFormatter(string jsonPath)
    {
        // Read the JSON content and inject a nice formatted view
        var jsonContent = File.ReadAllText(jsonPath);
        var js = (IJavaScriptExecutor)_driver;
        // Navigate to a blank page and inject formatted JSON
        _driver.Navigate().GoToUrl("about:blank");
        Pause(200);
        js.ExecuteScript($@"
            document.title = 'TestRunReport.json';
            document.body.style.fontFamily = 'Consolas, monospace';
            document.body.style.fontSize = '13px';
            document.body.style.padding = '20px';
            document.body.style.lineHeight = '1.4';
            document.body.style.background = '#1e1e1e';
            document.body.style.color = '#d4d4d4';

            var json = {JsonSerializer.Serialize(jsonContent)};
            var obj = JSON.parse(json);

            function render(obj, depth) {{
                if (depth > 5) return '<span style=""color:#808080"">...</span>';
                if (obj === null) return '<span style=""color:#569cd6"">null</span>';
                if (typeof obj === 'string') return '<span style=""color:#ce9178"">' + JSON.stringify(obj) + '</span>';
                if (typeof obj === 'number') return '<span style=""color:#b5cea8"">' + obj + '</span>';
                if (typeof obj === 'boolean') return '<span style=""color:#569cd6"">' + obj + '</span>';
                if (Array.isArray(obj)) {{
                    if (obj.length === 0) return '[]';
                    var items = obj.map(function(v) {{ return render(v, depth + 1); }});
                    var indent = '  '.repeat(depth);
                    var innerIndent = '  '.repeat(depth + 1);
                    return '[<br/>' + items.map(function(v) {{ return innerIndent + v; }}).join(',<br/>') + '<br/>' + indent + ']';
                }}
                var keys = Object.keys(obj);
                if (keys.length === 0) return '{{}}';
                var indent = '  '.repeat(depth);
                var innerIndent = '  '.repeat(depth + 1);
                var pairs = keys.map(function(k) {{
                    return innerIndent + '<span style=""color:#9cdcfe"">""' + k + '""</span>: ' + render(obj[k], depth + 1);
                }});
                return '{{<br/>' + pairs.join(',<br/>') + '<br/>' + indent + '}}';
            }}

            document.body.innerHTML = '<pre style=""white-space:pre-wrap;"">' + render(obj, 0) + '</pre>';
        ");
        Pause(500);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 11: MediatR Tracking Screenshot (with red arrow)
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature11_MediatR_Screenshot()
    {
        // Generate a report with MediatR-showing diagram
        var features = new[]
        {
            new Feature { DisplayName = "Order Service", Scenarios = [
                Sc("mediatr-1", "Creating an order via MediatR command pipeline", true, ExecutionResult.Passed, 456, ["Smoke","Orders"],
                    Steps("Given", "the Orders API is running",
                          "When", "I create an order via the MediatR pipeline",
                          "Then", "the CreateOrderCommand is handled",
                          "And", "the order is persisted to Cosmos DB",
                          "And", "an OrderCreatedEvent is published"))
            ]}
        };
        var diagrams = new[] { new DiagramAsCode("mediatr-1", "", MediatRDiagramSource) };
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow,
            null, Path.Combine(_tempDir, "MediatR.html"),
            "MediatR Tracking Demo", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs, showStepNumbers: true);

        OpenUrl(new Uri(path).AbsoluteUri);
        WaitFor(By.CssSelector("details.feature"));
        ExpandAll(); Pause(300);

        try
        {
            WaitForDiagramSvg(30);
            var diagramContainer = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'start', behavior:'instant'}); window.scrollBy(0, -20);",
                diagramContainer);
            Pause(500);

            // Inject red arrow pointing at the MediatR "Send: CreateOrderCommand" arrow
            ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                var container = document.querySelector('[data-diagram-type=""plantuml""]');
                if (!container) return;
                container.style.position = 'relative';
                var arrow = document.createElement('div');
                arrow.style.cssText = 'position:absolute;right:30px;top:60px;z-index:9999;pointer-events:none;';
                arrow.innerHTML = `<svg width='180' height='100' viewBox='0 0 180 100' xmlns='http://www.w3.org/2000/svg'>
                    <path d='M 170 5 C 140 10, 90 25, 40 55 C 20 70, 15 80, 10 90'
                          stroke='#e53e3e' stroke-width='3' fill='none' stroke-linecap='round' stroke-dasharray='8,4'/>
                    <polygon points='2,85 15,95 8,98' fill='#e53e3e'/>
                    <text x='100' y='15' fill='#e53e3e' font-size='12' font-weight='bold' font-family='sans-serif'>MediatR</text>
                </svg>`;
                container.appendChild(arrow);
            ");
            Pause(300);
        }
        catch { }

        SaveScreenshot("whats-new-mediatr-tracking.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 12: DiagramFocus Screenshot (full view with highlights)
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature12_DiagramFocus_Screenshot()
    {
        // Generate a report with the Focus diagram
        var features = new[]
        {
            new Feature { DisplayName = "Order Service", Scenarios = [
                Sc("focus-1", "Creating an order shows focused fields in diagram", true, ExecutionResult.Passed, 342, ["Smoke"],
                    Steps("Given", "the Orders API is running",
                          "When", "I create an order with DiagramFocus on status and totalAmount",
                          "Then", "the response fields status and totalAmount are highlighted"))
            ]}
        };
        var diagrams = new[] { new DiagramAsCode("focus-1", "", FocusDiagramSource) };
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow,
            null, Path.Combine(_tempDir, "Focus.html"),
            "DiagramFocus Demo", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs, showStepNumbers: true);

        OpenUrl(new Uri(path).AbsoluteUri);
        WaitFor(By.CssSelector("details.feature"));
        ExpandAll(); Pause(300);

        try
        {
            WaitForDiagramSvg(30);
            // Scroll down to show the response note with highlighted fields
            var diagramContainer = _driver.FindElement(By.CssSelector("[data-diagram-type='plantuml']"));
            // Scroll so the bottom of the diagram (response note) is visible
            ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                var el = arguments[0];
                var svg = el.querySelector('svg');
                if (svg) {
                    var svgRect = svg.getBoundingClientRect();
                    // Scroll to show the bottom third of the diagram
                    var targetY = window.scrollY + svgRect.top + svgRect.height * 0.3;
                    window.scrollTo(0, targetY);
                } else {
                    el.scrollIntoView({block:'center'});
                }
            ", diagramContainer);
            Pause(500);
        }
        catch { }

        SaveScreenshot("whats-new-diagram-focus.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 13: Failure Diagnostics GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature13_Failure_Diagnostics_GIF()
    {
        BeginFrameCapture("feature13");
        NavigateToReport();

        InjectFakeCursor();

        // Click "Failed" status filter
        try
        {
            var failedBtn = _driver.FindElement(By.CssSelector("[data-status-filter='Failed']"));
            JsClick(failedBtn); Hold(2);

            // Click "Next Failure" button
            try
            {
                var nextFailBtn = _driver.FindElement(By.CssSelector(".next-failure-btn, [data-next-failure]"));
                JsClick(nextFailBtn); Hold(2);
                // Click again
                JsClick(nextFailBtn); Hold(2);
            }
            catch { }

            // Expand the failed scenario to show error details
            try
            {
                var failedFeature = _driver.FindElement(By.CssSelector(".feature.has-failed details, details.feature"));
                ScrollTo(failedFeature); Hold(1);
                ExpandAll(); Hold(2);

                // Scroll to error message area
                try
                {
                    var errorArea = _driver.FindElement(By.CssSelector(".error-message, .stack-trace, pre"));
                    ScrollTo(errorArea); Hold(2);
                }
                catch { }
            }
            catch { }
        }
        catch { Hold(3); }

        Hold(1);
        StitchGif("whats-new-feature13-failures.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 14: Category Filtering GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature14_Category_Filter_GIF()
    {
        BeginFrameCapture("feature14");
        NavigateToReport();

        ScrollToTop(); Hold(1);

        // Click "Smoke" category
        try
        {
            var smokeBtn = _driver.FindElement(By.XPath("//button[text()='Smoke']"));
            JsClick(smokeBtn); Hold(2);

            // Scroll down to see filtered results
            var features = _driver.FindElement(By.CssSelector("details.feature"));
            ScrollTo(features); Hold(1.5);
            ScrollToTop(); Hold(0.5);

            // Click "OR" toggle to "AND"
            try
            {
                var orBtn = _driver.FindElement(By.XPath("//button[text()='OR'] | //button[text()='All']"));
                JsClick(orBtn); Hold(1);
            }
            catch { }

            // Add another category
            try
            {
                var ordersBtn = _driver.FindElement(By.XPath("//button[text()='Orders']"));
                JsClick(ordersBtn); Hold(2);
            }
            catch { }

            // Clear
            try
            {
                var clearBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Clear')]"));
                JsClick(clearBtn); Hold(1);
            }
            catch { }
        }
        catch { Hold(3); }

        ScrollToTop(); Hold(1);
        StitchGif("whats-new-feature14-categories.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 17: Framework Support (3 screenshots)
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature17_Framework_xUnit_Screenshot()
    {
        var basePath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(typeof(WikiGifTests).Assembly.Location)!,
            "..", "..", "..", "..", "..", "examples", "Example.Api", "tests"));

        var xunitReport = Path.Combine(basePath,
            "Example.Api.Tests.Component.xUnit3", "bin", "Debug", "net10.0", "Reports", "Specifications.html");
        Assert.True(File.Exists(xunitReport), $"xUnit3 report not found: {xunitReport}");
        OpenFile(xunitReport);
        WaitFor(By.TagName("body")); ExpandAll(); Pause(500);
        try { WaitForDiagramSvg(20); } catch { Pause(2000); }
        SaveScreenshot("whats-new-framework-xunit.png");
    }

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature17_Framework_ReqNRoll_Screenshot()
    {
        var basePath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(typeof(WikiGifTests).Assembly.Location)!,
            "..", "..", "..", "..", "..", "examples", "Example.Api", "tests"));

        // Try ReqNRoll.xUnit3 first, fall back to xUnit2
        var report = Path.Combine(basePath,
            "Example.Api.Tests.Component.ReqNRoll.xUnit3", "bin", "Debug", "net10.0", "Reports", "Specifications.html");
        if (!File.Exists(report))
            report = Path.Combine(basePath,
                "Example.Api.Tests.Component.ReqNRoll.xUnit2", "bin", "Debug", "net10.0", "Reports", "Specifications.html");
        Assert.True(File.Exists(report), "ReqNRoll report not found");
        OpenFile(report);
        WaitFor(By.TagName("body")); ExpandAll(); Pause(500);
        try { WaitForDiagramSvg(20); } catch { Pause(2000); }
        SaveScreenshot("whats-new-framework-reqnroll.png");
    }

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature17_Framework_LightBDD_Screenshot()
    {
        var basePath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(typeof(WikiGifTests).Assembly.Location)!,
            "..", "..", "..", "..", "..", "examples", "Example.Api", "tests"));

        var report = Path.Combine(basePath,
            "Example.Api.Tests.Component.LightBDD.xUnit2", "bin", "Debug", "net10.0", "Reports", "Specifications.html");
        Assert.True(File.Exists(report), "LightBDD report not found");
        OpenFile(report);
        WaitFor(By.TagName("body")); ExpandAll(); Pause(500);
        try { WaitForDiagramSvg(20); } catch { Pause(2000); }
        SaveScreenshot("whats-new-framework-lightbdd.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 19: Scenario Timeline (20+ scenarios, top = Expand All)
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature19_Scenario_Timeline_Screenshot()
    {
        // Generate report with 20+ rich data (already have 51 scenarios in our dataset)
        NavigateToReport();

        // Click "Scenario Timeline" button
        try
        {
            var timelineBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Scenario Timeline')]"));
            JsClick(timelineBtn);
            Pause(500);
        }
        catch { }

        // Scroll so the top of the screenshot shows "Expand All Features" button
        try
        {
            var expandBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Expand All Features')]"));
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'start', behavior:'instant'}); window.scrollBy(0, -10);",
                expandBtn);
            Pause(300);
        }
        catch { }

        SaveScreenshot("whats-new-scenario-timeline.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 20: Export Filtered HTML & CSV GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact(Skip = "Long-running GIF/screenshot generation — unskip when regenerating wiki assets, then re-skip afterwards")]
    public void Feature20_Export_GIF()
    {
        BeginFrameCapture("feature20");
        NavigateToReport();

        // Apply a filter first (Failed)
        try
        {
            var failedBtn = _driver.FindElement(By.CssSelector("[data-status-filter='Failed']"));
            JsClick(failedBtn); Hold(1.5);
        }
        catch { }

        // Click "Export Filtered HTML"
        try
        {
            var exportHtmlBtn = _driver.FindElement(By.XPath("//button[contains(text(),'Export Filtered HTML')]"));
            JsClick(exportHtmlBtn); Hold(2);
        }
        catch { }

        // Brief screen clear with "Opening downloaded file..." message
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var overlay = document.createElement('div');
            overlay.style.cssText = 'position:fixed;inset:0;background:#f8f9fa;z-index:99999;display:flex;align-items:center;justify-content:center;flex-direction:column;';
            overlay.innerHTML = '<div style=""font-size:18px;color:#57606a;font-family:sans-serif;"">Opening downloaded file...</div>';
            document.body.appendChild(overlay);
        ");
        Hold(2);

        // "Open" the export — navigate to the report again as if the downloaded file opened
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var overlay = document.querySelector('[style*=""z-index:99999""]');
            if (overlay) overlay.remove();
        ");
        Pause(200);

        // Show the report (simulating the exported file opening)
        Hold(2);
        ScrollToTop(); Hold(1);

        StitchGif("whats-new-feature20-export.gif");
    }
}
