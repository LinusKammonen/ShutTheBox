import { leaveGameFromGameCode, leaveRoomFromRoomCode, joinRoomFromRoomCode, startGameFromRoom, getRoom, currentGames, rollDice, move, getFinishedGame, createRoom, getCurrentGameState, authMe } from "./game-services.js";
import { listenForGameFinished, listenForGameUpdates, joinGame, joinRoom, joinLobby, listenLobbyUpdates, listenForRoomUpdates } from "./gamehub-services.js";

const buttonElement = document.querySelector("#roll");
const buttonElementMove = document.querySelector("#move")
const tiles = document.querySelectorAll(".stb-tile")
const buttonElementNewRoom = document.querySelector(".stb-btn--ghost")
const currentPlayerName = document.querySelector("#activePlayerName")
const currentPlayerScore = document.querySelector("#activePlayerScore")
const currentPlayersList = document.querySelector(".stb-players") 
const leaderboard = document.querySelector(".stb-leaderboard")
const popupGameOver = document.querySelector("#popupGameOver")
const tilesLeft = document.querySelector("#tilesLeft")
const popupNewRoom = document.querySelector("#popupNewRoom") 
const buttonX = document.querySelector("#x")
const instruction = document.querySelector("#instruction")
const tilesList = document.querySelector("#tilesList");
const gameContent = document.querySelector(".stb-layout")
const buttonHolder = document.querySelector("#diceHolder")
gameContent.classList.add("hidden")
const btnStartGame = document.querySelector("#startGame")
const btnLeaveRoom = document.querySelector("#leaveRoom")
const btnLeaveGame = document.querySelector("#leaveGame")
btnStartGame.classList.add("hidden")
const roomCard = document.querySelector("#roomCard")
roomCard.classList.remove("no-space")
roomCard.classList.remove("hidden")

let textSpectators = document.querySelector("#spectators")
let textRoomName = document.querySelector("#roomName")
let roomPlayerCount = document.querySelector("#roomPlayerCount")
let roomStatusText = document.querySelector("#roomStatusText")
let sumtext = document.querySelector("#sum")
let die1text = document.querySelector("#die1")
let die2text = document.querySelector("#die2")
die1text.classList.add("hidden")
die2text.classList.add("hidden")
let gamestarted = {}
let finishedGame = {};
let die1 = 0;
let die2 = 0;
let sum = 0;
let currentDiceRoll = {}
let currentPlayers = {}
let currentPlayer = {}
let currentOpenNumbers = {}
let currentValidMoves = {}
let gamecode = {}
let currentGameState = {}
let playerType = {}
let roomcode = {};
let room = {};
let me = {};
let gameover = {};

me = await authMe()
displayUser(me)

gamestarted = localStorage.getItem("gamestarted")

if (gamestarted != "true"){
    initRoom()
}
else if(gamestarted == "true")
{
    initGame()
}

listenForGameFinished(updateGameFinishedGame)
listenForGameUpdates(updateGame)
listenForRoomUpdates(updateRoom);

// //https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/dataset -- för att hitta html-properties
// //https://stackoverflow.com/questions/195951/how-can-i-change-an-elements-class-with-javascript --för att lägga till klass vid klick

tilesList.addEventListener("click", async (event)=>{

    const pick1 = event.target.closest("button")
    if (!pick1) return;

    if(pick1.classList.contains("stb-tile--selected"))
    {
        updateTiles(findValidMoves(currentGameState.validMoves))
        for (let i = 0; i < tiles.length; i ++)
        {
            tiles[i].classList.remove("stb-tile--selected")                 
        }
        return;
    }
    if (!pick1.classList.contains("stb-tile--selected"))
    {
        pick1.classList.add("stb-tile--selected")            
    }
    const selectedTiles = document.querySelectorAll(".stb-tile--selected")
    let picks = [selectedTiles.length]
    for (let i = 0; i < selectedTiles.length; i++)
    {
        picks[i] = selectedTiles[i].dataset.value
    }
    
    let nextValidMove = []
    // https://stackoverflow.com/questions/53606337/check-if-array-contains-all-elements-of-another-array --- för every och includes
  
    for (let i = 0; i < currentGameState.validMoves.length; i++)
    {
        if(picks.every(pick => currentGameState.validMoves[i].toString().includes(pick)))
        {
            nextValidMove += currentGameState.validMoves[i]
        }
    }
    updateTiles(findValidMoves(nextValidMove))
    
    
});


