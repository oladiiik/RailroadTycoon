using System.Collections.Generic;
using Core.Algorithms.Graph;  

namespace Core.Game;

public sealed class GameState
{
    public double Budget        { get; private set; }
    public double CostPerKm     { get; }
    public double RewardPerCity { get; }
    public float  Threshold     { get; }

    public IReadOnlyList<Point> Cities        => _cities;
    public IReadOnlyList<(int from,int to)> Connections => _links;
    
    private readonly List<Point>     _cities;
    private readonly List<(int,int)> _links      = new();
    private readonly HashSet<int>    _connected  = new();   
    private readonly float[,]        _hmap;
    
    public GameState(
        float[,] hmap,
        List<Point> cities,
        double initialBudget,
        double costPerKm,
        double rewardPerCity,
        float threshold)
    {
        _hmap         = hmap;
        _cities       = cities;
        Budget        = initialBudget;
        CostPerKm     = costPerKm;
        RewardPerCity = rewardPerCity;
        Threshold     = threshold;
    }
    
    public (bool success, double? cost, string? reason) TryConnect(int from, int to)
    {
        if (from < 0 || from >= _cities.Count ||
            to   < 0 || to   >= _cities.Count || from == to)
            return (false, null, "index-out-of-range");

        if (_links.Exists(l => l == (from,to) || l == (to,from)))
            return (false, null, "already-connected");

        bool graphEmpty = _connected.Count == 0;
        bool aInGraph   = _connected.Contains(from);
        bool bInGraph   = _connected.Contains(to);

        if (!graphEmpty && !(aInGraph ^ bInGraph))
            return (false, null, "unreachable");          
        
        double distKm = _cities[from].DistanceTo(_cities[to]);
        double cost   = distKm * CostPerKm;
        if (cost > Budget) return (false, cost, "no-budget");
        
        if (!EconomicAnalyzer.HasDirectPath(_cities[from], _cities[to], _hmap, Threshold))
            return (false, cost, "blocked-by-relief");
        
        int rewardNodes = 0;
        
        if (graphEmpty) {
            rewardNodes = 1;  
        }
        else {
            if (!_connected.Contains(from)) rewardNodes++;
            if (!_connected.Contains(to))   rewardNodes++;
        }
        
        Budget -= cost;
        Budget += rewardNodes * RewardPerCity;

        _links.Add((from, to));
        _connected.Add(from);
        _connected.Add(to);

        return (true, cost, null);
    }
}