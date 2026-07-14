using OneTouchAutomation.Models;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Workflows;
using OneTouchAutomation.Services.Input;

namespace Test.Automation;

public sealed class WorkflowEditorModelTests
{
    [Fact]
    public void FromDefinition_PreservesConditionalTerminalTransition()
    {
        var editor = WorkflowEditorModel.FromDefinition(new WorkflowDefinition
        {
            Id = "daily",
            Name = "Daily",
            StartNodeId = "check",
            Nodes =
            [
                new WorkflowNodeDefinition
                {
                    Id = "check",
                    Name = "Check main menu",
                    BehaviorId = "delay",
                    Parameters = new DelayBehaviorParameters()
                }
            ],
            Transitions =
            [
                new WorkflowTransitionDefinition
                {
                    FromNodeId = "check",
                    ToNodeId = null,
                    Outcome = WorkflowNodeOutcome.Failure,
                    ExpectedStatus = "not-main-menu"
                }
            ]
        });

        var transition = Assert.Single(editor.Transitions);
        Assert.Equal("check", transition.FromNodeId);
        Assert.Equal(WorkflowNodeOutcome.Failure, transition.Outcome);
        Assert.Equal("not-main-menu", transition.ExpectedStatus);
        Assert.True(transition.IsTerminal);
    }

    [Fact]
    public void FromDefinition_ThenToParameters_PreservesClickTemplateSettings()
    {
        var source = new WorkflowNodeDefinition
        {
            Id = "click",
            Name = "Click reward",
            BehaviorId = "click-template",
            Parameters = new ClickTemplateBehaviorParameters
            {
                TemplatePath = "C:\\templates\\reward.png",
                Threshold = 0.91,
                UseRegion = true,
                RegionX = 10,
                RegionY = 20,
                RegionWidth = 300,
                RegionHeight = 200,
                ClickOffsetX = 6,
                ClickOffsetY = -2,
                ClickMode = MouseClickMode.Repeat,
                ClickRepeatCount = 3,
                ClickIntervalMilliseconds = 120,
                ClickHoldDurationMilliseconds = 80
            }
        };

        var editor = WorkflowNodeEditorModel.FromDefinition(source);
        var parameters = Assert.IsType<ClickTemplateBehaviorParameters>(editor.ToParameters());

        Assert.Equal("C:\\templates\\reward.png", parameters.TemplatePath);
        Assert.Equal(0.91, parameters.Threshold);
        Assert.True(parameters.UseRegion);
        Assert.Equal(300, parameters.RegionWidth);
        Assert.Equal(MouseClickMode.Repeat, parameters.ClickMode);
        Assert.Equal(3, parameters.ClickRepeatCount);
        Assert.Equal(6, parameters.ClickOffsetX);
        Assert.Equal(-2, parameters.ClickOffsetY);
    }

    [Fact]
    public void FromDefinition_ThenToParameters_PreservesCheckScreenTemplate()
    {
        var editor = WorkflowNodeEditorModel.FromDefinition(new WorkflowNodeDefinition
        {
            Id = "check",
            Name = "Check main menu",
            BehaviorId = "check-screen",
            Parameters = new CheckScreenBehaviorParameters
            {
                Screen = new OneTouchAutomation.Services.Vision.ScreenDefinition
                {
                    Id = "main-menu",
                    Name = "Main Menu",
                    RequiredElements =
                    [
                        new OneTouchAutomation.Services.Vision.ScreenElementDefinition
                        {
                            TemplatePath = "C:\\templates\\menu.png",
                            Threshold = 0.92,
                            UseRegion = true,
                            RegionX = 15,
                            RegionY = 20,
                            RegionWidth = 300,
                            RegionHeight = 200
                        }
                    ],
                    ExcludedElements =
                    [
                        new OneTouchAutomation.Services.Vision.ScreenElementDefinition
                        {
                            TemplatePath = "C:\\templates\\loading.png"
                        }
                    ]
                }
            }
        });

        var parameters = Assert.IsType<CheckScreenBehaviorParameters>(editor.ToParameters());
        var required = Assert.Single(parameters.Screen.RequiredElements);
        Assert.Equal("main-menu", parameters.Screen.Id);
        Assert.Equal("C:\\templates\\menu.png", required.TemplatePath);
        Assert.Equal(0.92, required.Threshold);
        Assert.True(required.UseRegion);
        Assert.Equal(300, required.RegionWidth);
        Assert.Equal("C:\\templates\\loading.png", Assert.Single(parameters.Screen.ExcludedElements).TemplatePath);
    }
}
