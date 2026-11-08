namespace ShutTheBox_GameEngine;

public class GameServices
{
    private readonly IDice dice;
    private readonly Dictionary<string, GameState> games = new();
    private readonly Dictionary<string, GameResult> results = new();

    internal object SyncRoot { get; } = new();

    public event Action<GameState, GameResult>? GameFinished;

    public GameServices(IDice? dice = null)
    {
        this.dice = dice ?? new RandomDice();
    }

    public GameState CreateGame(string roomCode, string roomName, IReadOnlyList<RoomPlayer> roomPlayers)
    {
        if (roomPlayers.Count == 0)
        {
            throw new GameException("Ett spel måste ha minst en spelare.");
        }

        lock (SyncRoot)
        {
            GameState game = new GameState
            {
                GameCode = CodeGenerator.NewCode(code => games.ContainsKey(code)),
                RoomCode = roomCode,
                RoomName = roomName,
                UpdateReason = UpdateReasons.GameStarted,
                IsStarted = true,
                StartedAtUtc = DateTimeOffset.UtcNow,
                Rules = new Rules { IsMultiplayer = roomPlayers.Count > 1 },
                Players = roomPlayers.Select(roomPlayer => new Player
                {
                    UserId = roomPlayer.UserId,
                    Name = roomPlayer.Name,
                    IsHost = roomPlayer.IsHost,
                    IsActive = true
                }).ToList()
            };

            SetCurrentPlayer(game, game.Players[0]);
            games[game.GameCode] = game;
            return game;
        }
    }

    public GameState? GetGameFromGameCode(string gamecode)
    {
        lock (SyncRoot)
        {
            return games.GetValueOrDefault(gamecode);
        }
    }

    public GameState GetGame(string gamecode)
    {
        return GetGameFromGameCode(gamecode)
            ?? throw new NotFoundException($"Hittade inget spel med koden {gamecode}.");
    }

    public List<GameState> GetActiveGames()
    {
        lock (SyncRoot)
        {
            return games.Values.Where(game => !game.IsGameOver).ToList();
        }
    }

    public GameState? GetCurrentGameForUser(string userId)
    {
        lock (SyncRoot)
        {
            return games.Values.FirstOrDefault(game =>
                !game.IsGameOver &&
                game.Players.Any(player => player.UserId == userId && !player.HasLeft));
        }
    }

    public GameResult GetResult(string gamecode)
    {
        lock (SyncRoot)
        {
            GameState game = GetGame(gamecode);
            return results.GetValueOrDefault(game.GameCode)
                ?? throw new GameException("Spelet är inte slut än.");
        }
    }

    public List<List<int>> GetValidMoves(string gamecode)
    {
        return GetGame(gamecode).ValidMoves;
    }

    public GameState RollDice(string gamecode, string userId)
    {
        return Update(gamecode, game =>
        {
            Player currentPlayer = RequireCurrentPlayer(game, userId);

            if (game.CurrentDiceRoll != null)
            {
                throw new GameException("Du har redan slagit. Stäng brickor som matchar summan först.");
            }

            bool rollOneDie = currentPlayer.OpenNumbers.All(number => number <= 6);
            int dieOne = dice.Roll();
            int dieTwo = rollOneDie ? 0 : dice.Roll();

            game.CurrentDiceRoll = new CurrentDiceRoll
            {
                Die1 = dieOne,
                Die2 = dieTwo,
                DiceCount = rollOneDie ? 1 : 2,
                Total = dieOne + dieTwo
            };
            game.ValidMoves = FindValidMoves(currentPlayer.OpenNumbers, game.CurrentDiceRoll.Total);
            game.UpdateReason = UpdateReasons.DiceRolled;

            if (game.ValidMoves.Count == 0)
            {
                EndPlayerRound(game, currentPlayer, ExitReasons.NoValidMoves);
            }
        });
    }

