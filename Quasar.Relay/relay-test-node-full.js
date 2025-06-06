const WebSocket = require('ws');
const crypto = require('crypto');
const readline = require('readline');

// Configuration
const role = process.argv[2] || 'client'; // 'server' or 'client'
const targetId = process.argv[3] || ''; // Target device ID to connect to

const config = {
    serverUrl: 'wss://relay.nextcloudcyber.com',
    deviceId: role === 'server' ? 'test-server-node' : 'test-client-node',
    password: 'test-password',
    deviceName: role === 'server' ? 'Node.js Test Server' : 'Node.js Test Client',
    useEncryption: false, // Set to true to enable encryption
    version: '2.0.0',
    features: ['relay'], // Supported features
    targetDeviceId: targetId, // Target device to connect to
    role: role // Role of this instance (server or client)
};

// Message types enum - include both string values (v2) and numeric values (original protocol)
const MessageType = {
    // String values for v2 protocol
    Register: "register",
    RegisterResponse: "register_response",
    Connect: "connect",
    ConnectResponse: "connect_response",
    ConnectionRequest: "connection_request",
    Offer: "offer",
    Answer: "answer",
    IceCandidate: "ice_candidate",
    Heartbeat: "heartbeat",
    HeartbeatResponse: "heartbeat_response",
    Error: "error",
    Welcome: "welcome",
    // Numeric values from C# RelayMessageType enum
    RegisterNumeric: 0,
    RegisterResponseNumeric: 1,
    ConnectNumeric: 2,
    ConnectResponseNumeric: 3,
    ConnectionRequestNumeric: 4,
    OfferNumeric: 5,
    AnswerNumeric: 6,
    IceCandidateNumeric: 7,
    HeartbeatNumeric: 8,
    HeartbeatAcknowledgeNumeric: 9,
    ErrorNumeric: 10,
    WelcomeNumeric: 11
};

// Device types enum - using string values like the HTML test client
const DeviceType = {
    Server: "server",
    Desktop: "desktop",
    Mobile: "mobile", 
    Client: "client"
};

// Generate random UUID for message IDs (v2 protocol format)
function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function log(...args) {
    const timestamp = new Date().toLocaleTimeString();
    const rolePrefix = config.role ? `[${config.role.toUpperCase()}] ` : '';
    console.log(`[${timestamp}] ${rolePrefix}`, ...args);
}

// Handles encryption of message data using AES-GCM
function encrypt(plaintext) {
    if (!config.useEncryption) {
        return plaintext;
    }

    try {
        // Generate a random IV (initialization vector)
        const iv = crypto.randomBytes(16); 
        
        // Use a placeholder key derivation for demo purposes
        // In production, would use a secure key derivation method
        const key = crypto.createHash('sha256').update(config.password).digest();
        
        // Create cipher and encrypt
        const cipher = crypto.createCipheriv('aes-256-gcm', key, iv);
        let encrypted = cipher.update(plaintext, 'utf8', 'base64');
        encrypted += cipher.final('base64');
        const authTag = cipher.getAuthTag().toString('base64');
        
        // Return encrypted data with IV and authTag
        return {
            iv: iv.toString('base64'),
            data: encrypted,
            tag: authTag
        };
    } catch (error) {
        log('Encryption error:', error.message);
        return null;
    }
}

// Handles decryption of message data using AES-GCM
function decrypt(encryptedData) {
    if (!config.useEncryption) {
        return encryptedData;
    }

    try {
        const { iv, data, tag } = encryptedData;
        
        // Use the same key derivation as encryption
        const key = crypto.createHash('sha256').update(config.password).digest();
        
        // Create decipher
        const decipher = crypto.createDecipheriv(
            'aes-256-gcm', 
            key, 
            Buffer.from(iv, 'base64')
        );
        
        decipher.setAuthTag(Buffer.from(tag, 'base64'));
        
        // Decrypt
        let decrypted = decipher.update(data, 'base64', 'utf8');
        decrypted += decipher.final('utf8');
        
        return decrypted;
    } catch (error) {
        log('Decryption error:', error.message);
        return null;
    }
}

// Send a message with proper formatting and optional encryption
function sendMessage(socket, message) {
    try {
        const jsonString = JSON.stringify(message);
        log(`Sending: ${jsonString}`);
        
        if (socket.encryptionRequired && config.useEncryption) {
            const encryptedPayload = encrypt(jsonString);
            if (encryptedPayload) {
                const wrapper = { encrypted: true, data: encryptedPayload };
                const binaryData = Buffer.from(JSON.stringify(wrapper), 'utf8');
                socket.send(binaryData);
                log('Sent encrypted message (binary)');
            } else {
                log('Failed to encrypt message');
            }
        } else {
            const binaryData = Buffer.from(jsonString, 'utf8');
            socket.send(binaryData);
            log('Sent message (binary)');
        }
    } catch (error) {
        log('Error sending message:', error.message);
    }
}

