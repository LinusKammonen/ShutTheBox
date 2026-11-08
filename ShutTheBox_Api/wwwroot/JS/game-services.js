import {post, get} from "./api-engine.js";

export async function authMe(){
    return get(`auth/me`)
}
export async function rollDice(gameCode) {
    return post(`games/${gameCode}/roll`);
}

export async function move(gameCode, selectedTiles){
    return post(`games/${gameCode}/move`, {selectedNumbers: selectedTiles});
}

export async function getFinishedGame(gameCode) {
    return get(`games/${gameCode}`)
}

export async function createRoom(name, maxPlayers){
    return post(`rooms`, {name: name, maxPlayers: maxPlayers})
}

export async function currentGames() {
    return get(`games/current`)
}

export async function getCurrentGameState(gameCode){
    return get(`games/${gameCode}/state`)
}

export async function getRoom(roomCode){
    return get(`rooms/${roomCode}`)
}

export async function startGameFromRoom(roomCode){
    return post(`games/from-room/${roomCode}/start`)
}

export async function joinRoomFromRoomCode(roomCode){
    return post(`rooms/${roomCode}/join`)
}

export async function leaveRoomFromRoomCode(roomCode){
    return post(`rooms/${roomCode}/leave`)
}

export async function leaveGameFromGameCode(gameCode){
    return post(`games/${gameCode}/leave`)
}
// export async function getTiles() {

// }
