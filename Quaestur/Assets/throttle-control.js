
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
            //console.log(this.current.toString(16) + "/" + this.max.toString(16) + " => " + hexValue);
            return hexToBytes(hexValue);
        }
	}
}

async function post(url, data, callback) {
    const xhr = new XMLHttpRequest();
    xhr.open("POST", url, true);
    xhr.onreadystatechange = () => {
        if (xhr.readyState === XMLHttpRequest.DONE && xhr.status === 200) {
            callback(xhr.response, xhr.status);
        }
    };
    xhr.send(data);
}

async function sendTask(worker, name)
{
    let request = {};
    request.type = "throttle_hash_request";
    request.name = name;
    request.counter = self.currentRound.counter;
    request.tasks = new Array();
    for (let i = 0; i < 16; i++)
    {
        let value = self.gen.next();
        if (value != null) {
            let task = {};
            task.start = hexToBytes(self.currentRound.prefix).concat(value);
            task.target = hexToBytes(self.currentRound.target);
            request.tasks.push(task);
        } else {
            break;
        }
    }
    if (request.tasks.length > 0) {
        console.log("snd " + request.name + " ctr=" + request.counter + " tsk=" + request.tasks.length);
        worker.postMessage(request);
    } else {
        console.log("nmt " + request.name)
    }
}

async function sendUpdate()
{
    let update = {};
    update.type = "throttle_process_update";
    update.done = self.done;
    postMessage(update);
}

async function handleMessage(e) {
    if (e.data.type == "throttle_hash_result") {
        console.log("rcv " + e.data.name + " ctr=" + e.data.counter + " dne=" + e.data.done + " suc=" + e.data.success);
        if (e.data.counter = self.currentRound.counter) {
            self.done += e.data.done;
            if ((self.done % 256) == 0) {
                sendUpdate();
            }
            if (e.data.success) {
                self.round++;
                self.done = self.round * (1 << self.currentRound.bitlength);
                sendUpdate();
                self.currentRound.solvedMiddle = e.data.middle;
                post("/throttle", JSON.stringify(self.currentRound), function(throttleDataString, status) {
                    self.currentRound = JSON.parse(throttleDataString);
                    if ('counter' in self.currentRound) {
                        self.gen = new Generator(self.currentRound.bitlength);
                        self.workers.forEach((w) => {
                            sendTask(w.worker, w.name);
                        });
                    } else {
                        self.workers.forEach((w) => w.worker.terminate());
                        self.workers = null;
                        self.currentRound.type = "throttle_process_result";
                        postMessage(self.currentRound);
                        return;
                    }
                });
            }
        }
        sendTask(e.target, e.data.name);
    }
}

async function start(firstRound) {
    self.currentRound = firstRound;
    self.gen = new Generator(self.currentRound.bitlength);
    self.workers = new Array();
	self.done = 0;
    self.round = 0;
	for (i = 0; i < 4; i++) {
		let worker = {};
        worker.name = "w" + i.toString();
        worker.worker = new Worker("/Assets/throttle-hash.js");
        worker.worker.onmessage = handleMessage;
        self.workers.push(worker);
        sendTask(worker.worker, worker.name);
	}
}

onmessage = (e) => {
    if (e.data.type == "throttle_start_request") {
        start(e.data);
    }
};

