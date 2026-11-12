import React, { useMemo } from 'react';
import { Button } from "customer_portal-ui-shared";
import { ProgressBar } from '../File/constants';
import * as Icon from 'react-bootstrap-icons';
import { getPartSectionsTitle, HI_PART_ID, HI_PART_NAME, HI_DEFAULT_SECTIONS, findPartInGrid } from './partUtils';

interface SideTooltipProps {
    label: string;
    children: React.ReactNode;
}

const SideTooltip: React.FC<SideTooltipProps> = ({ label, children }) => (
    <div className="side-tooltip-wrap">
        {children}
        <span className="side-tooltip-box" role="tooltip">{label}</span>
    </div>
);
const cx = (...classes: (string | false | null | undefined)[]): string =>
    classes.filter(Boolean).join(' ');

const getPartsFromAPI = (data: any | null) => {
    if (!data?.reportPartGrid) return [];

    const parts: { id: string; label: string; tooltip: string; }[] = [];

    parts.push({
        id: "VMD",
        label: "VMD",
        tooltip: "Vessel Main Data"
    });

    data.reportPartGrid.forEach((part: any) => {
        const partName =
            part?.name?.trim() ||
            part?.partName?.trim();

        if (!partName) return;
        if (partName === "Vessel Main Data") return;

        if (partName.startsWith("Part")) {
            const rawPart = partName.replace(/^Part\s+/i, "");
            const isHiPart = partName === HI_PART_NAME;
            const partId = isHiPart ? HI_PART_ID : rawPart;

            parts.push({
                id: partId,
                label: isHiPart ? "H&I" : rawPart,
                tooltip: isHiPart ? "Part H & I" : `Part ${rawPart}`
            });
        }
    });

    return parts;
};

