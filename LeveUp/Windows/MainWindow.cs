using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using Action = System.Action;
using ImGuiTable = OtterGui.ImGuiTable;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    
    private Plugin Plugin;
    private readonly string[] tableHeaders = ["Name", "Level", "Location", "Exp", "Gil", "Item"];
    private readonly string[] expansions = ["A Realm Reborn", "Heavensward", "Stormblood", "Shadow Bringers", "End Walker", "Dawntrail"];

    private int[] previousLevels;
    private int[] targetLevels;

    private Vector2 minSize = new (717, 200);
    private Vector2 minSizeX = new(float.MaxValue, 200);
    private Vector2 resizeableSize = new(float.MaxValue, float.MaxValue);
    private float expandedHeight = 600;
    private bool firstLoad = true;

    private Leve suggestedLeve;

    private bool resize;
    private Recipe selectedRecipe;
    private uint selectedMapId; 
    
    
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Leve Up!##With a hidden ID", ImGuiWindowFlags.None)
    {
        SetSizeConstraints(minSizeX);
        
        Plugin = plugin;
        
        if (targetLevels == null)
        {
            previousLevels = new int[8];
            targetLevels = new int[8];
            unsafe
            {
                for (var i = 0; i < targetLevels.Length; i++)
                    targetLevels[i] = UIState.Instance()->PlayerState.ClassJobLevels[i + 7];
            } 
        }
    }

    public void Dispose() { }

    private float xfloat = 0;
    private float yfloat = 0;
    public override void Draw()
    {
        if (ImGui.BeginTabBar("JobBar", ImGuiTabBarFlags.FittingPolicyMask))
        {
            var jobs = Plugin.Leves.Keys.ToArray();
            
            for(var i = 0; i < jobs.Length; i++) CreateTab(jobs[i], i);

            ImGui.EndTabBar();
        }

        if (resize)
        {
            ImGui.SetWindowSize(new Vector2(ImGui.GetWindowWidth(), expandedHeight));
            resize = false;
        }
        else expandedHeight = ImGui.GetWindowHeight();

        ImGui.End();
    }
    private void CreateTab(string job, int index)
    {
        if (ImGui.BeginTabItem(job))
        {
            ImGui.BeginChild("TabContent", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);
            
            LeveCalculator(job, index);
            LeveCollapsableMenu(job, index);
            
            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }

    
    private void LeveCalculator(string job, int index)
    {
        
        var jobExpIndex = index + 7; //CPR starts at 7
        if(ImGui.BeginChild("Calculator", new Vector2(ImGui.GetWindowWidth(), 100)))
        {
            if (SizeConstraints.Value.MaximumSize == minSizeX)
            {
                resize = true;
                SetSizeConstraints(resizeableSize);
            }
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
                var expNeeded = Plugin.ParamGrows.GetRow((uint)lvl).ExpToNext;
                ImGui.TableNextColumn(); 
                
                //Left Side
                ImGui.Text($"[ Level: {lvl} ] [ {exp:N0}/{expNeeded:N0}  ({(float)exp/expNeeded:P}%) ]");
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                ImGui.Text("Target Level: ");
                ImGui.SetNextItemWidth(85);
                ImGui.InputInt("", ref targetLevels[index]);
                if (targetLevels[index] < lvl) targetLevels[index] = lvl;
                else if (targetLevels[index] > 100) targetLevels[index] = 100;
                ImGui.TableNextColumn();
                
                //Right Side
                var totalExpNeeded = 0;
                if (lvl != targetLevels[index])
                {
                    for (var i = (uint)lvl; i < targetLevels[index]; i++)
                        totalExpNeeded += Plugin.ParamGrows.GetRow(i).ExpToNext;
                    totalExpNeeded -= exp;
                }
                else totalExpNeeded = 0;

                ImGui.Text($"Total Exp Needed: {totalExpNeeded:N0}");
                
                ImGui.Text($"Suggested Leve: {(totalExpNeeded == 0 ? string.Empty : suggestedLeve.Name)}");
                ImGui.EndTable();
            }
            ImGui.EndChild();
        }
    }

    private void LeveCollapsableMenu(string job, int index)
    {
        if (ImGui.CollapsingHeader("Leve List"))
        {
            SetSizeConstraints(resizeableSize);
            if(ImGui.BeginChild("Table", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar))
            {
                for(var i = 0; i < expansions.Length; i++)
                {
                    ImGui.Spacing();
                    CreateTitleBar(expansions[i]);
                    CreateTable(Plugin.Leves[job][i]);
                }
                ImGui.EndChild();
            }
        }
        else
        {
            SetSizeConstraints(minSizeX);
        }
    }

    private void CreateTable(List<Leve> leves)
    {
        ImGuiTable.DrawTable(
            label: "MyTable",
            data: leves,                                            
            drawRow: DrawRow,                                       
            flags: ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Sortable 
                   | ImGuiTableFlags.RowBg | ImGuiTableFlags.Hideable,  
            columnTitles: tableHeaders                            
        );
    }
    
    private void DrawRow(Leve leve)
    {
        DrawCell(leve.Name);
        DrawCell(leve.ClassJobLevel.ToString());
        DrawCell(leve.PlaceNameIssued.Value.Name, true, leve);
        DrawCell(leve.ExpReward.ToString("N0"));
        DrawCell(leve.GilReward.ToString("N0"));
        var craft = Plugin.CraftLeves.GetRow((uint)leve.DataId);
        var objective = craft.UnkData3[0];
        var itemCount = objective.ItemCount > 1 ? 'x' + objective.ItemCount.ToString() : "";
        DrawCell($"{Plugin.GetItem(objective.Item).Name} {itemCount}", true, leve, objective);
        
    }
    
    private void DrawCell(string content, bool highlight = false, Leve? leve = null, CraftLeve.CraftLeveUnkData3Obj? objective = null)
    {
        ImGui.TableNextColumn();
        ImGui.Text(content);

        if(!highlight) return;
        
        var itemMin = ImGui.GetItemRectMin();
        var padding = ImGui.GetStyle().CellPadding;
        
        var min = itemMin - padding;
        var max = ImGui.GetItemRectMax() with {X = itemMin.X + ImGui.GetColumnWidth()} + (padding);
        
        if (ImGui.IsMouseHoveringRect(min, max))
        {
            ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.4f)));
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(7.0f, 7.0f, 7.0f, 0.4f)));
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.4f)));
                var column = ImGui.TableGetColumnIndex();
                switch (column)
                {
                    case 2:
                    {
                        var level = leve.LevelLevemete.Value;
                        var coords = MapUtil.WorldToMap(new Vector3(level.X, level.Y, level.Z), level.Map.Value.OffsetX, level.Map.Value.OffsetY, 0, level.Map.Value.SizeFactor);
                        var payload = new MapLinkPayload(level.Territory.Value.RowId, level.Map.Value.RowId, coords.X, coords.Y);
                        Plugin.GameGui.OpenMapWithMapLink(payload);
                        break;
                    }
                    case 5:
                        unsafe
                        {
                            var recipe = Plugin.RecipeMap[(leve.LeveAssignmentType.Value.RowId, (uint)objective.Item)];
                            AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipe);
                        }

                        break;
                }
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

    private void SetSizeConstraints(Vector2 max)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = minSize,
            MaximumSize = max
        };
    }
}
