using DSharpPlus.Entities;

namespace EmojiRaces;

public class RacePreface {
    public class InsufficentFundsException : Exception { }
    public class RacerDoesNotExistException : Exception { }
    public class RacerBetDoesNotExistExpcetion : Exception { }
    public class UserNeverBetException : Exception { }
    public class BetTooLowException : Exception { }
    public static readonly Dictionary<string, string> RacerDictionary = new Dictionary<string, string>(new KeyValuePair<string, string>[] {
        new KeyValuePair<string, string>("🤑", ":money_mouth:"),
        new KeyValuePair<string, string>("🥸", ":disguised_face:"),
        new KeyValuePair<string, string>("😆", ":laughing:"),
        new KeyValuePair<string, string>("🤣", ":rofl:"),
        new KeyValuePair<string, string>("😍", ":heart_eyes:"),
        new KeyValuePair<string, string>("🤪", ":zany_face:"),
        new KeyValuePair<string, string>("🤓", ":nerd:"),
        new KeyValuePair<string, string>("😭", ":sob:"),
        new KeyValuePair<string, string>("🥶", ":cold_face:"),
        new KeyValuePair<string, string>("😄", ":smile:"),
    });
    public static readonly List<string> PotentialRacers = new List<string>(RacerDictionary.Keys);

    private static string _randomRacer() {
        var random = new Random();
        return PotentialRacers[random.Next(PotentialRacers.Count)];
    }

    private readonly object _betsLock = new object();
    public Dictionary<DiscordUser, Dictionary<string, int>> Bets { get; private set; } = new Dictionary<DiscordUser, Dictionary<string, int>>();
    public Dictionary<string, int> BetsByRacer { get {
        var result = new Dictionary<string, int>();
        foreach (var racer in Racers)
            result[racer] = 0; // Populate result
        foreach (var (_, d) in Bets)
            foreach(var (racer, amount) in d)
                result[racer] += amount; // Collect bets
        return result;
    } }
    public readonly DiscordGuild Guild;
    public readonly HashSet<string> Racers;

    public event Func<Task> BetsChanged = async () => { };

    public RacePreface(DiscordGuild g) {
        Guild = g;

        Racers = new HashSet<string>();
        while (Racers.Count < 5)
            Racers.Add(_randomRacer());
    }

    // Places a bet. Raises InsufficentFundsException and RacerDoesNotExistException.
    public void PlaceBet(DiscordUser m, string racer, int amount) {
        if (!Racers.Contains(racer)) {
            throw new RacerDoesNotExistException();
        }

        if (!UserStates.Instance.DecrementBalance(m, amount)) {
            throw new InsufficentFundsException();
        }

        lock(_betsLock) {
            if (!Bets.ContainsKey(m)) { // User hasn't bet at all
                Bets[m] = new Dictionary<string, int>();
            }

            if (Bets[m].ContainsKey(racer)) {
                Bets[m][racer] += amount;
            } else { // User hasn't bet on this racer yet
                Bets[m][racer] = amount;
            }

            Task.Run(BetsChanged.Invoke);
        }
    }

    // Removes a bet. Raises RacerBetDoesNotExistException, BetTooLowException, 
    // UserNeverBetException and RacerDoesNotExistException.
    public void RemoveBet(DiscordUser m, string racer, int amount) {
        if (!Racers.Contains(racer)) {
            throw new RacerDoesNotExistException();
        }

        lock(_betsLock) {
            if (!Bets.ContainsKey(m)) {
                throw new UserNeverBetException();
            } if (!Bets[m].ContainsKey(racer)) { 
                throw new RacerBetDoesNotExistExpcetion();
            } if (Bets[m][racer] < amount) {
                throw new BetTooLowException();
            } 

            Bets[m][racer] -= amount;
            UserStates.Instance.IncrementBalance(m, amount);

            Task.Run(BetsChanged.Invoke);
        }
    }

    // Get the bets placed by m
    public Dictionary<string, int> GetBets(DiscordUser m) {
        lock(_betsLock) {
            if (!Bets.ContainsKey(m))
                Bets[m] = new Dictionary<string, int>();
            return Bets[m];
        } 
    }
}
