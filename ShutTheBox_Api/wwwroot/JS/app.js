import { getActiveGames, authMe, createRoom, currentGames, getOpenLobbies } from "./app-services.js"
import { joinLobby, listenLobbyUpdates } from "./gamehub-services.js";


let openLobbies = [];
const lobbies = document.querySelector("#active-lobby-holder")
const openLobbiesHolder = document.querySelector("#open-lobby-holder")
let games = {}
let me = {}
let userId = {}
const myLobbies = document.querySelector("#own-lobby-holder")
let myGames = {};
let myOngoingGames = {};
const buttonX = document.querySelector("#x")
const popup = document.querySelector("#popupNewRoom")
const currentUser = document.querySelector("#currentUser")
const buttonCreateMultiplayerRoom = document.querySelector("#btn-create-multiplayer-room")
const btnCreateRoom = document.querySelector("#btn-create-room")
const minPlayers = document.querySelector("#minPlayers")
const maxPlayers = document.querySelector("#maxPlayers")
let textWelcome = document.querySelector("#textWelcome")



await joinLobby()
listenLobbyUpdates(updateLobby)

async function updateLobby(update){
     games = await getActiveGames();
     openLobbies = await getOpenLobbies();
     showActiveLobbies()
     showOpenLobbies()
}



function displayUser (user){
     
     let username = document.createElement("p")
     username.textContent = "Inloggad: " + user.username
     currentUser.append(username)
}

me = await authMe();
await displayUser(me)

textWelcome.textContent = `Hej ${me.username}! Ta en titt över våra spelrum.`
userId = me.userId
games = await getActiveGames();
openLobbies = await getOpenLobbies();
await showActiveLobbies();
await showOpenLobbies()

try 
{
     myOngoingGames = await currentGames()
     myGames = games.filter(games => games.gameCode == (myOngoingGames.gameCode))    //Eftersom CurrentGames inte ger oss data om spelare
     await showMyLobbies();
     
}
catch(error)
{
     myLobbies.innerHTML = `<h2 class="stb-logo">${error.message}</h2>
     
     <button id="btn-start-game" class="stb-btn stb-btn--primary">Starta nytt singelplayerspel</button>`
     
     const buttonStartNewGame = document.querySelector("#btn-start-game")

     buttonStartNewGame.addEventListener("click", async() =>
     {
          const newRoom = await createRoom("", 1)
          const gamecode = newRoom.state.gameCode  
          localStorage.setItem("gamecode", `${gamecode}`)
          localStorage.setItem("playerType", "player")
          localStorage.setItem("gamestarted","true")
          window.open(`game.html`, "_self")
          
     })
}
     
buttonX.addEventListener("click", () =>{
          popup.close()
})
     
buttonCreateMultiplayerRoom.addEventListener("click", ()=>{
          
     popup.showModal()
          
});
     

maxPlayers.addEventListener("click", async () => {
     console.log("hej")
     GetMinPlayers()
})

minPlayers.innerHTML = `<option value="2">2 players</option>`


function GetMinPlayers(){
     
     minPlayers.innerHTML = null;
     for(let i = 2; i <= maxPlayers.value; i++){
          let option = document.createElement("option")
          option.value = i
          option.textContent = `${option.value} players`
          minPlayers.append(option)
     }
}


     
btnCreateRoom.addEventListener("click", async() => {
          
     try{
          let roomNameInput = document.querySelector("#lobbyName")
          await createRoom(roomNameInput.value, minPlayers.value, maxPlayers.value)
          //https://stackoverflow.com/questions/11563638/how-do-i-get-the-value-of-text-input-field-using-javascript --hämta värdet på textinput   let newRoom = await createRoom(roomNameInput.value, maxPlayers.value)
          //https://developer.mozilla.org/en-US/docs/Web/API/Location/reload
          window.location.reload()
     }
     catch(error){
          popupError(error)
     }
})    
     
     
lobbies.addEventListener("click", async (event)=>{
          
     try{
          const game = event.target.closest("a")
          if (!game) return;
          const gamecode = game.name
          //vi måste nu passa över gamecode i nästa steg
          //https://stackoverflow.com/questions/29364121/passing-variable-when-opening-another-html-page-using-javascript
          localStorage.setItem("gamecode", `${gamecode}`)
          localStorage.setItem("playerType", "spectator")
          localStorage.setItem("gamestarted","true")
          window.open(`game.html`, "_self")
     }
     catch(error){
          popupError(error)
     }
          
});
     
