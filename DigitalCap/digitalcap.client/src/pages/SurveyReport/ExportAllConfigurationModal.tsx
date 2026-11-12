import React, { useState, useEffect } from 'react';
import { Modal, Form, Accordion } from 'react-bootstrap';
import { Button, BUTTONS } from 'customer_portal-ui-shared';
import AppLoader from '../../Common/AppLoader';
import { getAllParts } from '../../service/SurveyReportService';
import {
    type ConfigSection,
    toConfigSection,
    getDisplayGroups,
    collectSelectedSections,
    isSelected,
    SELECT_AT_LEAST_ONE
} from '../../utils/exportSettings';

interface ConfigPart {
    PartId: number;
    PartName: string;
    RequiredInReport: boolean;
    sections: ConfigSection[];
}

interface ExportAllConfigurationModalProps {
    show: boolean;
    onHide: () => void;
    onExport: (selectedSettings: any) => void;
    projectId: number;
    imo: number;
    mode?: 'export' | 'validate';
}

export const ExportAllConfigurationModal: React.FC<ExportAllConfigurationModalProps> = ({
    show,
    onHide,
    onExport,
    projectId,
    imo,
    mode = 'export'
}) => {
    const [parts, setParts] = useState<ConfigPart[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [isExporting, setIsExporting] = useState(false);
    const [loadError, setLoadError] = useState<string | null>(null);
    const [validationError, setValidationError] = useState<string | null>(null);

    useEffect(() => {
        if (show) {
            setIsExporting(false);
            setValidationError(null);
            setLoadError(null);
            loadParts();
        } else {
            setParts([]);
            setValidationError(null);
            setLoadError(null);
        }
    }, [show, projectId, imo]);

    const loadParts = async () => {
        setIsLoading(true);
        setLoadError(null);
        setValidationError(null);
        try {
            const res = await getAllParts(projectId, imo);
            if (res.data && Array.isArray(res.data)) {
                const partsData: ConfigPart[] = res.data.map((p: any) => ({
                    PartId: Number(p.partId ?? p.PartId ?? 0),
                    PartName: p.partName ?? p.PartName ?? "",
                    RequiredInReport: !!(p.requiredInReport ?? p.RequiredInReport),
                    sections: (p.sections ?? p.Sections ?? []).map(toConfigSection)
                }));
                setParts(partsData);
            } else {
                setParts([]);
            }
        } catch (error) {
            setLoadError("Unable to load report sections. Please try again.");
            setParts([]);
        } finally {
            setIsLoading(false);
        }
    };

    const handlePartCheckboxChange = (partId: number, checked: boolean) => {
        setValidationError(null);
        setParts(prev => prev.map(p => {
            if (p.PartId !== partId) return p;
            return {
                ...p,
                sections: p.sections.map(s => {
                    if (checked) {
                        return { ...s, IncludeInReport: true };
                    } else {
                        return s.RequiredInReport ? s : { ...s, IncludeInReport: false };
                    }
                })
            };
        }));
    };

    const handleSectionCheckboxChange = (partId: number, sectionId: string, checked: boolean) => {
        setValidationError(null);
        setParts(prev => prev.map(p => {
            if (p.PartId !== partId) return p;
            return {
                ...p,
                sections: p.sections.map(s => s.SectionId === sectionId ? { ...s, IncludeInReport: checked } : s)
            };
        }));
    };

    const handleTankTypeCheckboxChange = (partId: number, tankTypeId: number, checked: boolean) => {
        setValidationError(null);
        setParts(prev => prev.map(p => {
            if (p.PartId !== partId) return p;
            return {
                ...p,
                sections: p.sections.map(s => s.TankTypeId === tankTypeId ? { ...s, IncludeInReport: checked } : s)
            };
        }));
    };

    const handleExportClick = async () => {
        setValidationError(null);
        const payload = parts.map(p => {
            const s = collectSelectedSections(p.sections);
            return {
                PartId: p.PartId,
                PartName: p.PartName,
                RequiredInReport: p.RequiredInReport,
                IncludeInReport: s.length > 0,
                sections: s
            };
        });

        const totalSelected = payload.reduce((acc, p) => acc + p.sections.length, 0);
        if (totalSelected === 0) {
            setValidationError(SELECT_AT_LEAST_ONE);
            return;
        }

        setIsExporting(true);
        try {
            await onExport(payload);
        } finally {
            setIsExporting(false);
        }
    };

    return (
        <Modal show={show} onHide={onHide} size="lg" centered backdrop="static">
            <Modal.Header closeButton={!isExporting}>
                <Modal.Title>{mode === 'validate' ? 'Validate all parts sections' : 'Export All Configurations'}</Modal.Title>
            </Modal.Header>
            <Modal.Body style={{ maxHeight: '60vh', overflowY: 'auto' }}>
                {isLoading ? (
                    <AppLoader inline />
                ) : loadError ? (
                    <p className="text-danger p-2">{loadError}</p>
                ) : (
                    <Accordion className="p-2">
                        {parts.length === 0 ? (
                            <p>No report sections found.</p>
                        ) : (
                            parts.map((part, index) => {
                                const { normal, tankTypes } = getDisplayGroups(part.sections);
                                const hasItems = normal.length > 0 || tankTypes.length > 0;
                                const isPartChecked = part.sections.some(s => isSelected(s));
                                const isPartDisabled = part.RequiredInReport || part.sections.some(s => s.RequiredInReport);

                                return (
                                    <Accordion.Item eventKey={String(index)} key={part.PartId} className="mb-2">
                                        <Accordion.Header>
                                            <div className="d-flex align-items-center" onClick={(e) => e.stopPropagation()}>
                                                <Form.Check
                                                    type="checkbox"
                                                    id={`part-${part.PartId}`}
                                                    checked={isPartChecked}
                                                    disabled={isPartDisabled}
                                                    onChange={(e) => handlePartCheckboxChange(part.PartId, e.target.checked)}
                                                    className="me-3"
                                                />
                                                <span>{part.PartName}</span>
                                            </div>
                                        </Accordion.Header>
                                        <Accordion.Body>
                                            {part.sections.length === 0 || !hasItems ? (
                                                <p className="text-muted">No sections in this part.</p>
                                            ) : (
                                                <>
                                                    {normal.map(section => (
                                                        <Form.Check
                                                            key={section.SectionId}
                                                            type="checkbox"
                                                            id={`part-${part.PartId}-section-${section.SectionId}`}
                                                            label={section.SectionName}
                                                            checked={section.RequiredInReport || section.IncludeInReport}
                                                            disabled={section.RequiredInReport}
                                                            onChange={(e) => handleSectionCheckboxChange(part.PartId, section.SectionId, e.target.checked)}
                                                            className="mb-3"
                                                        />
                                                    ))}
                                                    {tankTypes.map(t => (
                                                        <Form.Check
                                                            key={`part-${part.PartId}-tanktype-${t.tankTypeId}`}
                                                            type="checkbox"
                                                            id={`part-${part.PartId}-tanktype-${t.tankTypeId}`}
                                                            label={t.tankType}
                                                            checked={t.required || t.included}
                                                            disabled={t.required}
                                                            onChange={(e) => handleTankTypeCheckboxChange(part.PartId, t.tankTypeId, e.target.checked)}
                                                            className="mb-3"
                                                        />
                                                    ))}
                                                </>
                                            )}
                                        </Accordion.Body>
                                    </Accordion.Item>
                                );
                            })
                        )}
                    </Accordion>
                )}
            </Modal.Body>
            {validationError && (
                <div className="text-danger px-3 pb-2">{validationError}</div>
            )}
            <Modal.Footer className="gap-2">
                <Button variant={BUTTONS.TERTIARY} onClick={onHide} disabled={isExporting}>Close</Button>
                <Button variant={BUTTONS.PRIMARY} onClick={handleExportClick} disabled={isLoading || isExporting}>
                    {isExporting
                        ? (mode === 'validate' ? 'Validating...' : 'Exporting...')
                        : (mode === 'validate' ? 'Validate All' : 'Export')}
                </Button>
            </Modal.Footer>
        </Modal>
    );
};