    public GameState MakeMove(string gamecode, string userId, IEnumerable<int> selectedNumbers)
    {
        return Update(gamecode, game =>
        {
            Player currentPlayer = RequireCurrentPlayer(game, userId);

            if (game.CurrentDiceRoll == null)
            {
                throw new GameException("Du måste slå tärningarna först.");
            }

            List<int> selected = selectedNumbers.OrderBy(number => number).ToList();

            if (selected.Count == 0)
            {
                throw new GameException($"Välj brickor som tillsammans blir {game.CurrentDiceRoll.Total} innan du går vidare.");
            }

            if (!game.ValidMoves.Any(move => move.SequenceEqual(selected)))
            {
                throw new GameException(
                    $"Brickorna [{string.Join(", ", selected)}] är inget giltigt drag för summan {game.CurrentDiceRoll.Total}.");
            }

            currentPlayer.OpenNumbers.RemoveAll(number => selected.Contains(number));
            game.CurrentDiceRoll = null;
            game.ValidMoves = new List<List<int>>();
            game.UpdateReason = UpdateReasons.MoveMade;

            if (currentPlayer.OpenNumbers.Count == 0)
            {
                EndPlayerRound(game, currentPlayer, ExitReasons.BoxShut);
            }
        });
    }

    public GameState LeaveGame(string gamecode, string userId)
    {
        return Update(gamecode, game =>
        {
            Player? player = game.Players.FirstOrDefault(p => p.UserId == userId);

            if (player == null)
            {
                RemoveSpectatorFrom(game, userId);
                return;
            }

            if (game.IsGameOver || player.HasLeft)
            {
                return;
            }

            player.HasLeft = true;
            game.UpdateReason = UpdateReasons.PlayerLeft;

            if (player.IsActive)
            {
                EndPlayerRound(game, player, ExitReasons.Left);
            }
        });
    }

    public GameState AddSpectator(string gamecode, string userId, string nickname)
    {
        return Update(gamecode, game =>
        {
            bool isPlayer = game.Players.Any(player => player.UserId == userId);
            bool isSpectator = game.Spectators.Any(spectator => spectator.UserId == userId);

            if (isPlayer || isSpectator)
            {
                return;
            }

            game.Spectators.Add(new Spectator { UserId = userId, Nickname = nickname });
            game.UpdateReason = UpdateReasons.SpectatorJoined;
        });
    }

    public GameState RemoveSpectator(string gamecode, string userId)
    {
        return Update(gamecode, game => RemoveSpectatorFrom(game, userId));
    }

    private static void RemoveSpectatorFrom(GameState game, string userId)
    {
        if (game.Spectators.RemoveAll(spectator => spectator.UserId == userId) > 0)
        {
            game.UpdateReason = UpdateReasons.SpectatorLeft;
        }
    }

    public static List<List<int>> FindValidMoves(IEnumerable<int> openNumbers, int total)
    {
        List<List<int>> validMoves = new List<List<int>>();
        int[] sortedNumbers = openNumbers
            .Where(number => number <= total)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        List<int> current = new List<int>();

        FindCombinations(sortedNumbers, 0, total, current, validMoves);

        return validMoves;
    }

    private static void FindCombinations(int[] openNumbers, int startIndex, int remaining, List<int> current, List<List<int>> validMoves)
    {
        if (remaining == 0)
        {
            validMoves.Add(new List<int>(current));
            return;
        }

        for (int i = startIndex; i < openNumbers.Length; i++)
        {
            int number = openNumbers[i];

            if (number > remaining)
            {
                break;
            }

            current.Add(number);
            FindCombinations(openNumbers, i + 1, remaining - number, current, validMoves);
            current.RemoveAt(current.Count - 1);
        }
    }

    private GameState Update(string gamecode, Action<GameState> change)
    {
        GameState game;
        GameResult? finishedResult = null;

        lock (SyncRoot)
        {
            game = GetGame(gamecode);
            bool wasOver = game.IsGameOver;

            change(game);

            if (!wasOver && game.IsGameOver)
            {
                finishedResult = results[game.GameCode];
            }
        }

        if (finishedResult != null)
        {
            GameFinished?.Invoke(game, finishedResult);
        }

        return game;
    }

