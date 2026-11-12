import React, { useContext, useState, useEffect, useCallback } from "react";
import {
    SideDialog,
    Input,
    DatePickerInput,
    Label,
    Button,
    BUTTONS,
    Divider,
    SideDialogManagerContext,
} from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import moment from "moment";
import {
    FINAL_GRADE_VALUES,
    SUB_GRADE_OPTIONS,
    SUB_GRADE_ROWS,
} from "../../constants/reportGrades";
import * as projectService from "../../service/ProjectService";
import type { ReportDetailsViewModel } from "../../service/ProjectService";
import { usePopup } from "../../Common/PopupProvider";
import { resultMessage } from "../../Common/UtilityMessage";

export interface ReportCertificateProps {
    id: string;
    projectId: number;
    initial: ReportDetailsViewModel;
    onSaveSuccess?: () => void;
}

function toDate(v: string | Date | undefined): Date | null {
    if (v == null || v === "") return null;
    const m = moment(v);
    return m.isValid() ? m.toDate() : null;
}

function numToInput(v: number | undefined): string {
    if (v === undefined || v === null || Number.isNaN(v)) return "";
    return v === 0 ? "" : String(v);
}

function inputToNum(s: string): number | undefined {
    const t = s.trim();
    if (t === "") return undefined;
    const n = parseInt(t, 10);
    return Number.isFinite(n) ? n : undefined;
}

