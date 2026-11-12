import React, { useContext, useEffect, useState, useMemo, useCallback } from "react";
import AppLoader from '../../Common/AppLoader';
import { AccordionGroup, Badge, Button, SideDialogManagerContext, BUTTONS, TextField } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import RevenueEdit from "./RevenueEdit";
import _uniqueId from "lodash/uniqueId";
import * as revenueService from "../../service/RevenueService";
import type { WorkflowTask, WorkflowSectionModel } from "../Workflow/workflowConverters";
import { convertTasksViewModelToSections } from "../Workflow/workflowConverters";
import TaskRowMeta from "../Workflow/TaskRowMeta";

interface RevenueListProps {
    projectId?: number;
}

interface TaskRowProps {
    task: WorkflowTask;
    onArrowClick: (task: WorkflowTask) => void;
}
const formatCurrency = (value: number | null | undefined): string => {
    const n = value === null || value === undefined || Number.isNaN(Number(value)) ? 0 : Number(value);
    return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(n);
};

const TaskRow: React.FC<TaskRowProps> = ({ task, onArrowClick }) => (
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
        <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={() => onArrowClick(task)}
            startIcon={<Icon.ArrowRight />}></Button>

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

const buildGroupData = (
    sections: WorkflowSectionModel[],
    onArrowClick: (task: WorkflowTask) => void
) =>
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
                        <TaskRow key={task.id} task={task} onArrowClick={onArrowClick} />
                    ))}
                </div>
            </div>,
        ],
        bgColor: "White",
        titleBold: false,
    }));

const RevenueList: React.FC<RevenueListProps> = ({ projectId }) => {
    const sideDialog = useContext(SideDialogManagerContext) as {
        addDialog?: (opts: { id: string; component: React.ReactNode }) => void;
    };
    const [loading, setLoading] = useState(false);
    const [capContractValue, setCapContractValue] = useState<number | null>(null);
    const [invoiceSum, setInvoiceSum] = useState<number | null>(null);
    const [sections, setSections] = useState<WorkflowSectionModel[]>([]);

    const loadTaskSections = useCallback(async () => {
        if (projectId == null) {
            setSections([]);
            return;
        }
        try {
            const tvm = await revenueService.fetchRevenueTaskSections(projectId);
            const filtered = revenueService.filterRevenueTasksViewModel(tvm) ?? tvm;
            setSections(convertTasksViewModelToSections(filtered));
        } catch (e) {
            setSections([]);
        }
    }, [projectId]);

    const openRevenueEdit = useCallback(
        (task: WorkflowTask) => {
            const add = sideDialog?.addDialog;
            if (!add) return;
            const dialogId = _uniqueId("sideDialog_");
            add({
                id: dialogId,
                component: (
                    <RevenueEdit
                        id={dialogId}
                        projectId={projectId}
                        taskId={task.id}
                        taskName={task.name}
                        taskData={task.singleTaskData}
                        onSaveSuccess={loadTaskSections}
                    />
                ),
            });
        },
        [sideDialog, projectId, loadTaskSections]
    );

    useEffect(() => {
        if (projectId == null) {
            setCapContractValue(null);
            setInvoiceSum(null);
            setSections([]);
            return;
        }

        let cancelled = false;
        setLoading(true);

        const summaryPromise = revenueService
            .fetchRevenueSummary(projectId)
            .then((summary) => {
                if (!cancelled) {
                    setCapContractValue(summary.capContractValue);
                    setInvoiceSum(summary.invoiceSum);
                }
            })
            .catch(() => {
                if (!cancelled) {
                    setCapContractValue(null);
                    setInvoiceSum(null);
                }
            });

        const tasksPromise = revenueService
            .fetchRevenueTaskSections(projectId)
            .then((tvm) => {
                if (cancelled) return;
                const filtered = revenueService.filterRevenueTasksViewModel(tvm) ?? tvm;
                setSections(convertTasksViewModelToSections(filtered));
            })
            .catch(() => {
                if (!cancelled) setSections([]);
            });

        Promise.all([summaryPromise, tasksPromise]).finally(() => {
            if (!cancelled) setLoading(false);
        });

        return () => {
            cancelled = true;
        };
    }, [projectId]);

    const groupData = useMemo(
        () => buildGroupData(sections, openRevenueEdit),
        [sections, openRevenueEdit]
    );

    const capDisplay = loading ? "…" : formatCurrency(capContractValue);
    const invoiceDisplay = loading ? "…" : formatCurrency(invoiceSum);

    return (
        <>
            <div className="section-title d-flex flex-column flex-md-row align-items-start align-items-md-center justify-content-between mb-3">
                <h5 className="text-nowrap flex-shrink-0 mb-2 mb-md-0 me-3">Revenue Tracker</h5>
                <div className="d-flex flex-column flex-md-row justify-content-md-end align-items-start align-items-md-center gap-2 gap-md-3 ms-md-auto mt-2 mt-md-0">
                    <div className="d-flex align-items-center">
                        <TextField label="CAP Contract Value" inline >
                            {capDisplay}
                        </TextField>
                    </div>
                    <span className="d-none d-md-inline-flex align-items-center"><Icon.CircleFill size={6} /></span>
                    <div className="d-flex align-items-center">
                        <TextField label="Client Invoices" inline >
                            {invoiceDisplay}
                        </TextField>
                    </div>
                </div>
            </div>
            <div className="revenue-list">
                {loading && sections.length === 0 ? (
                    <AppLoader inline />
                ) : sections.length === 0 ? (
                    <div className="text-center p-4 text-muted">No revenue tasks available</div>
                ) : (
                    <AccordionGroup data={groupData} initialOpen titleFontSize="0.9rem" />
                )}
            </div>
        </>
    );

}
export default RevenueList;