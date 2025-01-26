
const toHexString = (bytes) => {
  return Array.from(bytes, (byte) => {
    return ('0' + (byte & 0xff).toString(16)).slice(-2);
  }).join('');
};

class Queue {
    constructor() {
        this.first = null;
        this.list = null;
        this.count = 0;
    }
    enqueue(item) {
        let entry = {};
        entry.item = item;
        entry.next = null;
        if (this.last == null) {
            this.last = entry;
            this.first = entry;
        } else {
            this.last.next = entry;
            this.last = entry;
        }
        this.count++;
    }
    dequeue() {
        if (this.first == null) {
            return null;
        } else {
            let result = this.first;
            this.first = result.next;
            if (this.last == result) {
                this.last = null;
            }
            return result.item;
        }
    }
    count() {
        return this.count;
    }
    any() {
        return this.first != null;
    }
}

function process(response, tasks) {
    let t = tasks.dequeue();
    let start = new Uint8Array(t.start);
    let startString = toHexString(start);
    let targetString = toHexString(new Uint8Array(t.target));
    crypto.subtle.digest("SHA-256", start).then((middle) => {
        crypto.subtle.digest("SHA-256", middle).then((ende) => {
            let endeString = toHexString(new Uint8Array(ende));
            let success = (targetString == endeString);
            if (success) {
                response.success = true;
                response.middle = toHexString(new Uint8Array(middle));
            }
            response.done++;
            if (success || (!tasks.any())) {
                postMessage(response);
            } else {
                process(response, tasks);
            }
        });
    });
}

onmessage = (e) => {
    if (e.data.type == "throttle_hash_request") {
        let response = {};
        response.type = "throttle_hash_result";
        response.name = e.data.name;
        response.counter = e.data.counter;
        response.done = 0;
        response.success = false;
        let tasks = new Queue();
        e.data.tasks.forEach((t) => {
            tasks.enqueue(t);
        });
        if (tasks.any()) {
            process(response, tasks);
        }
    }
};

