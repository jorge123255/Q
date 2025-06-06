/**
 * Final attempt at a Node.js WebSocket client for Quasar relay
 * Closely follows the C# implementation seen in the source code
 */
const WebSocket = require('ws');
const crypto = require('crypto');

// Configuration 
const config = {
    serverUrl: 'wss://relay.nextcloudcyber.com',
    deviceId: 'node-test-final',
    password: 'final-test-password',
    deviceType: 'client',
    deviceName: 'Final Node.js Test Client',
    // Change this to an empty string to bypass encryption
    // The server may ignore our config.json setting
    encryptionKey: 'quasar-relay-server'
};

function log(...args) {
    const timestamp = new Date().toLocaleTimeString();
    console.log(`[${timestamp}]`, ...args);
}

// Format a message with the proper structure
function formatMessage(type, payload) {
    return {
        ...payload,
        type: type
    };
}

// Encryption handling
function encrypt(message, key) {
    try {
        // Generate initialization vector
        const iv = crypto.randomBytes(16);
        
        // Create key from password using SHA-256 (matches common .NET implementation)
        const derivedKey = crypto.createHash('sha256').update(key).digest();
        
        // Create cipher with AES-256-GCM
        const cipher = crypto.createCipheriv('aes-256-gcm', derivedKey, iv);
        
        // Encrypt the data
        let encrypted = cipher.update(message, 'utf8', 'base64');
        encrypted += cipher.final('base64');
        
        // Get the auth tag
        const authTag = cipher.getAuthTag();
        
        return {
            iv: iv.toString('base64'),
            encrypted: encrypted,
            tag: authTag.toString('base64')
        };
    } catch (error) {
        log('Encryption error:', error);
        return null;
    }
}

// Main client function
function startClient() {
    log('Connecting to', config.serverUrl);
    log('Test client starting...');
    
    const socket = new WebSocket(config.serverUrl);
    let clientId = null;
    let encryptionRequired = false;
    
    // Handle WebSocket events
    socket.on('open', () => {
        log('Connection established');
        
        // Don't send anything yet, wait for welcome message
    });
    
    socket.on('message', (data) => {
        try {
            const message = JSON.parse(data.toString());
            log('Received:', JSON.stringify(message, null, 2));
            
            // Handle welcome message
            if (message.type === 'welcome') {
                clientId = message.clientId;
                encryptionRequired = message.encryptionRequired;
                
                log(`Client ID: ${clientId}, Encryption Required: ${encryptionRequired}`);
                
                // Create register message
                const registerMessage = {
                    id: config.deviceId,
                    password: config.password,
                    deviceType: config.deviceType,
                    name: config.deviceName,
                    type: 0 // RelayMessageType.Register
                };
                
                const messageJson = JSON.stringify(registerMessage);
                
                // Send based on encryption requirement
                if (encryptionRequired && config.encryptionKey) {
                    log('Encrypting registration message');
                    
                    const encryptedData = encrypt(messageJson, config.encryptionKey);
                    if (encryptedData) {
                        // Send encrypted message
                        // Try several formats to see what works
                        // Format 1: Send as JSON with special properties
                        const encryptedMessage = {
                            encrypted: true,
                            encryptedData: encryptedData.encrypted,
                            iv: encryptedData.iv,
                            tag: encryptedData.tag
                        };
                        
                        log('Sending encrypted message format 1');
                        socket.send(JSON.stringify(encryptedMessage));
                    }
                } else {
                    // Send plaintext
                    log('Sending plaintext message');
                    socket.send(messageJson);
                }
            }
            
            // Handle errors
            if (message.type === 'error') {
                log('Server error:', message.error);
                
                // If we get an unknown message type error, try a different format
                if (message.error === 'Unknown message type' && encryptionRequired) {
                    log('Trying alternative encryption format...');
                    
                    // Create register message
                    const registerMessage = {
                        id: config.deviceId,
                        password: config.password,
                        deviceType: config.deviceType,
                        name: config.deviceName,
                        type: 0 // RelayMessageType.Register
                    };
                    
                    const messageJson = JSON.stringify(registerMessage);
                    const encryptedData = encrypt(messageJson, config.encryptionKey);
                    
                    if (encryptedData) {
                        // Format 2: Different property naming
                        const altFormat = {
                            type: "encrypted",
                            data: encryptedData.encrypted,
                            iv: encryptedData.iv,
                            tag: encryptedData.tag
                        };
                        
                        log('Sending encrypted message format 2');
                        socket.send(JSON.stringify(altFormat));
                    }
                }
            }
        } catch (error) {
            log('Error processing message:', error);
        }
    });
    
    socket.on('error', (error) => {
        log('WebSocket error:', error);
    });
    
    socket.on('close', (code, reason) => {
        log(`Connection closed: ${code} - ${reason}`);
    });
}

// Start client
startClient();
