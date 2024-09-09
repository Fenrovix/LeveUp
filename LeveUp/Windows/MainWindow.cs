using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using OtterGui.Raii;
using ImGuiTable = Dalamud.Interface.Utility.ImGuiTable;
using Map = Lumina.Excel.GeneratedSheets.Map;
using Timer = System.Timers.Timer;

namespace LeveUp.Windows;

public unsafe class MainWindow : Window, IDisposable
{
    private readonly string[] tableHeaders = ["Name", "Level", "Location", "Exp", "Item"];
    private readonly string[] expansions = ["A Realm Reborn", "Heavensward", "Stormblood", "Shadow Bringers", "End Walker", "Dawntrail"];
    
    private readonly int[] targetLevels;

    private readonly Vector2 minSize = new (717, 230);
    private readonly Vector2 minSizeX = new(float.MaxValue, 230);
    private readonly Vector2 resizeableSize = new(float.MaxValue, float.MaxValue);
    private float expandedHeight = 600;
    

    private bool resize;
    private readonly PlayerState* playerState;
    private readonly CalculatorData[] calculatorData;

    private Leve? clickedLeve;
    private int clickedIndex;
    private bool popupOpened;

    private readonly Timer mouseClickTimer;
    private bool menuClickDelay;
    private bool largeLeves;

    private int index;
    private int JobIndex => index + 7;

    private bool isSuggestedTable;
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow()
        : base("Leve Up!##MainWindow", ImGuiWindowFlags.None)
    {
        SetSizeConstraints(minSizeX);
        
        playerState = PlayerState.Instance();
        
        if (targetLevels == null)
        {
            targetLevels = new int[8];
            for (var i = 0; i < targetLevels.Length; i++)
                targetLevels[i] = playerState->ClassJobLevels[i + 7];
        }
        
        calculatorData = new CalculatorData[8];
        for (var i = 0; i < calculatorData.Length; i++) calculatorData[i] = new CalculatorData(playerState, i);

        mouseClickTimer = new Timer(120);
        mouseClickTimer.AutoReset = false;
        mouseClickTimer.Elapsed += (sender, args) =>
        {
            menuClickDelay = false;
        }; 
    }

    public void Dispose() { }
    
