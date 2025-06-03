const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const { Pool } = require('pg');
const { v4: uuidv4 } = require('uuid');
const dotenv = require('dotenv');
const cors = require('cors');
const helmet = require('helmet');

// Load environment variables
dotenv.config();

// Initialize Express app
const app = express();
app.use(express.json());
app.use(cors());
app.use(helmet());

// Create HTTP server
const server = http.createServer(app);

// Initialize WebSocket server
const wss = new WebSocket.Server({ server });

// Initialize PostgreSQL connection pool
const pool = new Pool({
  host: process.env.DB_HOST || 'database',
  port: process.env.DB_PORT || 5432,
  database: process.env.DB_NAME || 'quasar',
  user: process.env.DB_USER || 'quasar',
  password: process.env.DB_PASSWORD || 'quasar_password'
});

// Ensure database connection
pool.query('SELECT NOW()', (err) => {
  if (err) {
    console.error('Database connection error:', err);
  } else {
    console.log('Connected to database');
    
    // Create tables if they don't exist
    initializeDatabase();
  }
});

// Initialize database tables
async function initializeDatabase() {
  try {
    // Devices table to store registered clients and servers
    await pool.query(`
      CREATE TABLE IF NOT EXISTS devices (
        id SERIAL PRIMARY KEY,
        device_id VARCHAR(20) UNIQUE NOT NULL,
        device_type VARCHAR(10) NOT NULL,
        password VARCHAR(64) NOT NULL,
        name VARCHAR(100),
        last_online TIMESTAMP DEFAULT NOW(),
        created_at TIMESTAMP DEFAULT NOW()
      )
    `);
    
    // Sessions table to track active connections
    await pool.query(`
      CREATE TABLE IF NOT EXISTS sessions (
        id SERIAL PRIMARY KEY,
        session_id UUID UNIQUE NOT NULL,
        client_id VARCHAR(20) REFERENCES devices(device_id),
        server_id VARCHAR(20) REFERENCES devices(device_id),
        status VARCHAR(20) NOT NULL,
        started_at TIMESTAMP DEFAULT NOW(),
        last_activity TIMESTAMP DEFAULT NOW()
      )
    `);
    
    console.log('Database tables initialized');
  } catch (error) {
    console.error('Error initializing database:', error);
  }
}

// Map to track connected clients
const connectedDevices = new Map();