// Relay functionality methods
function sendConnectionRequest(socket, targetId) {
    if (!targetId) {
        log('Error: Target device ID is required');
        return;
    }
    
    log(`Sending connection request to device: ${targetId}`);
    
    const connectionRequest = {
        type: 'connection_request',
        id: generateUUID(),
        deviceId: config.deviceId,
        targetId: targetId,
        timestamp: Date.now()
    };
    
    sendMessage(socket, connectionRequest);
}

function sendOffer(socket, targetId, offerSdp) {
    if (!targetId || !offerSdp) {
        log('Error: Target device ID and offer SDP are required');
        return;
    }
    
    log(`Sending WebRTC offer to device: ${targetId}`);
    
    const offer = {
        type: 'offer',
        id: generateUUID(),
        deviceId: config.deviceId,
        targetId: targetId,
        sdp: offerSdp,
        timestamp: Date.now()
    };
    
    sendMessage(socket, offer);
}

function sendAnswer(socket, targetId, answerSdp) {
    if (!targetId || !answerSdp) {
        log('Error: Target device ID and answer SDP are required');
        return;
    }
    
    log(`Sending WebRTC answer to device: ${targetId}`);
    
    const answer = {
        type: 'answer',
        id: generateUUID(),
        deviceId: config.deviceId,
        targetId: targetId,
        sdp: answerSdp,
        timestamp: Date.now()
    };
    
    sendMessage(socket, answer);
}

function sendIceCandidate(socket, targetId, candidate) {
    if (!targetId || !candidate) {
        log('Error: Target device ID and ICE candidate are required');
        return;
    }
    
    log(`Sending ICE candidate to device: ${targetId}`);
    
    const iceCandidate = {
        type: 'ice_candidate',
        id: generateUUID(),
        deviceId: config.deviceId,
        targetId: targetId,
        candidate: candidate,
        timestamp: Date.now()
    };
    
    sendMessage(socket, iceCandidate);
}

// Interactive command handling
const commandMap = {
    'q': { desc: 'Quit', fn: () => process.exit(0) },
    'c': { desc: 'Send connection request', fn: (socket, args) => {
        const targetId = args[0] || config.targetDeviceId;
        if (!targetId) {
            log('Please specify a target device ID');
            return;
        }
        sendConnectionRequest(socket, targetId);
    }},
    'o': { desc: 'Send WebRTC offer (dummy SDP for testing)', fn: (socket, args) => {
        const targetId = args[0] || config.targetDeviceId;
        if (!targetId) {
            log('Please specify a target device ID');
            return;
        }
        const dummySdp = 'v=0\r\no=- 1234567890 1 IN IP4 127.0.0.1\r\ns=-\r\nt=0 0\r\na=group:BUNDLE 0\r\nm=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\nc=IN IP4 0.0.0.0\r\na=ice-ufrag:dummy\r\na=ice-pwd:dummy';
        sendOffer(socket, targetId, dummySdp);
    }},
    'a': { desc: 'Send WebRTC answer (dummy SDP for testing)', fn: (socket, args) => {
        const targetId = args[0] || config.targetDeviceId;
        if (!targetId) {
            log('Please specify a target device ID');
            return;
        }
        const dummySdp = 'v=0\r\no=- 9876543210 1 IN IP4 127.0.0.1\r\ns=-\r\nt=0 0\r\na=group:BUNDLE 0\r\nm=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\nc=IN IP4 0.0.0.0\r\na=ice-ufrag:dummy-answer\r\na=ice-pwd:dummy-answer';
        sendAnswer(socket, targetId, dummySdp);
    }},
    'i': { desc: 'Send ICE candidate (dummy for testing)', fn: (socket, args) => {
        const targetId = args[0] || config.targetDeviceId;
        if (!targetId) {
            log('Please specify a target device ID');
            return;
        }
        const dummyCandidate = {
            candidate: 'candidate:1 1 UDP 2122194687 192.168.1.100 12345 typ host',
            sdpMid: '0',
            sdpMLineIndex: 0
        };
        sendIceCandidate(socket, targetId, dummyCandidate);
    }},
    'h': { desc: 'Display help', fn: () => {
        log('Available commands:');
        for (const [cmd, info] of Object.entries(commandMap)) {
            log(` ${cmd} - ${info.desc}`);
        }
    }}
};

function setupCommandInterface(socket) {
    const rl = readline.createInterface({
        input: process.stdin,
        output: process.stdout
    });
    
    rl.on('line', (input) => {
        const parts = input.trim().split(' ');
        const command = parts[0];
        const args = parts.slice(1);
        
        if (commandMap[command]) {
            commandMap[command].fn(socket, args);
        } else {
            log('Unknown command. Type "h" for help.');
        }
    });
    
    return rl;
}

