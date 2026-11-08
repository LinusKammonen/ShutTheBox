import { startLoading, withLoading } from "./loading.js";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/game", { accessTokenFactory: () => localStorage.getItem("token") })
  .withAutomaticReconnect()
  .build();

const lobbyStatus = ["#active-lobby-heading"];
const roomStatus = ["#roomStatusText"];
const gameStatus = ["#instruction"];
const anyStatus = [...gameStatus, ...roomStatus, "#textWelcome"];

let currentGameCode = null;
let currentGameRole = null;
let startPromise = null;
let stopReconnectStatus = null;

async function ensureConnection(target) {
  if (connection.state === signalR.HubConnectionState.Connected) {
    return;
  }

  if (!startPromise) {
    startPromise = withLoading("Ansluter till servern…", () => connection.start(), { target })
      .finally(() => { startPromise = null; });
  }

  await startPromise;
}

function invoke(text, target, methodName, ...args) {
  return withLoading(text, async () => {
    await ensureConnection(target);
    return await connection.invoke(methodName, ...args);
  }, { target });
}

/* ============================================================
   LOBBY
   ============================================================ */

export async function joinLobby() {
  return await invoke("Ansluter till lobbyn…", lobbyStatus, "JoinLobby");
}

export async function leaveLobby() {
  return await invoke("Lämnar lobbyn…", lobbyStatus, "LeaveLobby");
}

export function listenLobbyUpdates(callback) {
  connection.on("LobbyUpdated", callback);
}

/* ============================================================
   ROOM
   ============================================================ */

export async function joinRoom(roomCode) {
  return await invoke("Ansluter till rummet…", roomStatus, "JoinRoom", roomCode);
}

export async function leaveRoom(roomCode) {
  return await invoke("Lämnar rummet…", roomStatus, "LeaveRoom", roomCode);
}

export function listenForRoomUpdates(callback) {
  connection.on("RoomUpdated", callback);
}

/* ============================================================
   GAME
   ============================================================ */

export async function joinGame(gameCode, role) {
  currentGameCode = gameCode;
  currentGameRole = role;

  return await invoke("Ansluter till spelet…", gameStatus, "JoinGame", gameCode, role);
}

export async function leaveGame(gameCode) {
  const result = await invoke("Lämnar spelet…", gameStatus, "LeaveGame", gameCode);

  if (gameCode === currentGameCode) {
    currentGameCode = null;
    currentGameRole = null;
  }

  return result;
}

export function listenForGameUpdates(callback) {
  connection.on("GameUpdated", callback);
}

export function listenForGameFinished(callback) {
  connection.on("GameFinished", callback);
}

/* ============================================================
   CONNECTION
   ============================================================ */

function clearReconnectStatus() {
  if (stopReconnectStatus) {
    stopReconnectStatus();
    stopReconnectStatus = null;
  }
}

connection.onreconnecting((error) => {
  console.log("SignalR reconnecting:", error);
  clearReconnectStatus();
  stopReconnectStatus = startLoading("Tappade anslutningen – ansluter igen…", { target: anyStatus });
});

connection.onreconnected(async (connectionId) => {
  console.log("SignalR reconnected:", connectionId);
  clearReconnectStatus();

  if (!currentGameCode) {
    return;
  }

  try {
    await invoke("Ansluter till spelet igen…", gameStatus, "JoinGame", currentGameCode, currentGameRole);

    console.log("Rejoined game:", currentGameCode);
  } catch (error) {
    console.error("Could not rejoin game:", error);
  }
});

connection.onclose((error) => {
  console.log("SignalR connection closed:", error);
  clearReconnectStatus();
});