    private static Player RequireCurrentPlayer(GameState game, string userId)
    {
        if (game.IsGameOver)
        {
            throw new GameException("Spelet är redan slut.");
        }

        Player? player = game.Players.FirstOrDefault(p => p.UserId == userId);

        if (player == null)
        {
            throw new GameException("Du är inte med i det här spelet.");
        }

        if (game.CurrentPlayerUserId != userId)
        {
            throw new GameException($"Det är inte din tur. Det är {game.CurrentPlayerName}s tur.");
        }

        return player;
    }

    private void EndPlayerRound(GameState game, Player player, string exitReason)
    {
        player.IsActive = false;
        player.ExitReason = exitReason;

        if (exitReason == ExitReasons.NoValidMoves)
        {
            game.UpdateReason = UpdateReasons.NoValidMoves;
        }

        if (player.UserId != game.CurrentPlayerUserId)
        {
            return;
        }

        game.CurrentDiceRoll = null;
        game.ValidMoves = new List<List<int>>();
        AdvanceTurn(game);
    }

    private void AdvanceTurn(GameState game)
    {
        int currentIndex = game.Players.FindIndex(player => player.UserId == game.CurrentPlayerUserId);

        for (int step = 1; step <= game.Players.Count; step++)
        {
            Player next = game.Players[(currentIndex + step) % game.Players.Count];

            if (next.IsActive)
            {
                SetCurrentPlayer(game, next);
                return;
            }
        }

        FinishGame(game);
    }

    private static void SetCurrentPlayer(GameState game, Player player)
    {
        foreach (Player p in game.Players)
        {
            p.IsCurrentPlayer = p == player;
        }

        game.CurrentPlayerUserId = player.UserId;
        game.CurrentPlayerName = player.Name;
    }

    private void FinishGame(GameState game)
    {
        game.IsGameOver = true;
        game.EndedAtUtc = DateTimeOffset.UtcNow;
        game.UpdateReason = UpdateReasons.GameFinished;
        game.CurrentDiceRoll = null;
        game.ValidMoves = new List<List<int>>();

        foreach (Player player in game.Players)
        {
            player.IsCurrentPlayer = false;
        }

        List<Player> finishers = game.Players.Where(p => p.ExitReason != ExitReasons.Left).ToList();
        List<Player> abandoned = game.Players.Where(p => p.ExitReason == ExitReasons.Left).ToList();
        List<PlayerResult> playerResults = new List<PlayerResult>();

        foreach (Player player in finishers)
        {
            int placement = 1 + finishers.Count(other => other.CurrentScore < player.CurrentScore);
            bool isWinner = placement == 1;

            player.HasWon = isWinner;
            player.HasLost = !isWinner;
            playerResults.Add(ToResult(player, placement, isWinner));
        }

        foreach (Player player in abandoned)
        {
            player.HasLost = true;
            playerResults.Add(ToResult(player, finishers.Count + 1, false));
        }

        int winnerCount = playerResults.Count(result => result.IsWinner);
        game.TiedWinners = winnerCount > 1;

        results[game.GameCode] = new GameResult
        {
            GameCode = game.GameCode,
            RoomCode = game.RoomCode,
            StartedAtUtc = game.StartedAtUtc,
            EndedAtUtc = game.EndedAtUtc.Value,
            PlayerCount = game.Players.Count,
            EndReason = finishers.Count > 0 ? EndReasons.Completed : EndReasons.Abandoned,
            HasWinner = winnerCount > 0,
            TiedWinners = game.TiedWinners,
            Players = playerResults.OrderBy(result => result.Placement).ToList()
        };
    }

    private static PlayerResult ToResult(Player player, int placement, bool isWinner)
    {
        return new PlayerResult
        {
            UserId = player.UserId,
            Name = player.Name,
            IsWinner = isWinner,
            Placement = placement,
            FinalScore = player.CurrentScore,
            ExitReason = player.ExitReason,
            Abandoned = player.ExitReason == ExitReasons.Left
        };
    }
}
