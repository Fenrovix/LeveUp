using ImGuiNET;

namespace LeveUp;

public static class ImGuiHelper
{
    public static void CreateTitleBar(string title)
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
    
    public static void SetSpacing(int spacing)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + spacing);
    }

    public static void SetLineSpacing(float spacing)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing);
    }
}
