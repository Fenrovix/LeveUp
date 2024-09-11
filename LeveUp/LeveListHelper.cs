using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using LeveUp.Windows;
using Lumina.Excel.GeneratedSheets;
using OtterGui;
using ImGuiTable = Dalamud.Interface.Utility.ImGuiTable;

namespace LeveUp;

public static class LeveListHelper
{
    public static MainWindow? MainWindow;
    public static bool Opened { get; private set; }
    
    private static readonly string[] TableHeaders = ["Name", "Level", "Location", "Exp", "Item"];
    private static readonly string[] Expansions = ["A Realm Reborn", "Heavensward", "Stormblood", "Shadow Bringers", "End Walker", "Dawntrail"];
    
    private static Leve? ClickedLeve;
    private static int ClickedIndex;
    private static bool PopupOpened;
    
    private static bool MenuClickDelay;
    

    public static void DrawLeveTable(string job)
    {
        LeveCollapsableMenu(job);
        DrawLeveReplacementPopup();
    }
    
    private static void LeveCollapsableMenu(string job)
    {
        if (ImGui.CollapsingHeader("Leve List"))
        {
            if(!Opened)
            {
                MainWindow!.SetSizeConstraints(MainWindow!.ResizeableSize);
                Opened = true;
            }
            if(ImGui.BeginChild("Table", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar))
            {
                for(var i = 0; i < Expansions.Length; i++)
                {
                    ImGuiHelper.SetSpacing(5);
                    ImGuiHelper.CreateTitleBar(Expansions[i]);
                    CreateLeveTable(Data.Leves[job][i]);
                }
                ImGui.EndChild();
            }
        }
        else
        {
            Opened = false;
            MainWindow!.SetSizeConstraints(MainWindow!.MinSizeX);
        }
    }
    
    private static void CreateLeveTable(IEnumerable<Leve?> leves)
    {
        ImGuiTable.DrawTable(
            label: "test",
            data: leves,                                            
            drawRow: DrawLeveRow,                                       
            flags: ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Sortable 
                   | ImGuiTableFlags.RowBg | ImGuiTableFlags.Hideable,  
            columnTitles: TableHeaders                            
        );
    }
    
    private static void DrawLeveRow(Leve? leve)
    {
        DrawLeveCell(leve.Name, leve, true);
        DrawLeveCell(leve.ClassJobLevel.ToString(), leve);
        DrawLeveCell(leve.PlaceNameIssued.Value.Name, leve, true);
        DrawLeveCell(leve.ExpReward.ToString("N0"), leve);
        var craft = Data.CraftLeves.GetRow((uint)leve.DataId);
        var objective = craft.UnkData3[0];
        var itemCount = objective.ItemCount > 1 ? 'x' + objective.ItemCount.ToString() : "";
        DrawLeveCell($"{Data.GetItem(objective.Item).Name} {itemCount}", leve, true, objective);
    }
    
    private static void DrawLeveCell(string content, Leve? leve, bool highlight = false, CraftLeve.CraftLeveUnkData3Obj? objective = null)
    {
        var column = ImGui.TableGetColumnIndex();
        ImGui.TableNextColumn();
        ImGui.Text(content);
        
        if(MenuClickDelay || !highlight || PopupOpened) return;
        
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
                        ClickedLeve = leve;
                        ClickedIndex = TabHelper.Index;
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
                        unsafe
                        {
                            AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipe);
                        }
                        break;
                }
            }
        }
    }
    private static void DrawLeveReplacementPopup()
    {
        if (MenuClickDelay && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            MenuClickDelay = false;
        }
        if(ClickedLeve != null && !PopupOpened) ImGui.OpenPopup("ReplacementPopup");
        if(ImGui.BeginPopup("ReplacementPopup"))
        {
            PopupOpened = true;
            MenuClickDelay = true;
            ImGuiUtil.Center($"Replace suggested leve with:\n{ClickedLeve.Name}?");
            if (ImGui.Button("Yes"))
            {
                MenuClickDelay = false;
                //Add new logic
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                MenuClickDelay = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        if (!ImGui.IsPopupOpen("ReplacementPopup") && (ClickedLeve != null || PopupOpened))
        {
            ClickedLeve = null;
            PopupOpened = false;
        }
    }
}
