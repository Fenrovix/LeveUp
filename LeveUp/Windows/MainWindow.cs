using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace LeveUp.Windows;

public unsafe class MainWindow : Window, IDisposable
{
    
    private Plugin Plugin;
    private readonly string[] tableHeaders = ["Name", "Level", "Location", "Exp", "Gil", "Item"];
    private readonly string[] expansions = ["A Realm Reborn", "Heavensward", "Stormblood", "Shadow Bringers", "End Walker", "Dawntrail"];
    
    private int[] targetLevels;

    private Vector2 minSize = new (717, 215);
    private Vector2 minSizeX = new(float.MaxValue, 215);
    private Vector2 resizeableSize = new(float.MaxValue, float.MaxValue);
    private float expandedHeight = 600;
    

    private bool resize;
    private readonly PlayerState* playerState;
    private readonly CalculatorData[] calculatorData;
    
    
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Leve Up!##With a hidden ID", ImGuiWindowFlags.None)
    {
        SetSizeConstraints(minSizeX);
        
        Plugin = plugin;
        
        playerState = PlayerState.Instance();
        
        if (targetLevels == null)
        {
            targetLevels = new int[8];
            for (var i = 0; i < targetLevels.Length; i++)
                targetLevels[i] = playerState->ClassJobLevels[i + 7];
        }
        
        calculatorData = new CalculatorData[8];
        for (var i = 0; i < calculatorData.Length; i++) calculatorData[i] = new CalculatorData(playerState, i);
    }

    public void Dispose() { }
    
    public override void Draw()
    {
        if (ImGui.BeginTabBar("JobBar", ImGuiTabBarFlags.FittingPolicyMask))
        {
            var jobs = Data.Leves.Keys.ToArray();
            
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
        var jobIndex = index + 7;
        if (ImGui.BeginTabItem(job))
        {
            ImGui.BeginChild("TabContent", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);
            if (playerState->ClassJobLevels[jobIndex] == 0)
            {
                ImGui.Text("Unlock Job First");
            }
            else
            {
                LeveCalculator(job, index);
                LeveCollapsableMenu(job, index);
            }
            
            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }
    
    private void LeveCalculator(string job, int index)
    {
        var jobIndex = index + 7;
        if(ImGui.BeginChild("Calculator", new Vector2(ImGui.GetWindowWidth(), 120)))
        {
            if (SizeConstraints!.Value.MaximumSize == minSizeX)
            {
                resize = true;
                SetSizeConstraints(resizeableSize);
            }
            if(ImGui.BeginTable("Stats", 1))
            {
                calculatorData[index].CheckTargetLevel(targetLevels[index]);

                ImGui.TableNextColumn();
                ImGui.Text(calculatorData[index].LevelExpLabel);
                ImGui.SameLine();
                //ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
                ImGui.Text("Target Level:");
                //ImGui.SetCursorPos(new Vector2(85, ImGui.GetCursorPosY() - 23));
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                ImGui.InputInt("", ref targetLevels[index]);
                if (targetLevels[index] < playerState->ClassJobLevels[jobIndex])
                    targetLevels[index] = playerState->ClassJobLevels[jobIndex];
                else if (targetLevels[index] > 100) 
                    targetLevels[index] = 100;
                ImGui.SameLine();
                SetLineSpacing(20);
                ImGui.Text(calculatorData[index].TotalExpLabel);
                //ImGui.Text("Suggested Leve: " + (calculatorData[index].SuggestedLeve?.Name ?? ""));
                ImGui.EndTable();
            }

            if (calculatorData[index].SuggestedLeve[0] != null)
            {
                SetSpacing(5);
                CreateTable(calculatorData[index].SuggestedLeve);
                SetSpacing(10);
                ImGui.Text(calculatorData[index].HighQualityTurnInLabel);
                ImGui.SameLine();
                SetLineSpacing(50);
                ImGui.Text(calculatorData[index].NormalQualityTurnInLabel);
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
                    SetSpacing(5);
                    CreateTitleBar(expansions[i]);
                    CreateTable(Data.Leves[job][i]);
                }
                ImGui.EndChild();
            }
        }
        else
        {
            SetSizeConstraints(minSizeX);
        }
    }

    private void CreateTable(IEnumerable<Leve?> leves)
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
    
    private void DrawRow(Leve? leve)
    {
        DrawCell(leve.Name);
        DrawCell(leve.ClassJobLevel.ToString());
        DrawCell(leve.PlaceNameIssued.Value.Name, true, leve);
        DrawCell(leve.ExpReward.ToString("N0"));
        DrawCell(leve.GilReward.ToString("N0"));
        var craft = Data.CraftLeves.GetRow((uint)leve.DataId);
        var objective = craft.UnkData3[0];
        var itemCount = objective.ItemCount > 1 ? 'x' + objective.ItemCount.ToString() : "";
        DrawCell($"{Data.GetItem(objective.Item).Name} {itemCount}", true, leve, objective);
        
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
                        var recipe = Data.RecipeMap[(leve.LeveAssignmentType.Value.RowId, (uint)objective.Item)];
                        AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipe);

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

    private void SetSpacing(int spacing)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + spacing);
    }

    private void SetLineSpacing(int spacing)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing);
    }
}
