import React, { useEffect, useState } from "react";
import { BreadcrumbBar, Button, Divider, BUTTONS, CheckboxInput, Label, AbsSelect } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import * as exportService from '../../../../service/ExportService';
import * as gradingService from '../../../../service/GradingService';
import { usePopup } from '../../../../Common/PopupProvider';
import { resultMessage } from '../../../../Common/UtilityMessage';
import { usePermissions } from '../../../../context/PermissionContext';
import { toFriendlyErrorMessage } from '../../../../utils/errorMessage';
import AppLoader from '../../../../Common/AppLoader';

export interface VesselType {
    id: string;
    vesselType: string;
    vesselTypeId?: number;
}

export interface ExportSection {
    sectionId: string;
    sectionName: string;
    requiredInReport: boolean;
    includeInReport: boolean;
    tankTypeId: number;
    isSubSection?: boolean;
    parentId?: string;
    tankType?: string;
}

export interface ExportPart {
    partId: number;
    partName: string;
    requiredInReport: boolean;
    includeInReport: boolean;
    hasSections: boolean;
    sections: ExportSection[];
    tankTypes: TankTypeSection[];
}

export interface TankTypeSection {
    tankTypeId: number;
    tankTypeName: string;
    requiredInReport: boolean;
    includeInReport: boolean;
    tankSections: ExportSection[];
}

