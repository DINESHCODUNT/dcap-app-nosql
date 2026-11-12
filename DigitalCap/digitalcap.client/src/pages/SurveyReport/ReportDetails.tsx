import React, { useContext, useEffect, useState, useCallback } from "react";
import AppLoader from '../../Common/AppLoader';
import * as Icon from "react-bootstrap-icons";
import _uniqueId from "lodash/uniqueId";
import moment from "moment";
import { TextField, Button, BUTTONS, Divider, SideDialogManagerContext } from "customer_portal-ui-shared";
import { useNavigate } from 'react-router-dom';
import ReportCertificate from "./ReportCerticiate";
import * as projectService from "../../service/ProjectService";
import type { ReportDetailsViewModel } from "../../service/ProjectService";
import {
    getFinalGradeLabel,
    getSubGradeLabel,
    subGradeBarColorClass,
    subGradeBarWidthPct,
    finalGradeBarWidthPct,
    SUB_GRADE_ROWS,
    finalGradeBarColorClass,
    getFinalGradeTextColor,
    getSubGradeTextColor
} from "../../constants/reportGrades";

const FieldBlock: React.FC<{ label: string; value: React.ReactNode; className?: string }> = ({
    label,
    value,
    className,
}) => (
    <TextField label={label} className={className}>
        {value}
    </TextField>
);

function displayNum(n: number | undefined): string {
    if (n === undefined || n === null || n === 0) return "-";
    return String(n);
}

function displayDate(d: string | Date | undefined): string {
    if (d == null || d === "") return "-";
    const m = moment(d);
    return m.isValid() ? m.format("DD-MMM-YYYY") : "-";
}

export interface ReportDetailsProps {
    projectId?: number;
    globalPermission: (perm: string) => boolean;
    canEditReportDetails?: boolean;
}

