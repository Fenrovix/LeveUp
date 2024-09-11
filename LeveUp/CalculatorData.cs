using FFXIVClientStructs.FFXIV.Client.Game.UI;
using LeveUp.Windows;
using Lumina.Excel.GeneratedSheets;

namespace LeveUp;

public class CalculatorData(int index)
{
    // public properties
    public List<(Leve? leve, int nq, int hq)> LeveTable { get; private set; } = new();
    public bool LeveOverriden { get; private set; }
    
    // private properties
    private int TargetLevel { get; set; }
    private int TotalExpNeeded { get; set; }
   
    // Read only properties
    private string Job { get; } = Data.Jobs[index];
    private int JobIndex { get; } = index + 7;  // 7 is where CRP starts in param list
    

    private int currentLevelExpCached;
    private int NextLevelExpNeeded => Data.ParamGrows!.GetRow((uint)Data.PlayerJobLevel(JobIndex))!.ExpToNext;
    

    public void CheckTargetLevel(int targetLevel)
    {
        if (targetLevel != TargetLevel || currentLevelExpCached != Data.PlayerJobExperience(JobIndex))
        {
            currentLevelExpCached = Data.PlayerJobExperience(JobIndex);
            TargetLevel = targetLevel;
            Calculate();   
        }
    }

    public void Calculate()
    {
        if(Configuration.Automatic) CalculateAutomatic();
    }

    private void CalculateAutomatic()
    {
        LeveTable.Clear();
        TotalExpNeeded = 0;
        var currentLevel = Data.PlayerJobLevel(JobIndex);
        for (var level = currentLevel; level < TargetLevel; level++)
        {
            var expToNext = Data.ParamGrows.GetRow((ushort)level).ExpToNext;
            if (level == currentLevel) expToNext -= Data.PlayerJobExperience(JobIndex);

            var levePair = Data.BestLeves[Job][GetLevelKey(level)];
            var selectedLeve = Configuration.LargeLeves && levePair.large != null ? levePair.large : levePair.normal;
            
            var nq = (int)MathF.Ceiling((float)expToNext / selectedLeve.ExpReward);
            var hq = (int)MathF.Ceiling((float)expToNext / (selectedLeve.ExpReward * 2));

            if (LeveTable.Count > 0 && LeveTable[^1].leve.DataId == selectedLeve.DataId)
            {
                var lastLeve = LeveTable[^1];
                LeveTable[^1] = (lastLeve.leve, lastLeve.nq + nq, lastLeve.hq + hq);
            }
            else LeveTable.Add((selectedLeve, nq, hq));

            TotalExpNeeded += expToNext;
        }
    }
    
    private static int GetLevelKey(int level)
    {
        if (level < 5) return 1;
        return level <= 50
                   ? level / 5 * 5  // For levels <= 50, clamp down to nearest multiple of 5
                   : level / 2 * 2; // For levels > 50, clamp down to even number
    }
    
    public string LevelExpLabel => 
        $"[ Level: {Data.PlayerJobLevel(JobIndex)} ] [ {Data.PlayerJobExperience(JobIndex):N0}/{NextLevelExpNeeded:N0}" +
        $"  ({(float)Data.PlayerJobExperience(JobIndex)/NextLevelExpNeeded:P}%) ]";

    public string TotalExpLabel => $"Total Exp Needed: {Math.Clamp(TotalExpNeeded, 0, float.MaxValue):N0}";
    
    //public string NormalQualityTurnInLabel => $"{Math.Clamp(MathF.Ceiling((float)TotalExpNeeded/SuggestedLeves[0].ExpReward), 0, float.MaxValue):N0}x";
    //public string HighQualityTurnInLabel => $"{Math.Clamp(MathF.Ceiling((float)TotalExpNeeded/SuggestedLeves[0].ExpReward / 2), 0, float.MaxValue):N0}x";
}
