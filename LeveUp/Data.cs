

using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Quest = Lumina.Excel.GeneratedSheets2.Quest;

namespace LeveUp;

public static unsafe class Data
{
    public static int[] TargetLevels;
    public static PlayerState* PlayerStateCached;
    public static CalculatorData[] Calculations;
    public static Span<short> PlayerClassJobLeves => PlayerStateCached->ClassJobLevels;
    public static int PlayerJobLevel(int jobIndex) => PlayerStateCached->ClassJobLevels[jobIndex]; 
    public static int PlayerJobExperience(int jobIndex) => PlayerStateCached->ClassJobExperience[jobIndex];
    public static int ExpToNextLevel(int level) => ParamGrows!.GetRow((ushort)level)!.ExpToNext;
    
    public static Dictionary<string, Dictionary<int, (Leve? normal, Leve? large)>> BestLeves = new();
    public static readonly string[] Jobs = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"];

    public static readonly MapLinkPayload[] GuildReceptionists =
    {
        new (132, 2, 10.8f, 12.1f), new (128, 11, 10.2f, 15f),
        new (128, 11, 10.2f, 15f), new (131, 14, 10.5f, 13.2f),
        new (133, 3, 12.5f, 8.3f), new (131, 14, 13.9f, 13.2f),
        new (131, 73, 8.9f, 13.6f), new (128, 11, 10f, 8f)
    };

    
    public static Dictionary<string, List<Leve>[]> Leves = new();
    public static Dictionary<(uint jobId, uint itemId), uint> RecipeMap = new();

    public static ExcelSheet<CraftLeve>? CraftLeves;
    public static ExcelSheet<Item>? Items;
    public static ExcelSheet<RecipeLookup>? RecipeLookups;
    public static ExcelSheet<ParamGrow>? ParamGrows;
    
    public static void Initialize()
    {
        GenerateExcelSheets();
        GenerateDictionaries();
        InitializeCalculatorData();
        PrecomputeBestLeves();
    }

    private static void GenerateExcelSheets()
    {
        CraftLeves = Plugin.DataManager.GetExcelSheet<CraftLeve>()!;
        Items = Plugin.DataManager.GetExcelSheet<Item>()!;
        RecipeLookups = Plugin.DataManager.GetExcelSheet<RecipeLookup>()!;
        ParamGrows = Plugin.DataManager.GetExcelSheet<ParamGrow>()!;
    }

    private static void InitializeCalculatorData()
    {
        PlayerStateCached = PlayerState.Instance();
        
        TargetLevels = new int[8];
        for (var i = 0; i < TargetLevels.Length; i++)
            TargetLevels[i] = PlayerStateCached->ClassJobLevels[i + 7];
        
        Calculations = new CalculatorData[8];
        for (var i = 0; i < Calculations.Length; i++) Calculations[i] = new CalculatorData(i);
    }

    private static void PrecomputeBestLeves()
    {
        foreach (var job in Jobs)
        {
            // Initialize the dictionary for storing best Leves for each level
            BestLeves.Add(job, new Dictionary<int, (Leve? normal, Leve? large)>());
            
            // Flatten all Leves into a single list
            var allLeves = Leves[job].SelectMany(l => l).ToList();

            // Group Leves by ClassJobLevel
            var levesByLevel = allLeves.GroupBy(l => (int)l.ClassJobLevel)
                                       .ToDictionary(g => g.Key, g => g.ToList());
            
            // Initialize variables to store the best Leves found so far
            Leve? bestNormalLeveSoFar = null;
            Leve? bestLargeLeveSoFar = null;

            // Iterate over levels from 1 to 98
            for (var level = 1; level <= 98;)
            {
                // Try to get the Leves for the current level
                if (levesByLevel.TryGetValue(level, out var currentLevelLeves))
                {
                    foreach (var leve in currentLevelLeves)
                    {
                        // Check the AllowanceCost and determine the best normal and large Leves
                        switch (leve.AllowanceCost)
                        {
                            case 1:
                                if (bestNormalLeveSoFar == null || leve.ExpReward > bestNormalLeveSoFar.ExpReward)
                                    bestNormalLeveSoFar = leve;
                                break;
                            case 10:
                                if (bestLargeLeveSoFar == null || leve.ExpReward > bestLargeLeveSoFar.ExpReward)
                                    bestLargeLeveSoFar = leve;
                                break;
                        }
                    }
                }

                // Store the best Leve for this level
                BestLeves[job].Add(level, (bestNormalLeveSoFar, bestLargeLeveSoFar));

                // Adjust the level increment based on the pattern described
                switch (level)
                {
                    case 1:
                        level = 5;
                        break;
                    case < 50:
                        level += 5;
                        break;
                    default:
                        level += 2;
                        break;
                }
            }
        }
    }



