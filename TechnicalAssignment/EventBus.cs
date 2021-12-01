using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TechnicalAssignment {
    public class EventBusEvtArgs : EventArgs {
        public EventBusEvtArgs(JsonObject evt) { this.evt = evt; }
        public JsonObject evt { get; set; }
    }

    class EventBus {
        private Dictionary<string, List<Action<JsonObject>>> subscriptions = new Dictionary<string, List<Action<JsonObject>>>();
        private Dictionary<string, Dictionary<string, List<Action<JsonObject>>>> namespaceSubscriptions = new Dictionary<string, Dictionary<string, List<Action<JsonObject>>>>();

        private Action<JsonObject> fnRelayRemote = null;

        public EventBus(Action<JsonObject> fnRelayRemote) {
            this.fnRelayRemote = fnRelayRemote;
        }

        
        public EventBus(Dispatcher dispatcher, Action<JsonObject> fnRelayRemote) {
            this.fnRelayRemote = (JsonObject evt) => {
                dispatcher.Invoke(fnRelayRemote, evt);
            };
        }

        public void subscribe(string eventId, Action<JsonObject> handler, string ns = "") {
            if (!this.subscriptions.ContainsKey(eventId)) { this.subscriptions[eventId] = new List<Action<JsonObject>>(); }
            this.subscriptions[eventId].Add(handler);

            if (ns.Length > 0) {
                if (!this.namespaceSubscriptions.ContainsKey(ns)) { this.namespaceSubscriptions[ns] = new Dictionary<string, List<Action<JsonObject>>>(); }
                if (!this.namespaceSubscriptions[ns].ContainsKey(eventId)) { this.namespaceSubscriptions[ns][eventId] = new List<Action<JsonObject>>(); }
                this.namespaceSubscriptions[ns][eventId].Add(handler);
            }
        }

        public void subscribe(string eventId, DispatcherObject executeTarget, Action<JsonObject> fnHandler, string ns = "") {
            this.subscribe(eventId, executeTarget.Dispatcher, fnHandler, ns);
        }

        public void subscribe(string eventId, Dispatcher dispatcher, Action<JsonObject> fnHandler, string ns = "") {
            this.subscribe(eventId, (JsonObject evt) => {
                dispatcher.Invoke(fnHandler, evt);
            }, ns);
        }

        public bool unsubscribe(string eventId, Action<JsonObject> handler) {
            if (!this.subscriptions.ContainsKey(eventId)) { return false; }

            try { this.subscriptions[eventId].Remove(handler); }
            catch (Exception /*ex*/) { return false; }

            foreach (var ns in this.namespaceSubscriptions) {
                if (!ns.Value.ContainsKey(eventId)) { continue; }

                try { ns.Value[eventId].Remove(handler); }
                catch (Exception /*ex*/) { }
            }

            return true;
        }

        public void unsubscribeNamespaceEvent(string ns, string eventId) {
            if (!this.namespaceSubscriptions.ContainsKey(ns)) { return; }
            if (!this.namespaceSubscriptions[ns].ContainsKey(eventId)) { return; }

            for (var idx = 0; idx < this.namespaceSubscriptions[ns][eventId].Count; ++idx) {
                this.unsubscribe(eventId, this.namespaceSubscriptions[ns][eventId][idx]);
            }
        }

        public void unsubscribeNamespace(string ns) {
            if (!this.namespaceSubscriptions.ContainsKey(ns)) { return; }

            foreach (var eventId in this.namespaceSubscriptions[ns]) {
                this.unsubscribeNamespaceEvent(ns, eventId.Key);
            }
        }

        public void send(string eventId, JsonNode data = null, bool local = false) {
            JsonObject evtRoot = new JsonObject();
            evtRoot["id"] = eventId;
            evtRoot["data"] = data;
            this.send(evtRoot, local);
        }

        public void send(JsonNode evt, bool local = false) {
            JsonObject evtObj = null;
            try { evtObj = evt.AsObject(); }
            catch { return; }

            if(!evtObj.ContainsKey("id")) { return; }
            if(!local) { Task.Run(() => this.fnRelayRemote.Invoke(evtObj)); }

            // Send to explicit subscriptions
            string eventId = (string)evtObj["id"];
            if (this.subscriptions.ContainsKey(eventId)) {
                foreach(var handler in this.subscriptions[eventId]) {
                    Task.Run(() => handler.Invoke(evtObj));
                }
            }

            // Send to wildcard subscriptions
            if (this.subscriptions.ContainsKey("*")) {
                foreach (var handler in this.subscriptions["*"]) {
                    Task.Run(() => handler.Invoke(evtObj));
                }
            }
        }
    }
}
