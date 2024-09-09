using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;

namespace LeveUp;

public unsafe class CalculatorData(PlayerState* playerState, int jobIndex)
{
    // public properties
    public bool LargeLeves { get; set; } = false;
    public Leve?[] SuggestedLeve { get; private set; } = [null];
    public bool LeveOverriden { get; private set; }
    
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
        if (Level == TargetLevel && !LeveOverriden)
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

            if(LeveOverriden) return;
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

    public void OverrideSuggestedLeve(Leve leve)
    {
        SuggestedLeve[0] = leve;
        LeveOverriden = true;
    }

    public void RemoveSuggestedLeveOverride()
    {
        LeveOverriden = false;
        Calculate();
    }

    public void SetLargeLeve(bool largeLeve)
    {
        LargeLeves = largeLeve;
        Calculate();
    }
    
    public string LevelExpLabel => 
        $"[ Level: {Level} ] [ {CurrentLevelExp:N0}/{NextLevelExpNeeded:N0}  ({(float)CurrentLevelExp/NextLevelExpNeeded:P}%) ]";

    public string TotalExpLabel => $"Total Exp Needed: {Math.Clamp(TotalExpNeeded, 0, float.MaxValue):N0}";
    
    public string NormalQualityTurnInLabel => $"Turn in NQ Leve: {Math.Clamp(MathF.Ceiling((float)TotalExpNeeded/SuggestedLeve[0].ExpReward), 0, float.MaxValue):N0}x times";
    public string HighQualityTurnInLabel => $"Turn in HQ Leve: {Math.Clamp(MathF.Ceiling((float)TotalExpNeeded/SuggestedLeve[0].ExpReward / 2), 0, float.MaxValue):N0}x times";
}
