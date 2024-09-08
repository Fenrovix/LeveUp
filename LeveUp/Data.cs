

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace LeveUp;

public static class Data
{
    public const string LogFile = @"H:\Code\log3.txt";
    public static readonly string[] Jobs = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"];
    
    public static Dictionary<string, List<Leve>[]> Leves = new();
    public static Dictionary<(uint jobId, uint itemId), uint> RecipeMap = new();

    public static ExcelSheet<CraftLeve>? CraftLeves;
    public static ExcelSheet<Item>? Items;
    public static ExcelSheet<RecipeLookup>? RecipeLookups;
    public static ExcelSheet<ParamGrow>? ParamGrows;
    
    public static void Initialize()
    {
        CraftLeves = Plugin.DataManager.GetExcelSheet<CraftLeve>()!;
        Items = Plugin.DataManager.GetExcelSheet<Item>()!;
        RecipeLookups = Plugin.DataManager.GetExcelSheet<RecipeLookup>()!;
        ParamGrows = Plugin.DataManager.GetExcelSheet<ParamGrow>()!;
        

        foreach (var job in Jobs)
        {
            Leves.Add(job, new List<Leve>[6]);
            for (var i = 0; i < Leves[job].Length; i++) Leves[job][i] = new List<Leve>();
        }

        GenerateDictionaries();
    }
    
    private static void GenerateDictionaries()
    {
        
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
