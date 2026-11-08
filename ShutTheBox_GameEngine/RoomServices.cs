namespace ShutTheBox_GameEngine;

public class RoomServices
{
    public const int MaxPlayersLimit = 8;

    private readonly GameServices gameServices;
    private readonly Dictionary<string, Room> rooms = new();

    public RoomServices(GameServices gameServices)
    {
        this.gameServices = gameServices;
        this.gameServices.GameFinished += OnGameFinished;
    }

    private object SyncRoot => gameServices.SyncRoot;

    public CreateRoomResult CreateRoom(string name, int minPlayers, int maxPlayers, string userId, string userName)
    {
        lock (SyncRoot)
        {
            if (maxPlayers < 1 || maxPlayers > MaxPlayersLimit)
            {
                throw new GameException($"Max antal spelare måste vara mellan 1 och {MaxPlayersLimit}.");
            }

            if (maxPlayers == 1)
            {
                minPlayers = 1;
            }
            else if (minPlayers < 2 || minPlayers > maxPlayers)
            {
                throw new GameException($"Min antal spelare måste vara mellan 2 och {maxPlayers}.");
            }

            EnsureUserIsFree(userId);

            Room room = new Room
            {
                RoomCode = CodeGenerator.NewCode(code => rooms.ContainsKey(code)),
                Name = CreateRoomName(name, maxPlayers, userName),
                MinPlayers = minPlayers,
                MaxPlayers = maxPlayers,
                Players = { new RoomPlayer { UserId = userId, Name = userName, IsHost = true } }
            };
            rooms[room.RoomCode] = room;

            GameState? state = maxPlayers == 1 ? StartGameFromRoom(room) : null;
            return new CreateRoomResult(room, state);
        }
    }

    public static string CreateRoomName(string name, int maxPlayers, string userName)
    {
        string owner = userName.EndsWith('s') || userName.EndsWith('x') || userName.EndsWith('z')
            ? userName
            : userName + "s";

        if (maxPlayers == 1)
        {
            return $"{owner} singelplayerrum";
        }

        return string.IsNullOrWhiteSpace(name) ? $"{owner} rum" : name.Trim();
    }

    public Room GetRoom(string roomCode)
    {
        lock (SyncRoot)
        {
            return rooms.GetValueOrDefault(roomCode)
                ?? throw new NotFoundException($"Hittade inget rum med koden {roomCode}.");
        }
    }

    public List<Room> GetOpenRooms()
    {
        lock (SyncRoot)
        {
            return rooms.Values
                .Where(room => room.Status == RoomStatuses.Open)
                .OrderBy(room => room.CreatedAtUtc)
                .ToList();
        }
    }

    public Room JoinRoom(string roomCode, string userId, string userName)
    {
        lock (SyncRoot)
        {
            Room room = GetRoom(roomCode);

            if (room.Players.Any(player => player.UserId == userId))
            {
                return room;
            }

            if (room.Status != RoomStatuses.Open)
            {
                throw new GameException("Spelet i det här rummet har redan startat.");
            }

            if (room.Players.Count >= room.MaxPlayers)
            {
                throw new GameException("Rummet är fullt.");
            }

            EnsureUserIsFree(userId);

            room.Players.Add(new RoomPlayer { UserId = userId, Name = userName, IsHost = false });
            return room;
        }
    }

    public Room LeaveRoom(string roomCode, string userId)
    {
        lock (SyncRoot)
        {
            Room room = GetRoom(roomCode);
            RoomPlayer? player = room.Players.FirstOrDefault(p => p.UserId == userId);

            if (player == null)
            {
                return room;
            }

            if (room.Status == RoomStatuses.Playing)
            {
                throw new GameException("Spelet har redan startat. Lämna spelet i stället.");
            }

            room.Players.Remove(player);

            if (room.Players.Count == 0)
            {
                room.Status = RoomStatuses.Closed;
                rooms.Remove(room.RoomCode);
            }
            else if (player.IsHost)
            {
                room.Players[0].IsHost = true;
            }

            return room;
        }
    }

    public GameState StartGame(string roomCode, string userId)
    {
        lock (SyncRoot)
        {
            Room room = GetRoom(roomCode);

            if (room.Status != RoomStatuses.Open)
            {
                throw new GameException("Spelet har redan startat.");
            }

            if (!room.Players.Any(player => player.UserId == userId && player.IsHost))
            {
                throw new GameException("Endast värden kan starta spelet.");
            }

            if (room.Players.Count < room.MinPlayers)
            {
                throw new GameException($"Det behövs minst {room.MinPlayers} spelare för att starta.");
            }

            return StartGameFromRoom(room);
        }
    }

    private GameState StartGameFromRoom(Room room)
    {
        GameState game = gameServices.CreateGame(room.RoomCode, room.Name, room.Players);
        room.Status = RoomStatuses.Playing;
        room.GameCode = game.GameCode;
        return game;
    }

    private void EnsureUserIsFree(string userId)
    {
        Room? openRoom = rooms.Values.FirstOrDefault(room =>
            room.Status == RoomStatuses.Open &&
            room.Players.Any(player => player.UserId == userId));

        if (openRoom != null)
        {
            throw new GameException($"Du är redan med i rummet \"{openRoom.Name}\". Lämna det först.");
        }

        if (gameServices.GetCurrentGameForUser(userId) != null)
        {
            throw new GameException("Du är redan med i ett pågående spel. Lämna det först.");
        }
    }

    private void OnGameFinished(GameState game, GameResult result)
    {
        lock (SyncRoot)
        {
            if (rooms.TryGetValue(game.RoomCode, out Room? room))
            {
                room.Status = RoomStatuses.Finished;
            }
        }
    }
}