    private static void GenerateDictionaries()
    {
        foreach (var job in Jobs)
        {
            Leves.Add(job, new List<Leve>[6]);
            for (var i = 0; i < Leves[job].Length; i++) Leves[job][i] = new List<Leve>();
        }       
        var leveSheet = Plugin.DataManager.GameData.Excel.GetSheet<Leve>();
        for (uint i = 0; i < leveSheet.RowCount; i++)
        {
            var leve = leveSheet.GetRow(i);
            var jobId = leve.LeveAssignmentType.Value.RowId;
            if (jobId < 5 || jobId > 12) continue;

            var jobName = leve.ClassJobCategory.Value.Name;
            if(!Leves.ContainsKey(jobName)) Leves.Add(jobName, []);
            
            try
            {
                Leves[jobName][ExpansionIndex(leve.ClassJobLevel)].Add(leve);
                var key = GetRecipeMapKey(leve);
                var recipeId = GetRecipeId(jobName, key.itemId);
                RecipeMap.TryAdd(key, recipeId);
            }
            catch (Exception ex)
            {
                Plugin.ChatGui.PrintError(ex.Message);
            }
        }
    }
    private static uint GetRecipeId(string jobName, uint itemId)
    {
        return jobName switch
        {
            "CRP" => RecipeLookups.FirstOrDefault(r => r.CRP?.Value?.ItemResult?.Value?.RowId == itemId)?.CRP?.Value?.RowId ?? 0,
            "LTW" => RecipeLookups.FirstOrDefault(r => r.LTW?.Value?.ItemResult?.Value?.RowId == itemId)?.LTW?.Value?.RowId ?? 0,
            "BSM" => RecipeLookups.FirstOrDefault(r => r.BSM?.Value?.ItemResult?.Value?.RowId == itemId)?.BSM?.Value?.RowId ?? 0,
            "ARM" => RecipeLookups.FirstOrDefault(r => r.ARM?.Value?.ItemResult?.Value?.RowId == itemId)?.ARM?.Value?.RowId ?? 0,
            "CUL" => RecipeLookups.FirstOrDefault(r => r.CUL?.Value?.ItemResult?.Value?.RowId == itemId)?.CUL?.Value?.RowId ?? 0,
            "ALC" => RecipeLookups.FirstOrDefault(r => r.ALC?.Value?.ItemResult?.Value?.RowId == itemId)?.ALC?.Value?.RowId ?? 0,
            "WVR" => RecipeLookups.FirstOrDefault(r => r.WVR?.Value?.ItemResult?.Value?.RowId == itemId)?.WVR?.Value?.RowId ?? 0,
            "GSM" => RecipeLookups.FirstOrDefault(r => r.GSM?.Value?.ItemResult?.Value?.RowId == itemId)?.GSM?.Value?.RowId ?? 0,
            _ => 0
        };
    }
    
    public static Item? GetItem(int id)
    {
        return GetItem((uint)id);
    }
    public static Item? GetItem(uint id)
    {
        return Items.GetRow(id)!;
    }
    private static (uint jobId, uint itemId) GetRecipeMapKey(Leve leve)
    {
        var craft = CraftLeves.GetRow((uint)leve.DataId);
        return (leve.LeveAssignmentType.Value!.RowId, (uint)craft!.UnkData3[0].Item);
    }

    public static int ExpansionIndex(ushort level) => Math.Max((level / 10) - 4, 0);
}