const ReportDetails: React.FC<ReportDetailsProps> = ({
    projectId,
    globalPermission,
    canEditReportDetails = true,
}) => {
    const sideDialog = useContext(SideDialogManagerContext) as {
        addDialog?: (opts: { id: string; component: React.ReactNode }) => void;
    };

    const [model, setModel] = useState<ReportDetailsViewModel | null>(null);
    const [loading, setLoading] = useState(false);
    const [loadError, setLoadError] = useState<string | null>(null);
    const navigate = useNavigate();
    const loadReportDetails = useCallback(async () => {
        if (projectId == null || Number.isNaN(projectId)) {
            setModel(null);
            return;
        }
        setLoading(true);
        setLoadError(null);
        try {
            const m = await projectService.fetchReportDetailsModel(projectId);
            setModel(m);
        } catch (e) {
            setLoadError("Could not load report details.");
            setModel({ projectId });
        } finally {
            setLoading(false);
        }
    }, [projectId]);

    useEffect(() => {
        loadReportDetails();
    }, [loadReportDetails]);

    const handleEdit = () => {
        if (!model || projectId == null) return;
        const id = _uniqueId("reportDetailsEdit_");
        sideDialog?.addDialog?.({
            id,
            component: (
                <ReportCertificate
                    id={id}
                    projectId={projectId}
                    initial={model}
                    onSaveSuccess={loadReportDetails}
                />
            ),
        });
    };

    const handleViewReport = () => {
        navigate('/app/project/report', { state: { projectId } });
    };

    if (projectId == null) {
        return null;
    }

    if (loading && !model) {
        return (
            <div className="mt-3">
                <AppLoader inline />
            </div>
        );
    }

    const m = model ?? { projectId };

    return (
        <>
            <div className="mt-3">
                <div className="section-title mb-3">
                    <h5 className='_600'>CAP Report /Certificates</h5>
                    {canEditReportDetails &&
                        globalPermission("AbsProjectLandingPageReassessmentButton") && (
                            <div className="section-actions">
                                <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={handleEdit} startIcon={<Icon.Pencil />}></Button>
                            </div>
                        )}
                </div>
                {loadError && (
                    <div className="mb-2 tx-14" style={{ color: "coral" }}>
                        {loadError}
                    </div>
                )}
                <div className="project-landing-row">
                    <FieldBlock
                        label="CAP Certificate No."
                        value={displayNum(m.capCertificateNumber)}
                    />
                    <FieldBlock label="Class Report No." value={displayNum(m.classReportNumber)} />
                    <FieldBlock
                        label="Structural Report No."
                        value={displayNum(m.structuralReportNumber)}
                    />
                    <FieldBlock
                        label="CAP Cert. Issuance Date"
                        value={displayDate(m.capCertificateIssuanceDate)}
                    />
                    <FieldBlock label="Class Report Date" value={displayDate(m.classReportDate)} />
                    <FieldBlock
                        label="Structural Report Date"
                        value={displayDate(m.structuralReportDate)}
                    />
                </div>
                <Divider className="divider-1" />
                <div className="section-title mb-2 d-flex flex-wrap flex-md-nowrap align-items-center justify-content-between">
                    <h5 className="_600 mb-md-0 mt-2">Grading</h5>
                    <div className="section-actions mt-2 mt-md-0">
                        <div className="d-flex flex-wrap align-items-center gap-3">
                            <span className="d-flex align-items-center gap-1 tx-13 text-nowrap">
                                <span className="report-details-dot satisfactory"></span>
                                A - B Satisfactory
                            </span>
                            <span className="d-flex align-items-center gap-1 tx-13 text-nowrap">
                                <span className="report-details-dot marginal"></span>
                                C - Marginal
                            </span>
                            <span className="d-flex align-items-center gap-1 tx-13 text-nowrap">
                                <span className="report-details-dot poor"></span>
                                D - Poor
                            </span>
                        </div>
                    </div>
                </div>

                <div className="grade-wrapper">
                    {/* All grade cards in one unified grid — Final Grade first */}
                    <div className="row row-cols-2 row-cols-md-3 row-cols-lg-5 g-3">
                        {/* Final Grade card */}
                        <div className="col">
                            <div className="grade-summary-card">
                                <div className="d-flex justify-content-between align-items-center mb-2">
                                    <span className="tx-13 _500 text-muted">Final Grade</span>
                                    <span className="tx-13 _600" style={{ color: getFinalGradeTextColor(m.finalGrade) }}>
                                        {getFinalGradeLabel(m.finalGrade)}
                                    </span>
                                </div>
                                <div className="grade-track grade-track-mini">
                                    <div
                                        className={`grade-bar grade-bar-mini ${finalGradeBarColorClass(m.finalGrade)}`}
                                        style={{ width: `${finalGradeBarWidthPct(m.finalGrade)}%` }}
                                    />
                                </div>
                            </div>
                        </div>

                        {/* Sub-grade cards */}
                        {SUB_GRADE_ROWS.map(({ key, label }) => {
                            const v = m[key];
                            const gradeLabel = getSubGradeLabel(v);
                            const barColor = subGradeBarColorClass(v);
                            const isDPoor = gradeLabel === 'D';
                            const subLabel = (() => {
                                if (gradeLabel === '-') return 'Not assessed';
                                if (['A+', 'A', 'A-'].includes(gradeLabel)) return 'Excellent';
                                if (['B+', 'B', 'B-'].includes(gradeLabel)) return 'Satisfactory';
                                if (['C+', 'C', 'C-'].includes(gradeLabel)) return 'Marginal';
                                if (gradeLabel === 'D') return 'Unsatisfactory';
                                return '';
                            })();
                            return (
                                <div className="col" key={key}>
                                    <div className={`grade-summary-card ${isDPoor ? 'poor' : ''}`}>
                                        <div className="d-flex justify-content-between align-items-center mb-2">
                                            <span className="tx-13 _500 text-muted">{label}</span>
                                            <span className="tx-13 _600" style={{ color: getSubGradeTextColor(v) }}>
                                                {gradeLabel}
                                            </span>
                                        </div>
                                        <div className="grade-track grade-track-mini">
                                            <div
                                                className={`grade-bar grade-bar-mini ${barColor}`}
                                                style={{ width: `${subGradeBarWidthPct(v)}%` }}
                                            />
                                        </div>
                                        <span className="tx-12 text-muted">{subLabel}</span>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>

                {/* View Report/Certificates button — centred below grading */}
                <div className="d-flex justify-content-center mt-4">
                    <Button variant={BUTTONS.SECONDARY} onClick={handleViewReport}>
                        View Report/Certificates →
                    </Button>
                </div>
            </div>
        </>
    );
};

export default ReportDetails;
