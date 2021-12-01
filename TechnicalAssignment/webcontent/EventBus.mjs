//======================================================================================================================

class EventBus {
    //------------------------------------------------------------------------------------------------------------------

    constructor(fnRelayRemote) {
        this.relayRemote = fnRelayRemote;
        this.subscriptions = {};
        this.namespaceSubscriptions = {};
    }

    //------------------------------------------------------------------------------------------------------------------

    subscribe(eventId, fnHandler, namespace) {
        if(!Array.isArray(this.subscriptions[eventId])) { this.subscriptions[eventId] = []; }
        this.subscriptions[eventId].push(fnHandler);

        if(typeof namespace == 'string' && namespace.length > 0) {
            if(typeof this.namespaceSubscriptions[namespace] !== 'object') { this.namespaceSubscriptions[namespace] = {}; }

            if(!Array.isArray(this.namespaceSubscriptions[namespace][eventId])) { this.namespaceSubscriptions[namespace][eventId] = []; }
            this.namespaceSubscriptions[namespace][eventId].push(fnHandler);
        }
    }

    //------------------------------------------------------------------------------------------------------------------

    unsubscribe(eventId, fnHandler) {
        if(!Array.isArray(this.subscriptions[eventId])) { return false; }

        var pos = this.subscriptions[eventId].indexOf(fnHandler);
        if (pos <= -1) { return false; }

        this.subscriptions[eventId].splice(pos, 1);

        // Also delete namespace hints
        for(var namespace in this.namespaceSubscriptions) {
            if(typeof this.namespaceSubscriptions[namespace] !== 'object') { continue; }
            if(!Array.isArray(this.namespaceSubscriptions[namespace][eventId])) { continue; }

            var nsPos = this.namespaceSubscriptions[namespace][eventId].indexOf(fnHandler);
            if (nsPos <= -1) { continue; }

            this.namespaceSubscriptions[namespace][eventId].splice(nsPos, 1);
        }

        return true;
    }

    //------------------------------------------------------------------------------------------------------------------

    unsubscribeNamespaceEvent(namespace, eventId) {
        if(typeof namespace !== 'string' || typeof eventId !== 'string') { return false; }
        if(!Array.isArray(this.subscriptions[eventId])) { return false; }
        if(!Array.isArray(this.namespaceSubscriptions[namespace][eventId])) { return false; }

        for(let fnHandler of this.namespaceSubscriptions[namespace][eventId]) {
            this.unsubscribe(eventId, fnHandler);
        }
    }

    //------------------------------------------------------------------------------------------------------------------

    unsubscribeNamespace(namespace) {
        if(typeof this.namespaceSubscriptions[namespace] !== 'object') { return false; }

        for(var subscriptionEventId in this.namespaceSubscriptions[namespace]) {
            this.unsubscribeNamespaceEvent(namespace, subscriptionEventId);
        }
    }

    //------------------------------------------------------------------------------------------------------------------

    send(eventId, data = null, local = false) {
        this.relay({ id: eventId, data: data }, local);
    }

    //------------------------------------------------------------------------------------------------------------------

    relay(event, local = false) {
        // Send to remote
        if(!local) { this.relayRemote(event); }

        // Send to explicit subscriptions
        if(Array.isArray(this.subscriptions[event.id])) {
            for(var idx = 0; idx < this.subscriptions[event.id].length; ++idx) {
                try {
                    new Promise((reject,resolve) => { this.subscriptions[event.id][idx](event); });
                    
                }
                catch(e) {
                    this.relay({id: 'error', data: 'Subscription ' + event.id + ' threw error (' + e + ')'}, false);
                }
            }
        }

        // Send to wildcard subscriptions
        if(Array.isArray(this.subscriptions['*'])) {
            for(var idx = 0; idx < this.subscriptions['*'].length; ++idx) {
                try {
                    new Promise((reject,resolve) => { this.subscriptions['*'][idx](event); });
                }
                catch(e) {
                    this.relay({id: 'error', data: 'Subscription ' + event.id + ' threw error (' + e + ')'}, false);
                }
            }
        }
    }

    //------------------------------------------------------------------------------------------------------------------
};

//======================================================================================================================

export { EventBus };

//======================================================================================================================
