using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.PlantUml;
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
            // -resize 960x: scale to 960px wide (wiki-friendly size, ~50% reduction in pixels)
            // -fuzz 2% -layers optimize: treat near-identical pixels as same, store only diffs
            Arguments = $"-delay 3 -loop 0 \"{_currentFrameDir}\\*.png\" -resize 960x -fuzz 2% -layers optimize \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        process.WaitForExit(600_000);
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
        // Force rendering of all PlantUML diagrams - IntersectionObserver doesn't fire
        // reliably in headless Chrome for elements inside expanded <details>
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "if (window._renderDiagramsInContainer) window._renderDiagramsInContainer(document.body);");

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
          <back:#FFEB3B><b>"status": "Confirmed"</b></back>,
          <back:#FFEB3B><b>"totalAmount": 250.50</b></back>,
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
                        "System.Net.Http.HttpRequestException: Connection refused (Stock Service:5001)\nResponse body: {\"error\":\"Connection refused\"}",
                        "   at System.Net.Http.HttpClient.SendAsync()\n   at OrderTests.Insufficient_stock_returns_409() in /src/Tests/OrderTests.cs:line 87"),
                    Sc("order-3", "Listing orders returns paginated results", true, ExecutionResult.Passed, 189, ["Smoke","Orders"]),
                    Sc("order-4", "Updating order status sends notification", true, ExecutionResult.Passed, 267, ["Orders"]),
                    Sc("order-5", "Cancelling order within grace period succeeds", true, ExecutionResult.Passed, 198, ["Orders"]),
                    Sc("order-6", "Cancelling order after cut-off returns 409", false, ExecutionResult.Passed, 156, ["Orders","ErrorHandling"]),
                    // Parameterized tests
                    ScParam("order-p1", "Order validation", "Amount: 100, Currency: GBP, Region: EU", ExecutionResult.Passed, 34, ["Orders","Validation"], new(){["Amount"]="100.00",["Currency"]="GBP",["Region"]="EU"}),
                    ScParam("order-p2", "Order validation", "Amount: 250.50, Currency: USD, Region: US", ExecutionResult.Passed, 28, ["Orders","Validation"], new(){["Amount"]="250.50",["Currency"]="USD",["Region"]="US"}),
                    ScParam("order-p3", "Order validation", "Amount: 999.99, Currency: EUR, Region: APAC", ExecutionResult.Failed, 1102, ["Orders","Validation"], new(){["Amount"]="999.99",["Currency"]="EUR",["Region"]="APAC"}, "System.Net.Http.HttpRequestException: Connection refused (Stock Service:5001)"),
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
                        error: "System.Net.Http.HttpRequestException: Connection refused (Stock Service:5001)",
                        stack: "   at System.Net.Http.HttpClient.SendAsync()\n   at InventoryTests.Low_stock_triggers_alert() in /src/Tests/InventoryTests.cs:line 134"),
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
                    Sc("ship-3", "Failed delivery triggers return process", false, ExecutionResult.Failed, 445, ["Shipping","ErrorHandling"],
                        error: "System.Net.Http.HttpRequestException: Connection refused (Stock Service:5001)",
                        stack: "   at System.Net.Http.HttpClient.SendAsync()\n   at ShippingTests.Failed_delivery_triggers_return_process() in /src/Tests/ShippingTests.cs:line 56"),
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
            new("ship-3", "", FailureDiagramSource),
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

    [Fact]
    public void Feature01_Interactive_Report_GIF()
    {
        BeginFrameCapture("feature01");
        NavigateToReport();

        // Scene 1: Overview of the full report
        ScrollToTop(); Hold(2);

        // Click "Expand All Features"
        var expandBtn = _driver.FindElement(By.CssSelector(".collapse-expand-all"));
        JsClick(expandBtn); Hold(1.5);

        // Scroll down through expanded features
        var firstScenario = _driver.FindElement(By.CssSelector("details.scenario"));
        ScrollTo(firstScenario); Hold(1.5);
        ScrollToTop(); Hold(0.5);

        // Filter 1: Status = Failed
        var failedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Failed']"));
        JsClick(failedBtn); Hold(2);

        // Scroll to show filtered results
        var filteredFeature = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(filteredFeature); Hold(1.5);
        ScrollToTop(); Hold(0.5);

        // Clear
        var clearBtn = _driver.FindElement(By.CssSelector(".export-btn[onclick*='clear_all']"));
        JsClick(clearBtn); Hold(1);

        // Filter 2: Status = Passed
        var passedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Passed']"));
        JsClick(passedBtn); Hold(1);

        // Scroll to show results
        filteredFeature = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(filteredFeature); Hold(1);
        ScrollToTop(); Hold(0.5);

        // Clear
        clearBtn = _driver.FindElement(By.CssSelector(".export-btn[onclick*='clear_all']"));
        JsClick(clearBtn); Hold(0.5);

        // Filter 3: Dependency filter
        var depBtns = _driver.FindElements(By.CssSelector(".dependency-toggle[data-dependency]"));
        if (depBtns.Count > 0) { JsClick(depBtns[0]); Hold(1); }

        // Clear
        clearBtn = _driver.FindElement(By.CssSelector(".export-btn[onclick*='clear_all']"));
        JsClick(clearBtn); Hold(0.5);

        // Filter 4: P95 duration filter
        var p95Btn = _driver.FindElement(By.CssSelector(".percentile-btn[data-threshold-ms]"));
        // Find the P95 specifically — it's typically the 3rd percentile button
        var percentileBtns = _driver.FindElements(By.CssSelector(".percentile-btn[data-threshold-ms]"));
        if (percentileBtns.Count >= 3) JsClick(percentileBtns[2]); // P95
        else if (percentileBtns.Count > 0) JsClick(percentileBtns[0]);
        Hold(1);

        // Clear
        clearBtn = _driver.FindElement(By.CssSelector(".export-btn[onclick*='clear_all']"));
        JsClick(clearBtn); Hold(0.5);

        // Filter 5: Happy Paths Only
        var happyBtn = _driver.FindElement(By.CssSelector(".happy-path-toggle"));
        JsClick(happyBtn); Hold(1.5);

        // Scroll to show results
        filteredFeature = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(filteredFeature); Hold(1);
        ScrollToTop(); Hold(0.5);

        // Filter 6: Category filter (Smoke)
        var smokeBtn = _driver.FindElement(By.CssSelector(".category-toggle[data-category='Smoke']"));
        JsClick(smokeBtn); Hold(1);

        // Final Clear All and return to overview
        clearBtn = _driver.FindElement(By.CssSelector(".export-btn[onclick*='clear_all']"));
        JsClick(clearBtn); Hold(1);
        ScrollToTop(); Hold(2);

        StitchGif("whats-new-feature01-report.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 2: Advanced Search GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature02_Search_GIF()
    {
        BeginFrameCapture("feature02");
        NavigateToReport();

        // Focus on search bar
        ScrollToTop(); Hold(1);
        var searchInput = _driver.FindElement(By.Id("searchbar"));

        // Search 1: "order" — matches Order Management scenarios
        HideCursor();
        TypeSlowly(searchInput, "order"); Hold(1.5);
        // Scroll down to show filtered results
        var visibleFeature = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(visibleFeature); Hold(1);
        ScrollToTop(); Hold(0.5);
        ShowCursor();

        // Search 2: "@tag:Smoke" — matches Smoke-tagged scenarios
        searchInput.Clear(); Pause(100);
        ((IJavaScriptExecutor)_driver).ExecuteScript("document.getElementById('searchbar').dispatchEvent(new Event('keyup'));");
        Hold(0.5);
        HideCursor();
        TypeSlowly(searchInput, "@tag:Smoke"); Hold(1.5);
        visibleFeature = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(visibleFeature); Hold(1);
        ScrollToTop(); Hold(0.5);
        ShowCursor();

        // Search 3: "$status:failed" — matches failed scenarios
        searchInput.Clear(); Pause(100);
        ((IJavaScriptExecutor)_driver).ExecuteScript("document.getElementById('searchbar').dispatchEvent(new Event('keyup'));");
        Hold(0.5);
        HideCursor();
        TypeSlowly(searchInput, "$status:failed"); Hold(1.5);
        visibleFeature = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(visibleFeature); Hold(1);
        ScrollToTop(); Hold(0.5);
        ShowCursor();

        // Search 4: "payment && stripe" — matches payment scenarios mentioning Stripe
        searchInput.Clear(); Pause(100);
        ((IJavaScriptExecutor)_driver).ExecuteScript("document.getElementById('searchbar').dispatchEvent(new Event('keyup'));");
        Hold(0.5);
        HideCursor();
        TypeSlowly(searchInput, "payment && stripe"); Hold(1.5);
        visibleFeature = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(visibleFeature); Hold(1);
        ScrollToTop(); Hold(0.5);
        ShowCursor();

        // Search 5: "stock || inventory" — matches Stock Service or Inventory scenarios
        searchInput.Clear(); Pause(100);
        ((IJavaScriptExecutor)_driver).ExecuteScript("document.getElementById('searchbar').dispatchEvent(new Event('keyup'));");
        Hold(0.5);
        HideCursor();
        TypeSlowly(searchInput, "stock || inventory"); Hold(1.5);
        visibleFeature = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(visibleFeature); Hold(1);
        ScrollToTop(); Hold(0.5);
        ShowCursor();

        // Show search help
        var helpBtn = _driver.FindElement(By.CssSelector(".search-help-toggle"));
        JsClick(helpBtn); Hold(2);

        // Clear
        searchInput.Clear(); Pause(100);
        ((IJavaScriptExecutor)_driver).ExecuteScript("document.getElementById('searchbar').dispatchEvent(new Event('keyup'));");
        Hold(1);

        StitchGif("whats-new-feature02-search.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 3: Parameterized Test Grouping GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature03_Parameterized_Tests_GIF()
    {
        BeginFrameCapture("feature03");
        NavigateToReport();

        // Expand all features and scenarios to reveal the param table
        var expandFeatures = _driver.FindElement(By.XPath("//button[contains(text(),'Expand All Features')]"));
        JsClick(expandFeatures); Pause(300);
        var expandScenarios = _driver.FindElement(By.XPath("//button[contains(text(),'Expand All Scenarios')]"));
        JsClick(expandScenarios); Pause(500);

        // Scroll to the param table (Order validation parameterized group)
        var paramTable = WaitFor(By.CssSelector(".param-test-table"), 10);
        ScrollTo(paramTable); Hold(4);

        // Click on a passed row (row index 0 — first param combination)
        var rows = _driver.FindElements(By.CssSelector(".param-test-table tbody tr[data-row-idx]"));
        if (rows.Count > 0)
        {
            JsClick(rows[0]); Hold(3);

            // Scroll down to show the expanded diagram for this row
            try
            {
                var detailPanel = _driver.FindElement(By.CssSelector(".param-detail-panel[style*='display: block'], .param-detail-panel:not([style*='display: none'])"));
                ScrollTo(detailPanel); Hold(2.5);
            }
            catch { }

            // Scroll back up to the table
            ScrollTo(paramTable); Hold(1.5);
        }

        // Click on the failed row (EUR/APAC combination)
        try
        {
            var failedRow = _driver.FindElement(By.CssSelector(".param-test-table tbody tr.row-failed"));
            JsClick(failedRow); Hold(3);

            // Scroll down to show the error details for the failed row
            try
            {
                var detailPanel = _driver.FindElement(By.CssSelector(".param-detail-panel[style*='display: block'], .param-detail-panel:not([style*='display: none'])"));
                ScrollTo(detailPanel); Hold(3);
            }
            catch { }
        }
        catch { }

        // Click another passed row to show diagram swap
        if (rows.Count > 3)
        {
            ScrollTo(paramTable); Hold(0.5);
            JsClick(rows[3]); Hold(2);
        }

        // Hold the final param table view
        ScrollTo(paramTable); Hold(4);

        StitchGif("whats-new-feature03-params.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 5: Sequence Diagrams GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature05_Sequence_Diagrams_GIF()
    {
        BeginFrameCapture("feature05");
        NavigateToReport();

        // Navigate to first scenario with diagram
        ExpandAll(); Pause(300);

        // Wait for PlantUML browser rendering to complete
        WaitForDiagramSvg(30);

        // Scroll to a diagram
        var diagramContainer = _driver.FindElement(By.CssSelector(".plantuml-browser"));
        ScrollTo(diagramContainer); Hold(4);

        // Click "Expanded" details radio button
        var expandedBtn = _driver.FindElement(By.CssSelector(".details-radio-btn[data-state='expanded']"));
        JsClick(expandedBtn);
        Pause(500); // Wait for diagram re-render
        try { WaitForDiagramSvg(15); } catch { Pause(2000); }
        Hold(3);

        // Click "Collapsed" details radio button
        var collapsedBtn = _driver.FindElement(By.CssSelector(".details-radio-btn[data-state='collapsed']"));
        JsClick(collapsedBtn);
        Pause(500);
        try { WaitForDiagramSvg(15); } catch { Pause(2000); }
        Hold(3);

        // Click "Truncated" back
        var truncBtn = _driver.FindElement(By.CssSelector(".details-radio-btn[data-state='truncated']"));
        JsClick(truncBtn);
        Pause(500);
        try { WaitForDiagramSvg(15); } catch { Pause(2000); }
        Hold(3);

        // Toggle Headers: Hidden → Shown
        try
        {
            var headersToggle = _driver.FindElement(By.CssSelector(".header-toggle-btn, button[onclick*='toggle_headers']"));
            JsClick(headersToggle); Hold(1.5);
            // Toggle back
            JsClick(headersToggle); Hold(1);
        }
        catch { }

        // Double-click to zoom
        var svg = _driver.FindElement(By.CssSelector(".plantuml-browser svg"));
        new Actions(_driver).DoubleClick(svg).Perform();
        Hold(3);
        // Close zoom — try close button, fall back to double-click again
        try
        {
            var closeZoom = _driver.FindElement(By.CssSelector(".diagram-zoom-close, .zoom-close, [onclick*='zoom']"));
            JsClick(closeZoom); Hold(1);
        }
        catch
        {
            new Actions(_driver).DoubleClick(svg).Perform();
            Hold(1);
        }

        // Right-click context menu
        try
        {
            svg = _driver.FindElement(By.CssSelector(".plantuml-browser svg"));
            new Actions(_driver).ContextClick(svg).Perform();
            Hold(3);
            // Dismiss context menu
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "document.querySelectorAll('.diagram-context-menu').forEach(m => m.style.display = 'none');");
            Hold(0.5);
        }
        catch { }

        Hold(1);
        StitchGif("whats-new-feature05-diagrams.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 6: Database Tracking (static screenshot with zoom + arrow)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature06_Database_Tracking_Screenshot()
    {
        NavigateToReport();
        ExpandAll(); Pause(300);

        WaitForDiagramSvg(30);

        // Find and scroll to a diagram that shows Cosmos DB
        var diagramContainer = _driver.FindElement(By.CssSelector(".plantuml-browser"));
        ScrollTo(diagramContainer); Pause(300);

        // Zoom into the diagram area by scrolling it to center
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var el = arguments[0];
            el.scrollIntoView({block:'start', behavior:'instant'});
            window.scrollBy(0, -20);
        ", diagramContainer);
        Pause(500);

        // Wait for SVG to be visible after scroll
        try { WaitForDiagramSvg(10); } catch { Pause(1000); }

        // Inject a hand-drawn red arrow pointing at the Cosmos DB participant in the SVG
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var container = document.querySelector('.plantuml-browser');
            if (!container) return;
            var svg = container.querySelector('svg');
            if (!svg) return;
            container.style.position = 'relative';

            // Find the Cosmos DB text in the SVG to position the arrow near it
            var texts = svg.querySelectorAll('text');
            var cosmosEl = null;
            for (var i = 0; i < texts.length; i++) {
                if (texts[i].textContent.indexOf('Cosmos') >= 0) { cosmosEl = texts[i]; break; }
            }

            var arrow = document.createElement('div');
            arrow.style.cssText = 'position:absolute;z-index:9999;pointer-events:none;';

            if (cosmosEl) {
                // Position arrow relative to the Cosmos DB text in SVG
                var svgRect = svg.getBoundingClientRect();
                var cosmosRect = cosmosEl.getBoundingClientRect();
                var relX = cosmosRect.left - svgRect.left + cosmosRect.width + 10;
                var relY = cosmosRect.top - svgRect.top - 80;
                arrow.style.left = relX + 'px';
                arrow.style.top = Math.max(10, relY) + 'px';
            } else {
                // Fallback position
                arrow.style.right = '30px';
                arrow.style.top = '60px';
            }

            arrow.innerHTML = `<svg width='200' height='120' viewBox='0 0 200 120' xmlns='http://www.w3.org/2000/svg' style='overflow:visible;'>
                <path d='M 180 10 C 150 15, 100 20, 50 60 C 30 75, 20 85, 15 100'
                      stroke='#e53e3e' stroke-width='3' fill='none' stroke-linecap='round' stroke-dasharray='8,4'/>
                <polygon points='5,95 20,105 10,110' fill='#e53e3e'/>
                <text x='100' y='10' fill='#e53e3e' font-size='13' font-weight='bold' font-family='sans-serif'>Cosmos DB</text>
            </svg>`;
            container.appendChild(arrow);
        ");
        Pause(300);

        SaveScreenshot("whats-new-database-tracking.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 7: Component Diagram Screenshot
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature07_Component_Diagram_Screenshot()
    {
        // Generate a rich component diagram using the GenerateHtmlReport API
        var (features, diagrams) = CreateRichShowcaseData();

        // Create a component diagram PlantUML without C4 (browser-side doesn't support C4 includes)
        var componentDiagramPlantUml = """
            @startuml
            skinparam componentStyle rectangle
            skinparam backgroundColor white
            skinparam defaultFontSize 12
            skinparam wrapWidth 200

            actor "Caller\n[Person]" as caller #369
            rectangle "Orders API\n[Software System]" as api #369
            rectangle "Stock Service\n[Software System]" as stock #369
            rectangle "Payment API\n[Software System]" as payments #369
            rectangle "Stripe Gateway\n[Software System]" as stripe #369
            database "Cosmos DB\n[Database]" as cosmos #369
            database "SQL Server\n[Database]" as sql #369
            database "Redis Cache\n[Cache]" as redis #369
            queue "EventGrid\n[Event Broker]" as events #369
            rectangle "Notification Service\n[Software System]" as notifications #369
            rectangle "Auth Service\n[Software System]" as auth #369
            rectangle "Shipping API\n[Software System]" as shipping #369
            rectangle "Analytics Engine\n[Software System]" as analytics #369

            caller --> api : HTTP: GET, POST, PUT, DELETE\n45 calls across 12 tests
            api --> stock : HTTP: PUT, GET\n12 calls across 8 tests
            api --> payments : HTTP: POST\n6 calls across 4 tests
            payments --> stripe : HTTP: POST\n6 calls across 4 tests
            api --> cosmos : Create Item; Query; Upsert\n18 calls across 10 tests
            payments --> sql : INSERT; SELECT\n8 calls across 4 tests
            api --> redis : Get (Hit); Get (Miss); Set\n15 calls across 9 tests
            api --> events : Send (Event Protocol)\n10 calls across 7 tests
            events --> notifications : Send (Event Protocol)\n5 calls across 3 tests
            api --> auth : HTTP: POST\n8 calls across 6 tests
            api --> shipping : HTTP: POST, GET\n5 calls across 3 tests
            api --> analytics : HTTP: POST\n4 calls across 2 tests
            shipping --> notifications : Send (Event Protocol)\n3 calls across 2 tests
            @enduml
            """;

        // Use GenerateHtmlReport with componentDiagramPlantUml parameter
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow.AddMinutes(-3), DateTime.UtcNow,
            null, Path.Combine(_tempDir, "ComponentDiagramReport.html"),
            "E‑Commerce Service — Test Run Report",
            includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: componentDiagramPlantUml);

        OpenUrl(new Uri(path).AbsoluteUri);
        WaitFor(By.CssSelector("details.feature"), 10);
        Pause(500);

        // Toggle Component Diagram section visible
        try
        {
            var cdToggle = _driver.FindElement(By.XPath("//button[contains(text(),'Component Diagram')]"));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", cdToggle);
            Pause(500);
        }
        catch { }

        // Wait for component diagram PlantUML to render
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            wait.Until(d =>
            {
                try { return d.FindElement(By.CssSelector("#component-diagram .plantuml-browser svg")).Displayed; }
                catch { return false; }
            });
            Pause(1000);
        }
        catch { Pause(3000); }

        // Scroll to component diagram section
        try
        {
            var compSection = _driver.FindElement(By.Id("component-diagram"));
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'start', behavior:'instant'}); window.scrollBy(0, -10);",
                compSection);
            Pause(500);
        }
        catch { }

        SaveScreenshot("whats-new-component-diagram.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 8: Internal Flow GIF (10 spans, Activity → Flame)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature08_Internal_Flow_GIF()
    {
        // Generate a report with internal flow data so we get Sequence/Activity/Flame Chart tabs
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Order Service",
                Scenarios = [
                    Sc("flow-1", "Creating an order reserves stock and publishes event", true, ExecutionResult.Passed, 850, ["Smoke","Orders"],
                        Steps("Given", "the Orders API is running", "And", "Stock Service has 50 units of SKU-42",
                              "When", "I create an order for 3 units of SKU-42",
                              "Then", "the order is confirmed with status Created"))
                ]
            }
        };
        var diagrams = new[] { new DiagramAsCode("flow-1", "", OrderDiagramSource) };

        // Generate internal flow segments for the test
        using var activitySource = new ActivitySource("TestTrackingDiagrams.Wiki.InternalFlow.Report");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        Activity.Current = null;
        var baseTime = DateTime.UtcNow;

        var root = activitySource.StartActivity("HTTP POST /api/orders", ActivityKind.Server)!;
        root.SetStartTime(baseTime);
        root.SetEndTime(baseTime.AddMilliseconds(850));
        var rootCtx = new ActivityContext(root.TraceId, root.SpanId, ActivityTraceFlags.Recorded);

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

        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-flow-1"] = new(
                Guid.Empty, RequestResponseType.Request, "flow-1",
                root.StartTimeUtc, root.StartTimeUtc + root.Duration,
                spans.ToArray())
        };

        var boundaryLogs = new[]
        {
            ("POST: /api/orders", new DateTimeOffset(baseTime.AddMilliseconds(5), TimeSpan.Zero))
        };

        var flowHtml = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, "flow-1", boundaryLogs, WholeTestFlowVisualization.Both);

        // Generate the full report incorporating internal flow
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow.AddMinutes(-3), DateTime.UtcNow,
            null, Path.Combine(_tempDir, "InternalFlowReport.html"),
            "Internal Flow Tracking Demo",
            includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            showStepNumbers: true,
            internalFlowTracking: true,
            wholeTestSegments: segments,
            wholeTestVisualization: WholeTestFlowVisualization.Both);

        foreach (var s in spans) s.Dispose();

        BeginFrameCapture("feature08");
        OpenUrl(new Uri(path).AbsoluteUri);
        WaitFor(By.CssSelector("details.feature")); Pause(500);
        ExpandAll(); Pause(500);
        InjectFakeCursor();

        // Wait for sequence diagram to render
        WaitForDiagramSvg(30);

        // Show the Sequence Diagrams tab (starting view)
        var diagContainer = _driver.FindElement(By.CssSelector(".plantuml-browser"));
        ScrollTo(diagContainer); Hold(5);

        // Click "Activity Diagrams" tab
        var activityTab = _driver.FindElement(By.CssSelector(".diagram-toggle-btn[data-dtype='activity']"));
        JsClick(activityTab); Hold(2);

        // Wait for activity diagram to render
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
            wait.Until(d =>
            {
                try { return d.FindElement(By.CssSelector("[data-diagram-type='activity'] svg, .plantuml-browser svg")).Displayed; }
                catch { return false; }
            });
        }
        catch { Pause(2000); }
        Hold(5);

        // Click "Flame Chart" tab
        var flameTab = _driver.FindElement(By.CssSelector(".diagram-toggle-btn[data-dtype='flame']"));
        JsClick(flameTab); Hold(2);

        // Wait for flame chart to render
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d =>
            {
                try { return d.FindElement(By.CssSelector(".iflow-flame, [data-diagram-type='flamechart']")).Displayed; }
                catch { return false; }
            });
        }
        catch { Pause(2000); }

        // Scroll through flame chart bars
        try
        {
            var flameBars = _driver.FindElements(By.CssSelector(".iflow-flame-bar"));
            if (flameBars.Count > 0)
            {
                // Hover over a few bars to show tooltips
                MoveCursorTo(flameBars[0]); Hold(2);
                if (flameBars.Count > 3) { MoveCursorTo(flameBars[3]); Hold(2); }
                if (flameBars.Count > 6) { MoveCursorTo(flameBars[6]); Hold(2); }
            }
        }
        catch { }
        Hold(3);

        // Switch back to Sequence Diagrams
        var seqTab = _driver.FindElement(By.CssSelector(".diagram-toggle-btn[data-dtype='seq']"));
        JsClick(seqTab); Hold(3);

        StitchGif("whats-new-feature08-internal-flow.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 9: CI Summary (screenshot of rendered markdown)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature09_CI_Summary_Screenshot()
    {
        // Generate the actual CI Summary markdown using the real CiSummaryGenerator
        var (features, diagrams) = CreateRichShowcaseData();
        var markdown = CiSummaryGenerator.GenerateMarkdown(
            features, diagrams, diagrams,
            DateTime.UtcNow.AddMinutes(-3), DateTime.UtcNow,
            maxDiagrams: 5,
            diagramFormat: DiagramFormat.PlantUml);

        // Create an HTML page that renders the markdown in GitHub's job summary style
        var escapedMd = System.Text.Json.JsonSerializer.Serialize(markdown);
        var viewerPath = Path.Combine(_tempDir, "CiSummary.html");
        File.WriteAllText(viewerPath, $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>
                <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Noto Sans, Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.5; color: #1f2328; background: #f6f8fa; margin: 0; padding: 0; }
                    .gh-header { background: #24292f; color: white; padding: 12px 24px; display: flex; align-items: center; gap: 12px; }
                    .gh-header svg { flex-shrink: 0; }
                    .gh-header strong { font-size: 16px; }
                    .gh-header .subtext { color: #8b949e; font-size: 13px; }
                    .gh-breadcrumb { background: #f6f8fa; border-bottom: 1px solid #d0d7de; padding: 8px 24px; font-size: 13px; color: #57606a; }
                    .gh-breadcrumb a { color: #0969da; text-decoration: none; }
                    .gh-body { max-width: 960px; margin: 24px auto; background: white; border: 1px solid #d0d7de; border-radius: 6px; padding: 32px; }
                    .gh-body h1 { font-size: 24px; font-weight: 600; border-bottom: 1px solid #d0d7de; padding-bottom: 0.3em; margin-top: 0; }
                    .gh-body h2 { font-size: 20px; font-weight: 600; border-bottom: 1px solid #d0d7de; padding-bottom: 0.3em; margin-top: 24px; }
                    .gh-body table { border-collapse: collapse; width: auto; }
                    .gh-body th, .gh-body td { border: 1px solid #d0d7de; padding: 6px 13px; }
                    .gh-body th { background: #f6f8fa; font-weight: 600; }
                    .gh-body details { margin: 8px 0; }
                    .gh-body details > summary { cursor: pointer; font-weight: 600; padding: 4px 0; }
                    .gh-body pre { background: #f6f8fa; border: 1px solid #d0d7de; border-radius: 6px; padding: 16px; overflow: auto; font-size: 13px; }
                    .gh-body code { background: #f6f8fa; padding: 0.2em 0.4em; border-radius: 3px; font-size: 85%; }
                    .gh-body img { max-width: 100%; }
                    .gh-body p { margin: 8px 0; }
                </style>
            </head>
            <body>
                <div class="gh-header">
                    <svg viewBox="0 0 24 24" fill="white" width="32" height="32"><path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z"/></svg>
                    <div><strong>GitHub Actions</strong><br/><span class="subtext">acme-corp/e-commerce-api &bull; E-Commerce CI &bull; Run #847 &bull; main</span></div>
                </div>
                <div class="gh-breadcrumb">
                    <a href="#">acme-corp/e-commerce-api</a> / <a href="#">Actions</a> / <a href="#">E-Commerce CI</a> / Run #847 &bull; <strong>Summary</strong>
                </div>
                <div class="gh-body" id="content"></div>
                <script>
                    var md = {{escapedMd}};
                    document.getElementById('content').innerHTML = marked.parse(md);
                </script>
            </body>
            </html>
            """);

        OpenFile(viewerPath);
        WaitFor(By.CssSelector(".gh-body"));
        Pause(1500); // wait for marked.js to render

        // Expand the first failed scenario <details> to reveal the embedded diagram
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var allDetails = document.querySelectorAll('.gh-body details');
            for (var i = 0; i < allDetails.length; i++) {
                allDetails[i].setAttribute('open', '');
            }
        ");
        Pause(2000); // wait for diagram images to load after expanding

        // Scroll down so the first diagram image is visible
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var img = document.querySelector('.gh-body img');
            if (img) {
                img.scrollIntoView({ block: 'center' });
            }
        ");
        Pause(500);

        SaveScreenshot("whats-new-ci-summary.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 10: TestRunReport.json GIF (browser JSON viewer)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature10_JSON_Report_GIF()
    {
        // Generate a rich JSON report
        var (features, diagrams) = CreateRichShowcaseData();
        var jsonPath = Path.Combine(_tempDir, "TestRunReport.json");
        ReportGenerator.GenerateTestRunReportData(
            features, DateTime.UtcNow.AddMinutes(-3), DateTime.UtcNow,
            jsonPath, DataFormat.Json, diagrams);

        BeginFrameCapture("feature10");

        // Create a dark-themed interactive JSON viewer page
        var jsonContent = File.ReadAllText(jsonPath);
        var viewerHtml = CreateInteractiveJsonViewerHtml(jsonContent);
        var viewerPath = Path.Combine(_tempDir, "JsonViewer.html");
        File.WriteAllText(viewerPath, viewerHtml);

        OpenFile(viewerPath);
        WaitFor(By.TagName("body"));
        Pause(500);
        InjectFakeCursor();

        // Show initial fully-collapsed view
        Hold(2);

        // Directly manipulate DOM to expand/collapse JSON nodes (click handlers unreliable in headless)
        var js = (IJavaScriptExecutor)_driver;

        // Helper: expand a node by its children ID
        void ExpandNode(string childrenId)
        {
            js.ExecuteScript($@"
                var ch = document.getElementById('{childrenId}');
                if (ch) {{
                    ch.style.display = 'inline';
                    // Hide the summary (next sibling before children is the summary)
                    var sm = document.getElementById('{childrenId}'.replace('jch','jsm'));
                    if (sm) sm.style.display = 'none';
                    // Update toggle arrow
                    var toggle = ch.previousElementSibling;
                    while (toggle && !toggle.classList.contains('json-toggle')) toggle = toggle.previousElementSibling;
                    if (toggle) toggle.textContent = '\u25BC';
                }}
            ");
            Pause(300);
        }

        void CollapseNode(string childrenId)
        {
            js.ExecuteScript($@"
                var ch = document.getElementById('{childrenId}');
                if (ch) {{
                    ch.style.display = 'none';
                    var sm = document.getElementById('{childrenId}'.replace('jch','jsm'));
                    if (sm) sm.style.display = 'inline';
                    var toggle = ch.previousElementSibling;
                    while (toggle && !toggle.classList.contains('json-toggle')) toggle = toggle.previousElementSibling;
                    if (toggle) toggle.textContent = '\u25B6';
                }}
            ");
            Pause(300);
        }

        // Expand root object (jch0 = root children)
        ExpandNode("jch0");
        Hold(2.5);

        // Expand first child node (jch1 = first child with children)
        ExpandNode("jch1");
        Hold(2);

        // Expand second child node (jch2)
        ExpandNode("jch2");
        Hold(2);

        // Scroll down to show expanded content
        js.ExecuteScript("window.scrollBy(0, 300);");
        Pause(200);
        Hold(2);

        // Expand a third section (jch3)
        ExpandNode("jch3");
        Hold(2.5);

        // Scroll down more
        js.ExecuteScript("window.scrollBy(0, 400);");
        Pause(200);
        Hold(2);

        // Collapse one section
        CollapseNode("jch3");
        Hold(2);

        // Scroll to top for final view
        ScrollToTop(); Hold(2);

        StitchGif("whats-new-feature10-json.gif");
    }

    private string CreateInteractiveJsonViewerHtml(string jsonContent)
    {
        var escapedJson = JsonSerializer.Serialize(jsonContent);
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>TestRunReport.json</title>
                <style>
                    body { font-family: 'Consolas', 'Monaco', monospace; font-size: 13px; padding: 20px; line-height: 1.5; background: #ffffff; color: #24292f; margin: 0; }
                    .json-toolbar { background: #f6f8fa; border: 1px solid #d0d7de; border-radius: 6px; padding: 8px 16px; margin-bottom: 16px; display: flex; align-items: center; gap: 12px; font-family: -apple-system, sans-serif; font-size: 14px; }
                    .json-toolbar .filename { font-weight: 600; color: #0969da; }
                    .json-toolbar .meta { color: #57606a; font-size: 12px; }
                    .json-toggle { cursor: pointer; user-select: none; display: inline-block; width: 16px; text-align: center; color: #57606a; font-family: monospace; }
                    .json-toggle:hover { color: #0969da; }
                    .json-key { color: #0550ae; }
                    .json-string { color: #0a3069; }
                    .json-number { color: #0550ae; font-weight: 600; }
                    .json-bool { color: #cf222e; }
                    .json-null { color: #8250df; }
                    .json-bracket { color: #24292f; }
                    .json-comma { color: #57606a; }
                    .json-summary { color: #57606a; font-style: italic; cursor: pointer; background: #f6f8fa; padding: 1px 6px; border-radius: 3px; font-size: 12px; }
                    .json-summary:hover { background: #eaeef2; }
                    .json-content { border: 1px solid #d0d7de; border-radius: 6px; padding: 16px; background: #ffffff; font-family: 'Consolas', 'Monaco', monospace; font-size: 13px; white-space: pre; overflow: auto; }
                </style>
            </head>
            <body>
                <div class="json-toolbar">
                    <span class="filename">📄 TestRunReport.json</span>
                    <span class="meta">Generated by TestTrackingDiagrams</span>
                </div>
                <div class="json-content" id="jsonRoot"></div>
                <script>
                    var jsonText = {{escapedJson}};
                    var obj = JSON.parse(jsonText);

                    function toggleNode(toggleEl) {
                        var childrenEl = toggleEl.getAttribute('data-target');
                        var summaryEl = toggleEl.getAttribute('data-summary');
                        var ch = document.getElementById(childrenEl);
                        var sm = document.getElementById(summaryEl);
                        if (!ch) return;
                        var isOpen = ch.style.display !== 'none';
                        ch.style.display = isOpen ? 'none' : 'inline';
                        if (sm) sm.style.display = isOpen ? 'inline' : 'none';
                        toggleEl.textContent = isOpen ? '\u25B6' : '\u25BC';
                    }

                    var _nodeId = 0;
                    function renderJson(val, depth) {
                        var indent = '  '.repeat(depth);
                        var innerIndent = '  '.repeat(depth + 1);

                        if (val === null) return '<span class="json-null">null</span>';
                        if (typeof val === 'string') return '<span class="json-string">"' + escHtml(val).substring(0, 200) + (val.length > 200 ? '...' : '') + '"</span>';
                        if (typeof val === 'number') return '<span class="json-number">' + val + '</span>';
                        if (typeof val === 'boolean') return '<span class="json-bool">' + val + '</span>';

                        var isArray = Array.isArray(val);
                        var keys = isArray ? val : Object.keys(val);
                        var count = isArray ? val.length : keys.length;
                        var open = isArray ? '[' : '{';
                        var close = isArray ? ']' : '}';
                        var summary = isArray ? (count + ' items') : (count + ' keys');

                        if (count === 0) return '<span class="json-bracket">' + open + close + '</span>';

                        var nid = _nodeId++;
                        var chId = 'jch' + nid;
                        var smId = 'jsm' + nid;
                        var html = '';
                        html += '<span class="json-toggle" data-target="' + chId + '" data-summary="' + smId + '" onclick="toggleNode(this)">\u25B6</span>';
                        html += '<span class="json-bracket">' + open + '</span>';
                        html += '<span class="json-summary" id="' + smId + '" onclick="toggleNode(this.previousElementSibling.previousElementSibling)">' + summary + '</span>';
                        html += '<span id="' + chId + '" style="display:none">';

                        if (isArray) {
                            for (var i = 0; i < val.length; i++) {
                                html += '\n' + innerIndent + renderJson(val[i], depth + 1) + (i < val.length - 1 ? '<span class="json-comma">,</span>' : '');
                            }
                        } else {
                            for (var i = 0; i < keys.length; i++) {
                                var k = keys[i];
                                html += '\n' + innerIndent + '<span class="json-key">"' + escHtml(k) + '"</span>: ' + renderJson(val[k], depth + 1) + (i < keys.length - 1 ? '<span class="json-comma">,</span>' : '');
                            }
                        }

                        html += '\n' + indent + '<span class="json-bracket">' + close + '</span>';
                        html += '</span>';
                        return html;
                    }

                    function escHtml(s) { var d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

                    document.getElementById('jsonRoot').innerHTML = renderJson(obj, 0);
                </script>
            </body>
            </html>
            """;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 11: MediatR Tracking Screenshot (with red arrow)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
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

        WaitForDiagramSvg(30);
        var diagramContainer = _driver.FindElement(By.CssSelector(".plantuml-browser"));
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].scrollIntoView({block:'start', behavior:'instant'}); window.scrollBy(0, -20);",
            diagramContainer);
        Pause(500);

        // Inject red arrow pointing at the MediatR messages in the SVG
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var container = document.querySelector('.plantuml-browser');
            if (!container) return;
            var svg = container.querySelector('svg');
            if (!svg) return;
            container.style.position = 'relative';

            // Find 'Send: CreateOrderCommand' text in the SVG
            var texts = svg.querySelectorAll('text');
            var mediatrEl = null;
            for (var i = 0; i < texts.length; i++) {
                if (texts[i].textContent.indexOf('Send') >= 0 || texts[i].textContent.indexOf('CreateOrder') >= 0) {
                    mediatrEl = texts[i]; break;
                }
            }

            var arrow = document.createElement('div');
            arrow.style.cssText = 'position:absolute;z-index:9999;pointer-events:none;';

            if (mediatrEl) {
                var svgRect = svg.getBoundingClientRect();
                var elRect = mediatrEl.getBoundingClientRect();
                var relX = elRect.right - svgRect.left + 10;
                var relY = elRect.top - svgRect.top - 60;
                arrow.style.left = Math.min(relX, svgRect.width - 200) + 'px';
                arrow.style.top = Math.max(10, relY) + 'px';
            } else {
                arrow.style.right = '30px';
                arrow.style.top = '100px';
            }

            arrow.innerHTML = `<svg width='180' height='100' viewBox='0 0 180 100' xmlns='http://www.w3.org/2000/svg'>
                <text x='80' y='15' fill='#e53e3e' font-size='13' font-weight='bold' font-family='sans-serif'>MediatR</text>
                <path d='M 100 20 C 80 30, 40 45, 20 70 C 15 78, 10 85, 8 90'
                      stroke='#e53e3e' stroke-width='3' fill='none' stroke-linecap='round' stroke-dasharray='8,4'/>
                <polygon points='0,85 13,95 6,98' fill='#e53e3e'/>
            </svg>`;
            container.appendChild(arrow);
        ");
        Pause(300);

        SaveScreenshot("whats-new-mediatr-tracking.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 12: DiagramFocus Screenshot (full view with highlights)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature12_DiagramFocus_Screenshot()
    {
        // Generate diagram using the REAL PlantUmlCreator infrastructure with DiagramFocus,
        // so the output matches what real TTD users see (correct note placement, arrow colors, etc.)
        var traceId = Guid.NewGuid();
        var logs = new List<RequestResponseLog>();

        // Step 1: Test → Orders API: POST /api/orders
        var req1Id = Guid.NewGuid();
        logs.Add(new RequestResponseLog(
            "Creating an order shows focused fields in diagram", "focus-1",
            HttpMethod.Post,
            JsonSerializer.Serialize(new
            {
                productId = "SKU-42", quantity = 3, customerId = "cust-1234",
                shippingAddress = "123 High St", billingAddress = "123 High St",
                paymentMethod = "card_visa_4242", couponCode = (string?)null,
                giftWrap = false, deliveryNotes = "Leave at door"
            }, new JsonSerializerOptions { WriteIndented = true }),
            new Uri("http://orders-api/api/orders"),
            [("Content-Type", "application/json")],
            "Orders API", "Test",
            RequestResponseType.Request, traceId, req1Id, false));

        // Step 2: Orders API → Stock Service: PUT /stock/reserve
        var req2Id = Guid.NewGuid();
        logs.Add(new RequestResponseLog(
            "Creating an order shows focused fields in diagram", "focus-1",
            HttpMethod.Put, null,
            new Uri("http://stock-service/stock/reserve"), [],
            "Stock Service", "Orders API",
            RequestResponseType.Request, traceId, req2Id, false));

        // Step 3: Stock Service → Orders API: 200 OK
        logs.Add(new RequestResponseLog(
            "Creating an order shows focused fields in diagram", "focus-1",
            HttpMethod.Put, null,
            new Uri("http://stock-service/stock/reserve"), [],
            "Stock Service", "Orders API",
            RequestResponseType.Response, traceId, req2Id, false,
            HttpStatusCode.OK));

        // Step 4: Orders API → Cosmos DB: Create Item
        var req3Id = Guid.NewGuid();
        logs.Add(new RequestResponseLog(
            "Creating an order shows focused fields in diagram", "focus-1",
            "Create Item",
            JsonSerializer.Serialize(new { id = "ord-7f3a", status = "created" },
                new JsonSerializerOptions { WriteIndented = true }),
            new Uri("http://cosmos-db/orders"), [],
            "Cosmos DB", "Orders API",
            RequestResponseType.Request, traceId, req3Id, false));

        // Step 5: Cosmos DB → Orders API: 201
        logs.Add(new RequestResponseLog(
            "Creating an order shows focused fields in diagram", "focus-1",
            "Create Item", null,
            new Uri("http://cosmos-db/orders"), [],
            "Cosmos DB", "Orders API",
            RequestResponseType.Response, traceId, req3Id, false,
            HttpStatusCode.Created));

        // Step 6: Orders API → Test: 201 Created (response with DiagramFocus)
        logs.Add(new RequestResponseLog(
            "Creating an order shows focused fields in diagram", "focus-1",
            HttpMethod.Post,
            JsonSerializer.Serialize(new
            {
                orderId = "ord-7f3a", status = "Confirmed", totalAmount = 250.50,
                currency = "GBP", createdAt = "2026-04-17T19:35:06Z",
                updatedAt = "2026-04-17T19:35:06Z", customerId = "cust-1234",
                shippingAddress = "123 High St", paymentRef = "pay-001",
                trackingNumber = (string?)null
            }, new JsonSerializerOptions { WriteIndented = true }),
            new Uri("http://orders-api/api/orders"), [],
            "Orders API", "Test",
            RequestResponseType.Response, traceId, req1Id, false,
            HttpStatusCode.Created)
        {
            FocusFields = ["status", "totalAmount"]
        });

        // Generate PlantUML using real TTD infrastructure
        var plantUmlResults = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
            logs,
            focusEmphasis: FocusEmphasis.Bold,
            focusDeEmphasis: FocusDeEmphasis.LightGray | FocusDeEmphasis.SmallerText,
            maxEncodedDiagramLength: 10000,
            serviceTypeOverrides: new Dictionary<string, string>
            {
                ["Cosmos DB"] = "database"
            }).ToList();

        var plantUmlSource = plantUmlResults.Single().PlantUmls.First().PlainText;

        var features = new[]
        {
            new Feature { DisplayName = "Order Service", Scenarios = [
                Sc("focus-1", "Creating an order shows focused fields in diagram", true, ExecutionResult.Passed, 342, ["Smoke"],
                    Steps("Given", "the Orders API is running",
                          "When", "I create an order with DiagramFocus on status and totalAmount",
                          "Then", "the response fields status and totalAmount are highlighted"))
            ]}
        };
        var diagrams = new[] { new DiagramAsCode("focus-1", "", plantUmlSource) };
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow,
            null, Path.Combine(_tempDir, "Focus.html"),
            "DiagramFocus Demo", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs, showStepNumbers: true);

        OpenUrl(new Uri(path).AbsoluteUri);
        WaitFor(By.CssSelector("details.feature"));
        ExpandAll(); Pause(300);

        WaitForDiagramSvg(30);

        // Set Details to "Expanded" so all notes/payloads are fully visible
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "if (window._setReportDetails) window._setReportDetails('expanded');");
        Pause(500);

        // Scroll to show the response note at the bottom with highlighted fields
        var diagramContainer = _driver.FindElement(By.CssSelector(".plantuml-browser"));
        ((IJavaScriptExecutor)_driver).ExecuteScript(@"
            var el = arguments[0];
            var svg = el.querySelector('svg');
            if (svg) {
                var svgRect = svg.getBoundingClientRect();
                // Scroll to show the bottom half of the diagram where the response note lives
                var targetY = window.scrollY + svgRect.top + svgRect.height - window.innerHeight + 40;
                window.scrollTo(0, Math.max(0, targetY));
            } else {
                el.scrollIntoView({block:'end'});
            }
        ", diagramContainer);
        Pause(500);

        SaveScreenshot("whats-new-diagram-focus.png");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 13: Failure Diagnostics GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature13_Failure_Diagnostics_GIF()
    {
        BeginFrameCapture("feature13");
        NavigateToReport();

        InjectFakeCursor();

        // Scroll directly to the failure clustering section and expand it
        try
        {
            var clusterSection = _driver.FindElement(By.CssSelector(".failure-clusters"));
            ScrollTo(clusterSection); Hold(1);

            // Expand the failure cluster via JS setAttribute (click on <details> unreliable in headless)
            var clusterDetails = _driver.FindElements(By.CssSelector(".failure-cluster"));
            if (clusterDetails.Count > 0)
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].setAttribute('open', '');", clusterDetails[0]);
                Pause(300);
                Hold(3);

                // Click a scenario link within the cluster to jump to it
                try
                {
                    var links = _driver.FindElements(By.CssSelector(".failure-cluster-scenario-link"));
                    if (links.Count > 0)
                    {
                        JsClick(links[0]); Hold(3);
                    }
                }
                catch { }
            }
        }
        catch { }

        // Show the expanded failure details with error message
        try
        {
            var errorArea = _driver.FindElement(By.CssSelector(".failure-result"));
            ScrollTo(errorArea); Hold(3);
        }
        catch { }

        // Click "Failed" status filter to show only failures
        ScrollToTop(); Hold(1);
        var failedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Failed']"));
        JsClick(failedBtn); Hold(3);

        // Show the Feature Summary with red rows
        try
        {
            var featureSummary = _driver.FindElement(By.CssSelector(".features-summary-details"));
            JsClick(featureSummary); Hold(2.5);
        }
        catch { }

        // Click "Next Failure" button to cycle through failures
        var nextFailBtn = _driver.FindElement(By.CssSelector(".jump-to-failure"));
        JsClick(nextFailBtn); Hold(3);

        // Show the error details for this failure
        ExpandAll(); Hold(1);
        try
        {
            var errorArea = _driver.FindElement(By.CssSelector(".failure-result pre, .failure-result"));
            ScrollTo(errorArea); Hold(3);
        }
        catch { }

        // Click Next Failure to go to another failure
        JsClick(nextFailBtn); Hold(3);

        // Scroll back to top for overview
        ScrollToTop(); Hold(3);

        StitchGif("whats-new-feature13-failures.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 14: Category Filtering GIF
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Feature14_Category_Filter_GIF()
    {
        BeginFrameCapture("feature14");
        NavigateToReport();

        ScrollToTop(); Hold(1);

        // Click "Smoke" category
        var smokeBtn = _driver.FindElement(By.CssSelector(".category-toggle[data-category='Smoke']"));
        JsClick(smokeBtn); Hold(2);

        // Scroll down to see filtered results
        var features = _driver.FindElement(By.CssSelector("details.feature"));
        ScrollTo(features); Hold(1.5);
        ScrollToTop(); Hold(0.5);

        // Toggle OR → AND mode
        try
        {
            var modeBtn = _driver.FindElement(By.CssSelector(".cat-mode-toggle"));
            JsClick(modeBtn); Hold(1);
        }
        catch { }

        // Add another category
        try
        {
            var ordersBtn = _driver.FindElement(By.CssSelector(".category-toggle[data-category='Orders']"));
            JsClick(ordersBtn); Hold(2);
        }
        catch { }

        // Clear
        var clearBtn = _driver.FindElement(By.CssSelector(".export-btn[onclick*='clear_all']"));
        JsClick(clearBtn); Hold(1);

        ScrollToTop(); Hold(1);
        StitchGif("whats-new-feature14-categories.gif");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FEATURE 17: Framework Support (3 screenshots)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void Feature19_Scenario_Timeline_Screenshot()
    {
        // Generate report with 20+ rich data (already have 45+ scenarios in our dataset)
        NavigateToReport();

        // Click "Scenario Timeline" button
        var timelineBtn = _driver.FindElement(By.CssSelector(".timeline-toggle"));
        JsClick(timelineBtn);
        Pause(500);

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

    [Fact]
    public void Feature20_Export_GIF()
    {
        BeginFrameCapture("feature20");
        NavigateToReport();

        // Apply a filter first (Failed)
        var failedBtn = _driver.FindElement(By.CssSelector(".status-toggle[data-status='Failed']"));
        JsClick(failedBtn); Hold(1.5);

        // Click "Export Filtered HTML"
        var exportHtmlBtn = _driver.FindElement(By.CssSelector(".export-btn[onclick*='export_html']"));
        JsClick(exportHtmlBtn); Hold(2);

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