async function makemove()
{
    const selectedButtons = document.querySelectorAll(".stb-tile--selected")
    let selectedTiles = [];
    for (let i = 0; i < selectedButtons.length; i++)
    {
       selectedTiles[i] = selectedButtons[i].dataset.value
    }
    await move(gamecode, selectedTiles)
    for (let i = 0; i < selectedButtons.length; i++)
    {
        selectedButtons[i].classList.remove("stb-tile--selected")
    }
}

//https://developer.mozilla.org/en-US/docs/Web/API/EventTarget/addEventListener -- för arrow funktionen
//https://stackoverflow.com/questions/50623279/js-event-handler-async-function -- la till async och await för att inte returnera en promise 

buttonElement.addEventListener("click", async() =>
{
    //https://stackoverflow.com/questions/54507913/how-to-use-a-for-loop-to-console-log-each-item-of-an-array --- för loop
    //https://stackoverflow.com/questions/24403732/how-to-check-if-array-is-empty-or-does-not-exist -- om arrayen är tom eller inte finns

    try 
    {
        if(!currentGameState.rules.isMultiplayer)
        {
            if(currentGameState.validMoves.length > 0)
            {
                await makemove()
            }
            await rollDice(gamecode) 
        }
        else
        {
            if(currentGameState.currentDiceRoll != null || buttonElement.textContent == "VALIDERA")
            {
                await makemove()
            }
            else if(currentGameState.currentDiceRoll == null){
                await rollDice(gamecode)
                buttonElement.textContent = "VALIDERA"
            }
        }
    }
    catch (error)
    {
        popupError(error)
    }
       
})


function displayUser (user){

     let username = document.createElement("p")
     username.textContent = "Inloggad: " + user.username
     currentUser.append(username)
}
function getGameCode(){
    gamecode = localStorage.getItem("gamecode")
};
function getPlayerType(){
    playerType = localStorage.getItem("playerType")
}

async function updateGameFinishedGame(update) {
    try{
        await displayFinishedGame(update.players)
        gameover = true;
    }
    catch(error){
    }
}


async function updateGame(update){
    
    currentGameState = update
    console.log(currentGameState)
    currentDiceRoll = currentGameState.currentDiceRoll
    currentPlayers = currentGameState.players
    currentPlayer = currentPlayers.filter(currentPlayers => currentPlayers.userId == (currentGameState.currentPlayerUserId))    
    currentOpenNumbers = currentPlayer[0].openNumbers
    currentValidMoves = currentGameState.validMoves
    showSpectators(update)
    updatePlayers(currentPlayers)
    updateTiles(findValidMoves(currentValidMoves))
    updateUsedTiles(currentOpenNumbers)
    updateInstruction(currentGameState)
    updateRollButton(update)
    if(update.updateReason == "DiceRolled")
    {
        updateDice(update)
    }
    
    
}

