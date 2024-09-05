using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.Attributes;
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

    private int[] targetLevels;
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

        if (targetLevels == null)
        {
            targetLevels = new int[8];
            unsafe
            {
                for (var i = 0; i < targetLevels.Length; i++)
                    targetLevels[i] = UIState.Instance()->PlayerState.ClassJobLevels[i + 7];
            } 
        }
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("JobBar", ImGuiTabBarFlags.FittingPolicyMask))
        {
            var jobs = Plugin.Leves.Keys.ToArray();
            
            for(var i = 0; i < jobs.Length; i++) CreateTab(jobs[i], i);

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private void CreateTab(string job, int index)
    {
        if (ImGui.BeginTabItem(job))
        {
            ImGui.BeginChild("TabContent", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);
            
            LeveCalculator(job, index);
            LeveCollapsableMenu(job);
            
            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }

    private void LeveCalculator(string job, int index)
    {
        var jobExpIndex = index + 7; //CPR starts at 7
        if(ImGui.BeginChild("Calculator", new Vector2(ImGui.GetWindowWidth(), 100)))
        {
            if(ImGui.BeginTable("", 2))
            {
                var lvl = 0;
                var exp = 0;
                unsafe
                {
                    var playerState = UIState.Instance()->PlayerState;
                    lvl = playerState.ClassJobLevels[jobExpIndex];
                    exp = playerState.ClassJobExperience[jobExpIndex];
                }
                ImGui.TableNextColumn();
                ImGui.Text($"Level: {lvl}       Exp: {exp:N0}");
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                ImGui.Text("Target Level: ");
                ImGui.SetNextItemWidth(85);
                ImGui.InputInt("", ref targetLevels[index]);
                if (targetLevels[index] < lvl) targetLevels[index] = lvl;
                else if (targetLevels[index] > 100) targetLevels[index] = 100;
                ImGui.TableNextColumn();
                ImGui.Text("Test");
                ImGui.EndTable();
            }
            ImGui.EndChild();
        }
    }

    private void LeveCollapsableMenu(string job)
    {
        if (ImGui.CollapsingHeader("Leve List"))
        {
            if(ImGui.BeginChild("Table", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar))
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
                ImGui.EndChild();
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
