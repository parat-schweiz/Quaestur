
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

function process() {
    let t = self.tasks.dequeue();
    let start = new Uint8Array(t.start);
    let startString = toHexString(start);
    let targetString = toHexString(new Uint8Array(t.target));
    console.log("prc " + self.response.name + " strt=" + startString + " tgt=" + targetString);
    crypto.subtle.digest("SHA-256", start).then((middle) => {
        crypto.subtle.digest("SHA-256", middle).then((ende) => {
            let endeString = toHexString(new Uint8Array(ende));
            let success = (targetString == endeString);
            if (success) {
                console.warn("SUCCESS!!!!!!");
                self.response.success = true;
                self.response.middle = toHexString(new Uint8Array(middle));
            }
            self.response.done++;
            if (success || (!self.tasks.any())) {
                console.log("rsp " + self.response.name + " ctr=" + self.response.counter + " dne=" + self.response.done + " suc=" + self.response.success);
                postMessage(self.response);
                self.tasks = null;
                self.response = null;
            } else {
                process();
            }
        });
    });
}

onmessage = (e) => {
    if (e.data.type == "throttle_hash_request") {
        console.log("req " + e.data.name + " ctr=" + e.data.counter + " tsk=" + e.data.tasks.length);
        self.response = {};
        self.response.type = "throttle_hash_result";
        self.response.name = e.data.name;
        self.response.counter = e.data.counter;
        self.response.done = 0;
        self.response.success = false;
        self.tasks = new Queue();
        e.data.tasks.forEach((t) => {
            self.tasks.enqueue(t);
        });
        if (self.tasks.any()) {
            process();
        }
    }
};