const ReportCertificate: React.FC<ReportCertificateProps> = ({
    id,
    projectId,
    initial,
    onSaveSuccess,
}) => {
    const popup = usePopup();
    const sideDialog = useContext(SideDialogManagerContext) as {
        removeDialog?: (dialogId: string) => void;
    };

    const handleClose = useCallback(() => {
        sideDialog?.removeDialog?.(id);
    }, [id, sideDialog]);

    const [saving, setSaving] = useState(false);
    const [capCert, setCapCert] = useState("");
    const [classReport, setClassReport] = useState("");
    const [structReport, setStructReport] = useState("");
    const [capDate, setCapDate] = useState<Date | null>(null);
    const [classDate, setClassDate] = useState<Date | null>(null);
    const [structDate, setStructDate] = useState<Date | null>(null);
    const [finalGrade, setFinalGrade] = useState<number | undefined>(undefined);
    const [structuralGrade, setStructuralGrade] = useState<number | undefined>(undefined);
    const [fatigueGrade, setFatigueGrade] = useState<number | undefined>(undefined);
    const [renewalGrade, setRenewalGrade] = useState<number | undefined>(undefined);
    const [materialGrade, setMaterialGrade] = useState<number | undefined>(undefined);
    const [gaugingGrade, setGaugingGrade] = useState<number | undefined>(undefined);
    const [hullGirder, setHullGirder] = useState<number | undefined>(undefined);

    useEffect(() => {
        setCapCert(numToInput(initial.capCertificateNumber));
        setClassReport(numToInput(initial.classReportNumber));
        setStructReport(numToInput(initial.structuralReportNumber));
        setCapDate(toDate(initial.capCertificateIssuanceDate));
        setClassDate(toDate(initial.classReportDate));
        setStructDate(toDate(initial.structuralReportDate));
        setFinalGrade(initial.finalGrade);
        setStructuralGrade(initial.structuralGrade);
        setFatigueGrade(initial.fatigueGrade);
        setRenewalGrade(initial.renewalGrade);
        setMaterialGrade(initial.materialGrade);
        setGaugingGrade(initial.gaugingGrade);
        setHullGirder(initial.hullGirderStrength);
    }, [initial]);

    const setSubGrade = (key: (typeof SUB_GRADE_ROWS)[number]["key"], value: number) => {
        switch (key) {
            case "structuralGrade":
                setStructuralGrade(value);
                break;
            case "fatigueGrade":
                setFatigueGrade(value);
                break;
            case "renewalGrade":
                setRenewalGrade(value);
                break;
            case "materialGrade":
                setMaterialGrade(value);
                break;
            case "gaugingGrade":
                setGaugingGrade(value);
                break;
            case "hullGirderStrength":
                setHullGirder(value);
                break;
        }
    };

    const getSubGrade = (key: (typeof SUB_GRADE_ROWS)[number]["key"]): number | undefined => {
        switch (key) {
            case "structuralGrade":
                return structuralGrade;
            case "fatigueGrade":
                return fatigueGrade;
            case "renewalGrade":
                return renewalGrade;
            case "materialGrade":
                return materialGrade;
            case "gaugingGrade":
                return gaugingGrade;
            case "hullGirderStrength":
                return hullGirder;
            default:
                return undefined;
        }
    };

    const handleSave = async () => {
        const payload: ReportDetailsViewModel = {
            gradesId: initial.gradesId,
            projectId,
            projectName: initial.projectName,
            capCertificateNumber: inputToNum(capCert),
            classReportNumber: inputToNum(classReport),
            structuralReportNumber: inputToNum(structReport),
            capCertificateIssuanceDate: capDate ? moment(capDate).format("DD-MMM-YYYY") : undefined,
            classReportDate: classDate ? moment(classDate).format("DD-MMM-YYYY") : undefined,
            structuralReportDate: structDate ? moment(structDate).format("DD-MMM-YYYY") : undefined,
            finalGrade,
            structuralGrade,
            fatigueGrade,
            renewalGrade,
            materialGrade,
            gaugingGrade,
            hullGirderStrength: hullGirder,
            cap1CertificateIssuanceDate: initial.cap1CertificateIssuanceDate,
            cap1FinalGrade: initial.cap1FinalGrade,
        };

        setSaving(true);
        try {
            await projectService.saveReportDetails(payload);
            popup.success(resultMessage("Report Details", "Updated"), "Edit Report Details");
            onSaveSuccess?.();
            handleClose();
        } catch (e: any) {
            popup.error(e?.response?.data || e?.message || "Unable to save report details. Please try again.", "Edit Report Details");
        } finally {
            setSaving(false);
        }
    };

    return (
        <>
            <SideDialog
                id={id}
                confirmOnClose={false}
                className="right-side-dialog"
                showWarningMinimize={() => false}
                title={
                    <div className="side-panel-title">
                        <h5>Edit Report Details</h5>
                        <div className="side-header-actions">
                            <Button className="close-button" onClick={handleClose}>
                                <Icon.X />
                            </Button>
                        </div>
                    </div>
                }
                minimizedTitle="Edit Report Details"
            >
                {() => (
                    <>
                        <h3 className="form-group-title">Project Information</h3>
                        <div className="row">
                            <div className="col-md-4 form-group">
                                <Label name="CAP Certificate No." bold />
                                <Input
                                    label=""
                                    placeholder="Enter CAP Certificate No."
                                    value={capCert}
                                    onChange={(value: string) => setCapCert(value)}
                                />
                            </div>
                            <div className="col-md-4 form-group">
                                <Label name="Class Report No." bold />
                                <Input
                                    label=""
                                    placeholder="Enter Class Report No."
                                    value={classReport}
                                    onChange={(value: string) => setClassReport(value)}
                                />
                            </div>
                            <div className="col-md-4 form-group">
                                <Label name="Structural Report No." bold />
                                <Input
                                    label=""
                                    placeholder="Enter Structural Report No."
                                    value={structReport}
                                    onChange={(value: string) => setStructReport(value)}
                                />
                            </div>
                            <div className="col-md-4 form-group">
                                <Label name="CAP Certificate Issuance Date" bold />
                                <DatePickerInput
                                    onChange={(d: Date) => setCapDate(d)}
                                    value={capDate}
                                />
                            </div>
                            <div className="col-md-4 form-group">
                                <Label name="Class Report Date" bold />
                                <DatePickerInput
                                    onChange={(d: Date) => setClassDate(d)}
                                    value={classDate}
                                />
                            </div>
                            <div className="col-md-4 form-group">
                                <Label name="Structural Report Date" bold />
                                <DatePickerInput
                                    onChange={(d: Date) => setStructDate(d)}
                                    value={structDate}
                                />
                            </div>
                        </div>
                        <h3 className="form-group-title">Select Grades</h3>
                        <div className="row">
                            <div className="col-md-12">
                                <table className="table grading-table">
                                    <tbody>
                                        <tr>
                                            <td className="row-label">Final Grade</td>
                                            <td>
                                                <div className="grades" id="final">
                                                    {FINAL_GRADE_VALUES.map((g) => (
                                                        <button
                                                            key={g}
                                                            type="button"
                                                            className={
                                                                "grade-btn" +
                                                                (finalGrade === g ? " active" : "")
                                                            }
                                                            onClick={() => setFinalGrade(g)}
                                                        >
                                                            {g}
                                                        </button>
                                                    ))}
                                                </div>
                                            </td>
                                        </tr>
                                        {SUB_GRADE_ROWS.map(({ key, label }) => (
                                            <tr key={key}>
                                                <td className="row-label">{label}</td>
                                                <td>
                                                    <div className="grades" id={key}>
                                                        {SUB_GRADE_OPTIONS.map(({ value, label: lb }) => (
                                                            <button
                                                                key={value}
                                                                type="button"
                                                                className={
                                                                    "grade-btn" +
                                                                    (getSubGrade(key) === value
                                                                        ? " active"
                                                                        : "")
                                                                }
                                                                onClick={() => setSubGrade(key, value)}
                                                            >
                                                                {lb}
                                                            </button>
                                                        ))}
                                                    </div>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        <div className="col-md-12 form-group">
                            <Divider className="divider-1" />
                        </div>
                        <div className="col-md-12 form-group">
                            <div className="d-flex gap-2">
                                <Button
                                    variant={BUTTONS.PRIMARY}
                                    onClick={handleSave}
                                    disabled={saving}
                                >
                                    {saving ? "Updating…" : "Update"}
                                </Button>
                                <Button variant={BUTTONS.SECONDARY} onClick={handleClose}>
                                    Cancel
                                </Button>
                            </div>
                        </div>
                    </>
                )}
            </SideDialog>
        </>
    );
};

export default ReportCertificate;