// WebSocket connection handler
wss.on('connection', (ws) => {
  let deviceId = null;
  let deviceType = null;
  
  console.log('New WebSocket connection established');
  
  // Handle incoming messages
  ws.on('message', async (message) => {
    try {
      const data = JSON.parse(message);
      
      // Handle different message types
      switch (data.type) {
        case 'register':
          // Handle device registration
          await handleRegistration(ws, data);
          break;
          
        case 'connect':
          // Handle connection request
          await handleConnectionRequest(ws, data);
          break;
          
        case 'offer':
        case 'answer':
        case 'ice-candidate':
          // Handle signaling messages
          await forwardSignalingMessage(ws, data);
          break;
          
        case 'heartbeat':
          // Update last seen timestamp
          if (deviceId) {
            await pool.query(
              'UPDATE devices SET last_online = NOW() WHERE device_id = $1',
              [deviceId]
            );
            ws.send(JSON.stringify({ type: 'heartbeat-ack' }));
          }
          break;
          
        default:
          console.warn('Unknown message type:', data.type);
      }
    } catch (error) {
      console.error('Error handling message:', error);
      ws.send(JSON.stringify({ 
        type: 'error', 
        error: 'Failed to process message' 
      }));
    }
  });
  
  // Handle disconnection
  ws.on('close', async () => {
    if (deviceId) {
      console.log(`Device disconnected: ${deviceId}`);
      connectedDevices.delete(deviceId);
      
      // Update device status in database
      try {
        await pool.query(
          'UPDATE devices SET last_online = NOW() WHERE device_id = $1',
          [deviceId]
        );
        
        // Update any active sessions
        if (deviceType === 'server') {
          await pool.query(
            'UPDATE sessions SET status = $1 WHERE server_id = $2 AND status = $3',
            ['disconnected', deviceId, 'active']
          );
        } else if (deviceType === 'client') {
          await pool.query(
            'UPDATE sessions SET status = $1 WHERE client_id = $2 AND status = $3',
            ['disconnected', deviceId, 'active']
          );
        }
      } catch (error) {
        console.error('Error updating device status:', error);
      }
    }
  });
  
  // Handle registration
  async function handleRegistration(ws, data) {
    try {
      const { id, password, type, name } = data;
      
      // Validate required fields
      if (!id || !password || !type) {
        ws.send(JSON.stringify({
          type: 'registration-response',
          success: false,
          error: 'Missing required fields'
        }));
        return;
      }
      
      // Check if device exists
      const deviceResult = await pool.query(
        'SELECT * FROM devices WHERE device_id = $1',
        [id]
      );
      
      if (deviceResult.rows.length > 0) {
        // Device exists, verify password
        const device = deviceResult.rows[0];
        if (device.password !== password) {
          ws.send(JSON.stringify({
            type: 'registration-response',
            success: false,
            error: 'Invalid credentials'
          }));
          return;
        }
        
        // Update device info
        await pool.query(
          'UPDATE devices SET last_online = NOW(), name = COALESCE($1, name) WHERE device_id = $2',
          [name, id]
        );
      } else {
        // New device, create record
        await pool.query(
          'INSERT INTO devices (device_id, device_type, password, name) VALUES ($1, $2, $3, $4)',
          [id, type, password, name]
        );
      }
      
      // Store connection in memory
      deviceId = id;
      deviceType = type;
      connectedDevices.set(id, { ws, type });
      
      // Send success response
      ws.send(JSON.stringify({
        type: 'registration-response',
        success: true,
        id: id
      }));
      
      console.log(`Device registered: ${id} (${type})`);
    } catch (error) {
      console.error('Registration error:', error);
      ws.send(JSON.stringify({
        type: 'registration-response',
        success: false,
        error: 'Registration failed'
      }));
    }
  }
  
  // Handle connection request
  async function handleConnectionRequest(ws, data) {
    try {
      const { targetId, sessionId } = data;
      
      // Validate that the requester is registered
      if (!deviceId) {
        ws.send(JSON.stringify({
          type: 'connect-response',
          success: false,
          error: 'Not registered'
        }));
        return;
      }
      
      // Check if target exists and is online
      const targetDevice = connectedDevices.get(targetId);
      if (!targetDevice) {
        ws.send(JSON.stringify({
          type: 'connect-response',
          success: false,
          error: 'Target device not online'
        }));
        return;
      }
      
      // Create or update session
      let session;
      if (sessionId) {
        // Check existing session
        const sessionResult = await pool.query(
          'SELECT * FROM sessions WHERE session_id = $1',
          [sessionId]
        );
        
        if (sessionResult.rows.length === 0) {
          ws.send(JSON.stringify({
            type: 'connect-response',
            success: false,
            error: 'Invalid session'
          }));
          return;
        }
        
        session = sessionResult.rows[0];
        
        // Update session
        await pool.query(
          'UPDATE sessions SET status = $1, last_activity = NOW() WHERE session_id = $2',
          ['active', sessionId]
        );
      } else {
        // Create new session
        const newSessionId = uuidv4();
        const clientId = deviceType === 'client' ? deviceId : targetId;
        const serverId = deviceType === 'server' ? deviceId : targetId;
        
        const sessionResult = await pool.query(
          'INSERT INTO sessions (session_id, client_id, server_id, status) VALUES ($1, $2, $3, $4) RETURNING *',
          [newSessionId, clientId, serverId, 'pending']
        );
        
        session = sessionResult.rows[0];
      }
      
      // Notify target device
      targetDevice.ws.send(JSON.stringify({
        type: 'connection-request',
        fromId: deviceId,
        sessionId: session.session_id
      }));
      
      // Send response to requester
      ws.send(JSON.stringify({
        type: 'connect-response',
        success: true,
        sessionId: session.session_id
      }));
      
      console.log(`Connection request: ${deviceId} -> ${targetId}, Session: ${session.session_id}`);
    } catch (error) {
      console.error('Connection request error:', error);
      ws.send(JSON.stringify({
        type: 'connect-response',
        success: false,
        error: 'Connection request failed'
      }));
    }
  }
  
  // Forward signaling messages between peers
  async function forwardSignalingMessage(ws, data) {
    try {
      const { targetId, sessionId } = data;
      
      // Validate sender is registered
      if (!deviceId) {
        ws.send(JSON.stringify({
          type: 'error',
          error: 'Not registered'
        }));
        return;
      }
      
      // Validate session
      const sessionResult = await pool.query(
        'SELECT * FROM sessions WHERE session_id = $1',
        [sessionId]
      );
      
      if (sessionResult.rows.length === 0) {
        ws.send(JSON.stringify({
          type: 'error',
          error: 'Invalid session'
        }));
        return;
      }
      
      const session = sessionResult.rows[0];
      
      // Validate sender is part of the session
      if (session.client_id !== deviceId && session.server_id !== deviceId) {
        ws.send(JSON.stringify({
          type: 'error',
          error: 'Not authorized for this session'
        }));
        return;
      }
      
      // Find target device
      const targetDevice = connectedDevices.get(targetId);
      if (!targetDevice) {
        ws.send(JSON.stringify({
          type: 'error',
          error: 'Target device not online'
        }));
        return;
      }
      
      // Forward the message
      const forwardMessage = {
        ...data,
        fromId: deviceId
      };
      
      targetDevice.ws.send(JSON.stringify(forwardMessage));
      
      // Update session activity
      await pool.query(
        'UPDATE sessions SET last_activity = NOW() WHERE session_id = $1',
        [sessionId]
      );
      
      console.log(`Forwarded ${data.type} from ${deviceId} to ${targetId}`);
    } catch (error) {
      console.error('Error forwarding message:', error);
      ws.send(JSON.stringify({
        type: 'error',
        error: 'Failed to forward message'
      }));
    }
  }
});

// REST API endpoints

// Get device status
app.get('/api/device/:id', async (req, res) => {
  try {
    const { id } = req.params;
    
    const result = await pool.query(
      'SELECT device_id, device_type, name, last_online FROM devices WHERE device_id = $1',
      [id]
    );
    
    if (result.rows.length === 0) {
      return res.status(404).json({ error: 'Device not found' });
    }
    
    const device = result.rows[0];
    const isOnline = connectedDevices.has(device.device_id);
    
    res.json({
      ...device,
      online: isOnline
    });
  } catch (error) {
    console.error('Error fetching device:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
});

// Generate a new device ID
app.get('/api/generate-id', (req, res) => {
  // Generate a 9-digit numeric ID (similar to TeamViewer)
  const min = 100000000;
  const max = 999999999;
  const id = Math.floor(Math.random() * (max - min + 1)) + min;
  
  res.json({ id: id.toString() });
});

// Start the server
const PORT = process.env.PORT || 8080;
server.listen(PORT, () => {
  console.log(`Signaling server running on port ${PORT}`);
});