    public override void Draw()
    {
        if (ImGui.BeginTabBar("JobBar", ImGuiTabBarFlags.FittingPolicyMask))
        {
            var jobs = Data.Leves.Keys.ToArray();
            for(index = 0; index < jobs.Length; index++) CreateTab(jobs[index]);

            ImGui.EndTabBar();
        }

        if (resize)
        {
            ImGui.SetWindowSize(new Vector2(ImGui.GetWindowWidth(), expandedHeight));
            resize = false;
        }
        else expandedHeight = ImGui.GetWindowHeight();
        
        DrawLeveReplacementPopup();
        ImGui.End();
    }
    private void CreateTab(string job)
    {
        if (ImGui.BeginTabItem(job))
        {
            ImGui.BeginChild("TabContent", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);
            
            LeveCalculator();
            
            LeveCollapsableMenu(job);
            
            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }
    
    private void LeveCalculator()
    {
        if(ImGui.BeginChild("Calculator", new Vector2(ImGui.GetWindowWidth(), 135), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (playerState->ClassJobLevels[JobIndex] == 0)
            {
                if (ImGui.Button("Unlock Job"))
                    Plugin.GameGui.OpenMapWithMapLink(Data.GuildReceptionists[index]);
            }
            else
            {
                if (SizeConstraints!.Value.MaximumSize == minSizeX)
                {
                    resize = true;
                    SetSizeConstraints(resizeableSize);
                }

                if (ImGui.BeginTable("Stats", 1))
                {
                    calculatorData[index].CheckTargetLevel(targetLevels[index]);

                    ImGui.TableNextColumn();
                    ImGui.Text(calculatorData[index].LevelExpLabel);
                    ImGui.SameLine();
                    SetLineSpacing(5);
                    ImGui.Text("Target Level:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    ImGui.InputInt("", ref targetLevels[index]);
                    var addLevel = playerState->ClassJobLevels[JobIndex] == 100 ? 0 : 1;
                    targetLevels[index] = Math.Clamp(targetLevels[index], playerState->ClassJobLevels[JobIndex]+addLevel, 100);
                    ImGui.SameLine();
                    SetLineSpacing(15);
                    ImGui.Text(calculatorData[index].TotalExpLabel);
                    ImGui.EndTable();
                }

                if (calculatorData[index].SuggestedLeve[0] != null)
                {
                    SetSpacing(5);
                    CreateTable(calculatorData[index].SuggestedLeve, true);
                    SetSpacing(10);

                    if (ImGui.BeginTable("", 3))
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(calculatorData[index].HighQualityTurnInLabel);
                        ImGui.Text(calculatorData[index].NormalQualityTurnInLabel);

                        ImGui.TableNextColumn();
                        
                        if (calculatorData[index].LeveOverriden)
                        {
                            if (ImGui.Button("Reset Suggested Leve")) calculatorData[index].RemoveSuggestedLeveOverride();
                        }

                        ImGui.TableNextColumn();
                        if(ImGui.Checkbox("Use Large Leves?", ref largeLeves))
                        {
                            calculatorData[0].SetLargeLeve(largeLeves);
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("Large Leves use 10 leve Allowances\n" +
                                       "(Not Recommended)");
                            ImGui.EndTooltip();
                        }
                        ImGui.EndTable();
                    }
                }
            }
            SetSpacing(5);
            ImGui.Separator();
            ImGui.EndChild();
        }
    }

    private void LeveCollapsableMenu(string job)
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
                    CreateTable(Data.Leves[job][i], false);
                }
                ImGui.EndChild();
            }
        }
        else
        {
            SetSizeConstraints(minSizeX);
        }
    }

    private void CreateTable(IEnumerable<Leve?> leves, bool suggestedTable)
    {
        isSuggestedTable = suggestedTable;
        ImGuiTable.DrawTable(
            label: "",
            data: leves,                                            
            drawRow: DrawRow,                                       
            flags: ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Sortable 
                   | ImGuiTableFlags.RowBg | ImGuiTableFlags.Hideable,  
            columnTitles: tableHeaders                            
        );
    }
    
    private void DrawRow(Leve? leve)
    {
        DrawCell(leve.Name, leve, true);
        DrawCell(leve.ClassJobLevel.ToString(), leve);
        DrawCell(leve.PlaceNameIssued.Value.Name, leve, true);
        DrawCell(leve.ExpReward.ToString("N0"), leve);
        var craft = Data.CraftLeves.GetRow((uint)leve.DataId);
        var objective = craft.UnkData3[0];
        var itemCount = objective.ItemCount > 1 ? 'x' + objective.ItemCount.ToString() : "";
        DrawCell($"{Data.GetItem(objective.Item).Name} {itemCount}", leve, true, objective);
    }
    
    private void DrawCell(string content, Leve? leve, bool highlight = false, CraftLeve.CraftLeveUnkData3Obj? objective = null)
    {
        ImGui.TableNextColumn();
        ImGui.Text(content);
        var column = ImGui.TableGetColumnIndex();
        if((isSuggestedTable && column == 0) || menuClickDelay || !highlight || popupOpened) return;
        
        var itemMin = ImGui.GetItemRectMin();
        var padding = ImGui.GetStyle().CellPadding;
        
        var min = itemMin - padding;
        var max = ImGui.GetItemRectMax() with {X = itemMin.X + ImGui.GetColumnWidth()} + padding;
        
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
                switch (column)
                {
                    case 0:
                    {
                        clickedLeve = leve;
                        clickedIndex = index;
                        break;
                    }
                    case 2:
                    {
                        var level = leve.LevelLevemete.Value;
                        var coords = MapUtil.WorldToMap(new Vector3(level.X, level.Y, level.Z), level.Map.Value.OffsetX, level.Map.Value.OffsetY, 0, level.Map.Value.SizeFactor);
                        var payload = new MapLinkPayload(level.Territory.Value.RowId, level.Map.Value.RowId, coords.X, coords.Y);
                        Plugin.GameGui.OpenMapWithMapLink(payload);
                        break;
                    }
                    case 4:
                        var recipe = Data.RecipeMap[(leve.LeveAssignmentType.Value.RowId, (uint)objective.Item)];
                        AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipe);

                        break;
                }
            }
        }
    }

    private void DrawLeveReplacementPopup()
    {
        if(clickedLeve != null && !popupOpened) ImGui.OpenPopup("ReplacementPopup");
        if(ImGui.BeginPopup("ReplacementPopup"))
        {
            popupOpened = true;
            menuClickDelay = true;
            OtterGui.ImGuiUtil.Center($"Replace suggested leve with:\n{clickedLeve.Name}?");
            if (ImGui.Button("Yes"))
            {
                calculatorData[clickedIndex].OverrideSuggestedLeve(clickedLeve);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }

        if (!ImGui.IsPopupOpen("ReplacementPopup") && (clickedLeve != null || popupOpened))
        {
            mouseClickTimer.Start();
            clickedLeve = null;
            popupOpened = false;
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
