using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    
    private Plugin Plugin;
    private readonly string[] tableHeaders = ["Name", "Level" ,"Location","Exp", "Item"];
    private readonly string[] expansions = ["A Realm Reborn", "Heavensward", "Stormblood", "Shadow Bringers", "End Walker", "Dawntrail"];

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Leve Up!##With a hidden ID", ImGuiWindowFlags.None)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("JobBar", ImGuiTabBarFlags.FittingPolicyMask))
        {
            var jobs = Plugin.Leves.Keys.ToArray();
            
            foreach(var job in jobs) CreateTab(job);

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private void CreateTab(SeString job)
    {
        if (ImGui.BeginTabItem(job))
        {
            // Begin a child window for scrollable content
            ImGui.BeginChild("TabContent", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
            
            LeveCollapsableMenu(job);

            // End the child window
            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }

    private void LeveCalculator(SeString job)
    {
        
    }

    private void LeveCollapsableMenu(SeString job)
    {
        if (ImGui.CollapsingHeader("Leve List"))
        {
            var index = 0;
            var expansionCap = 50;
            foreach (var expansion in expansions)
            {
                ImGui.Spacing();
                CreateTitleBar(expansion);
                {
                    index = CreateTable(Plugin.Leves[job], index, expansionCap);
                    expansionCap += 10;
                }
            }
        }
    }

    private int CreateTable(List<Leve> leves, int startIndex, int expansionCap)
    {
        if(ImGui.BeginTable("LeveTable", tableHeaders.Length, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
        {
            foreach(var header in tableHeaders) ImGui.TableSetupColumn(header);
            ImGui.TableHeadersRow();
            for (var i = startIndex; i < leves.Count; i++)
            {
                if (leves[i].ClassJobLevel == expansionCap)
                {
                    ImGui.EndTable();
                    return i;
                }
                CreateTableRow(leves[i]);
            }
            ImGui.EndTable();
        }
        return -1;
    }
    private void CreateTableRow(Leve leve)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text(leve.Name);
        ImGui.TableNextColumn();
        ImGui.Text(leve.ClassJobLevel.ToString());
        ImGui.TableNextColumn();
        ImGui.Text(leve.PlaceNameStart.Value.Name);
        ImGui.TableNextColumn();
        ImGui.Text(leve.ExpReward.ToString("N0"));
        ImGui.TableNextColumn();
        var craft = Plugin.CraftLeves.GetRow((uint)leve.DataId);
        var objective = craft.UnkData3[0];
        var itemCount = objective.ItemCount > 1 ? 'x' + objective.ItemCount.ToString() : "";
        if (ImGui.Button($"{Plugin.GetItem(objective.Item).Name} {itemCount}"))
        {
            unsafe
            {
                var recipe = Plugin.RecipeMap[(leve.LeveAssignmentType.Value.RowId, (uint)objective.Item)];
                AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipe);
                Plugin.ChatGui.Print(recipe.ToString());
            }
        }
    }
    
    private void CreateTitleBar(string title)
    {
        var windowWidth = ImGui.GetWindowSize().X;
        var textSize = ImGui.CalcTextSize(title).X;
        var textPosX = (windowWidth - textSize) / 2;
        
        ImGui.Dummy(new Vector2(0, 10));
        
        ImGui.SetCursorPosX(textPosX);
        ImGui.Text(title);
        
        ImGui.Dummy(new Vector2(0, 10));
        
        ImGui.Separator();
    }
}
