using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;

namespace LeveUp;

public unsafe class CalculatorData(PlayerState* playerState, int jobIndex)
{
    // public properties
    public bool LargeLeves { get; set; } = false;
    public Leve?[] SuggestedLeve { get; private set; } = [null];
    
    // private properties
    private int TargetLevel { get; set; }
    private int TotalExpNeeded { get; set; }
    

   
    // Read only properties
    private PlayerState* PlayerState { get; } = playerState;
    private string Job { get; } = Data.Jobs[jobIndex];
    private int JobIndexOffset { get; } = jobIndex + 7;  // 7 is where CRP starts in param list
    
    private int Level => PlayerState->ClassJobLevels[JobIndexOffset];
    private int currentLevelExpCached;
    private int CurrentLevelExp => PlayerState->ClassJobExperience[JobIndexOffset];
    private int NextLevelExpNeeded => Data.ParamGrows!.GetRow((uint)Level)!.ExpToNext;
    

    public void CheckTargetLevel(int targetLevel)
    {
        if (targetLevel != TargetLevel || currentLevelExpCached != CurrentLevelExp)
        {
            currentLevelExpCached = CurrentLevelExp;
            TargetLevel = targetLevel;
            Calculate();   
        }
    }

    private void Calculate()
    {
        if (Level == TargetLevel)
        {
            TotalExpNeeded = 0;
            SuggestedLeve[0] = null;
        }
        else
        {
            TotalExpNeeded = 0;
            for (var i = (uint)Level; i < TargetLevel; i++)
                TotalExpNeeded += Data.ParamGrows.GetRow(i).ExpToNext;
            TotalExpNeeded -= CurrentLevelExp;

            var bestExp = 0u;
            foreach (var expansion in Data.Leves[Job])
            {
                foreach (var leve in expansion)
                {
                    if(leve.Name.ToString().Contains("(L)") && !LargeLeves) continue;
                    if (leve.ClassJobLevel <= Level && leve.ExpReward > bestExp)
                    {
                        SuggestedLeve[0] = leve;
                        bestExp = leve.ExpReward;
                    }
                }
            }
        }
    }
    
    
    
    public string LevelExpLabel => 
        $"[ Level: {Level} ] [ {CurrentLevelExp:N0}/{NextLevelExpNeeded:N0}  ({(float)CurrentLevelExp/NextLevelExpNeeded:P}%) ]";

    public string TotalExpLabel => $"Total Exp Needed: {TotalExpNeeded:N0}";
    
    public string NormalQualityTurnInLabel => $"Turn in Normal Quality Leve: {MathF.Ceiling((float)TotalExpNeeded/SuggestedLeve[0].ExpReward)}x times";
    public string HighQualityTurnInLabel => $"Turn in High Quality Leve: {MathF.Ceiling((float)TotalExpNeeded/SuggestedLeve[0].ExpReward / 2)}x times";
}
