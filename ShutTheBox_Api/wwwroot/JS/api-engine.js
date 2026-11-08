import { withLoading } from "./loading.js";

const baseUrl = "";
const version = "v1";
const apiEndpointBase = `${baseUrl}/${version}`;

if (!localStorage.getItem("token")) {
  window.location.replace("/login.html");
  throw new Error("Inte inloggad");
}

export async function get(endpoint) {
    return await run(endpoint, "GET");
}

export async function post(endpoint, body) {
    return await run(endpoint, "POST", body);    
}

const gameStatus = ["#instruction"];
const roomStatus = ["#roomStatusText"];

function describe(endpoint, method) {
  if (method === "GET") {
    if (endpoint === "auth/me") return { text: "Hämtar användare…", target: ["#currentUser"] };
    if (endpoint === "games/active") return { text: "Hämtar spelrum…", target: ["#active-lobby-heading"] };
    if (endpoint === "rooms/open") return { text: "Hämtar spelrum…", target: ["#open-lobby-heading"] };
    if (endpoint === "games/current") return { text: "Hämtar dina spel…", target: ["#own-lobby-heading", ...roomStatus] };
    if (/^games\/[^/]+\/state$/.test(endpoint)) return { text: "Hämtar spelet…", target: gameStatus };
    if (/^games\/[^/]+$/.test(endpoint)) return { text: "Hämtar resultatet…", target: gameStatus };
    if (/^rooms\/[^/]+$/.test(endpoint)) return { text: "Hämtar rummet…", target: roomStatus };
    return { text: "Laddar…", target: [...gameStatus, ...roomStatus] };
  }

  if (/^games\/[^/]+\/roll$/.test(endpoint)) return { text: "Slår tärningarna…", target: gameStatus };
  if (/^games\/[^/]+\/move$/.test(endpoint)) return { text: "Stänger brickor…", target: gameStatus };
  if (/^games\/from-room\/[^/]+\/start$/.test(endpoint)) return { text: "Startar spelet…", target: roomStatus };
  if (/^games\/[^/]+\/leave$/.test(endpoint)) return { text: "Lämnar spelet…", target: gameStatus };
  if (endpoint === "rooms") return { text: "Skapar rum…", target: ["#btn-create-room", "#own-lobby-heading", ...gameStatus] };
  if (/^rooms\/[^/]+\/join$/.test(endpoint)) return { text: "Går med i rummet…", target: roomStatus };
  if (/^rooms\/[^/]+\/leave$/.test(endpoint)) return { text: "Lämnar rummet…", target: roomStatus };
  return { text: "Skickar…", target: [...gameStatus, ...roomStatus] };
}

function run(endpoint, method = "GET", body = null) {
  const { text, target } = describe(endpoint, method);
  return withLoading(
    text,
    () => request(endpoint, method, body),
    { blocking: method !== "GET", target }
  );
}

async function request(endpoint, method, body) {
  const url = `${apiEndpointBase}/${endpoint}`;

  const token = localStorage.getItem("token");
  const headers = {};

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  if (body !== null) {
    headers["Content-Type"] = "application/json";
  }


  const response = await fetch(url, {
    method,
    headers,
    body: body !== null ? JSON.stringify(body) : null,
  });

  if (response.ok) {
    return await response.json();
  }

  let errorData = null;

  try {

    errorData = await response.json();

  } catch (error) {
    
    popupError("Undefined error")
    // API:t returnerade inte JSON
  }
  if (response.status === 401) {
    localStorage.removeItem("token");
    window.location.replace("/login.html");
    const error = new Error("Du måste vara inloggad för att kunna spela.");
    error.status = 401;
    throw error;

  }

  const error = new Error(
    errorData?.detail || errorData?.message || `HTTP error ${response.status}`,
  );
  

  error.status = response.status;
  error.data = errorData;

  throw error;
}


function popupError(error){
  
    let popup = document.querySelector("#popup")
    popup.open = true
    let msg = document.querySelector("#popup-message")
    msg.textContent = `${error}`
    const okbtn = document.querySelector("#popup-btn-ok")
    okbtn.addEventListener("click", ()=> popup.open = false)
}
    
