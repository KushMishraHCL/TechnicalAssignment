import { EventBus } from './EventBus.mjs'

globalThis.sendToBus = (event) => {
    if (globalThis.backendSend) {
        globalThis.backendSend(event);
    }
};

globalThis.bus = new EventBus((event) => {
    window.chrome.webview.postMessage(JSON.stringify(event));
});

window.onload = async function () {
    window.chrome.webview.addEventListener('message', (backendEvent) => {
        globalThis.bus.relay(JSON.parse(backendEvent.data), true);
    });

    var app = new Vue({
        el: '#app',
        data: {
            events: []
        },

        mounted() {
            bus.subscribe('backend_test', (e) => {
                this.events.push(e);
            });
        },

        methods: {
            sendTestEvent() {
                bus.send('frontend_test', 'Hello from Frontend');
            }
        }
    })
};