const ManageExport: React.FC = () => {
    const popup = usePopup();
    const { pagePermissions } = usePermissions();
    const canEdit = !!((pagePermissions as any)?.manageExport?.edit ?? (pagePermissions as any)?.ManageExport?.edit);
    const [vesselTypes, setVesselTypes] = useState<VesselType[]>([]);
    const [selectedVesselType, setSelectedVesselType] = useState<string>("");
    const [exportParts, setExportParts] = useState<ExportPart[]>([]);
    const [loading, setLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [validationError, setValidationError] = useState<string>("");

    useEffect(() => {
        loadVesselTypes();
    }, []);

    const loadVesselTypes = async () => {
        try {
            const res = await gradingService.getVesselType();
            const data = res.data?.data || res.data || [];
            setVesselTypes(data.map((v: any) => ({
                id: String(v.id || v.vesselTypeId || ""),
                vesselType: v.vesselType || v.name || "",
                vesselTypeId: v.vesselTypeId || v.id
            })));
        } catch (err) {
        }
    };

    const onVesselChange = async (selected: string[] | any) => {
        const vesselId = (selected && selected[0]) || "";
        setSelectedVesselType(vesselId);
        setValidationError("");

        if (!vesselId) {
            setExportParts([]);
            return;
        }

        setLoading(true);
        try {
            const res = await exportService.getReportExportPartByVesselType(vesselId);
            let data = res.data;
            if (res.data?.data) {
                data = res.data.data;
            } else if (res.data?.exportParts) {
                data = res.data.exportParts;
            } else if (Array.isArray(res.data)) {
                data = res.data;
            }

            const parsedParts = parseExportPartsData(data);
            setExportParts(parsedParts);
        } catch (err) {
            setValidationError("Error loading export parts. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    const parseExportPartsData = (data: any): ExportPart[] => {
        if (!data) return [];

        if (Array.isArray(data) && data.length > 0) {
            return data.map((part: any) => {
                const sections: ExportSection[] = (part.sections || [])
                    .filter((sec: any) => (sec.tankTypeId || sec.TankTypeId || 0) === 0)
                    .map((sec: any) => ({
                        sectionId: String(sec.sectionId || sec.SectionId || ""),
                        sectionName: sec.sectionName || sec.SectionName || "",
                        requiredInReport: sec.requiredInReport || sec.RequiredInReport || false,
                        includeInReport: sec.includeInReport || sec.IncludeInReport || true,
                        tankTypeId: 0,
                        tankType: "" 
                    }));

                const tankTypeMap = new Map<number, TankTypeSection>();

                (part.sections || []).forEach((sec: any) => {
                    const tankTypeId = sec.tankTypeId || sec.TankTypeId || 0;
                    if (tankTypeId > 0) {
                        if (!tankTypeMap.has(tankTypeId)) {
                            tankTypeMap.set(tankTypeId, {
                                tankTypeId: tankTypeId,
                                tankTypeName: sec.tankType || sec.TankType || `Tank Type ${tankTypeId}`,
                                requiredInReport: sec.requiredInReport || sec.RequiredInReport || false,
                                includeInReport: sec.includeInReport || sec.IncludeInReport || true,
                                tankSections: []
                            });
                        }
                        const tankType = tankTypeMap.get(tankTypeId)!;
                        tankType.tankSections.push({
                            sectionId: String(sec.sectionId || sec.SectionId || ""),
                            sectionName: sec.sectionName || sec.SectionName || "",
                            requiredInReport: sec.requiredInReport || sec.RequiredInReport || false,
                            includeInReport: sec.includeInReport || sec.IncludeInReport || true,
                            tankTypeId: tankTypeId,
                            tankType: sec.tankType || sec.TankType || tankType.tankTypeName || ""
                        });
                    }
                });

                return {
                    partId: part.partId || part.PartId || 0,
                    partName: part.partName || part.PartName || "",
                    requiredInReport: part.requiredInReport || part.RequiredInReport || false,
                    includeInReport: part.includeInReport || part.IncludeInReport || true,
                    hasSections: (sections.length > 0 || tankTypeMap.size > 0) ? true : (part.hasSections !== undefined ? part.hasSections : false),
                    sections: sections,
                    tankTypes: Array.from(tankTypeMap.values())
                };
            });
        }

        if (data.exportParts && Array.isArray(data.exportParts)) {
            return parseExportPartsData(data.exportParts);
        }

        return [];
    };

    const togglePart = (partId: number) => {
        setExportParts(prevParts => {
            return prevParts.map(part => {
                if (part.partId !== partId) return part;

                const newRequiredInReport = !part.requiredInReport;

                if (newRequiredInReport && !part.hasSections) {
                    setValidationError("This part has no sections and can't be required.");
                    return part;
                }

                if (newRequiredInReport) {
                    return {
                        ...part,
                        requiredInReport: true,
                        includeInReport: true
                    };
                } else {
                    return {
                        ...part,
                        requiredInReport: false,
                        includeInReport: false,
                        sections: part.sections.map(sec => ({
                            ...sec,
                            requiredInReport: false,
                            includeInReport: false
                        })),
                        tankTypes: part.tankTypes.map(tt => ({
                            ...tt,
                            requiredInReport: false,
                            includeInReport: false,
                            tankSections: tt.tankSections.map(ts => ({
                                ...ts,
                                requiredInReport: false,
                                includeInReport: false
                            }))
                        }))
                    };
                }
            });
        });
    };

    const toggleSection = (partId: number, sectionId: string) => {
        setExportParts(prevParts => {
            return prevParts.map(part => {
                if (part.partId !== partId) return part;

                const updatedSections = part.sections.map(sec => {
                    if (sec.sectionId === sectionId) {
                        return {
                            ...sec,
                            requiredInReport: !sec.requiredInReport,
                            includeInReport: !sec.requiredInReport
                        };
                    }
                    return sec;
                });

                return {
                    ...part,
                    sections: updatedSections
                };
            });
        });
    };

    const toggleTankType = (partId: number, tankTypeId: number) => {
        setExportParts(prevParts => {
            return prevParts.map(part => {
                if (part.partId !== partId) return part;

                const updatedTankTypes = part.tankTypes.map(tt => {
                    if (tt.tankTypeId === tankTypeId) {
                        const newRequired = !tt.requiredInReport;
                        return {
                            ...tt,
                            requiredInReport: newRequired,
                            includeInReport: newRequired,
                            tankSections: tt.tankSections.map(ts => ({
                                ...ts,
                                requiredInReport: newRequired,
                                includeInReport: newRequired
                            }))
                        };
                    }
                    return tt;
                });

                return {
                    ...part,
                    tankTypes: updatedTankTypes
                };
            });
        });
    };

    const validateParts = (): boolean => {
        setValidationError("");

        for (const part of exportParts) {
            if (part.requiredInReport) {
                if (part.hasSections) {
                    const hasSectionChecked = part.sections.some(sec => sec.requiredInReport) ||
                        part.tankTypes.some(tt => tt.requiredInReport);

                    if (!hasSectionChecked) {
                        setValidationError(`"${part.partName}" cannot be marked as required. At least one section is required.`);
                        return false;
                    }
                } else {
                    setValidationError(`"${part.partName}" has no sections and can't be required.`);
                    return false;
                }
            }
        }

        return true;
    };

    const handleClear = () => {
        setSelectedVesselType("");
        setExportParts([]);
        setValidationError("");
    };

    const handleSave = async () => {
        if (!validateParts()) {
            return;
        }

        setIsSaving(true);
        setValidationError("");

        try {
            const exportPartsPayload = exportParts.map(part => {
                const sections: any[] = [];
                part.sections.forEach(sec => {
                    sections.push({
                        SectionId: sec.sectionId,
                        SectionName: sec.sectionName,
                        RequiredInReport: sec.requiredInReport,
                        IncludeInReport: sec.includeInReport,
                        TankTypeId: sec.tankTypeId,
                        TankType: sec.tankType || ""
                    });
                });

                part.tankTypes.forEach(tt => {
                    tt.tankSections.forEach(ts => {
                        sections.push({
                            SectionId: ts.sectionId,
                            SectionName: ts.sectionName,
                            RequiredInReport: ts.requiredInReport,
                            IncludeInReport: ts.includeInReport,
                            TankTypeId: ts.tankTypeId,
                            TankType: ts.tankType || tt.tankTypeName || ""
                        });
                    });
                });

                return {
                    PartId: part.partId,
                    PartName: part.partName,
                    RequiredInReport: part.requiredInReport,
                    IncludeInReport: part.includeInReport,
                    sections: sections
                };
            });

            const payload = {
                SelectedImoNumber: 0,
                SelectedVesselTypeId: parseInt(selectedVesselType) || 0,
                exportParts: exportPartsPayload
            };

            const res = await exportService.saveRequiredInExport(payload);

            const isSuccess = res.data === true || (res.data?.isSuccess !== false && res.data?.data !== false);

            if (isSuccess && res.data !== false) {
                setSelectedVesselType("");
                setExportParts([]);
                setValidationError("");
                popup.success({
                    title: "Save Export Settings",
                    message: resultMessage("Settings", "Saved")
                });
            } else {
                setValidationError("Failed to save export settings. Please try again.");
                popup.error({ message: "Failed to save export settings. Please try again." });
            }
        } catch (err: any) {
            const errorMsg = toFriendlyErrorMessage(err, "Failed to save export settings. Please try again.");
            setValidationError(errorMsg);
            popup.error({ message: errorMsg });
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <>
            <div className="page-title">
                <BreadcrumbBar
                    pageName="Manage Custom Export Settings"
                    parentPages={[{ name: "Project Configuration", link: "" }]}
                />
            </div>

            <div className="page-content p-4">
                <div className="content-body">
                    {/* Vessel Type */}
                    <div className="row">
                        <div className="col-md-6 form-group">
                            <Label name="Vessel Type" bold />
                            <AbsSelect
                                key={selectedVesselType || 'empty'}
                                mult={false}
                                onChange={onVesselChange}
                                options={vesselTypes.map((v) => ({
                                    text: v.vesselType,
                                    value: v.id,
                                }))}
                                selected={selectedVesselType && selectedVesselType.trim() !== "" ? [selectedVesselType] : []}
                                placeholder="Select a Vessel Type"
                            />
                        </div>
                    </div>

                    {/* Validation Error */}
                    {validationError && (
                        <div className="alert alert-warning d-flex align-items-center mt-3" role="alert">
                            <Icon.ExclamationTriangleFill className="me-2" size={20} />
                            <div>
                                <strong>Warning:</strong> {validationError}
                            </div>
                        </div>
                    )}

                    {/* Loading */}
                    {loading && (
                        <AppLoader inline />
                    )}

                    {/* Parts and Sections */}
                    {!loading && exportParts.length > 0 && (
                        <div className="parts-section mt-4">
                            <Label name="Select Mandate Parts for export" bold />
                            <div className="checkbox-grid mt-3" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '15px' }}>
                                {exportParts.map((part) => (
                                    <div key={part.partId} className="checkbox-item">
                                        <CheckboxInput
                                            label={part.partName}
                                            checked={part.requiredInReport}
                                            disabled={!canEdit}
                                            onChange={() => togglePart(part.partId)}
                                        />
                                    </div>
                                ))}
                            </div>

                            {/* Sections Display */}
                            <div className="sections-section mt-4">
                                {exportParts.map((part) => {
                                    if (!part.requiredInReport) return null;

                                    return (

                                        <div key={part.partId} className="card mb-3" data-partid={part.partId}>
                                            <div className="card-header">
                                                <h6 className="_600 mb-0">{part.partName} Sections</h6>
                                            </div>
                                            <div className="card-body">
                                                <div className="checkbox-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '15px' }}>
                                                    {/* Regular Sections */}
                                                    {part.sections.map((section) => (
                                                        <div key={section.sectionId} className="checkbox-item">
                                                            <CheckboxInput
                                                                label={section.sectionName}
                                                                checked={section.requiredInReport}
                                                                disabled={!canEdit}
                                                                onChange={() => toggleSection(part.partId, section.sectionId)}
                                                            />
                                                        </div>
                                                    ))}

                                                    {/* Tank Type Sections */}
                                                    {part.tankTypes.map((tankType) => (
                                                        <div key={`${part.partId}-${tankType.tankTypeId}`} className="checkbox-item">
                                                            <CheckboxInput
                                                                label={tankType.tankTypeName}
                                                                checked={tankType.requiredInReport}
                                                                disabled={!canEdit}
                                                                onChange={() => toggleTankType(part.partId, tankType.tankTypeId)}
                                                            />
                                                        </div>
                                                    ))}
                                                </div>
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    )}

                    {!loading && selectedVesselType && exportParts.length === 0 && (
                        <div className="mt-4">
                            <Label name="Choose Vessel Type" />
                        </div>
                    )}

                    {/* Footer */}
                    <div className="row mt-4">
                        <div className="col-md-12 form-group">
                            <Divider className="divider-1" />
                        </div>
                        <div className="col-md-12 form-group">
                            <div className="d-flex gap-2 justify-content-end">
                                <Button
                                    variant={BUTTONS.PRIMARY}
                                    onClick={handleSave}
                                    disabled={!canEdit || isSaving || !selectedVesselType || exportParts.length === 0}
                                >
                                    {isSaving ? "Saving..." : "Save"}
                                </Button>
                                <Button
                                    variant={BUTTONS.SECONDARY}
                                    onClick={handleClear}
                                >
                                    Cancel
                                </Button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

        </>
    );
};

export default ManageExport;
