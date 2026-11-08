import {post, get} from "./api-engine.js";

export async function getActiveGames(){
    return get(`games/active`)
}

export async function authMe(){
    return get(`auth/me`)
}

export async function createRoom(name, minPlayers, maxPlayers){
    return post(`rooms`, {name: name,  maxPlayers: maxPlayers ,minPlayers: minPlayers})
}

export async function currentGames() {
    return get(`games/current`)
}

export async function getOpenLobbies() {
    return get(`rooms/open`)
}