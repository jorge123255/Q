/**
 * Simplified Node.js WebSocket client for testing Quasar relay
 * Uses a minimal implementation to test connectivity and message formats
 */
const WebSocket = require('ws');
const crypto = require('crypto');

// Configuration
const config = {
    serverUrl: 'wss://relay.nextcloudcyber.com',
    deviceId: 'node-simple-client',
    password: 'simple-test-password',
    deviceType: 'client',
    deviceName: 'Simple Node.js Test Client'
};

// RelayMessageType enum from Quasar.Common.Relay.Models
const MessageType = {
    Register: 0,
    RegisterResponse: 1,
    Connect: 2,
    ConnectResponse: 3,
    ConnectionRequest: 4,
    Offer: 5,
    Answer: 6,
    IceCandidate: 7,
    Heartbeat: 8,
    HeartbeatAcknowledge: 9,
    Error: 10
};

// Utility functions
function log(...args) {
    const timestamp = new Date().toLocaleTimeString();
    console.log(`[${timestamp}]`, ...args);
}

// Create a key from the password - we'll try a different approach
function createKey(password) {
    return crypto.createHash('sha256').update(password).digest();
}

// Simple encryption with AES-256-GCM
function encryptMessage(message, password) {
    try {
        const iv = crypto.randomBytes(16);
        const key = createKey(password);
        const cipher = crypto.createCipheriv('aes-256-gcm', key, iv);
        
        let encrypted = cipher.update(message, 'utf8', 'base64');
        encrypted += cipher.final('base64');
        const authTag = cipher.getAuthTag();
        
        // Format: { iv, data, tag } - all base64-encoded
        return {
            iv: iv.toString('base64'),
            data: encrypted,
            tag: authTag.toString('base64')
        };
    } catch (error) {
        log('Encryption error:', error);
        return null;
    }
}

// Main client
function startClient() {
    log('Connecting to', config.serverUrl);
    log('Simple Node.js client started. Press Ctrl+C to exit.');
    
    const socket = new WebSocket(config.serverUrl);
    let encryptionRequired = true; // Default assumption
    let clientId = null;
    
    socket.on('open', () => {
        log('Connection established');
        
        // First, send unencrypted registration to see full server response
        const registerMsg = {
            type: MessageType.Register,
            id: config.deviceId,
            password: config.password,
            deviceType: config.deviceType,
            name: config.deviceName
        };
        
        // Send as plaintext first to see server response
        const msgStr = JSON.stringify(registerMsg);
        log('Sending plain registration:', msgStr);
        socket.send(msgStr);
    });
    
    socket.on('message', (data) => {
        const msgStr = data.toString();
        log('Received:', msgStr);
        
        try {
            const message = JSON.parse(msgStr);
            
            // Handle welcome message
            if (message.type === 'welcome') {
                clientId = message.clientId;
                encryptionRequired = message.encryptionRequired;
                
                log(`Received clientId: ${clientId}, encryptionRequired: ${encryptionRequired}`);
                
                if (encryptionRequired) {
                    log('Server requires encryption, trying again with encrypted message');
                    
                    // Create register message
                    const registerMsg = {
                        type: MessageType.Register,
                        id: config.deviceId,
                        password: config.password,
                        deviceType: config.deviceType,
                        name: config.deviceName
                    };
                    
                    // Convert to JSON
                    const plaintext = JSON.stringify(registerMsg);
                    
                    // Encrypt the message
                    const encrypted = encryptMessage(plaintext, config.password);
                    
                    if (encrypted) {
                        // Create a wrapper message
                        const wrapper = {
                            encrypted: true,
                            iv: encrypted.iv,
                            data: encrypted.data,
                            tag: encrypted.tag
                        };
                        
                        log('Sending encrypted registration');
                        socket.send(JSON.stringify(wrapper));
                    }
                }
                
                // Set up heartbeat (every 30 seconds)
                setInterval(() => {
                    if (socket.readyState === WebSocket.OPEN) {
                        const heartbeatMsg = {
                            type: MessageType.Heartbeat,
                            timestamp: Date.now()
                        };
                        
                        const msgStr = JSON.stringify(heartbeatMsg);
                        
                        if (encryptionRequired) {
                            const encrypted = encryptMessage(msgStr, config.password);
                            if (encrypted) {
                                const wrapper = {
                                    encrypted: true,
                                    iv: encrypted.iv,
                                    data: encrypted.data,
                                    tag: encrypted.tag
                                };
                                socket.send(JSON.stringify(wrapper));
                                log('Heartbeat sent (encrypted)');
                            }
                        } else {
                            socket.send(msgStr);
                            log('Heartbeat sent (plain)');
                        }
                    }
                }, 30000);
            }
            
            // Handle error message
            if (message.type === 'error') {
                log('Server error:', message.error);
            }
        } catch (error) {
            log('Error parsing message:', error.message);
        }
    });
    
    socket.on('error', (error) => {
        log('WebSocket error:', error.message);
    });
    
    socket.on('close', (code, reason) => {
        log(`Connection closed: ${code} - ${reason}`);
    });
}

// Start the client
startClient();