myLobbies.addEventListener("click", async (event)=>{
          
     try{
          const game = event.target.closest("a")
          if (!game) return;
          const gamecode = game.name     
          localStorage.setItem("gamecode", `${gamecode}`)
          localStorage.setItem("playerType", "player")
          localStorage.setItem("gamestarted","true")
          window.open(`game.html`, "_self")
     }
     catch(error){
          popupError(error)
     }
})
     
openLobbiesHolder.addEventListener("click", async (event)=>{
          
     try{
          const room = event.target.closest("a")
          if (!room) return;
          const roomcode = room.name
          localStorage.setItem("roomcode", `${roomcode}`)
          localStorage.setItem("gamestarted","false")
          localStorage.setItem("playerType", "player")
          window.open(`game.html?roomcode=${roomcode}`, "_self")
     }
     catch(error){
          popupError(error)
     }

})


async function showOpenLobbies(){

     openLobbiesHolder.innerHTML = null;
     let host = {}

     for(let i = 0; i < openLobbies.length; i++)
     {
          for(let j = 0; j < openLobbies[i].players.length; j++)
          {
               if (openLobbies[i].players[j].isHost == true)
               {
                    host = openLobbies[i].players[j].name
               }
          }
               //för att hitta html-properties -- https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset
               //för att hitta html-properties, specifikt för setAttribute -- https://stackoverflow.com/questions/1362513/can-i-use-javascript-to-set-the-name-attribute 
               //för att förhindra html injection -- https://www.youtube.com/watch?v=ns1LX6mEvyM & 
               //https://webextension-api.thunderbird.net/en/mv3/guides/innerHTML.html
               //för videon -- https://www.youtube.com/watch?v=znqUwx0b0HI&t=1s

               let holder = document.createElement("a")
               holder.classList.add("stb-lobby")
               holder.href="game.html"
               holder.setAttribute("name", `${openLobbies[i].roomCode}`)
               holder.setAttribute("onClick", "event.preventDefault();")
               let roomName = document.createElement("h1")
               roomName.classList.add("stb-logo")
               roomName.textContent = `${openLobbies[i].name}`
               holder.append(roomName)
               let video = `<video autoplay loop muted plays-inline class="back-video">
                        <source src="images/Skärminspelning 2026-09-18 191748.mp4" type="video/mp4">
                    </video>`
               holder.innerHTML += video
               let lobbyInfo = document.createElement("div")
               lobbyInfo.classList.add("stb-card-lobby")
               let hostText = document.createElement("p")
               hostText.textContent = `Host: ${host}`
               lobbyInfo.append(hostText)
               let playerCount = document.createElement("p")
               playerCount.textContent = `Antal spelare: ${openLobbies[i].currentPlayers}`
               lobbyInfo.append(playerCount)
               let maxPlayerCount = document.createElement("p")
               maxPlayerCount.textContent = `Max antal spelare: ${openLobbies[i].maxPlayers}`
               lobbyInfo.append(maxPlayerCount)
               holder.append(lobbyInfo)
               openLobbiesHolder.append(holder)
     }
}


function myRoomTitle(game) {
     if (!game.isMultiPlayer)
     {
          return "Singelplayerrum"
     }
     for (const prefix of [`${me.username}s `, `${me.username} `])
     {
          if (game.roomName.startsWith(prefix))
          {
               const rest = game.roomName.slice(prefix.length)
               if (rest.toLowerCase() == "rum")
               {
                    return "Mitt rum"
               }
               return rest.charAt(0).toUpperCase() + rest.slice(1)
          }
     }
     return game.roomName
}

