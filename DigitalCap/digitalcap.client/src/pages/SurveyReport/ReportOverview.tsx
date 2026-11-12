import { useLocation, useNavigate } from "react-router-dom";
import React, { useEffect, useState, useRef, useCallback } from "react";
import { BreadcrumbBar, Button, BUTTONS, NotificationContainer } from "customer_portal-ui-shared";
import * as Icon from 'react-bootstrap-icons';
import Dropdown from 'react-bootstrap/Dropdown';
import Modal from 'react-bootstrap/Modal';
import Offcanvas from 'react-bootstrap/Offcanvas';
import ReportSidebar from './ReportSidebar';
import ReportContent from './ReportContent';
import VesselMainData from './VesselMainData';
import PartSummary from './PartSummary';
import SurveyStatus from './SurveyStatus';
import StatutoryCertificates from './StatutoryCertificates';
import * as surveyReportService from '../../service/SurveyReportService';
import { formatVisitDateForSave } from '../../utils/dateTime';
import { applyVesselFieldUpdate } from '../../utils/vesselFieldSync';
import { applySurveyStatusFieldUpdate } from '../../utils/surveyStatusFieldSync';
import * as projectService from '../../service/ProjectService';
import { usePermissions } from '../../context/PermissionContext';
import { ROLES } from '../../constants/roles';
import { usePopup } from '../../Common/PopupProvider';
import { ExportConfigurationModal } from './ExportConfigurationModal';
import { ExportAllConfigurationModal } from './ExportAllConfigurationModal';
import { EMPTY_GUID } from '../../utils/exportSettings';
import { DESKTOP_URL, getFreshToken } from '../../utils/desktop';
import { measureConnectionSpeed, isNetworkConnectionError } from '../../utils/connectionSpeed';
import { toFriendlyErrorMessage } from '../../utils/errorMessage';
import TransferProgressBar from "../../Common/TransferProgressBar";
function UnsavedConfirmationModal({
    message,
    isLoading,
    onSaveDraft,
    onClose
}: {
    message?: string;
    isLoading?: boolean;
    onSaveDraft?: () => void;
    onClose?: () => void
}) {
    return (
        <div className="notification-model">
            <div className="notification-overlay">
                <div className="notification-body">
                    <div className="d-flex justify-content-end">
                        {onClose && (
                            <Button
                                variant={BUTTONS.TERTIARY}
                                onClick={onClose}
                                disabled={isLoading}
                            >
                                <Icon.X size={20} />
                            </Button>
                        )}
                    </div>

                    {isLoading ? (
                        <>
                            <p className="notification-title">Exporting Report</p>
                            <div className="d-flex align-items-center gap-2 mt-2">
                                <div className="spinner-border spinner-border-sm" />
                                <span>Exporting report, please wait...</span>
                            </div>
                            <Button className="btn-save mt-3" disabled={true}>
                                Export
                            </Button>
                        </>
                    ) : (
                        <>
                            <p className="notification-title">Your pdf is ready to export:</p>
                            <p className="notification-message">{message}</p>
                            <Button className="btn-save" onClick={onSaveDraft}>
                                Export
                            </Button>
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}

const CustomDropdownToggle = React.forwardRef<HTMLSpanElement, any>(
    ({ children, onClick, disabled }, ref) => (
        <span
            ref={ref}
            onClick={(e) => {
                e.preventDefault();
                if (!disabled) {
                    onClick(e);
                }
            }}
            style={{ cursor: disabled ? 'not-allowed' : 'pointer' }}
        >
            <Button
                variant={BUTTONS.SECONDARY}
                disabled={disabled}
                style={{ pointerEvents: 'none' }}
            >
                {children} <Icon.ChevronDown className="ms-1" size={12} />
            </Button>
        </span>
    )
);

const ReportOverview: React.FC = () => {

    const location = useLocation();
    const { hasRole } = usePermissions();

    const projectId = location.state?.projectId;
    const projectStatus = location.state?.projectStatus;

    const isOfflineProjectStatus =
        String(projectStatus ?? "").toLowerCase() === "offline";

    const [loading, setLoading] = useState(false);

    const [data, setData] = useState<any | null>(null);

    const popup = usePopup();
    const isOfflineProject = isOfflineProjectStatus || data?.synchedOnline != null;
    const isMountedRef = useRef(true);
    useEffect(() => {
        isMountedRef.current = true;
        return () => { isMountedRef.current = false; };
    }, []);

    const [showDiscardOfflineWarning] = useState(false);

    const isElectronUserAgent = typeof navigator !== "undefined" && navigator.userAgent.toLowerCase().includes("electron");
    const [showDownloadConfirm, setShowDownloadConfirm] = useState(false);
    const [showDownloadProcessing, setShowDownloadProcessing] = useState(false);
    const [downloadAbortController, setDownloadAbortController] = useState<AbortController | null>(null);
    const [downloadError, setDownloadError] = useState<string | null>(null);
    const [isDownloadInBackground, setIsDownloadInBackground] = useState<boolean>(false);
    const [downloadSuccessModal, setDownloadSuccessModal] = useState<boolean>(false);
    const [isCheckingSpeed, setIsCheckingSpeed] = useState<boolean>(false);
    const [downloadPercent, setDownloadPercent] = useState<number>(0);
    const downloadPollRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        return () => {
            if (downloadPollRef.current) {
                clearInterval(downloadPollRef.current);
                downloadPollRef.current = null;
            }
        };
    }, []);

    const handleDownloadOfflineProceed = async () => {
        setShowDownloadConfirm(false);
        setShowDownloadProcessing(true);
        setDownloadError(null);
        setIsDownloadInBackground(false);
        setDownloadPercent(0);

        if (downloadPollRef.current) {
            clearInterval(downloadPollRef.current);
            downloadPollRef.current = null;
        }

        const controller = new AbortController();
        setDownloadAbortController(controller);

        try {
            const token = await getFreshToken();

            downloadPollRef.current = setInterval(async () => {
                try {
                    const res = await fetch(`${DESKTOP_URL}/api/DataTransferAPI/Progress?projectId=${projectId}`, {
                        headers: {
                            "Authorization": `Bearer ${token}`
                        }
                    });
                    if (res.ok) {
                        const prog = await res.json();
                        if (typeof prog?.percent === 'number') {
                            setDownloadPercent(prog.percent);
                        }
                    }
                } catch {
                    // ignore polling errors
                }
            }, 500);

            const response = await fetch(`${DESKTOP_URL}/api/DataTransferAPI/Download?projectId=${projectId}`, {
                method: "POST",
                headers: {
                    "Authorization": `Bearer ${token}`
                },
                signal: controller.signal
            });

            if (downloadPollRef.current) {
                clearInterval(downloadPollRef.current);
                downloadPollRef.current = null;
            }

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(toFriendlyErrorMessage(errorText, "Download failed"));
            }

            setDownloadPercent(100);
            setShowDownloadProcessing(false);
            setIsDownloadInBackground(false);
            setDownloadSuccessModal(true);

            const freshToken = (await getFreshToken()) || token;
            setTimeout(() => {
                window.location.href = `${DESKTOP_URL}/SetToken#token=${encodeURIComponent(freshToken)}&redirectURL=${encodeURIComponent("/app/Project")}`;
            }, 2000);
        } catch (err: any) {
            if (downloadPollRef.current) {
                clearInterval(downloadPollRef.current);
                downloadPollRef.current = null;
            }
            setDownloadPercent(0);
            if (err?.name !== "AbortError") {
                console.error("Offline download failed", err);
                if (err instanceof TypeError || err?.name === "TypeError") {
                    popup.error("Unable to reach the DigitalCAP desktop application. Please make sure it is installed and running, then try again.", "Download Offline");
                } else if (isNetworkConnectionError(err)) {
                    popup.info({
                        title: "Connection Error",
                        message: "The download/upload process has been interrupted due to a loss of network connection. Please check your internet connection and try again.",
                        okText: "Close"
                    });
                } else {
                    setDownloadError(toFriendlyErrorMessage(err?.message, "Download failed. Please try again."));
                }
            }
            setShowDownloadProcessing(false);
            setIsDownloadInBackground(false);
        } finally {
            setDownloadAbortController(null);
        }
    };

    const handleStopDownload = () => {
        if (downloadPollRef.current) {
            clearInterval(downloadPollRef.current);
            downloadPollRef.current = null;
        }
        setDownloadPercent(0);
        setIsDownloadInBackground(false);
        if (downloadAbortController) {
            downloadAbortController.abort();
        }
        setShowDownloadProcessing(false);
        setDownloadAbortController(null);
    };

    const handleDownloadOfflineClick = async () => {
        setIsCheckingSpeed(true);
        try {
            const mbps = await measureConnectionSpeed();
            if (mbps === null || mbps <= 4) {
                popup.info(
                    "Unable to Sync/Download due to a very low internet connection. Please proceed when your internet connection is having at least 10 mbps download and upload speed.",
                    "Confirm"
                );
                return;
            }
            setShowDownloadConfirm(true);
        } finally {
            setIsCheckingSpeed(false);
        }
    };

    const [activeView, setActiveView] = useState<{
        part: string;
        section: string;
    }>({
        part: 'VMD',
        section: 'main-data',
    });

    const [reportIndex, setReportIndex] = useState<any>(null);
    const [reportData, setReportData] = useState<any>(null);

    const [hiReportData, setHiReportData] = useState<any>(null);

    const [isExporting, setIsExporting] = useState(false);
    const [isExportingAll, setIsExportingAll] = useState(false);
    const [showExportModal, setShowExportModal] = useState(false);
    const [showExportAllModal, setShowExportAllModal] = useState(false);
    const [exportAllMode, setExportAllMode] = useState<'export' | 'validate'>('export');
    const [showValidateModal, setShowValidateModal] = useState(false);
    const [showMobileSidebar, setShowMobileSidebar] = useState(false);
    const [exportTemplate, setExportTemplate] = useState<{ id: number, name: string } | null>(null);
    const [validateTemplate, setValidateTemplate] = useState<{ id: number, name: string } | null>(null);
    const [isNewProject, setIsNewProject] = useState(false);
    const [validateVisible, setValidateVisible] = useState(false);
    const [allValidateVisible, setAllValidateVisible] = useState(false);
    const [warningPanelTop, setWarningPanelTop] = useState<number>(90);
    const [isWarningHovered, setIsWarningHovered] = useState(false);
    const warningTimeoutRef = useRef<any>(null);
    const tooltipRef = useRef<HTMLDivElement>(null);
    const pageTopRef = useRef<HTMLDivElement>(null);

    const [validateWarnings, setValidateWarnings] = useState<string[]>([]);
    const [validateAllWarnings, setValidateAllWarnings] = useState<string[]>([]);
    const [isValidating, setIsValidating] = useState(false);
    const [isValidatingAll, setIsValidatingAll] = useState(false);
    const navigate = useNavigate();

    const isButtonloading = isExporting || isExportingAll || isValidating || isValidatingAll;

    useEffect(() => {
        if (validateVisible || allValidateVisible) {
            if (!isValidating && !isValidatingAll) {
                if (!isWarningHovered) {
                    warningTimeoutRef.current = setTimeout(() => {
                        setValidateVisible(false);
                        setAllValidateVisible(false);
                    }, 7000);
                } else if (warningTimeoutRef.current) {
                    clearTimeout(warningTimeoutRef.current);
                }
            }
        }
        return () => {
            if (warningTimeoutRef.current) {
                clearTimeout(warningTimeoutRef.current);
            }
        };
    }, [validateVisible, allValidateVisible, isWarningHovered, isValidating, isValidatingAll]);

    useEffect(() => {
        if (!validateVisible && !allValidateVisible) return;
        const update = () => {
            const bottom = pageTopRef.current?.getBoundingClientRect().bottom ?? 0;
            setWarningPanelTop(Math.max(16, Math.round(bottom + 8)));   // just below the title row; 16px from the top once scrolled past
        };
        update();
        window.addEventListener("scroll", update, { passive: true });
        window.addEventListener("resize", update);
        return () => { window.removeEventListener("scroll", update); window.removeEventListener("resize", update); };
    }, [validateVisible, allValidateVisible]);

    const hideWarningPanel = () => {
        if (warningTimeoutRef.current) {
            clearTimeout(warningTimeoutRef.current);
            warningTimeoutRef.current = null;
        }
        setValidateVisible(false);
        setAllValidateVisible(false);
        setIsWarningHovered(false);
    };

    const [modal, setModal] = useState<{
        message?: string;
        isLoading?: boolean;
        onSaveDraft?: () => void;
        onClose?: () => void;
    } | null>(null);

    const normalizeReportPartName = (name: string | undefined) => {
        const trimmed = (name ?? "").trim();
        if (!trimmed) return trimmed;
        if (trimmed.replace(/\s/g, "").toLowerCase() === "parth&i") {
            return "Part H & I";
        }
        return trimmed;
    };

    useEffect(() => {
        if (!projectId) return;

        surveyReportService.GetConfig(projectId)
            .then((configRes) => {
                const newProject = configRes.isNewProject;
                setIsNewProject(newProject);

                const indexFn = newProject
                    ? surveyReportService.getSurveyProjectIndex
                    : surveyReportService.getSurveyReportIndex;

                return indexFn(projectId).then((indexRes) => ({ indexRes, newProject }));
            })
            .then(({ indexRes, newProject }) => {
                const result = structuredClone(indexRes.data);
                if (result?.reportPartGrid?.length > 0) {
                    result.reportPartGrid = result.reportPartGrid.map((p: any) => ({
                        ...p,
                        templateId: p.templateId ?? p.partId,
                        name: normalizeReportPartName(p.name ?? p.partName),
                        sectionsList: (p.sectionsList || p.SectionsList || []).map((s: any) => ({
                            sectionId: s.sectionId ?? s.SectionId,
                            sectionName: s.sectionName ?? s.SectionName,
                        })),
                    }));
                }
                if (newProject) {
                    result.imonumber = result.imonumber ?? (result as any).Imonumber;
                }
                setReportIndex(result);
                setData(result);
            })
            .catch(() => {
            });

    }, [projectId]);

    useEffect(() => {
        if (!loading && pageTopRef.current) {
            const timeoutId = setTimeout(() => {
                pageTopRef.current?.scrollIntoView({ behavior: 'auto', block: 'start' });
            }, 100);
            return () => clearTimeout(timeoutId);
        }
    }, [activeView, loading]);

    useEffect(() => {

        if (
            !projectId ||
            !reportIndex ||
            !reportIndex.reportPartGrid
        ) return;

        let template;

        if (activeView.part === "VMD") {
            template = reportIndex.reportPartGrid.find(
                (x: any) =>
                    x.name === "Vessel Main Data" ||
                    x.templateId === 0
            );
        } else {
            template = reportIndex.reportPartGrid.find(
                (x: any) => {
                    const name = x.name || x.partName;
                    if (activeView.part === "HI") {
                        return name === "Part H & I" || name === "Part H&I";
                    }
                    return name === `Part ${activeView.part}`;
                }
            );
        }

        if (!template) return;

        setLoading(true);

        const fetchFn = isNewProject
            ? surveyReportService.SurveyIndexPartial
            : surveyReportService.IndexPartial;


        fetchFn(projectId, template.templateId, data?.imonumber || data?.imoNumber)
            .then(res => {
                const result = res.data?.data ?? res.data;

                setReportData(result);

                if (activeView.part === "HI") {
                    setHiReportData(structuredClone(result));
                }
            })
            .catch(err => console.error(err))
            .finally(() => setLoading(false));

    }, [
        activeView.part,
        projectId,
        reportIndex,
        isNewProject
    ]);

    const parseValidateGradingResponse = (data: string): string[] => {
        const warnings: string[] = [];
        if (!data || data === "" || data === "#") return warnings;

        const splitdata = data.split("#");

        if (splitdata[2] && splitdata[2] !== "") {
            splitdata[2].split(",").filter(x => x !== "")
                .forEach(item => warnings.push(`${item} — Grading is not selected`));
        }
        if (splitdata[0] && splitdata[0] !== "") {
            splitdata[0].split(",").filter(x => x !== "")
                .forEach(item => warnings.push(`${item} — After/Before Repair is not selected`));
        }
        if (splitdata[1] && splitdata[1] !== "") {
            splitdata[1].split(",").filter(x => x !== "")
                .forEach(item => warnings.push(`${item} — Description is not selected`));
        }
        if (splitdata[3] && splitdata[3] !== "") {
            splitdata[3].split(",").filter(x => x !== "")
                .forEach(item => warnings.push(`${item} — Add'l. Description is not entered`));
        }

        return warnings;
    };

    const parseExportAllValidationResponse = (data: string): string[] => {
        const warnings: string[] = [];
        if (!data || data === "" || data === "#") return warnings;

        const splitdata = data.split("#");

        if (splitdata[2] && splitdata[2] !== "") {
            splitdata[2].split(",").filter(x => x !== "")
                .forEach(item => warnings.push(`${item} — Grading is not selected`));
        }
        if (splitdata[0] && splitdata[0] !== "") {
            splitdata[0].split(",").filter(x => x !== "")
                .forEach(item => warnings.push(`${item} — After/Before Repair is not selected`));
        }
        if (splitdata[1] && splitdata[1] !== "") {
            splitdata[1].split(",").filter(x => x !== "")
                .forEach(item => warnings.push(`${item} — Description is not selected`));
        }
        if (splitdata[3] && splitdata[3] !== "") {
            splitdata[3].split(",").filter(x => x !== "")
                .forEach(item => warnings.push(`${item} — Add'l. Description is not entered`));
        }

        return warnings;
    };

    const handleValidate = async () => {
        try {
            setIsValidating(true);
            setValidateWarnings([]);

            let template: any = null;

            if (activeView.part === "VMD") {

                template = reportIndex?.reportPartGrid?.find(
                    (x: any) => x.name === "Vessel Main Data" || x.templateId === 0
                );
            } else if (activeView.part === "HI") {
                template = reportIndex?.reportPartGrid?.find(
                    (x: any) => (x.name === "Part H & I" || x.name === "Part H&I")
                );
            } else {
                template = reportIndex?.reportPartGrid?.find(
                    (x: any) => x.name === `Part ${activeView.part}`
                );
            }

            if (!template) return;

            let response: any;

            if (isNewProject) {
                const imo = data?.imonumber || data?.imoNumber;

                const checkShowRes = await surveyReportService.checkShowExportSettings(projectId, template.templateId, imo);
                if (checkShowRes.data === true) {
                    setValidateTemplate({ id: template.templateId, name: template.name });
                    setShowValidateModal(true);
                    return;
                }

                setValidateVisible(true);

                const sectionsRes = await surveyReportService.getAllSections(projectId, template.templateId, imo);
                const settingsData = sectionsRes.data as any[] ?? [];

                const settings = settingsData.map((s: any) => ({
                    ...s,
                    SectionId: s.sectionId ?? s.SectionId,
                    SectionName: s.sectionName ?? s.SectionName,
                    TankType: s.tankType ?? s.TankType ?? ""
                }));

                response = await surveyReportService.validateProjectGrading(
                    projectId,
                    template.templateId,
                    imo,
                    settings
                );

                const warnings = parseValidateGradingResponse(response.data);
                setValidateWarnings(warnings);
            } else {
                setValidateVisible(true);
                response = await surveyReportService.exportValidationPartial(
                    projectId,
                    template.templateId
                );

                const warnings = parseValidateGradingResponse(response.data);
                setValidateWarnings(warnings);
            }

        } catch (err: any) {
            popup.error(err?.response?.data || err?.message || "Validation failed. Please try again.", "Validate");
        } finally {
            setIsValidating(false);
        }
    };

    const runNewProjectValidateAll = async (settings: any[]) => {
        try {
            setIsValidatingAll(true);
            setValidateAllWarnings([]);
            setAllValidateVisible(true);

            const imo = data?.imonumber || data?.imoNumber;
            const validParts = (settings || []).filter(
                (p: any) => (p.partId ?? p.PartId) !== 0 && (p.includeInReport ?? p.IncludeInReport) !== false && (p.sections ?? []).length > 0
            );

            const payload: any = validParts.map((p: any) => ({
                PartId: p.partId ?? p.PartId,
                PartName: p.partName ?? p.PartName,
                RequiredInReport: p.requiredInReport ?? p.RequiredInReport ?? false,
                IncludeInReport: p.includeInReport ?? p.IncludeInReport ?? true,
                sections: (p.sections ?? []).map((s: any) => ({
                    SectionId: s.SectionId ?? s.sectionId,
                    SectionName: s.SectionName ?? s.sectionName,
                    IncludeInReport: s.IncludeInReport ?? s.includeInReport ?? true,
                    RequiredInReport: s.RequiredInReport ?? s.requiredInReport ?? false,
                    IsSubSection: false,
                    TankTypeId: s.TankTypeId ?? s.tankTypeId ?? 0,
                    ParentId: s.ParentId ?? s.parentId ?? EMPTY_GUID,
                    TankType: s.TankType ?? s.tankType ?? ""
                }))
            }));

            const { data: started } = await surveyReportService.startExportAllProjectValidation(projectId, imo, payload);
            const job = await surveyReportService.pollJob(surveyReportService.getValidationStatus, started.jobId, () => !isMountedRef.current);
            const warnings = parseExportAllValidationResponse(job.result ?? "");
            setValidateAllWarnings(warnings);
            if (warnings.length === 0) {
                setAllValidateVisible(false);
                popup.success("All parts have been successfully validated.", "Validate");
            }
        } catch (err: any) {
            if (err?.message !== "cancelled") {
                popup.error(toFriendlyErrorMessage(err, "Failed to validate. Please try again."), "Validate");
            }
        } finally {
            if (isMountedRef.current) {
                setIsValidatingAll(false);
            }
        }
    };

    const handleValidateAll = async () => {
        try {
            const imo = data?.imonumber || data?.imoNumber;

            if (!isNewProject) {
                setIsValidatingAll(true);
                setValidateAllWarnings([]);
                setAllValidateVisible(true);

                const { data: started } = await surveyReportService.startExportAllValidation(projectId);
                const job = await surveyReportService.pollJob(surveyReportService.getValidationStatus, started.jobId, () => !isMountedRef.current);
                const warnings = parseExportAllValidationResponse(job.result ?? "");
                setValidateAllWarnings(warnings);
                if (warnings.length === 0) {
                    setAllValidateVisible(false);
                    popup.success("All parts have been successfully validated.", "Validate");
                }
            } else {
                const show = await surveyReportService.checkShowExportAllSettings(projectId, imo);
                if (show.data === true) {
                    setExportAllMode('validate');
                    setShowExportAllModal(true);
                    return;
                }

                const res = await surveyReportService.getAllParts(projectId, imo);
                const parts = (res.data as any[]) || [];
                const settings = parts.map(p => ({
                    PartId: p.partId ?? p.PartId,
                    PartName: p.partName ?? p.PartName,
                    RequiredInReport: p.requiredInReport ?? p.RequiredInReport ?? false,
                    IncludeInReport: true,
                    sections: (p.sections ?? p.Sections ?? []).map((s: any) => ({
                        SectionId: s.sectionId ?? s.SectionId,
                        SectionName: s.sectionName ?? s.SectionName,
                        IncludeInReport: true,
                        RequiredInReport: s.requiredInReport ?? s.RequiredInReport ?? false,
                        IsSubSection: false,
                        TankTypeId: s.tankTypeId ?? s.TankTypeId ?? 0,
                        ParentId: s.parentId ?? s.ParentId ?? EMPTY_GUID,
                        TankType: s.tankType ?? s.TankType ?? ""
                    }))
                }));
                await runNewProjectValidateAll(settings);
            }
        } catch (err: any) {
            if (err?.message !== "cancelled") {
                popup.error(toFriendlyErrorMessage(err, "Failed to validate. Please try again."), "Validate");
            }
        } finally {
            if (isMountedRef.current) {
                setIsValidatingAll(false);
            }
        }
    };
    const handleNavigate = (part: string, section: string) => {
        if (activeView.part === part && activeView.section === section) {
            setShowMobileSidebar(false);
            return;
        }

        if (activeView.part !== part) {
            setReportData(null);
        }

        setActiveView({ part, section });
        setShowMobileSidebar(false);
    };

    const handleExport = async () => {
        try {
            if (activeView.part === "VMD") {
                setIsExporting(true);
                setModal({
                    message: "",
                    isLoading: true,
                    onSaveDraft: undefined,
                    onClose: () => setModal(null),
                });
                const response = await surveyReportService.exportCAPReportPdf(projectId);
                await processPdfResponse(response, getDynamicFileName("VMD"));
            } else if (activeView.part === "HI") {
                setIsExporting(true);
                setModal({
                    message: "",
                    isLoading: true,
                    onSaveDraft: undefined,
                    onClose: () => setModal(null),
                });
                const template = reportIndex.reportPartGrid.find(
                    (x: any) => (x.name === "Part H & I" || x.name === "Part H&I")
                );
                if (template) {
                    const imo = data?.imonumber || data?.imoNumber;
                    let valResponse: any;
                    if (isNewProject) {
                        await surveyReportService.checkShowExportSettings(projectId, template.templateId, imo);
                        const sectionsRes = await surveyReportService.getAllSections(projectId, template.templateId, imo);
                        const settingsData = sectionsRes.data as any[] ?? [];
                        const settings = settingsData.map((s: any) => ({
                            ...s,
                            SectionId: s.sectionId ?? s.SectionId,
                            SectionName: s.sectionName ?? s.SectionName,
                            TankType: s.tankType ?? s.TankType ?? ""
                        }));
                        valResponse = await surveyReportService.validateProjectGrading(projectId, template.templateId, imo, settings);
                    } else {
                        valResponse = await surveyReportService.exportValidationPartial(projectId, template.templateId);
                    }

                    const warnings = parseValidateGradingResponse(valResponse.data);
                    if (warnings.length > 0) {
                        setModal(null);
                        setIsExporting(false);
                        setValidateWarnings(warnings);
                        setValidateVisible(true);
                        return;
                    }

                    const response = await surveyReportService.exportHIPdf(projectId, template.templateId);
                    await processPdfResponse(response, getDynamicFileName("HI"));
                }
            } else {
                const template = reportIndex.reportPartGrid.find(
                    (x: any) => x.name === `Part ${activeView.part}`
                );
                if (template) {
                    setExportTemplate({ id: template.templateId, name: template.name });

                    if (isNewProject) {
                        const imo = data?.imonumber || data?.imoNumber;
                        const checkShowRes = await surveyReportService.checkShowExportSettings(projectId, template.templateId, imo);

                        if (checkShowRes.data === true) {
                            setShowExportModal(true);
                        } else {
                            const sectionsRes = await surveyReportService.getAllSections(projectId, template.templateId, imo);
                            let sections = sectionsRes.data as any[] ?? [];
                            if (sections.length === 0 && template.sectionsList && template.sectionsList.length > 0) {
                                sections = template.sectionsList.map((s: any) => ({
                                    ...s,
                                    SectionId: s.sectionId ?? s.SectionId,
                                    SectionName: s.sectionName ?? s.SectionName ?? s.name ?? s.Name,
                                    IncludeInReport: true,
                                    RequiredInReport: false
                                }));
                            }

                            const payload = [{
                                PartId: template.templateId,
                                PartName: template.name,
                                RequiredInReport: false,
                                IncludeInReport: true,
                                sections: sections
                            }];
                            executeExportPart(payload, template.templateId);
                        }
                    } else {
                        executeExportPart([], template.templateId);
                    }
                }
            }
        } catch (err) {
            setModal(null);
            setIsExporting(false);
            popup.error("Exported  falied!", "Export");
        }
    };

    const executeValidatePart = async (payload: any) => {
        try {
            setShowValidateModal(false);
            setIsValidating(true);
            setValidateWarnings([]);
            setValidateVisible(true);

            const imo = data?.imonumber || data?.imoNumber;
            const templateId = validateTemplate!.id;

            const selectedSettings = payload[0]?.sections ?? [];
            const settings = selectedSettings.map((s: any) => ({
                ...s,
                SectionId: s.SectionId ?? s.sectionId,
                SectionName: s.SectionName ?? s.sectionName,
                TankType: s.TankType ?? s.tankType ?? ""
            }));

            const response = await surveyReportService.validateProjectGrading(
                projectId,
                templateId,
                imo,
                settings
            );

            const warnings = parseValidateGradingResponse(response.data);
            setValidateWarnings(warnings);
        } catch (err: any) {
            popup.error(err?.response?.data || err?.message || "Validation failed. Please try again.", "Validate");
        } finally {
            setIsValidating(false);
        }
    };

    const getDynamicFileName = (partType: string) => {
        const d = new Date();
        const pad = (n: number) => n.toString().padStart(2, '0');
        const timestamp = `${pad(d.getMonth() + 1)}_${pad(d.getDate())}_${d.getFullYear()}_${pad(d.getHours())}_${pad(d.getMinutes())}_${pad(d.getSeconds())}`;

        const projectName = data?.projectName || (data as any)?.projectname || "";
        const imo = data?.imonumber || (data as any)?.imoNumber || "";

        if (partType === "All") {
            return `${projectName}_All_Template_${timestamp}.pdf`;
        }

        let partName = "";
        if (partType === "VMD") {
            partName = "Vessel Main Data";
        } else if (partType === "HI") {
            partName = "Part H & I";
        } else {
            partName = `Part ${partType}`;
        }

        return `${projectName}_${imo}_${partName}_${timestamp}.pdf`;
    };

    const executeExportPart = async (payload: any, passedTemplateId?: number) => {
        try {
            setShowExportModal(false);
            setIsExporting(true);
            setModal({
                message: "",
                isLoading: true,
                onSaveDraft: undefined,
                onClose: () => setModal(null),
            });

            const imo = data?.imonumber || data?.imoNumber;
            const templateId = passedTemplateId || exportTemplate!.id;
            let valResponse: any;

            if (isNewProject) {
                const selectedSettings = payload[0]?.sections ?? [];
                const settings = selectedSettings.map((s: any) => ({
                    ...s,
                    SectionId: s.SectionId ?? s.sectionId,
                    SectionName: s.SectionName ?? s.sectionName,
                    TankType: s.TankType ?? s.tankType ?? ""
                }));

                payload[0].sections = settings;

                valResponse = await surveyReportService.validateProjectGrading(
                    projectId,
                    templateId,
                    imo,
                    settings
                );
            } else {
                valResponse = await surveyReportService.exportValidationPartial(
                    projectId,
                    templateId
                );
            }

            const warnings = parseValidateGradingResponse(valResponse.data);
            if (warnings.length > 0) {
                setModal(null);
                setIsExporting(false);
                setValidateWarnings(warnings);
                setValidateVisible(true);
                return;
            }

            const response = await surveyReportService.exportPartPdf(projectId, templateId, payload);
            await processPdfResponse(response, getDynamicFileName(activeView.part));
        } catch (err) {
            setModal(null);
            setIsExporting(false);
            popup.error("Exported  falied!", "Export");
        }
    };

    const processPdfResponse = async (response: any, fileName: string) => {
        if (!response) {
            setModal(null);
            setIsExporting(false);
            setIsExportingAll(false);
            return;
        }

        const blob = new Blob([response.data], { type: "application/pdf" });
        const url = window.URL.createObjectURL(blob);

        setModal({
            message: `${fileName} is ready to download.`,
            isLoading: false,
            onSaveDraft: () => {
                const link = document.createElement("a");
                link.href = url;
                link.download = fileName;
                document.body.appendChild(link);
                link.click();
                link.remove();
                window.URL.revokeObjectURL(url);
                setModal(null);
                setIsExporting(false);
                setIsExportingAll(false);
                popup.success("Exported successfully!", "Export");
            },
            onClose: () => {
                window.URL.revokeObjectURL(url);
                setModal(null);
                setIsExporting(false);
                setIsExportingAll(false);
            }
        });
    };

    const handleResetTemplate = async () => {
        if (!reportIndex?.reportPartGrid) return;

        const partName = activeView.part === "VMD"
            ? "Vessel Main Data"
            : activeView.part === "HI"
                ? "Part H & I"
                : `Part ${activeView.part}`;

        const confirmed = await popup.confirm({
            title: "Reset Template",
            message: `Are you sure you want to reset the template ${partName}?`,
            confirmText: "Yes, Reset",
            cancelText: "Cancel",
            danger: true
        });
        if (!confirmed) return;

        try {
            let template: any = null;

            if (activeView.part === "VMD") {
                template = reportIndex.reportPartGrid.find(
                    (x: any) =>
                        x.name === "Vessel Main Data" ||
                        x.templateId === 0
                );
            } else if (activeView.part === "HI") {
                template = reportIndex.reportPartGrid.find(
                    (x: any) =>
                        (x.name === "Part H & I" || x.name === "Part H&I")
                );
            } else {
                template = reportIndex.reportPartGrid.find(
                    (x: any) =>
                        x.name === `Part ${activeView.part}`
                );
            }

            if (!template) return;

            await surveyReportService.deleteReport(
                projectId,
                template.templateId
            );

            const fetchFn = isNewProject
                ? surveyReportService.SurveyIndexPartial
                : surveyReportService.IndexPartial;

            const response = await fetchFn(
                projectId,
                template.templateId,
                data?.imonumber || data?.imoNumber
            );

            const result =
                response.data?.data || response.data;

            setReportData(result);
            popup.success("Template has been reset successfully.", "Reset Template");
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to reset template. Please try again.", title: "Reset Template" });
        }
    };

    const handlePortChange = (newPort: string) => {
        setReportData((prev: any) => {
            if (!prev) return prev;
            return {
                ...prev,
                reportPort: newPort,
                report: prev.report ? { ...prev.report, reportPort: newPort } : prev.report
            };
        });
    };

    const handleVesselNameChange = (newVesselName: string) => {
        setReportData((prev: any) => {
            if (!prev) return prev;
            return {
                ...prev,
                vessel: prev.vessel ? { ...prev.vessel, vesselName: newVesselName } : prev.vessel,
                report: prev.report ? {
                    ...prev.report,
                    vessel: prev.report.vessel ? { ...prev.report.vessel, vesselName: newVesselName } : prev.report.vessel
                } : prev.report
            };
        });
    };

    const handleReportNoChange = (newReportNo: string) => {
        setReportData((prev: any) => {
            if (!prev) return prev;
            return {
                ...prev,
                reportNo: newReportNo,
                vessel: prev.vessel ? { ...prev.vessel, reportNo: newReportNo } : prev.vessel,
                report: prev.report
                    ? {
                        ...prev.report,
                        reportNo: newReportNo,
                        vessel: prev.report.vessel ? { ...prev.report.vessel, reportNo: newReportNo } : prev.report.vessel
                    }
                    : prev.report
            };
        });
    };

    const handleReportDateChange = (newDate: any) => {
        setReportData((prev: any) => {
            if (!prev) return prev;
            const dateVal = newDate instanceof Date ? newDate.toISOString() : newDate;
            return {
                ...prev,
                reportDate: dateVal,
                vessel: prev.vessel
                    ? { ...prev.vessel, portDate: dateVal, lastVisitDate: dateVal }
                    : prev.vessel,
                report: prev.report
                    ? {
                        ...prev.report,
                        reportDate: dateVal,
                        vessel: prev.report.vessel
                            ? { ...prev.report.vessel, portDate: dateVal, lastVisitDate: dateVal }
                            : prev.report.vessel
                    }
                    : prev.report
            };
        });
    };

    const handleLastVisitDateChange = (newDate: any) => {
        setReportData((prev: any) => {
            if (!prev) return prev;
            const dateVal = newDate instanceof Date ? formatVisitDateForSave(newDate) : newDate;
            const updateVessel = (vessel: any) =>
                vessel ? { ...vessel, lastVisitDate: dateVal } : vessel;

            return {
                ...prev,
                vessel: updateVessel(prev.vessel),
                report: prev.report
                    ? { ...prev.report, vessel: updateVessel(prev.report.vessel) }
                    : prev.report
            };
        });
    };

    const handleFirstVisitDateChange = (newDate: any) => {
        setReportData((prev: any) => {
            if (!prev) return prev;
            const dateVal = newDate instanceof Date ? formatVisitDateForSave(newDate) : newDate;
            const updateVessel = (vessel: any) =>
                vessel ? { ...vessel, firstVisitDate: dateVal } : vessel;

            return {
                ...prev,
                vessel: updateVessel(prev.vessel),
                report: prev.report
                    ? { ...prev.report, vessel: updateVessel(prev.report.vessel) }
                    : prev.report
            };
        });
    };

    const handleVesselFieldChange = (field: string, value: any) => {
        setReportData((prev: any) => applyVesselFieldUpdate(prev, field, value));
    };

    const handleSurveyStatusFieldChange = (
        field: string,
        value: any,
        specialHullNo?: number | string
    ) => {
        setReportData((prev: any) =>
            applySurveyStatusFieldUpdate(prev, field, value, specialHullNo)
        );
    };

    const renderOtherContent = () => {

        const { part, section } = activeView;

        if (part === 'HI') return null;

        if (
            part === 'VMD' &&
            section === 'main-data'
        ) {

            return (
                <VesselMainData
                    data={data}
                    reportData={reportData}
                    isOfflineProject={isOfflineProject}
                    onPortChange={handlePortChange}
                    onReportNoChange={handleReportNoChange}
                    onReportDateChange={handleReportDateChange}
                    onLastVisitDateChange={handleLastVisitDateChange}
                    onFirstVisitDateChange={handleFirstVisitDateChange}
                    onVesselFieldChange={handleVesselFieldChange}
                    onVesselNameChange={handleVesselNameChange}
                />
            );
        }

        if (
            part === 'VMD' &&
            section === 'survey-status'
        ) {

            return (
                <SurveyStatus
                    data={data}
                    reportData={reportData}
                    isOfflineProject={isOfflineProject}
                    onPortChange={handlePortChange}
                    onReportNoChange={handleReportNoChange}
                    onReportDateChange={handleReportDateChange}
                    onLastVisitDateChange={handleLastVisitDateChange}
                    onFirstVisitDateChange={handleFirstVisitDateChange}
                    onVesselFieldChange={handleVesselFieldChange}
                    onSurveyStatusFieldChange={handleSurveyStatusFieldChange}
                    onVesselNameChange={handleVesselNameChange}
                />
            );
        }

        if (
            part === 'VMD' &&
            section === 'statutory'
        ) {

            return (
                <StatutoryCertificates
                    data={data}
                    reportData={reportData}
                    isOfflineProject={isOfflineProject}
                    onPortChange={handlePortChange}
                    onReportNoChange={handleReportNoChange}
                    onReportDateChange={handleReportDateChange}
                    onLastVisitDateChange={handleLastVisitDateChange}
                    onFirstVisitDateChange={handleFirstVisitDateChange}
                    onVesselFieldChange={handleVesselFieldChange}
                    onVesselNameChange={handleVesselNameChange}
                />
            );
        }

        return (
            <ReportContent
                key={`${part}-${section}`}
                activePart={part}
                activeSection={section}
                reportData={reportData}
                data={data}
                projectId={projectId}
                loading={loading}
                isPdfExport={false}
                onNavigate={handleNavigate}
                isNewProject={isNewProject}
                isOfflineProject={isOfflineProject}
                refreshCurrentReport={refreshCurrentReport}
            />
        );
    };

    const handleExportAll = async () => {
        try {
            const imo = data?.imonumber || data?.imoNumber;
            if (!isNewProject) {
                executeExportAll([]);
                return;
            }

            const show = await surveyReportService.checkShowExportAllSettings(projectId, imo);
            if (show.data === true) {
                setExportAllMode('export');
                setShowExportAllModal(true);
                return;
            }

            const res = await surveyReportService.getAllParts(projectId, imo);
            const parts = (res.data as any[]) || [];
            const settings = parts.map(p => ({
                PartId: p.partId ?? p.PartId,
                PartName: p.partName ?? p.PartName,
                RequiredInReport: p.requiredInReport ?? p.RequiredInReport ?? false,
                IncludeInReport: true,
                sections: (p.sections ?? p.Sections ?? []).map((s: any) => ({
                    SectionId: s.sectionId ?? s.SectionId,
                    SectionName: s.sectionName ?? s.SectionName,
                    IncludeInReport: true,
                    RequiredInReport: s.requiredInReport ?? s.RequiredInReport ?? false,
                    IsSubSection: false,
                    TankTypeId: s.tankTypeId ?? s.TankTypeId ?? 0,
                    ParentId: s.parentId ?? s.ParentId ?? EMPTY_GUID,
                    TankType: s.tankType ?? s.TankType ?? ""
                }))
            }));
            await executeExportAll(settings);
        } catch (err: any) {
            popup.error(toFriendlyErrorMessage(err, "Failed to start export. Please try again."), "Export");
        }
    };

    const executeExportAll = async (payload: any) => {
        try {
            setShowExportAllModal(false);
            setIsExportingAll(true);
            setModal({
                message: "",
                isLoading: true,
                onSaveDraft: undefined,
                onClose: () => setModal(null),
            });

            const imo = data?.imonumber || data?.imoNumber;
            const allWarnings: string[] = [];

            if (!isNewProject) {
                const { data: started } = await surveyReportService.startExportAllValidation(projectId);
                const job = await surveyReportService.pollJob(surveyReportService.getValidationStatus, started.jobId, () => !isMountedRef.current);
                const warnings = parseExportAllValidationResponse(job.result ?? "");
                allWarnings.push(...warnings);
            } else {
                try {
                    const settings = payload.map((p: any) => ({
                        ...p,
                        sections: (p.sections ?? []).map((s: any) => ({
                            ...s,
                            SectionId: s.SectionId ?? s.sectionId,
                            SectionName: s.SectionName ?? s.sectionName,
                            TankType: s.TankType ?? s.tankType ?? ""
                        }))
                    }));
                    payload = settings;

                    const { data: started } = await surveyReportService.startExportAllProjectValidation(projectId, imo, settings);
                    const job = await surveyReportService.pollJob(surveyReportService.getValidationStatus, started.jobId, () => !isMountedRef.current);
                    const warnings = parseExportAllValidationResponse(job.result ?? "");
                    allWarnings.push(...warnings);
                } catch (err: any) {
                    if (err?.message === "cancelled") throw err;
                }
            }

            if (allWarnings.length > 0) {
                setModal(null);
                setIsExportingAll(false);
                setValidateAllWarnings(allWarnings);
                setAllValidateVisible(true);
                return;
            }

            const { data: pdfStarted } = await surveyReportService.startExportFullPdf(projectId, payload);
            const pdfJob = await surveyReportService.pollJob(surveyReportService.getExportFullPdfStatus, pdfStarted.jobId, () => !isMountedRef.current);
            const bytes = Uint8Array.from(atob(pdfJob.base64 ?? ""), c => c.charCodeAt(0));
            await processPdfResponse({ data: new Blob([bytes], { type: "application/pdf" }) }, getDynamicFileName("All"));
        } catch (err: any) {
            setModal(null);
            setIsExportingAll(false);
            if (err?.message !== "cancelled") {
                popup.error(toFriendlyErrorMessage(err, "Export failed. Please try again."), "Export");
            }
        }
    };

    const refreshCurrentReport = useCallback(async () => {

        if (!projectId || !reportIndex) return;

        let template;

        if (activeView.part === "VMD") {
            template = reportIndex.reportPartGrid.find(
                (x: any) =>
                    x.name === "Vessel Main Data" ||
                    x.templateId === 0
            );
        } else {
            template = reportIndex.reportPartGrid.find(
                (x: any) => {
                    const name = x.name || x.partName;
                    if (activeView.part === "HI") {
                        return name === "Part H & I" || name === "Part H&I";
                    }
                    return name === `Part ${activeView.part}`;
                }
            );
        }

        if (!template) return;

        const fetchFn = isNewProject
            ? surveyReportService.SurveyIndexPartial
            : surveyReportService.IndexPartial;

        const res = await fetchFn(
            projectId,
            template.templateId,
            data?.imonumber || data?.imoNumber
        );

        const result = res.data?.data || res.data;

        setReportData(result);
    }, [activeView.part, projectId, reportIndex, isNewProject, data?.imonumber, data?.imoNumber]);

    const prevSectionRef = useRef(activeView.section);

    useEffect(() => {
        if (!reportData || !projectId || !reportIndex) {
            prevSectionRef.current = activeView.section;
            return;
        }

        if (prevSectionRef.current === activeView.section) {
            return;
        }

        prevSectionRef.current = activeView.section;
        refreshCurrentReport();
    }, [activeView.section, reportData, projectId, reportIndex, refreshCurrentReport]);

    const handleReleaseOffline = async () => {
        const confirmed = await popup.confirm({
            title: "Confirm",
            message: "Do you want to cancel the offline download and make the project available online? Any offline data will be lost.",
            confirmText: "Yes, Proceed",
            cancelText: "No",
            danger: true
        });
        if (!confirmed) return;

        try {
            await projectService.releaseOfflineDownload(projectId);
            await popup.success("Offline download cancelled", "Success");
            navigate(".", { replace: true, state: { ...location.state, projectStatus: "online" } });
            window.location.reload();
        } catch (error: any) {
            popup.error("Offline download cancelled falied", "Error");
        }
    };

    return (
        <>

            <div className="page-title" ref={pageTopRef}>

                {isOfflineProject && (
                    <div className="alert alert-danger mb-2" role="alert">
                        <ul className="mb-0 ps-3">
                            <li>
                                {data?.projectName} Report sections are Read-Only all parts have been downloaded for offline.
                            </li>
                            {data?.synchedOnline && (
                                <li>
                                    Contact the following CAP Surveyors if need any details: {data.synchedOnline.name} ({data.synchedOnline.emailId})
                                </li>
                            )}
                        </ul>
                    </div>
                )}
                {showDiscardOfflineWarning && (
                    <div className="alert alert-danger mb-2" role="alert" style={{ margin: "0 20px", fontSize: 16 }}>
                        <ul className="mb-0">
                            <li>
                                <p className="mb-0">
                                    The CAP Administrator has discarded your offline project. Any changes made in this
                                    project will not be synchronized with the online application.
                                </p>
                            </li>
                        </ul>
                    </div>
                )}

                <BreadcrumbBar
                    pageName="Report Overview"
                    parentPages={[
                        { name: "All Projects", onClick: () => navigate("/app/Project", { state: { showOverview: true } }), link: "" },
                        ...(data?.projectName ? [{ name: data.projectName, onClick: () => navigate("/app/Project", { state: { projectId: projectId } }), link: "" }] : [])
                    ]}
                >

                    <div className="d-flex flex-wrap gap-2 justify-content-md-end mt-2 mt-md-0">
                        <Dropdown onToggle={(isOpen) => { if (isOpen) hideWarningPanel(); }}>
                            <Dropdown.Toggle as={CustomDropdownToggle} id="dropdown-validate" disabled={isButtonloading || isOfflineProject || loading}>
                                Validate(s)
                            </Dropdown.Toggle>
                            <Dropdown.Menu>
                                <Dropdown.Item onClick={handleValidate} disabled={isButtonloading || isOfflineProject}>
                                    {isValidating ? "Validating..." : "Validate"}
                                </Dropdown.Item>
                                <Dropdown.Item onClick={handleValidateAll} disabled={isButtonloading || isOfflineProject}>
                                    {isValidatingAll ? "Validating All..." : "Validate All"}
                                </Dropdown.Item>
                            </Dropdown.Menu>
                        </Dropdown>

                        <Dropdown onToggle={(isOpen) => { if (isOpen) hideWarningPanel(); }}>
                            <Dropdown.Toggle as={CustomDropdownToggle} id="dropdown-export" disabled={isButtonloading || isOfflineProject || loading}>
                                Export(s)
                            </Dropdown.Toggle>
                            <Dropdown.Menu>
                                <Dropdown.Item onClick={handleExport} disabled={isButtonloading || isOfflineProject}>
                                    {isExporting ? "Exporting..." : "Export"}
                                </Dropdown.Item>
                                <Dropdown.Item onClick={handleExportAll} disabled={isButtonloading || isOfflineProject}>
                                    {isExportingAll ? "Exporting All..." : "Export All"}
                                </Dropdown.Item>
                            </Dropdown.Menu>
                        </Dropdown>

                        {isElectronUserAgent && data?.synchedOnline == null && (
                            <Button
                                variant={BUTTONS.SECONDARY}
                                onClick={handleDownloadOfflineClick}
                                disabled={showDownloadProcessing || isCheckingSpeed}
                                style={{ marginLeft: "8px" }}
                            >
                                Download Offline
                            </Button>
                        )}
                        {data?.synchedOnline != null && hasRole(ROLES.ADMIN_CAP_HQ) && (
                            <Button
                                variant={BUTTONS.SECONDARY}
                                onClick={handleReleaseOffline}
                                style={{ marginLeft: "8px" }}
                            >
                                Cancel Offline Download
                            </Button>
                        )}
                        {reportIndex?.resetTemplate === true && activeView.part !== "VMD" && (
                            <Button
                                variant={BUTTONS.SECONDARY}
                                onClick={handleResetTemplate}
                                disabled={isButtonloading || isOfflineProject || loading}
                            >
                                Reset Template
                            </Button>
                        )}
                    </div>
                </BreadcrumbBar>
            </div>

            {/* Reusable warning panel */}
            {(validateVisible || allValidateVisible) && (
                <div ref={tooltipRef} role="tooltip" className="warning-panel" style={{ top: warningPanelTop, maxHeight: `calc(100vh - ${warningPanelTop + 20}px)` }} onMouseEnter={() => setIsWarningHovered(true)} onMouseLeave={() => setIsWarningHovered(false)}>
                    <div className="warning-wrapper">
                        <h5 className="warning-title d-flex justify-content-between align-items-center">
                            {allValidateVisible ? "Validate All — Warnings" : "Validate — Warnings"}
                            <button
                                onClick={hideWarningPanel}
                                aria-label="Close"
                                className="warning-close-btn"
                            >
                                <Icon.XLg size={32} className="tx-32" />
                            </button>
                        </h5>

                        <div className="warning-content">
                            {(isValidating || isValidatingAll) ? (
                                <div className="d-flex align-items-center gap-2 p-3">
                                    <div className="spinner-border spinner-border-sm" />
                                    <span>Validating, please wait...</span>
                                </div>
                            ) : (() => {
                                const warnings = allValidateVisible
                                    ? validateAllWarnings
                                    : validateWarnings;

                                return warnings.length === 0 ? (
                                    <div className="cl-header">
                                        <div className="cl-header-text">
                                            <div className="cl-title">
                                                All validations passed
                                            </div>
                                            <div className="cl-subtitle">
                                                No missing fields found
                                            </div>
                                        </div>
                                    </div>
                                ) : (
                                    <>
                                        <div className="cl-header">
                                            <div className="cl-icon">
                                                <Icon.JournalCheck />
                                            </div>
                                            <div className="cl-header-text">
                                                <div className="cl-title">
                                                    {warnings.length} issue{warnings.length > 1 ? "s" : ""} found
                                                </div>
                                                <div className="cl-subtitle">
                                                    Required before exporting
                                                </div>
                                            </div>
                                            <span className="cl-badge">
                                                {warnings.length} pending
                                            </span>
                                        </div>
                                        <div className="cl-body">
                                            {warnings.map((w, i) => {
                                                const [label, status] = w.split(" — ");
                                                return (
                                                    <div className="cl-row" key={i}>
                                                        <span className="cl-row-label warn">{label}</span>
                                                        <span className="cl-row-status warn">{status}</span>
                                                    </div>
                                                );
                                            })}
                                        </div>
                                    </>
                                );
                            })()}
                        </div>
                    </div>
                </div>
            )}

            <div className="page-content p-0 position-relative">

                <Offcanvas show={showMobileSidebar} onHide={() => setShowMobileSidebar(false)} placement="end" className="d-md-none p-0">
                    <Offcanvas.Header closeButton className="border-bottom pb-2 pt-3 px-3 m-0">
                        <Offcanvas.Title>Sections</Offcanvas.Title>
                    </Offcanvas.Header>
                    <Offcanvas.Body className="p-0 d-flex flex-column overflow-hidden m-0">
                        <ReportSidebar
                            activePart={activeView.part}
                            activeSection={activeView.section}
                            onNavigate={handleNavigate}
                            data={data}
                            isOfflineProject={isOfflineProject}
                            isMobileView={true}
                        />
                    </Offcanvas.Body>
                </Offcanvas>

                <div className="report-content-wrapper position-relative">
                    <div className="d-md-none position-absolute" style={{ top: '16px', right: '16px', zIndex: 10 }}>
                        <Icon.Funnel 
                            size={24} 
                            onClick={() => setShowMobileSidebar(true)} 
                            style={{ cursor: 'pointer' }} 
                        />
                    </div>

                    <div className="report-wrapper">

                        <ReportSidebar
                            activePart={activeView.part}
                            activeSection={activeView.section}
                            onNavigate={handleNavigate}
                            data={data}
                            isOfflineProject={isOfflineProject}
                        />

                        {/* HI PART */}
                        {activeView.part === "HI" && (
                            <PartSummary
                                key={`${activeView.part}-${activeView.section}`}
                                activePart={activeView.part}
                                activeSection={activeView.section}
                                data={data}
                                reportData={hiReportData}
                                isOfflineProject={isOfflineProject}
                            />
                        )}

                        {activeView.part !== "HI" &&
                            renderOtherContent()}

                    </div>

                </div>

            </div>

            {/* PDF EXPORT */}
            {showDownloadConfirm && (
                <Modal
                    show={showDownloadConfirm}
                    onHide={() => setShowDownloadConfirm(false)}
                    centered
                    aria-labelledby="downloadconfirmlabel"
                >
                    <Modal.Header closeButton>
                        <Modal.Title id="downloadconfirmlabel">Confirm</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <div className="row">
                            <div className="col-md-12 modal-alert">
                                <div className="alert-content">
                                    <h5>Are you sure?</h5>
                                    <p className="mb-0">
                                        Download offline mode which include all parts, <br />
                                        Do you want to continue this?
                                    </p>
                                </div>
                            </div>
                        </div>
                    </Modal.Body>
                    <Modal.Footer className="gap-2">
                        <Button
                            variant={BUTTONS.SECONDARY}
                            onClick={() => setShowDownloadConfirm(false)}
                        >
                            Cancel
                        </Button>
                        <Button
                            variant={BUTTONS.SECONDARY}
                            onClick={handleDownloadOfflineProceed}
                        >
                            Yes, Proceed
                        </Button>
                    </Modal.Footer>
                </Modal>
            )}

            {showDownloadProcessing && (
                <Modal
                    show={showDownloadProcessing}
                    onHide={handleStopDownload}
                    centered
                    backdrop="static"
                    keyboard={false}
                    aria-labelledby="downloadprocessinglabel"
                >
                    <Modal.Header closeButton>
                        <Modal.Title id="downloadprocessinglabel">Download Offline</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <div className="row">
                            <div className="col-md-12 modal-alert">
                                <div className="alert-content">
                                    <h5>Downloading the {data?.projectName || 'project'} project in Offline Mode</h5>
                                    <div className="downloadprogressbar mt-3">
                                        <TransferProgressBar percent={downloadPercent} height="20px" variant="bg-primary" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </Modal.Body>
                    <Modal.Footer className="gap-2">
                        <Button
                            variant={BUTTONS.SECONDARY}
                            onClick={handleStopDownload}
                        >
                            No, Cancel
                        </Button>
                        <Button
                            variant={BUTTONS.SECONDARY}
                            onClick={() => {
                                setShowDownloadProcessing(false);
                                setIsDownloadInBackground(true);
                            }}
                        >
                            Download in Background
                        </Button>
                    </Modal.Footer>
                </Modal>
            )}

            {isDownloadInBackground && (
                <div className="minimize-section" style={{ position: 'fixed', bottom: '20px', right: '20px', zIndex: 1050 }}>
                    <div className="minimize-card card shadow" style={{ minWidth: '320px', backgroundColor: '#fff', border: '1px solid #dee2e6', borderRadius: '8px' }}>
                        <div className="card-body p-3">
                            <div className="d-flex align-items-center gap-3">
                                <div className="img-wrapper">
                                    <Icon.Download className="download-icon text-primary" size={20} />
                                </div>
                                <div className="alert-content flex-grow-1">
                                    <h6 className="mb-1" style={{ fontSize: '14px', fontWeight: 600 }}>Download Offline is Inprogress</h6>
                                    <TransferProgressBar percent={downloadPercent} height="6px" variant="bg-primary" />
                                </div>
                            </div>
                            <div className="actions d-flex justify-content-end gap-2 mt-2" style={{ gap: '8px' }}>
                                <button type="button" className="btn btn-sm btn-light" onClick={handleStopDownload}> No, Cancel </button>
                                <button type="button" className="btn btn-sm btn-light" onClick={() => { setIsDownloadInBackground(false); setShowDownloadProcessing(true); }}>
                                    <Icon.ArrowsAngleExpand />
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {downloadSuccessModal && (
                <Modal
                    show={downloadSuccessModal}
                    onHide={() => {
                        setDownloadSuccessModal(false);
                        getFreshToken().then((tok) => {
                            window.location.href = `${DESKTOP_URL}/SetToken#token=${encodeURIComponent(tok)}&redirectURL=${encodeURIComponent("/app/Project")}`;
                        });
                    }}
                    centered
                >
                    <Modal.Header closeButton>
                        <Modal.Title>Success</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <p className="mb-0">
                            Download completed successfully
                        </p>
                    </Modal.Body>
                    <Modal.Footer>
                        <Button
                            variant={BUTTONS.PRIMARY}
                            onClick={() => {
                                setDownloadSuccessModal(false);
                                getFreshToken().then((tok) => {
                                    window.location.href = `${DESKTOP_URL}/SetToken#token=${encodeURIComponent(tok)}&redirectURL=${encodeURIComponent("/app/Project")}`;
                                });
                            }}
                        >
                            Ok
                        </Button>
                    </Modal.Footer>
                </Modal>
            )}

            {downloadError && (
                <Modal show={Boolean(downloadError)} onHide={() => setDownloadError(null)} centered>
                    <Modal.Header closeButton>
                        <Modal.Title>Error</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <p>{downloadError}</p>
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant={BUTTONS.SECONDARY} onClick={() => setDownloadError(null)}>
                            Close
                        </Button>
                    </Modal.Footer>
                </Modal>
            )}

            <NotificationContainer position="bottom-right" />

            {showExportModal && exportTemplate && (
                <ExportConfigurationModal
                    show={showExportModal}
                    onHide={() => setShowExportModal(false)}
                    onExport={executeExportPart}
                    projectId={projectId}
                    templateId={exportTemplate.id}
                    imo={data?.imonumber || (data as any)?.imoNumber || 0}
                    templateName={exportTemplate.name}
                />
            )}

            {showValidateModal && validateTemplate && (
                <ExportConfigurationModal
                    show={showValidateModal}
                    onHide={() => setShowValidateModal(false)}
                    onExport={executeValidatePart}
                    projectId={projectId}
                    templateId={validateTemplate.id}
                    imo={data?.imonumber || (data as any)?.imoNumber || 0}
                    templateName={validateTemplate.name}
                    mode="validate"
                />
            )}

            {showExportAllModal && (
                <ExportAllConfigurationModal
                    show={showExportAllModal}
                    onHide={() => setShowExportAllModal(false)}
                    mode={exportAllMode}
                    onExport={async (selectedSettings) => {
                        setShowExportAllModal(false);
                        if (exportAllMode === 'validate') {
                            await runNewProjectValidateAll(selectedSettings);
                        } else {
                            await executeExportAll(selectedSettings);
                        }
                    }}
                    projectId={projectId}
                    imo={data?.imonumber || (data as any)?.imoNumber || 0}
                />
            )}

            {/* Speed Check Modal removed (no popup during speed check) */}

            {modal && (
                <UnsavedConfirmationModal
                    message={modal.message}
                    isLoading={modal.isLoading}
                    onSaveDraft={modal.onSaveDraft}
                    onClose={modal.onClose}
                />
            )}
        </>
    );
};

export default ReportOverview;
