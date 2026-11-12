import React, { useContext } from "react";
import { AccordionGroup, Badge, Button, SideDialogManagerContext, BUTTONS } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import type { TasksViewModel } from "../../Interface/Workflow";
import type { TaskStatus } from "../../Interface/Task";
import WorkflowEdit from "./WorkflowEdit";
import type { WorkflowTask as Task, WorkflowSectionModel as WorkflowSection } from "./workflowConverters";
import { convertTasksViewModelToSections } from "./workflowConverters";
import TaskRowMeta from "./TaskRowMeta";
let uniqueIdCounter = 0;
const getUniqueId = (prefix: string = 'id') => `${prefix}_${Date.now()}_${++uniqueIdCounter}`;

interface WorkflowListProps {
    projectId?: number;
    tasksViewModel?: TasksViewModel;
    taskStatuses?: TaskStatus[];
    /** Callback after a task is saved (e.g. refresh workflow list). */
    onWorkflowSaved?: () => void;
}


interface TaskRowProps {
    task: Task;
    onTaskClick?: (task: Task) => void;
}

const TaskRow: React.FC<TaskRowProps> = ({ task, onTaskClick }) => (
    <div className="task-row">
        <button className="star-btn">
            {task.starred ? (
                <Icon.StarFill className="text-warning" />
            ) : (
                <Icon.Star />
            )}
        </button>
        <div className="task-info">
            <div className="task-name">{task.name}</div>
            <TaskRowMeta task={task} />
        </div>
        <Badge color={task.badge.color} label={task.badge.label} />

        <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={() => onTaskClick?.(task)} startIcon={<Icon.ArrowRight />}></Button>

    </div>
);

interface SectionHeaderProps {
    title: string;
    completedTasks: number;
    totalTasks: number;
}

const SectionHeader: React.FC<SectionHeaderProps> = ({
    title,
    completedTasks,
    totalTasks,
}) => (
    <div className="section-title-alt flex-column">
        <h5 className="_600">{title}</h5>
        <p className="text-muted mb-0">
            {completedTasks}/{totalTasks} Tasks
        </p>
    </div>
);
const buildGroupData = (sections: WorkflowSection[], onTaskClick: (task: Task) => void) =>
    sections.map((section) => ({
        header: (
            <SectionHeader
                title={section.title}
                completedTasks={section.completedTasks}
                totalTasks={section.totalTasks}
            />
        ),
        body: [
            <div key={section.id}>
                <div className="task-list">
                    {section.tasks.map((task) => (
                        <TaskRow key={task.id} task={task} onTaskClick={onTaskClick} />
                    ))}
                </div>
            </div>,
        ],
        bgColor: "White",
        titleBold: false,
    }));

const WorkflowList: React.FC<WorkflowListProps> = ({ projectId, tasksViewModel, onWorkflowSaved }) => {
    const sideDialog = useContext(SideDialogManagerContext) as any;

    const handleTaskClick = (task: Task) => {
        const dialogId = getUniqueId("workflowEdit");
        if (sideDialog && sideDialog.addDialog) {
            sideDialog.addDialog({
                id: dialogId,
                component: (
                    <WorkflowEdit
                        id={dialogId}
                        taskName={task.name}
                        taskId={task.id}
                        projectId={projectId}
                        taskData={task.singleTaskData}
                        onSaveSuccess={onWorkflowSaved}
                    />
                )
            });
        }
    };

    const sections = convertTasksViewModelToSections(tasksViewModel);

    if (sections.length === 0) {
        return (
            <div className="text-center p-4 text-muted">
                No workflow sections available
            </div>
        );
    }

    const groupData = buildGroupData(sections, handleTaskClick);

    return (
        <>
            <AccordionGroup
                data={groupData}
                initialOpen
                titleFontSize="0.9rem"
            />
        </>
    )

}
export default WorkflowList;