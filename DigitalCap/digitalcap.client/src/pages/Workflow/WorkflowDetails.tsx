import React, { useState, useEffect } from "react";
import AppLoader from '../../Common/AppLoader';
import * as Icon from "react-bootstrap-icons";
import WorkflowList from "./WorkflowList";
import * as workflowService from "../../service/WorkflowService";
import type { WorkflowViewModel } from "../../Interface/Workflow";
import { ProgressState } from "../../Interface/Workflow";
import { formatDisplayDate } from "../../utils/dateTime";

interface WorkflowDetailsProps {
    projectId?: number;
}


const WorkflowDetails: React.FC<WorkflowDetailsProps> = ({ projectId }) => {
    const [workflowData, setWorkflowData] = useState<WorkflowViewModel | null>(null);
    const [loading, setLoading] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (projectId) {
            loadWorkflowData();
        }
    }, [projectId]);

    const loadWorkflowData = async () => {
        if (!projectId) return;

        setLoading(true);
        setError(null);
        try {
            const response = await workflowService.getWorkflowByProjectId(projectId);

            if (response.data) {
                const dataToSet = { ...response.data };
                if (dataToSet.sections && !dataToSet.tasksViewModel) {
                    dataToSet.tasksViewModel = {
                        sections: dataToSet.sections,
                        projectId: dataToSet.projectId,
                        projectName: dataToSet.projectName,
                    };
                }
                setWorkflowData(dataToSet);
            }
        } catch (err: any) {
            setError('Failed to load workflow data. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const formatStepDateMVC = (dateStr?: string | Date | null): string => {
        return formatDisplayDate(dateStr);
    };


    const getStateLabel = (state: number): string => {
        switch (state) {
            case ProgressState.NotStarted: return "Not Started";
            case ProgressState.InProgress: return "In Progress";
            case ProgressState.Completed: return "Completed";
            default: return "";
        }
    };
    const getSteps = (): Array<{ description: string; completed: boolean; date?: string }> => {
        if (!workflowData?.progressModel?.nodes?.length) return [];

        return workflowData.progressModel.nodes.map(node => {
            let description = node.title || "Unknown Step";
            const stateLabel = getStateLabel(node.state);
            if (stateLabel) description += ` - ${stateLabel}`;
            if (node.statusDate) description += ` ${formatStepDateMVC(node.statusDate)}`;

            return {
                description: `${node.title} - ${getStateLabel(node.state)}`,
                date: node.statusDate ? formatStepDateMVC(node.statusDate) : "",
                completed: node.state === ProgressState.Completed
            };
        });
    };

    const getCurrentStep = (): number => {
        if (!workflowData?.progressModel?.nodes) return 0;
        const nodes = workflowData.progressModel.nodes;


        for (let i = 0; i < nodes.length; i++) {
            if (nodes[i].state !== ProgressState.Completed) {
                return i;
            }
        }
        return nodes.length - 1;
    };

    const getCurrentStepInfo = (): { stepNumber: number; totalSteps: number; stepName: string; status: string } => {
        const steps = getSteps();
        const currentStepIndex = getCurrentStep();
        const currentNode = workflowData?.progressModel?.nodes?.[currentStepIndex];

        let status = 'in progress';
        if (currentNode) {
            if (currentNode.state === ProgressState.Completed) {
                status = 'completed';
            } else if (currentNode.state === ProgressState.NotStarted) {
                status = 'not started';
            }
        }

        return {
            stepNumber: currentStepIndex + 1,
            totalSteps: steps.length,
            stepName: currentNode?.title || steps[currentStepIndex]?.description || 'Unknown',
            status: status
        };
    };

    const getProgressPercentage = (): number => {
        if (!workflowData) return 0;
        const r = workflowData as WorkflowViewModel & { PercentComplete?: number };
        const raw = r.percentComplete ?? r.PercentComplete;
        const n = typeof raw === "number" ? raw : Number(raw);
        if (!Number.isFinite(n)) return 0;
        return Math.min(100, Math.max(0, n));
    };

    const getWorkflowProgressColor = (pct: number): string => {
        if (pct === 100) return "#28a745";
        return "#fd7e14";
    };

    if (loading) {
        return (
            <div className="card-area mb-3">
                <AppLoader inline />
            </div>
        );
    }

    if (error) {
        return (
            <div className="card-area mb-3">
                <div className="text-center p-4 text-danger">{error}</div>
            </div>
        );
    }

    const steps = getSteps();

    if (steps.length === 0) {
        return (
            <div className="card-area mb-3">
                <div className="text-center p-4 text-muted">No workflow steps available</div>
            </div>
        );
    }

    const stepInfo = getCurrentStepInfo();
    const progressPercentage = getProgressPercentage();

    return (
        <>
            <div className="card-area mb-3 mt-3 ">
                <div className="stepper-card">
                    <div className="custom-stepper">
                        {steps.map((step, index) => {
                            const isCompleted = step.completed;
                            const isCurrent = index === getCurrentStep() && !isCompleted;
                            const dotColor = isCompleted ? "#28a745" : isCurrent ? "#dc3545" : "#adb5bd";

                            return (
                                <React.Fragment key={`${step.description}-${index}`}>
                                    {/* Workflow Step */}
                                    <div
                                        className={`workflow-step ${index % 2 === 0 ? "step-up" : "step-down"}`}
                                    >
                                        <div
                                            className="workflow-circle"
                                            style={{ backgroundColor: dotColor }}
                                        >
                                            {isCompleted ? "✓" : (index + 1)}
                                        </div>

                                        <div className="workflow-label">
                                            <div>{step.description}</div>

                                            {step.date && <div>{step.date}</div>}
                                        </div>
                                    </div>

                                    {/* Connector - don't render after last step */}
                                    {index < steps.length - 1 && (
                                        <div
                                            className={`workflow-connector ${index % 2 === 0 ? "up-to-down" : "down-to-up"}`}
                                        />
                                    )}
                                </React.Fragment>
                            );
                        })}
                    </div>
                </div>
                <div className="section-title d-flex flex-column flex-md-row align-items-md-center">
                    <div className="workflow-summary-column d-flex flex-column flex-grow-1 mb-3 mb-md-0">
                        <h5>Step {stepInfo.stepNumber} of {stepInfo.totalSteps} — {stepInfo.stepName} {stepInfo.status}</h5>
                        <span className="project-tagline">
                            Milestone tasks are marked with a star. <Icon.StarFill size={12} className="text-warning" /> Updating milestone tasks will update the workflow status.
                        </span>
                    </div>
                    
                    <div className="workflow-progress-action flex-shrink-0 ms-md-4">
                        <div className="grade-wrapper">
                            <div className="d-flex justify-content-between mb-1">
                                <span className="grade-label final">Progress</span>
                                <span className="grade-value final">{progressPercentage}%</span>
                            </div>
                            <div className={`grade-track final${progressPercentage === 100 ? " task-progress completed" : ""}`}>
                                <div
                                    className="grade-bar bar-final"
                                    style={{
                                        width: `${progressPercentage}%`,
                                        backgroundColor: getWorkflowProgressColor(progressPercentage)
                                    }}
                                />
                            </div>
                        </div>
                    </div>

                </div>
            </div>
            <div className="workflow-list">
                <WorkflowList
                    projectId={projectId}
                    tasksViewModel={workflowData?.tasksViewModel}
                    onWorkflowSaved={loadWorkflowData}
                />
            </div>
        </>
    );
}

export default WorkflowDetails;
