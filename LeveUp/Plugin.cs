using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using SamplePlugin.Windows;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    string logFile = @"F:\Code\FFXIV Plugins\log3.txt";
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/lup";
    private const string ConfigCommandName = "/lupconfig";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("LeveUp");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Dictionary<SeString, List<Leve>> Leves = new();

    public ExcelSheet<CraftLeve> CraftLeves;
    public ExcelSheet<Item> Items;
    public ExcelSheet<RecipeLookup> RecipeLookups;
    public ExcelSheet<Recipe> Recipes;

    public Dictionary<(uint jobId, uint itemId), uint> RecipeMap = new();

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Use to open the main window."
        });
        CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Use to open the config window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        CraftLeves = DataManager.GetExcelSheet<CraftLeve>()!;
        Items = DataManager.GetExcelSheet<Item>()!;
        RecipeLookups = DataManager.GetExcelSheet<RecipeLookup>()!;
        Recipes = DataManager.GetExcelSheet<Recipe>()!;
        GenerateDict();

        var text = "";
        foreach (var i in RecipeLookups)
        {
            text += $"{i.ALC.Value.ItemResult.Value.RowId} | {i.ALC.Value.RowId}\n";
        }
        File.WriteAllText(logFile, text);
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
    
    private void GenerateDict()
    {
        var leveSheet = DataManager.GameData.Excel.GetSheet<Leve>();
        for (uint i = 0; i < leveSheet.RowCount; i++)
        {
            var leve = leveSheet.GetRow(i);
            var jobId = leve.LeveAssignmentType.Value.RowId;
            if (jobId < 5 || jobId > 12) continue;

            var jobName = leve.ClassJobCategory.Value.Name;
            if (!Leves.ContainsKey(jobName)) Leves.Add(jobName, new List<Leve>());
            Leves[jobName].Add(leve);

            try
            {
                var key = GetRecipeMapKey(leve);
                var recipeId = GetRecipeId(jobName, key.itemId);
                RecipeMap.TryAdd(key, recipeId);
            }
            catch (Exception ex)
            {
                ChatGui.PrintError(ex.Message);
            }
        }
    }
    private uint GetRecipeId(string jobName, uint itemId)
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

    public Item GetItem(int id)
    {
        return GetItem((uint)id);
    }

    public Item GetItem(uint id)
    {
        return Items.GetRow(id)!;
    }

    public uint GetItemIdFromLeve(Leve leve)
    {
        var craft = CraftLeves.GetRow((uint)leve.DataId);
        return (uint)craft!.UnkData3[0].Item;
    }

    private (uint jobId, uint itemId) GetRecipeMapKey(Leve leve)
    {
        var craft = CraftLeves.GetRow((uint)leve.DataId);
        return (leve.LeveAssignmentType.Value!.RowId, (uint)craft!.UnkData3[0].Item);
    }
}
