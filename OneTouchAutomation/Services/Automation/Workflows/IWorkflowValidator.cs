namespace OneTouchAutomation.Services.Automation.Workflows;

public interface IWorkflowValidator
{
    WorkflowValidationResult Validate(WorkflowDefinition workflow);
}
