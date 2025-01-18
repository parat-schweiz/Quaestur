
function hexToBytes(hex) {
    let bytes = [];
    for (let c = 0; c < hex.length; c += 2)
        bytes.push(parseInt(hex.substr(c, 2), 16));
    return bytes;
}

class Generator {
	constructor(bitlength) {
		this.current = -1n;
        this.max = (1n << BigInt(bitlength)) - 1n;
        this.bytelength = Math.ceil(bitlength / 8);
	}
	next() {
        this.current++;
        if (this.current > this.max) {
            return null;
        } else {
            let hexValue = this.current.toString(16);
            while (hexValue.length < (this.bytelength * 2)) {
                hexValue = "0" + hexValue;
            }
            return hexToBytes(hexValue);
        }
	}
}

async function post(that, url, data, callback) {
    const xhr = new XMLHttpRequest();
    xhr.open("POST", url, true);
    xhr.onreadystatechange = () => {
        if (xhr.readyState === XMLHttpRequest.DONE && xhr.status === 200) {
            callback(that, xhr.response, xhr.status);
        }
    };
    xhr.send(data);
}

class Control {
    constructor(firstRound) {
        this.currentRound = firstRound;
    }
    sendTask(worker, name) {
        let request = {};
        request.type = "throttle_hash_request";
        request.name = name;
        request.counter = this.currentRound.counter;
        request.tasks = new Array();
        for (let i = 0; i < 16; i++)
        {
            let value = this.gen.next();
            if (value != null) {
                let task = {};
                task.start = hexToBytes(this.currentRound.prefix).concat(value);
                task.target = hexToBytes(this.currentRound.target);
                request.tasks.push(task);
            } else {
                break;
            }
        }
        if (request.tasks.length > 0) {
            worker.postMessage(request);
        }
    }
    sendUpdate() {
        let update = {};
        update.type = "throttle_process_update";
        update.done = this.done;
        postMessage(update);
    }
    next(throttleDataString, status) {
        this.currentRound = JSON.parse(throttleDataString);
        if ('counter' in this.currentRound) {
            this.gen = new Generator(this.currentRound.bitlength);
            this.workers.forEach((w) => {
                this.sendTask(w.worker, w.name);
            });
        } else {
            this.workers.forEach((w) => { 
                w.worker.terminate();
                w.worker = null;
            });
            this.workers = null;
            this.currentRound.type = "throttle_process_result";
            postMessage(this.currentRound);
            self.control = null;
        }
    }
    handleMessage(e) {
        if (e.data.type == "throttle_hash_result") {
            if (e.data.counter = this.currentRound.counter) {
                this.done += e.data.done;
                if ((this.done % 256) == 0) {
                    this.sendUpdate();
                }
                if (e.data.success) {
                    this.round++;
                    this.done = this.round * (1 << this.currentRound.bitlength);
                    this.sendUpdate();
                    this.currentRound.solvedMiddle = e.data.middle;
                    post(this, "/throttle", JSON.stringify(this.currentRound), (that, throttleDataString, status) => {
                        that.next(throttleDataString, status);
                    });
                }
            }
            this.sendTask(e.target, e.data.name);
        }
    }
    start() {
        this.gen = new Generator(this.currentRound.bitlength);
        this.workers = new Array();
	    this.done = 0;
        this.round = 0;
	    for (let i = 0; i < 4; i++) {
		    let worker = {};
            worker.name = "w" + i.toString();
            worker.worker = new Worker("/Assets/throttle-hash.js");
            worker.worker.onmessage = (e) => {
                this.handleMessage(e);
            };
            this.workers.push(worker);
            this.sendTask(worker.worker, worker.name);
	    }
    }
}

function start(firstRound) {
    if (self.control == null) {
        self.control = new Control(firstRound);
        self.control.start();
    } else {
        setTimeout(() => start(firstRound), 100);
    }
}

onmessage = (e) => {
    if (e.data.type == "throttle_start_request") {
        start(e.data);
    }
};

