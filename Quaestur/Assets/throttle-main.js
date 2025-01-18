
function hexToBytes(hex) {
    let bytes = [];
    for (let c = 0; c < hex.length; c += 2)
        bytes.push(parseInt(hex.substr(c, 2), 16));
    return bytes;
}

function throttle(task, update, finished) {
    this.worker = new Worker("/Assets/throttle-control.js");
    worker.onmessage = (e) => {
        switch (e.data.type) {
            case "throttle_process_update":
                update(e.data.done);
                break;
            case "throttle_process_result":
                this.worker.terminate();
                this.worker = null;
                finished(e.data);
                break;
        }
    };
    task.type = "throttle_start_request";
    this.worker.postMessage(task);
}