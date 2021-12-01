using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Windows;

namespace TechnicalAssignment {
    public partial class MainWindow : Window {
        private EventBus bus;
        
        public MainWindow() {
            InitializeComponent();
            InitializeAsync();

            bus = new EventBus(this.Dispatcher, evt => {
                if (!this.webView.IsInitialized || this.webView.CoreWebView2 == null) { return; }
                this.webView.CoreWebView2.PostWebMessageAsString(evt.ToJsonString());
            });

            //JsonObject main = new JsonObject();
            //main["test"] = "Rofl";
            //main["test2"] = 2;

            //JsonArray testArr = new JsonArray();
            //testArr.Add("Hehe");
            //testArr.Add("who");
            //testArr.Add("dis");

            //main["test3"] = testArr;

            //string result = main.ToJsonString();

            //System.Diagnostics.Debug.WriteLine(result);

            //JsonObject resultObj = JsonObject.Parse(result).AsObject();

            //int parseValue = (int)resultObj["test2"];

            //System.Diagnostics.Debug.WriteLine("Found test value: " + parseValue);

            //// Test normal unsubscribe
            //Action<JsonObject> fnTest1Handler = evt => {
            //    System.Diagnostics.Debug.WriteLine("test1 sub called: " + evt.ToJsonString());
            //};

            //bus.subscribe("test1", fnTest1Handler);
            //bus.unsubscribe("test1", fnTest1Handler);

            //// Test namespaced unsubscribe
            //this.bus.subscribe("test1", evt => {
            //    System.Diagnostics.Debug.WriteLine("test1 sub called: " + evt.ToJsonString());
            //}, "testNS");
            //this.bus.unsubscribeNamespace("testNS");

            this.bus.subscribe("*", this, evt => { // Wildcard means all IDs
                Debug.WriteLine("[*] " + evt.ToJsonString());
            });

            this.bus.send(new JsonObject {
                { "id", "test1" },
                { "data", new JsonObject {
                    { "prop1", "hello" },
                    { "prop2", new JsonArray {
                        1, 2, 3, 4
                    }}
                } }
            });
        }

        async void InitializeAsync() {
            try {
                await webView.EnsureCoreWebView2Async();
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine("ERROR! " + ex.Message);
            }
        }

        private void webView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e) {
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping("TechnicalAssignment.webcontent", "webcontent", CoreWebView2HostResourceAccessKind.Allow);
            webView.Source = new Uri("https://TechnicalAssignment.webcontent/index.html");
        }

        private void webView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e) {
            try {
                this.bus.send(JsonObject.Parse(e.TryGetWebMessageAsString()), true);
            }
            catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine("Got invalid event from frontend: " + e.TryGetWebMessageAsString() + " (" + ex.Message + ")");
            }            
        }

        private void testButton_Click(object sender, RoutedEventArgs e) {
            this.bus.send("backend_test", "Hello from Backend");
        }
    }
}