const ReportSidebar: React.FC<any> = ({
    data,
    activePart,
    activeSection,
    onNavigate,
    isMobileView
}) => {
    const [expandedPart, setExpandedPart] = React.useState<string | null>(activePart);
    React.useEffect(() => { setExpandedPart(activePart); }, [activePart]);

    const getSectionsForPart = (partId: string) => {
        if (!data) return [];
        if (partId === "VMD") {
            return [
                { id: 'main-data', label: 'Vessel Main Data' },
                { id: 'survey-status', label: 'Survey Status' },
                { id: 'statutory', label: 'Statutory Certificates' }
            ];
        }
        if (partId === HI_PART_ID) {
            const partData = findPartInGrid(data?.reportPartGrid, partId);
            const apiSections = (partData?.sectionsList || []).map((sec: any) => ({
                id: sec.sectionId,
                label: sec.sectionName
            }));
            return apiSections.length > 0 ? apiSections : HI_DEFAULT_SECTIONS;
        }
        const partData = findPartInGrid(data?.reportPartGrid, partId);
        if (!partData) return [];
        return (partData?.sectionsList || []).map((sec: any) => ({
            id: sec.sectionId,
            label: sec.sectionName
        }));
    };

    const toggleAccordion = (partId: string) => {
        setExpandedPart(prev => prev === partId ? null : partId);
    };

    const parts = getPartsFromAPI(data);
    const sections = useMemo(() => {
        if (!data) return [];

        if (activePart === "VMD") {
            return [
                { id: 'main-data', label: 'Vessel Main Data' },
                { id: 'survey-status', label: 'Survey Status' },
                { id: 'statutory', label: 'Statutory Certificates' }
            ];
        }
        if (activePart === HI_PART_ID) {
            const partData = findPartInGrid(data?.reportPartGrid, activePart);
            const apiSections = (partData?.sectionsList || []).map((sec: any) => ({
                id: sec.sectionId,
                label: sec.sectionName
            }));

            return apiSections.length > 0 ? apiSections : HI_DEFAULT_SECTIONS;
        }

        const partData = findPartInGrid(data?.reportPartGrid, activePart);

        if (!partData) return [];

        return (partData?.sectionsList || []).map((sec: any) => ({
            id: sec.sectionId,
            label: sec.sectionName
        }));

    }, [data, activePart]);

    const activeSectionIndex = sections.findIndex((s) => s.id === activeSection);
    const totalSections = sections.length;
    const progressPct = totalSections > 0
        ? Math.round(((activeSectionIndex + 1) / totalSections) * 100)
        : 0;

    const handlePartClick = (partId: string) => {

        if (partId === "VMD") {
            onNavigate("VMD", "main-data");
            return;
        }

        const partData = findPartInGrid(data?.reportPartGrid, partId);
        const firstSection = partData?.sectionsList?.[0];

        if (partId === HI_PART_ID) {
            const firstSection = partData?.sectionsList?.[0];
            onNavigate(
                HI_PART_ID,
                firstSection?.sectionId ?? HI_DEFAULT_SECTIONS[0].id
            );
            return;
        }

        if (firstSection) {
            onNavigate(partId, firstSection.sectionId);
        } else if (partId === "HI" || partId === "HJ") {
            onNavigate(partId, "summary");
        }
    };
    return (
        <>
            {/* Desktop View */}
            {!isMobileView && (
                <>
                    <div className="aside-minibar d-none d-md-flex">
                        {parts.map((part) => (
                            <SideTooltip key={part.id} label={part.tooltip}>
                                <Button
                                    className={cx('part-btn', activePart === part.id && 'active')}
                                    onClick={() => handlePartClick(part.id)}
                                    aria-current={activePart === part.id ? 'true' : undefined}
                                >
                                    {part.label}
                                </Button>
                            </SideTooltip>
                        ))}
                    </div>
                    <div className="aside-bar d-none d-md-flex">
                        <div className="aside-header">
                            <h5 className="form-group-title">
                                {getPartSectionsTitle(activePart)}
                            </h5>
                            <ProgressBar pct={progressPct} />
                        </div>

                        <ul className="section-list" role="list">
                            {sections.map((sec) => {
                                const isActive = activeSection === sec.id;
                                return (
                                    <li key={sec.id} className={cx('section-item', isActive && 'active')}>
                                        <div
                                            className="section-item"
                                            onClick={() => onNavigate(activePart, sec.id)}
                                            aria-current={isActive ? 'page' : undefined}
                                        >
                                            <span>{sec.label}</span>
                                        </div>
                                    </li>
                                );
                            })}
                        </ul>
                    </div>
                </>
            )}

            {/* Mobile Accordion */}
            {isMobileView && (
                <div className="mobile-accordion d-flex d-md-none flex-column w-100 h-100 overflow-y-auto bg-white">
                    <div className="w-100">
                        {parts.map((part) => {
                            const isExpanded = expandedPart === part.id;
                            const partSections = getSectionsForPart(part.id);
                            
                            let currentProgress = 0;
                            if (activePart === part.id && partSections.length > 0) {
                                const idx = partSections.findIndex((s: any) => s.id === activeSection);
                                currentProgress = Math.round(((idx + 1) / partSections.length) * 100);
                            }

                            return (
                                <div key={part.id} className="accordion-item border-0 border-bottom border-light">
                                    <div 
                                        className="accordion-header m-0 p-3 d-flex justify-content-between align-items-center"
                                        style={{ 
                                            backgroundColor: isExpanded ? '#002a4e' : '#fff', 
                                            color: isExpanded ? '#fff' : '#002a4e',
                                            cursor: 'pointer'
                                        }}
                                        onClick={() => toggleAccordion(part.id)}
                                    >
                                        <h6 className="m-0 fw-bold">{part.tooltip || part.label}</h6>
                                        <Icon.ChevronDown size={16} style={{ transform: isExpanded ? 'rotate(180deg)' : 'none', transition: 'transform 0.3s' }} />
                                    </div>
                                    
                                    {isExpanded && (
                                        <div className="accordion-body p-0 border border-top-0 border-light" style={{ backgroundColor: '#fff' }}>
                                            {activePart === part.id && (
                                                <div className="px-3 pt-3 pb-2 border-bottom">
                                                    <ProgressBar pct={currentProgress} />
                                                </div>
                                            )}
                                            <ul className="list-unstyled m-0">
                                                {partSections.map((sec: any) => {
                                                    const isSecActive = activePart === part.id && activeSection === sec.id;
                                                    return (
                                                        <li key={sec.id} 
                                                            className="p-3 ps-4"
                                                            style={{
                                                                cursor: 'pointer',
                                                                backgroundColor: isSecActive ? '#f8f9fa' : '#fff',
                                                                color: isSecActive ? '#002a4e' : '#495057',
                                                                fontWeight: isSecActive ? '600' : '400',
                                                                borderLeft: isSecActive ? '4px solid #002a4e' : '4px solid transparent'
                                                            }}
                                                            onClick={() => onNavigate(part.id, sec.id)}
                                                        >
                                                            {sec.label}
                                                        </li>
                                                    );
                                                })}
                                            </ul>
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}
        </>
    )
}

export default ReportSidebar;