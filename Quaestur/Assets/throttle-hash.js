
const toHexString = (bytes) => {
  return Array.from(bytes, (byte) => {
    return ('0' + (byte & 0xff).toString(16)).slice(-2);
  }).join('');
};

onmessage = (e) => {
    if (e.data.type == "throttle_hash_request") {
        console.log("req " + e.data.name + " ctr=" + e.data.counter + " tsk=" + e.data.tasks.length);
        self.response = {};
        self.response.type = "throttle_hash_result";
        self.response.name = e.data.name;
        self.response.counter = e.data.counter;
        self.response.done = 0;
        self.response.success = false;
        self.taskcount = e.data.tasks.length;
        e.data.tasks.forEach((t) => {
	        let start = new Uint8Array(t.start);
	        let startString = toHexString(start);
    	    let targetString = toHexString(new Uint8Array(t.target));
            if (self.response != null) {
	            crypto.subtle.digest("SHA-256", start).then((middle) => {
                    if (self.response != null) {
		                crypto.subtle.digest("SHA-256", middle).then((ende) => {
                            if (self.response != null) {
		        	            let endeString = toHexString(new Uint8Array(ende));
	        		            let success = (targetString == endeString);
                                if (success) {
                                    self.response.success = true;
                                    self.response.middle = toHexString(new Uint8Array(middle));
                                }
                                self.response.done++;
                                if (success || (self.response.done == self.taskcount)) {
                                    self.response.done = self.taskcount;
                                    console.log("rsp " + self.response.name + " ctr=" + self.response.counter + " dne=" + self.response.done + " suc=" + self.response.success);
    			                    postMessage(self.response);
                                    self.response = null;
                                }
                            }
		                });
                    }
	            });
            }
        });
    }
};