// Main connection function
function connectToRelay() {
    log('Connecting to', config.serverUrl);
    log('Node.js Relay Test client started. Press Ctrl+C to exit.');
    
    const socket = new WebSocket(config.serverUrl);
    let commandInterface;
    
    socket.on('open', () => {
        log('Connection established');
        // Don't send anything initially, wait for Welcome message from server
        log('Waiting for server welcome message...');
    });
    
    socket.on('message', (data) => {
        try {
            const rawData = data.toString();
            log(`Received raw data: ${rawData}`);
            const message = JSON.parse(rawData);
            log(`Parsed message: ${JSON.stringify(message, null, 2)}`);
            
            // Access the message type, considering both lowercase 'type' and PascalCase 'Type'
            const messageType = message.type !== undefined ? message.type : message.Type;
            
            // Handle welcome message
            if (messageType === 'welcome') {
                socket.clientId = message.clientId;
                socket.encryptionRequired = message.encryptionRequired;
                log(`Client ID assigned: ${socket.clientId}`);
                log(`Encryption required: ${socket.encryptionRequired}`);
                
                log('Received welcome message, sending v2 authentication...');
                
                // Create auth message based on v2 protocol observed in relay-test-v2.html
                const authMessage = {
                    type: 'auth',  // Using 'auth' message type instead of 'register'
                    id: generateUUID(), // Generate unique message ID
                    deviceId: config.deviceId,
                    password: config.password,
                    clientType: DeviceType.Client,  // "client" string
                    name: config.deviceName,
                    version: config.version,
                    timestamp: Date.now(),
                    features: config.features
                };
                
                // Send the authentication message
                sendMessage(socket, authMessage);
                
                // In v2 protocol, we might not need to send heartbeats
                log('Successfully authenticated. Connection established without heartbeats.');
                
                // If this is a server instance, automatically initiate connection to the client
                if (config.role === 'server' && config.targetDeviceId) {
                    log(`SERVER MODE: Automatically connecting to client: ${config.targetDeviceId}`);
                    setTimeout(() => {
                        if (socket.readyState === WebSocket.OPEN) {
                            sendConnectionRequest(socket, config.targetDeviceId);
                        }
                    }, 2000); // Wait 2 seconds before initiating connection
                } else if (config.role === 'client') {
                    log('CLIENT MODE: Waiting for incoming connection requests...');
                }
                
                // Set up command interface after successful auth
                if (!commandInterface) {
                    commandInterface = setupCommandInterface(socket);
                    log('Command interface ready. Type "h" for help.');
                }
            }
            // Handle auth success message
            else if (messageType === 'auth_success') {
                log(`Authentication successful for client: ${message.clientId}`);
            }
            // Handle ping message from server - respond with pong
            else if (messageType === 'ping') {
                log('Received ping from server, sending pong response');
                const pongMsg = {
                    type: 'pong',
                    timestamp: Date.now()
                };
                sendMessage(socket, pongMsg);
            }
            // Handle connection request
            else if (messageType === 'connection_request') {
                log(`Received connection request from: ${message.deviceId}`);
                // In a real implementation, we would prompt the user to accept/reject
                // For testing, automatically accept the request
                log(`Automatically accepting connection from: ${message.deviceId}`);
                // Send offer would be next step in real implementation
            }
            // Handle WebRTC offer
            else if (messageType === 'offer') {
                log(`Received WebRTC offer from: ${message.deviceId}`);
                log(`SDP: ${message.sdp}`);
                // In a real implementation, we would create a WebRTC peer connection and set remote description
            }
            // Handle WebRTC answer
            else if (messageType === 'answer') {
                log(`Received WebRTC answer from: ${message.deviceId}`);
                log(`SDP: ${message.sdp}`);
                // In a real implementation, we would set the remote description on our peer connection
            }
            // Handle ICE candidate
            else if (messageType === 'ice_candidate') {
                log(`Received ICE candidate from: ${message.deviceId}`);
                log(`Candidate: ${JSON.stringify(message.candidate)}`);
                // In a real implementation, we would add this ice candidate to our peer connection
            }
            // Handle registration response (type 1)
            else if (messageType === MessageType.RegisterResponse || messageType === 'register_response' || messageType === '1') {
                const success = message.success || message.Success;
                if (success) {
                    log('Registration successful!');
                } else {
                    const error = message.error || message.Error || 'Unknown error';
                    log('Registration failed:', error);
                }
            }
            // Handle error messages
            else if (messageType === MessageType.Error || messageType === 'error' || messageType === '10') {
                const error = message.error || message.Error || 'Unknown error';
                log('Server error:', error);
            }
            // Handle other message types
            else {
                log('Received unknown message type:', messageType);
            }
        } catch (error) {
            log('Error processing message:', error.message);
        }
    });
    
    socket.on('error', (error) => {
        log('WebSocket error:', error.message);
    });
    
    socket.on('close', (code, reason) => {
        log(`Connection closed. Code: ${code}, Reason: ${reason || 'No reason provided'}`);
        
        // Clean up command interface
        if (commandInterface) {
            commandInterface.close();
        }
        
        // Attempt to reconnect after a delay
        setTimeout(() => {
            log('Attempting to reconnect...');
            connectToRelay();
        }, 5000);
    });
}

// Start the client
connectToRelay();
