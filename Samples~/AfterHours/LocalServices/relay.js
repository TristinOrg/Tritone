#!/usr/bin/env node
"use strict";

// Small local teaching relay for the checked-in After Hours generated schema.
// It implements Tritone's length-prefixed frames and protocol handshake directly.
const fs = require("node:fs");
const net = require("node:net");
const crypto = require("node:crypto");
const path = require("node:path");

const schemaPath = path.resolve(__dirname, "../Schemas/Network.json");
const schema = JSON.parse(fs.readFileSync(schemaPath, "utf8"));
const port = Number(process.env.AFTER_HOURS_RELAY_PORT || 27831);
const host = "127.0.0.1";
const magic = 0x31545254;
const messages = [...schema.Messages].sort((a, b) => a.Id - b.Id);
const message = Object.fromEntries(messages.map((entry) => [entry.Name, entry]));

function fingerprint() {
  let canonical = "";
  const append = (value) => {
    const text = value == null ? "" : String(value);
    canonical += `${text.length}:${text};`;
  };
  append(schema.ProtocolId);
  for (const item of messages) {
    append(item.Id);
    append(item.Name);
    append(item.Kind);
    append(item.Response);
    for (const field of item.Fields || []) {
      append(field.Name);
      append(field.Type);
    }
  }
  return crypto.createHash("sha256").update(canonical, "utf8").digest("hex");
}

const protocol = {
  id: schema.ProtocolId,
  major: schema.MajorVersion,
  minor: schema.MinorVersion,
  minimumMinor: schema.MinimumMinorVersion,
  fingerprint: fingerprint(),
};

if (process.argv.includes("--fingerprint")) {
  console.log(protocol.fingerprint);
  process.exit(0);
}

function int32(value) {
  const buffer = Buffer.allocUnsafe(4);
  buffer.writeInt32LE(value, 0);
  return buffer;
}

function string(value) {
  const bytes = Buffer.from(value, "utf8");
  return Buffer.concat([int32(bytes.length), bytes]);
}

function readInt(buffer, offset) {
  if (offset + 4 > buffer.length) throw new Error("incomplete int32");
  return buffer.readInt32LE(offset);
}

function readString(buffer, offset) {
  const length = readInt(buffer, offset);
  if (length < 0 || offset + 4 + length > buffer.length) throw new Error("invalid string length");
  return { value: buffer.toString("utf8", offset + 4, offset + 4 + length), next: offset + 4 + length };
}

function framed(payload) {
  return Buffer.concat([int32(payload.length), payload]);
}

function handshake(client) {
  const remote = client.protocol;
  let compatibility = 1;
  if (remote.id !== protocol.id) compatibility = 2;
  else if (remote.major !== protocol.major) compatibility = 3;
  else if (remote.minor < protocol.minimumMinor) compatibility = 4;
  else if (protocol.minor < remote.minimumMinor) compatibility = 5;
  else if (remote.minor === protocol.minor && remote.fingerprint !== protocol.fingerprint) compatibility = 6;
  const payload = Buffer.concat([
    int32(magic), int32(2), int32(compatibility), string(protocol.id),
    int32(protocol.major), int32(protocol.minor), int32(protocol.minimumMinor),
    string(protocol.fingerprint),
  ]);
  client.socket.write(framed(payload));
  client.ready = compatibility === 1;
  console.log(`${client.ready ? "ACCEPT" : "REJECT"} ${remote.id} v${remote.major}.${remote.minor} / ${compatibility}`);
}

function parseHello(payload) {
  let offset = 8; // signature + hello kind
  const id = readString(payload, offset); offset = id.next;
  const major = readInt(payload, offset); offset += 4;
  const minor = readInt(payload, offset); offset += 4;
  const minimumMinor = readInt(payload, offset); offset += 4;
  const hash = readString(payload, offset); offset = hash.next;
  if (offset !== payload.length) throw new Error("trailing bytes in protocol hello");
  return { id: id.value, major, minor, minimumMinor, fingerprint: hash.value };
}

function handleMessage(client, payload) {
  if (!client.ready) {
    if (payload.length < 8 || payload.readInt32LE(0) !== magic || payload.readInt32LE(4) !== 1)
      throw new Error("expected Tritone protocol hello");
    client.protocol = parseHello(payload);
    handshake(client);
    return;
  }
  if (payload.length < 4) throw new Error("message has no type identifier");
  const id = readInt(payload, 0);
  let offset = 4;
  if (id === message.CaseStatus.Id) {
    const caseId = readString(payload, offset); offset = caseId.next;
    const state = readString(payload, offset); offset = state.next;
    if (offset !== payload.length) throw new Error("trailing bytes in CaseStatus");
    console.log(`CASE ${caseId.value} / ${state.value}`);
    client.socket.write(framed(Buffer.concat([
      int32(message.CaseStatus.Id), string(caseId.value), string("RELAY_ACK"),
    ])));
  } else if (id === message.NightPing.Id) {
    const nonce = readInt(payload, offset); offset += 4;
    if (offset !== payload.length) throw new Error("trailing bytes in NightPing");
    client.socket.write(framed(Buffer.concat([int32(message.NightPong.Id), int32(nonce)])));
  } else {
    throw new Error(`unknown generated message id ${id}`);
  }
}

function handleConnection(socket) {
  const client = { socket, pending: Buffer.alloc(0), ready: false, protocol: null };
  console.log(`CONNECT ${socket.remoteAddress}:${socket.remotePort}`);
  socket.on("data", (chunk) => {
    try {
      client.pending = Buffer.concat([client.pending, chunk]);
      while (client.pending.length >= 4) {
        const length = client.pending.readInt32LE(0);
        if (length <= 0 || length > 1024 * 1024) throw new Error("frame length outside allowed range");
        if (client.pending.length < 4 + length) break;
        const payload = client.pending.subarray(4, 4 + length);
        client.pending = client.pending.subarray(4 + length);
        handleMessage(client, payload);
      }
    } catch (error) {
      console.error(`PROTOCOL ERROR ${error.message}`);
      socket.destroy();
    }
  });
  socket.on("close", () => console.log("DISCONNECT"));
  socket.on("error", (error) => console.error(`SOCKET ERROR ${error.message}`));
}

const server = net.createServer(handleConnection);
server.listen(port, host, () => {
  console.log(`After Hours local relay listening on ${host}:${port}`);
  console.log(`Protocol ${protocol.id} v${protocol.major}.${protocol.minor} / ${protocol.fingerprint}`);
  console.log("Press Ctrl+C to stop. This teaching relay binds to loopback only.");
});
server.on("error", (error) => {
  console.error(error.message);
  process.exitCode = 1;
});