async function updateRollButton(game){
    if(game.rules.isMultiplayer == true){
        if(game.currentDiceRoll == null)
        {
            buttonElement.textContent = "SLÅ TÄRNINGARNA"
        }
        else if(game.currentDiceRoll != null)
        {
            buttonElement.textContent = "VALIDERA"
        }
        if(game.currentPlayerUserId != me.userId)
        {
            buttonElement.classList.add("stb-tile--shut")
        }
        else{
            buttonElement.classList.remove("stb-tile--shut")
        }
    }
    
}
async function initGame()
{
    try {
    roomCard.classList.add("hidden")
    roomCard.classList.add("no-space")
    gameContent.classList.remove("hidden")
    await getGameCode()
    await getPlayerType()
    await joinGame(gamecode, playerType)
    if(playerType == "spectator"){
            buttonElement.classList.add("hidden")
            buttonElementNewRoom.classList.add("hidden")
            buttonElementNewRoom.disabled = true;
            btnLeaveGame.textContent = "Lämna"
            for(let i = 0; i < tiles.length; i++)
             {
                   tiles[i].disabled = true
                   tiles[i].classList.add("stb-disable-cursor")
             }
            
    }
    currentGameState = await getCurrentGameState(gamecode)
    currentDiceRoll = currentGameState.currentDiceRoll
    currentPlayers = currentGameState.players
    currentPlayer = currentPlayers.filter(currentPlayers => currentPlayers.userId == (currentGameState.currentPlayerUserId))   
    currentOpenNumbers = currentPlayer[0].openNumbers
    currentValidMoves = currentGameState.validMoves
    updatePlayers(currentPlayers)
    updateTiles(findValidMoves(currentValidMoves))
    updateUsedTiles(currentOpenNumbers)
    updateInstruction(currentGameState)
    

    showSpectators(currentGameState)
    updateRollButton(currentGameState)
    if (!currentValidMoves.length == 0)
    {   
         //kontrollerar att tärningen har kastats minst en gång
        updateDice(currentGameState)
    }   
    if(currentGameState.updateReason == "GameFinished")
    {
        finishedGame = await getFinishedGame(gamecode)
        await displayFinishedGame(finishedGame.players)
    }        

    }
    catch(error){
        popupError(error)
    }
}


async function updateRoom(update){
    try{
    room = update.room
    await updateRoomPlayers(room.players)
    roomPlayerCount.textContent = room.players.length
    if(room.players.length < room.minPlayers)
    {
        roomStatusText.textContent = "Väntar på spelare"
    }
    else if(room.players.length >= room.minPlayers)
    {
        roomStatusText.textContent = "Väntar på att hosten ska starta spelet"
    }
    if (update.updateReason == "GameStarted")
    {
        gamestarted = "true"
        gamecode = update.gameCode
        localStorage.setItem("gamecode", gamecode)
        initGame()
        return;
    }
    }
    catch(error){
        popupError(error)
    }
    
}
listenForRoomUpdates(updateRoom);
listenForGameUpdates(updateGame)    

async function initRoom()
{
    try {
    await getRoomCode()
    room = await getRoom(roomcode)
    await joinRoomFromRoomCode(roomcode)
    await joinRoom(roomcode)
    textRoomName.textContent = room.name
    room = await getRoom(roomcode)
    updateRoomPlayers(room.players)
    roomPlayerCount.textContent = room.players.length

    if(room.players.length < room.minPlayers)
    {
        roomStatusText.textContent = "Väntar på spelare"
    }
    else if(room.players.length < room.minPlayers)
    {
        roomStatusText.textContent = "Väntar på att hosten ska starta spelet"
    }
    for(let i = 0; i < room.players.length; i++){
        if(room.players[i].isHost == true){
            
            if(room.players[i].userId == me.userId)
            {
                btnStartGame.classList.remove("hidden")
                //då får man lov att starta spelet 
            }
        }
    }
    if(room.status == "Playing")
    {
        gamestarted = "true"
        let game = await currentGames()
        gamecode = game.gameCode
        localStorage.setItem("gamecode", `${gamecode}`)
        initGame()
        return;
    }

}
catch(error)
{
    popupError(error)
}
}

function getRoomCode(){
    roomcode = localStorage.getItem("roomcode")
}
    
function updateDice(dice){

    //https://stackoverflow.com/questions/5113246/make-div-invisible-in-css-and-javascript -- för visibility
    die2text.classList.remove("hidden")
    die1text.classList.remove("hidden")
    die1 = dice.currentDiceRoll.die1
    die2 = dice.currentDiceRoll.die2
    sum = dice.currentDiceRoll.total
    if (die2 == 0)
    {
        die2text.classList.add("hidden")
    }
    sumtext.textContent = sum.toString();
    die1text.dataset.value = die1.toString();
    die2text.dataset.value = die2.toString();
}

