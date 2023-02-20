using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Contracts;
using Elsa.Workflows.Core.Activities.Flowchart.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// Merge multiple branches into a single branch of execution.
/// </summary>
[Activity("Elsa", "Flow", "Merge multiple branches into a single branch of execution.")]
public class FlowJoin : Activity, IJoinNode
{
    /// <inheritdoc />
    public FlowJoin([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    [Input] public Input<FlowJoinMode> Mode { get; set; } = new(FlowJoinMode.WaitAll);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var flowchartExecutionContext = context.ParentActivityExecutionContext!;
        var flowchart = (Flowchart)flowchartExecutionContext.Activity;
        var inboundActivities = flowchart.Connections.LeftInboundActivities(this).ToList();
        var flowScope = flowchartExecutionContext.GetProperty<FlowScope>(Flowchart.ScopeProperty)!;
        var executionCount = flowScope.GetExecutionCount(this);
        var mode = context.Get(Mode);

        switch (mode)
        {
            case FlowJoinMode.WaitAll:
                // If all left-inbound activities have executed, complete & continue.
                var haveAllInboundActivitiesExecuted = inboundActivities.All(x => flowScope.GetExecutionCount(x) > executionCount);

                if (haveAllInboundActivitiesExecuted)
                    await context.CompleteActivityAsync();
                break;
            case FlowJoinMode.WaitAny:
                // Only complete if we haven't already executed.
                var alreadyExecuted = inboundActivities.Max(x => flowScope.GetExecutionCount(x)) == executionCount;

                if (!alreadyExecuted)
                {
                    await context.CompleteActivityAsync();
                    ClearBookmarks(flowchart, context);
                }
                break;
        }
    }

    private void ClearBookmarks(Flowchart flowchart, ActivityExecutionContext context)
    {
        // Clear any bookmarks created between this join and its most recent fork.
        var connections = flowchart.Connections;
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var inboundActivities = connections.LeftAncestorActivities(this).Select(x => workflowExecutionContext.FindNodeByActivity(x)).Select(x => x.NodeId).ToList();
        context.WorkflowExecutionContext.Bookmarks.RemoveWhere(x => inboundActivities.Contains(x.ActivityNodeId));
    }
}