async function showMyLobbies() {
     //https://stackoverflow.com/questions/8160494/how-to-make-a-whole-div-clickable-in-html-and-css-without-javascript -- för att göra divven klickbar och fortfarande behålla storleken
     if(myGames.length > 0)
     {
          let host = {}
          let gameType = {}

     for(let i = 0; i < myGames.length; i++)
     {
          for(let j = 0; j < myGames[i].players.length; j++)
          {
               if (myGames[i].players[j].isHost == true)
               {
                    host = myGames[i].players[j].name
               }
          }
          if(myGames[i].isMultiPlayer == true)
          {
               gameType = "Multiplayer"
          }
          else if(myGames[i].isMultiPlayer == false)
          {
               gameType = "Singleplayer"
          }
          if(myGames.length>0)
          {
               //https://stackoverflow.com/questions/20165590/make-a-clickable-link-with-onclick-but-without-href --- för att behålla interaktiviteten men samtidigt kunna kontrollera att redirecting sker i eventhandlern.
               let holder = document.createElement("a")
               holder.classList.add("stb-lobby")
               holder.href="game.html"
               holder.setAttribute("name", `${myGames[i].gameCode}`)
               holder.setAttribute("onClick", "event.preventDefault();")
               let roomName = document.createElement("h1")
               roomName.classList.add("stb-logo")
               roomName.textContent = myRoomTitle(myGames[i])
               holder.append(roomName)
               let video = `<video autoplay loop muted plays-inline class="back-video">
                        <source src="images/Skärminspelning 2026-09-18 191748.mp4" type="video/mp4">
                    </video>`
               holder.innerHTML += video
               let gameInfo = document.createElement("div")
               gameInfo.classList.add("stb-card-lobby")
               let gameTypeText = document.createElement("p")
               gameTypeText.textContent = `Spelläge: ${gameType}`
               gameInfo.append(gameTypeText)
               let hostText = document.createElement("p")
               hostText.textContent = `Host: ${host}`
               gameInfo.append(hostText)
               let playerCount = document.createElement("p")
               playerCount.textContent = `Antal spelare: ${myGames[i].playerCount}`
               gameInfo.append(playerCount)
               holder.append(gameInfo)
               myLobbies.append(holder)
          }
     }
}
     else 
     {
          myLobbies.innerHTML = "Inga aktiva rum hittades.";
     }
}


async function showActiveLobbies()
{

     lobbies.innerHTML = null;
     if(!games.length > 0)
     {
          lobbies.innerHTML = "Inga aktiva rum hittades.";
          return;
     }   
          let hostUserId = {}
          let host = {}
          let gameType = {}

     for(let i = 0; i < games.length; i++)
     {
          for(let j = 0; j < games[i].players.length; j++)
          {
               if (games[i].players[j].isHost == true)
               {
                    host = games[i].players[j].name
                    hostUserId = games[i].players[j].userId
               }
          }
          if(hostUserId == me.userId)
          {
               continue;
          }
          if(games[i].isMultiPlayer == true)
          {
               gameType = "Multiplayer"
          }
          else if(games[i].isMultiPlayer == false)
          {
               gameType = "Singleplayer"
          }
               let holder = document.createElement("a")
               holder.classList.add("stb-lobby")
               holder.href="game.html"
               holder.setAttribute("name", `${games[i].gameCode}`)
               holder.setAttribute("onClick", "event.preventDefault();")
               let roomName = document.createElement("h1")
               roomName.classList.add("stb-logo")
               roomName.textContent = `${games[i].roomName}`
               holder.append(roomName)
               let video = `<video autoplay loop muted plays-inline class="back-video">
                        <source src="images/Skärminspelning 2026-09-18 191748.mp4" type="video/mp4">
                    </video>`
               holder.innerHTML += video
               let gameInfo = document.createElement("div")
               gameInfo.classList.add("stb-card-lobby")
               let gameTypeText = document.createElement("p")
               gameTypeText.textContent = `Spelläge: ${gameType}`
               gameInfo.append(gameTypeText)
               let hostText = document.createElement("p")
               hostText.textContent = `Host: ${host}`
               gameInfo.append(hostText)
               let playerCount = document.createElement("p")
               playerCount.textContent = `Antal spelare: ${games[i].playerCount}`
               gameInfo.append(playerCount)
               holder.append(gameInfo)
               lobbies.append(holder)
     }
     if(lobbies.children.length == 0)
     {
          lobbies.innerHTML = "Inga aktiva rum hittades.";
     }

}



function popupError(error){

     // https://developer.mozilla.org/en-US/docs/Web/API/HTMLDialogElement/showModal -- för popup
    let errorpopup = document.querySelector("#popup")
    errorpopup.showModal()
    let msg = document.querySelector("#popup-message")
    msg.textContent = `${error}`
    const okbtn = document.querySelector("#popup-btn-ok")
    okbtn.addEventListener("click", ()=> errorpopup.close())
}



