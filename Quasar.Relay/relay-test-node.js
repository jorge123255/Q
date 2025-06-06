const WebSocket = require('ws');
const crypto = require('crypto');

// Configuration
const config = {
    serverUrl: 'wss://relay.nextcloudcyber.com',
    deviceId: 'test-client-node',
    password: 'test-password',
    deviceName: 'Node.js Test Client',
    useEncryption: false, // Set to true to enable encryption
    version: '2.0.0',
    features: ['relay'], // Supported features
    targetDeviceId: process.argv[2] || '' // Specify target device as command line argument
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
    Client: "client",
    Server: "server"
};

// Additional debugging to detect potential encoding issues
function detectEncoding(text) {
    if (text.charCodeAt(0) === 0xFEFF) {
        log('Warning: Text contains UTF-8 BOM');
    }
}

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
    console.log(`[${timestamp}]`, ...args);
}

// Handles encryption of message data using AES-GCM
function encrypt(plaintext) {
    if (!config.useEncryption) {
        return plaintext;
    }

    try {
        // Generate a random IV (initialization vector)
        const iv = crypto.randomBytes(16); 
        
        // Create key from password
        const key = crypto.createHash('sha256').update(config.encryptionKey).digest();
        
        // Create cipher with key and iv
        const cipher = crypto.createCipheriv(config.algorithm, key, iv);
        
        // Encrypt the data
        let encrypted = cipher.update(plaintext, 'utf8', 'base64');
        encrypted += cipher.final('base64');
        
        // Get the auth tag for GCM
        const authTag = cipher.getAuthTag().toString('base64');
        
        // Format for sending: IV + authTag + encrypted data
        // Formatting as JSON for clarity
        const encMessage = {
            iv: iv.toString('base64'),
            tag: authTag,
            encrypted: encrypted
        };
        
        log('Encrypted message');
        return JSON.stringify(encMessage);
    } catch (error) {
        log('Encryption error:', error.message);
        return null;
    }
}

function sendMessage(socket, message) {
    try {
        // Convert message to JSON string
        const jsonString = JSON.stringify(message);
        log(`Sending: ${jsonString}`);
        
        if (socket.encryptionRequired && config.useEncryption) {
            // Handle encryption if required
            const encryptedPayload = encrypt(jsonString);
            
            if (encryptedPayload) {
                const wrapper = {
                    encrypted: true,
                    data: encryptedPayload
                };
                
                // Convert to binary frame (Buffer) and send
                const binaryData = Buffer.from(JSON.stringify(wrapper), 'utf8');
                socket.send(binaryData);
                log('Sent encrypted message (binary)');
            } else {
                log('Failed to encrypt message');
            }
        } else {
            // Convert to binary frame (Buffer) and send
            const binaryData = Buffer.from(jsonString, 'utf8');
            socket.send(binaryData);
            log('Sent message (binary)');
        }
    } catch (error) {
        log('Error sending message:', error.message);
    }
}

function startClient() {
    log('Connecting to', config.serverUrl);
    const socket = new WebSocket(config.serverUrl);
    
    // Default to true until server tells us otherwise
    socket.encryptionRequired = true;
    
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
                // The server might be handling connection keepalive differently
                log('Successfully authenticated. Connection established without heartbeats.');
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
            
            // Handle registration response (type 1)
            if (messageType === MessageType.RegisterResponse || messageType === 'register_response' || messageType === '1') {
                const success = message.success !== undefined ? message.success : message.Success;
                const error = message.error || message.Error || 'Unknown error';
                
                if (success) {
                    log('Registration successful!');
                } else {
                    log('Registration failed:', error);
                }
            }
            
            // Handle errors (type 10)
            if (messageType === MessageType.Error || messageType === 'error' || messageType === '10') {
                const error = message.error || message.Error || 'Unknown error';
                log('Server error:', error);
            }
        } catch (err) {
            log('Error parsing message:', err.message);
        }
    });
    
    socket.on('close', (code, reason) => {
        log(`Connection closed: ${reason || 'Unknown reason'} (code: ${code})`);
    });
    
    socket.on('error', (err) => {
        log('WebSocket error:', err.message);
    });
    
    return socket;
}

// Start the client
const client = startClient();

// Handle process termination
process.on('SIGINT', () => {
    if (client && client.readyState === WebSocket.OPEN) {
        log('Closing connection...');
        client.close(1000, 'User terminated');
    }
    process.exit(0);
});

log('Node.js Relay Test client started. Press Ctrl+C to exit.');
