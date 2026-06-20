using System;

namespace OneTouchAutomation.Models;

public record SideBarItemModel
{
    public required Type   ModelType { get; init; }
    public required string IconKey   { get; init; } = OneTouchAutomation.Constants.Icon.Home;
    public required string Title     { get; init; } = "默认文字";
    
    // 判断是否有图标
    public bool HasIcon
    {
        get => !string.IsNullOrEmpty(IconKey);
    }
}