//https://stackoverflow.com/questions/9329446/loop-for-each-over-an-array-in-javascript -- för loopen
//https://stackoverflow.com/questions/7526601/setattributedisabled-false-changes-editable-attribute-to-false -- boolean


function findValidMoves(validMoves)
{
    let valid = {};
    for(let i = 0; i < validMoves.length; i ++)
    {
        for (let j = 0; j < validMoves[i].length; j++){
             
             valid += validMoves[i][j]
        }
    }

    let validnumbers = [];

    for(let i = 0; i < valid.length; i++)
    {
        validnumbers[i] = valid[i]
    }
    // valid är nu bara en talföljd, vi behöver ta bort dubletter 
    validnumbers = validnumbers.filter(onlyUnique)
    return validnumbers;
}

//https://stackoverflow.com/questions/1960473/get-all-unique-values-in-a-javascript-array-remove-duplicates 

function onlyUnique(value, index, array){
    return array.indexOf(value) === index;
}

function updateUsedTiles(openNumbers){
    for(let i = 0; i < tiles.length; i++)
        {
            tiles[i].classList.add("hidden")
        }
        for(let i = 0; i < tiles.length; i++) 
        {             
            for (let j = 0; j <openNumbers.length; j++)
            {
                if (tiles[i].dataset.value == openNumbers[j])
                {
                    tiles[i].classList.remove("hidden")             
                }
            }
        } 
        tilesLeft.textContent = openNumbers.length

}


function updateTiles(validMoves){
    for(let i = 0; i < tiles.length; i++)
        {
            tiles[i].classList.add("stb-tile--shut")
            tiles[i].disabled = true
        }
        for(let i = 0; i < tiles.length; i++) 
        {             
            for (let j = 0; j <validMoves.length; j++)
            {
                if (tiles[i].dataset.value == validMoves[j])
                {
                    tiles[i].classList.remove("stb-tile--shut")
                    if(playerType != "spectator")
                    {
                        tiles[i].disabled = false      
                    }             
                }  
            }

        } 
}

function showSpectators(game){
    
    let words = ["spionerar", "tjuvtittar", "spanar", "tittar förbi"]
    let spectators = [];
    textSpectators.textContent = "Åskådare: "

    for (let i = 0; i < game.spectators.length; i++){
        spectators[i] = game.spectators[i].nickname
        textSpectators.textContent += `En ${spectators[i]} ${words[i]}. ` 
    }
}

function displayFinishedGame(players){
    
    //https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/sort 
    //-- för att sortera så att vinnaren visas högst upp
    const sortedPlayers = [...players].sort((a, b) => a.placement - b.placement);

    leaderboard.innerHTML = null
    popupGameOver.showModal()

    let leaderboardCards = {}

    //https://stackoverflow.com/questions/31316445/javascript-innerhtml
    //https://www.youtube.com/watch?v=e0ihEHxd6vI -- för appendChild
    for (let i = 0; i < sortedPlayers.length; i++)
    {
        leaderboardCards[i] = document.createElement("li")
        leaderboardCards[i].classList.add("stb-player")
        let playerScore = document.createElement("span")
        let playerName = document.createElement("span")
        playerName.classList.add("stb-player-name")
        playerName.textContent = sortedPlayers[i].placement + ". " + sortedPlayers[i].name
        playerScore.classList.add("stb-player-score")
        playerScore.textContent = sortedPlayers[i].finalScore
        leaderboardCards[i].appendChild(playerName)
        leaderboardCards[i].appendChild(playerScore)
        leaderboard.appendChild(leaderboardCards[i])
    }

}



