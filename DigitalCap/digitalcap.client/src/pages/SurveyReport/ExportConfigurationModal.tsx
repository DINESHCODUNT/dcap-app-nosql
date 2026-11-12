import React, { useState, useEffect } from 'react';
import { Modal, Form } from 'react-bootstrap';
import { Button, BUTTONS } from 'customer_portal-ui-shared';
import AppLoader from '../../Common/AppLoader';
import { getAllSections } from '../../service/SurveyReportService';
import {
    type ConfigSection,
    toConfigSection,
    getDisplayGroups,
    collectSelectedSections,
    SELECT_AT_LEAST_ONE
} from '../../utils/exportSettings';

interface ExportConfigurationModalProps {
    show: boolean;
    onHide: () => void;
    onExport: (selectedSettings: any) => void;
    projectId: number;
    templateId: number;
    imo: number;
    templateName: string;
    mode?: 'export' | 'validate';
}

export const ExportConfigurationModal: React.FC<ExportConfigurationModalProps> = ({
    show,
    onHide,
    onExport,
    projectId,
    templateId,
    imo,
    templateName,
    mode = 'export'
}) => {
    const [sections, setSections] = useState<ConfigSection[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [isExporting, setIsExporting] = useState(false);
    const [loadError, setLoadError] = useState<string | null>(null);
    const [validationError, setValidationError] = useState<string | null>(null);

    useEffect(() => {
        if (show) {
            setIsExporting(false);
            setValidationError(null);
            setLoadError(null);
            loadSections();
        } else {
            setSections([]);
            setValidationError(null);
            setLoadError(null);
        }
    }, [show, templateId]);

    const loadSections = async () => {
        setIsLoading(true);
        setLoadError(null);
        setValidationError(null);
        try {
            const res = await getAllSections(projectId, templateId, imo);
            if (res.data && Array.isArray(res.data) && res.data.length > 0) {
                setSections(res.data.map(toConfigSection));
            } else {
                setSections([]);
            }
        } catch (error) {
            setLoadError("Unable to load sections. Please try again.");
            setSections([]);
        } finally {
            setIsLoading(false);
        }
    };

    const handleSectionCheckboxChange = (sectionId: string, checked: boolean) => {
        setValidationError(null);
        setSections(prev => prev.map(s => s.SectionId === sectionId ? { ...s, IncludeInReport: checked } : s));
    };

    const handleTankTypeCheckboxChange = (tankTypeId: number, checked: boolean) => {
        setValidationError(null);
        setSections(prev => prev.map(s => s.TankTypeId === tankTypeId ? { ...s, IncludeInReport: checked } : s));
    };

    const handleExportClick = async () => {
        setValidationError(null);
        const selected = collectSelectedSections(sections);
        if (selected.length === 0) {
            setValidationError(SELECT_AT_LEAST_ONE);
            return;
        }

        setIsExporting(true);
        try {
            const payload = [{
                PartId: templateId,
                PartName: templateName,
                RequiredInReport: false,
                IncludeInReport: true,
                sections: selected
            }];

            await onExport(payload);
        } finally {
            setIsExporting(false);
        }
    };

    const { normal, tankTypes } = getDisplayGroups(sections);
    const hasItems = normal.length > 0 || tankTypes.length > 0;

    return (
        <Modal show={show} onHide={onHide} size="lg" centered backdrop="static">
            <Modal.Header closeButton={!isExporting}>
                <Modal.Title>{mode === 'validate' ? 'Validate' : 'Export'} {templateName} Sections</Modal.Title>
            </Modal.Header>
            <Modal.Body style={{ maxHeight: '60vh', overflowY: 'auto' }}>
                {isLoading ? (
                    <AppLoader inline />
                ) : loadError ? (
                    <p className="text-danger p-2">{loadError}</p>
                ) : (
                    <div className="section-list p-2">
                        {!hasItems ? (
                            <p>No sections found.</p>
                        ) : (
                            <>
                                {normal.map(section => (
                                    <Form.Check
                                        key={section.SectionId}
                                        type="checkbox"
                                        id={`section-${section.SectionId}`}
                                        label={section.SectionName}
                                        checked={section.RequiredInReport || section.IncludeInReport}
                                        disabled={section.RequiredInReport}
                                        onChange={(e) => handleSectionCheckboxChange(section.SectionId, e.target.checked)}
                                        className="mb-3"
                                    />
                                ))}
                                {tankTypes.map(t => (
                                    <Form.Check
                                        key={`tanktype-${t.tankTypeId}`}
                                        type="checkbox"
                                        id={`tanktype-${t.tankTypeId}`}
                                        label={t.tankType}
                                        checked={t.required || t.included}
                                        disabled={t.required}
                                        onChange={(e) => handleTankTypeCheckboxChange(t.tankTypeId, e.target.checked)}
                                        className="mb-3"
                                    />
                                ))}
                            </>
                        )}
                    </div>
                )}
            </Modal.Body>
            {validationError && (
                <div className="text-danger px-3 pb-2">{validationError}</div>
            )}
            <Modal.Footer className="gap-2">
                <Button variant={BUTTONS.TERTIARY} onClick={onHide} disabled={isExporting}>Close</Button>
                <Button variant={BUTTONS.PRIMARY} onClick={handleExportClick} disabled={isLoading || isExporting}>
                    {isExporting ? (mode === 'validate' ? 'Validating...' : 'Exporting...') : (mode === 'validate' ? 'Validate' : 'Export')}
                </Button>
            </Modal.Footer>
        </Modal>
    );
};
