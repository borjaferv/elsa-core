using Elsa.Activities;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Activities.Primitives;
using Elsa.Services;

namespace Elsa.IntegrationTests.Scenarios.Persistence;

class SequentialWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Line 1"),
                new Event("Resume"){ Id = "Resume"},
                new WriteLine("Line 2"),
                new WriteLine("Line 3")
            }
        });
    }
}