function updateRoomPlayers(players){

    currentPlayersList.innerHTML = null
    let playerCards = {}

    for (let i = 0; i < players.length; i++){
        playerCards[i] = document.createElement("li");
        playerCards[i].classList.add("stb-player")
        let playerName = document.createElement("span")
        playerName.classList.add("stb-player-name")
        playerName.textContent = players[i].name
        if (players[i].isHost == true)
        {
            let playerHostBadge = document.createElement("span")
            playerHostBadge.classList.add("stb-player-host")
            playerHostBadge.textContent = "Host"
            playerName.appendChild(playerHostBadge)
        }
        playerCards[i].appendChild(playerName)
        currentPlayersList.appendChild(playerCards[i])
    }
        
}

function updatePlayers(players){
    
    currentPlayersList.innerHTML = null
    //https://stackoverflow.com/questions/31316445/javascript-innerhtml
    //https://www.youtube.com/watch?v=e0ihEHxd6vI -- för appendChild

    let playerCards = {}

    for (let i = 0; i < players.length; i++ )
    {
        playerCards[i] = document.createElement("li");
        playerCards[i].classList.add("stb-player")
        if (players[i].isCurrentPlayer == true)
        {
            playerCards[i].classList.add("stb-player--active")
        }

        let playerScore = document.createElement("span")
        let playerName = document.createElement("span")
        playerName.classList.add("stb-player-name")
        playerName.textContent = players[i].name
        playerScore.classList.add("stb-player-score")
        playerScore.textContent = players[i].currentScore
        if(players[i].exitReason == "NoValidMoves"){
            playerCards[i].classList.add("stb-tile--shut")
            playerName.textContent += " (Slut på drag!)"
        }

        if (players[i].isHost == true)
        {
            let playerHostBadge = document.createElement("span")
            playerHostBadge.classList.add("stb-player-host")
            playerHostBadge.textContent = "Host"
            playerName.appendChild(playerHostBadge)
        }
        playerCards[i].appendChild(playerName)
        playerCards[i].appendChild(playerScore)
        currentPlayersList.appendChild(playerCards[i])
    }


}

function updateInstruction(game){
    
    if (game.validMoves.length == 0 && game.updateReason != "DiceRolled")
    {
        //Säkerställer att tärningarna ännu inte kastats
          instruction.textContent = "kasta tärningarna."
    }
    else{
          instruction.textContent = "Stäng brickorna och kasta sedan tärningarna."
    }
}

btnLeaveRoom.addEventListener("click", async()=>{
    try{
        await leaveRoomFromRoomCode(roomcode)
        window.open("index.html", "_self")
    }
    catch(error){
        popupError(error)
    }
})

btnStartGame.addEventListener("click", async()=>{
    try{
       await startGameFromRoom(roomcode)
    }
    catch(error){
        popupError(error)
    }
})

buttonX.addEventListener("click", () => {
    popupGameOver.close()
})


buttonElementNewRoom.addEventListener("click", async() =>{
//https://developer.mozilla.org/en-US/docs/Web/API/Location/reload --- för att sidan ska uppdateras direkt vid start av nytt spel
    try{
        gameover == false;
        const newRoom = await createRoom("", 1)
        const gamecode = newRoom.state.gameCode  
        localStorage.setItem("gamecode", `${gamecode}`)
        localStorage.setItem("playerType", "player")
        localStorage.setItem("gamestarted","true")
        window.open(`game.html?gamecode=${gamecode}`, "_self")
    }
    catch(error){
        popupError(error.details)
    }

})

btnLeaveGame.addEventListener("click", async() =>{
    try{
        await leaveGameFromGameCode(gamecode)
        window.open("index.html", "_self")
    }
    catch(error){
        popupError(error)
    }
})

function popupError(error){
  
    let popup = document.querySelector("#popup")
    popup.showModal()
    let msg = document.querySelector("#popup-message")
    msg.textContent = `${error}`
    const okbtn = document.querySelector("#popup-btn-ok")
    okbtn.addEventListener("click", ()=> popup.close())
}


