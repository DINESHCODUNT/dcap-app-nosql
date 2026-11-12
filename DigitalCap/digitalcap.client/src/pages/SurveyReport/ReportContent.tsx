import React, { useState, useEffect, useMemo, useContext, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { BUTTONS, Button, Divider, Input, CheckboxInput, Label, SideDialogManagerContext, AbsSearch,Switch } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import RichTextEditor from '../../Common/RichTextEditor';
import UploadTankImage from './UploadTankImage';
import _uniqueId from "lodash/uniqueId";
import Lightbox from "yet-another-react-lightbox";
import Zoom from "yet-another-react-lightbox/plugins/zoom";
import Thumbnails from "yet-another-react-lightbox/plugins/thumbnails";
import Captions from "yet-another-react-lightbox/plugins/captions";
import "yet-another-react-lightbox/styles.css";
import "yet-another-react-lightbox/plugins/thumbnails.css";
import "yet-another-react-lightbox/plugins/captions.css";
import BulkImageUpload from './BulkImageUpload';
import ReportContentListView from './ReportContentListView';
import AppLoader from '../../Common/AppLoader';
import * as surveyReportService from '../../service/SurveyReportService';
import { usePopup } from '../../Common/PopupProvider';
import { useBulkUpload } from '../../Common/BulkUploadProvider';
import { getPartDisplayLabel, findPartInGrid, getPartNameFromGrid } from './partUtils';
interface ReportContentProps {
    activePart: string;
    activeSection: string;
    reportData: any;
    data: any;
    projectId: number;
    loading: boolean;
    isPdfExport?: boolean;

    onNavigate: (
        part: string,
        section: string
    ) => void;
    isNewProject?: boolean;
    isOfflineProject?: boolean;
    refreshCurrentReport: () => Promise<void>;
}

interface GradeEntry {
    grade: string;
    description: string;
}

interface TankImage {
    id: string;
    hasImage: boolean;
    selected: boolean;
    active: boolean;
    imageIndex: number;
    imageUrl: string | null;
}

const ReportContent: React.FC<ReportContentProps> = ({
    activePart,
    activeSection,
    reportData: rawReportData,
    data,
    projectId,
    loading,
    onNavigate,
    isNewProject,
    isOfflineProject, refreshCurrentReport
}) => {
    const navigate = useNavigate();
    const { isRunning: isBulkUploading } = useBulkUpload();
    const reportData = useMemo(() => {
        if (!rawReportData) return null;
        return isNewProject
            ? (rawReportData.report ?? rawReportData)
            : rawReportData;
    }, [rawReportData, isNewProject]);

    const isEmptyId = (value: any) => {
        if (value == null || value === "") return true;
        const normalized = String(value).trim().toLowerCase();
        return normalized === "0" || normalized === "00000000-0000-0000-0000-000000000000";
    };

    const idsEqual = (left: any, right: any) => {
        if (isEmptyId(left) || isEmptyId(right)) return false;
        return String(left).trim().toLowerCase() === String(right).trim().toLowerCase();
    };

    const firstId = (...values: any[]) => values.find((value) => !isEmptyId(value));

    const toArray = (raw: any): any[] => {
        if (!raw) return [];
        if (Array.isArray(raw)) return raw;
        if (Array.isArray(raw.$values)) return raw.$values;
        return [raw];
    };

    const collectIds = (item: any) => [
        item?.id,
        item?.sectionId,
        item?.sectionMappingId
    ];

    const namesEqual = (left: any, right: any) => {
        if (!left || !right) return false;
        return String(left).trim().toLowerCase() === String(right).trim().toLowerCase();
    };

    const mapTankCards = (tank: any) => {
        const tankCards = toArray(tank.tankImageCards || tank.imageCards);
        return tankCards.map((card: any) => ({
            id: card.cardNumber ?? card.id,
            fileName: card.cardName || card.fileName,
            currentCondition: card.currentCondition,
            src: card.src,
            descriptionId: card.descriptionId,
            additionalDescription: card.additionalDescription,
            conditionId: card.currentCondition ?? card.conditionId
        }));
    };

    const getSubSectionDescriptions = (sub: any) =>
        toArray(sub?.imageDescriptions ?? sub?.ImageDescriptions);

    const normalizeSubSection = (sub: any) => ({
        ...sub,
        id: firstId(sub.sectionMappingId, sub.sectionId, sub.id),
        subSectionName: sub.subSectionName || sub.sectionName || sub.tankName,
        grading: sub.grading ?? sub.Grading ?? [],
        imageDescriptions: getSubSectionDescriptions(sub),
        imageCards: toArray(sub.imageCards ?? sub.ImageCards).map((card: any) => ({
            ...card,
            id: card.cardNumber ?? card.id,
            fileName: card.cardName || card.fileName,
        })),
    });

    const normalizedSections = useMemo(() => {
        if (!reportData) return [];

        const appendTankSections = (sections: any[]) => {
            const tankSections = toArray(
                reportData?.tankReportView?.sections ??
                reportData?.template?.tankReportView?.sections
            );

            if (tankSections.length === 0) return sections;

            return [
                ...sections,
                {
                    id: "tank-sections",
                    subSections: tankSections.map((tank: any) => {
                        const cards = mapTankCards(tank);
                        return {
                            ...tank,
                            id: firstId(tank.id, tank.sectionId, tank.sectionMappingId),
                            subSectionName: tank.tankName || tank.sectionName || tank.subSectionName,
                            sectionName: tank.tankName || tank.sectionName,
                            imageDescriptions: getSubSectionDescriptions(tank),
                            imageCards: tank.imageCards?.length ? tank.imageCards : cards,
                            projectCards: tank.projectCards?.length
                                ? tank.projectCards
                                : cards.map((card: any) => ({
                                    cardId: card.id,
                                    src: card.src,
                                    descriptionId: card.descriptionId,
                                    additionalDescription: card.additionalDescription,
                                    conditionId: card.conditionId
                                }))
                        };
                    })
                }
            ];
        };


        if (isNewProject) {
            const templateSections = reportData?.template?.sections;
            const flatSubs = toArray(templateSections?.subSections);

            if (flatSubs.length > 0) {
                return appendTankSections([{
                    id: "project-subsections",
                    subSections: flatSubs.map(normalizeSubSection)
                }]);
            }
        }

        const raw = reportData?.template?.sections;
        const arr = toArray(raw);

        const sections = arr.map((section: any) => {
            const subSections = toArray(section.subSections).map(normalizeSubSection);

            return {
                ...section,
                subSections
            };
        });

        return appendTankSections(sections);
    }, [reportData, isNewProject]);

    const findMatchedSubSection = (
        sections: any[],
        activeSection: string,
        activeLabel?: string
    ): any => {
        const allSubSections = sections.flatMap((section) => toArray(section.subSections));

        const matchInList = (subSections: any[]) => {
            const matches = subSections.filter((s: any) =>
                collectIds(s).some((id) => idsEqual(id, activeSection)) ||
                namesEqual(s.subSectionName, activeLabel) ||
                namesEqual(s.sectionName, activeLabel) ||
                namesEqual(s.tankName, activeLabel)
            );

            if (matches.length === 0) return null;
            if (matches.length === 1) return matches[0];

            return matches.find((s: any) => getSubSectionDescriptions(s).length > 0) ?? matches[0];
        };

        const directMatch = matchInList(allSubSections);
        if (directMatch) return directMatch;

        if (!isNewProject) {
            for (const section of sections) {
                const subSections = toArray(section.subSections);

                if (idsEqual(section.id, activeSection) || namesEqual(section.sectionName, activeLabel)) {
                    const named = matchInList(subSections);
                    if (named) return named;
                }
            }
        }

        return null;
    };
    const [grades, setGrades] = useState<Record<string, any>>({});
    const [notes, setNotes] = useState("");
    const [visible, setVisible] = useState(false);
    const [lightboxOpen, setLightboxOpen] = useState(false);
    const [lightboxIndex, setLightboxIndex] = useState(0);
    const [tanks, setTanks] = useState<any[]>([]);
    const [tankSearchVal, setTankSearchVal] = useState<string>("");
    const [filterType, setFilterType] = useState<"ALL" | "WITH_IMAGE" | "MISSING">("ALL");
    const [viewMode, setViewMode] = useState<"GRID" | "LIST">("GRID");
    const [panelOpen, setPanelOpen] = useState(false);
    const [activeTank, setActiveTank] = useState<any>(null);
    const [gradingErrors, setGradingErrors] = useState<Record<string, string>>({});
    const [reportSectionOptions, setReportSectionOptions] = useState<any[]>([]);
    const getCommonApiParams = (tank: any) => {
        const templateId = tank.templateId ?? 0;
        const sectionId = tank.sectionId ?? activeSection;
        const cardId = Number(tank.cardId ?? 0);
        const cardName = String(tank.imageName || tank.id || "").trim();

        const isTankSection = toArray(
            reportData?.tankReportView?.sections ?? reportData?.template?.tankReportView?.sections
        ).some((ts: any) =>
            toArray(ts.sectionId)
                .map((id) => String(id).trim().toLowerCase())
                .includes(String(activeSection).trim().toLowerCase())
        );

        return {
            templateId,
            sectionId,
            cardId,
            cardName,
            useTankSectionApis: isTankSection,
            useNewNormalSectionApis: isNewProject && !isTankSection
        };
    };

    const handleConditionChange = async (tankId: string, value: any) => {
        const tank = tanks.find(t => t.id === tankId);
        if (!tank || String(tank.conditionId) === String(value)) return;

        const textValue = conditionOptions.find(c => String(c.value) === String(value))?.text;
        setTanks(prev => prev.map(t => t.id === tankId ? { ...t, conditionId: value, selectedCondition: textValue } : t));

        const { templateId, sectionId, cardId, cardName, useTankSectionApis, useNewNormalSectionApis } = getCommonApiParams(tank);

        try {
            if (useTankSectionApis) {
                await surveyReportService.updateTankCardCurrentCondition({
                    templateId, projectId, sectionId, cardNumber: cardId, value: value || 0, cardName
                });
            } else if (useNewNormalSectionApis) {
                await surveyReportService.updateGenericCurrentCondition({
                    templateId, projectId, sectionId, cardId, value: value || 0, cardName
                });
            } else {
                await surveyReportService.updateCurrentCondition({
                    templateId, projectId, sectionId, cardId, value: value || 0
                });
            }
        } catch (err) {
            console.error(err);
            handleSaveError();
        }
    };

    const handleDescriptionChange = async (tankId: string, value: any) => {
        const tank = tanks.find(t => t.id === tankId);
        if (!tank || String(tank.descriptionId) === String(value)) return;

        const textValue = activeDescriptionOptions.find(d => String(d.value) === String(value))?.text;
        setTanks(prev => prev.map(t => t.id === tankId ? { ...t, descriptionId: value, descriptionText: textValue } : t));

        const { templateId, sectionId, cardId, cardName, useTankSectionApis, useNewNormalSectionApis } = getCommonApiParams(tank);

        try {
            if (useTankSectionApis) {
                await surveyReportService.updateTankImageDescriptionDropdownCard({
                    templateId, projectId, sectionId, cardNumber: cardId, value: Number(value), cardName
                });
            } else if (useNewNormalSectionApis) {
                await surveyReportService.updateGenericImageDescriptionDropdownCard({
                    templateId, projectId, sectionId, cardId, value: Number(value), cardName
                });
            } else {
                await surveyReportService.updateImageDescriptionDropdownCard({
                    templateId, projectId, sectionId, cardId, value: value
                });
            }
        } catch (err) {
            console.error(err);
            handleSaveError();
        }
    };

    const handleAdditionalDescriptionChange = async (tankId: string, value: string) => {
        const tank = tanks.find(t => t.id === tankId);
        if (!tank || tank.additionalDescription === value) return;

        setTanks(prev => prev.map(t => t.id === tankId ? { ...t, additionalDescription: value } : t));

        const { templateId, sectionId, cardId, cardName, useTankSectionApis, useNewNormalSectionApis } = getCommonApiParams(tank);

        try {
            if (useTankSectionApis) {
                await surveyReportService.updateTankCardAdditionalDescription({
                    templateId, projectId, sectionId, cardNumber: cardId, value: value || "", cardName
                });
            } else if (useNewNormalSectionApis) {
                await surveyReportService.updateGenericAdditionalDescription({
                    templateId, projectId, sectionId, cardId, value: value || "", cardName
                });
            } else {
                await surveyReportService.updateAdditionalDescription({
                    templateId, projectId, sectionId, cardId, value: value || ""
                });
            }
        } catch (err) {
            console.error(err);
            handleSaveError();
        }
    };

    const handleImageUpload = async (tankId: string, file: File) => {
        const tank = tanks.find(t => t.id === tankId);
        if (!tank) return;

        const { templateId, sectionId, cardId, cardName, useTankSectionApis, useNewNormalSectionApis } = getCommonApiParams(tank);
        const replaceImage = !!tank.imageUrl;

        try {
            let uploadedImageBase64 = null;

            if (useTankSectionApis) {
                const result = await surveyReportService.uploadTankCardImage({
                    file, projectId, cardNumber: cardId, templateId, sectionId, imageId: 0, replaceImage, cardName,
                });
                uploadedImageBase64 = result?.data?.image ?? result?.data?.Image;
            } else if (useNewNormalSectionApis) {
                const result = await surveyReportService.uploadGenericSurveyImage({
                    file, projectId, cardId, templateId, sectionId, imageId: 0, replaceImage, cardName,
                });
                uploadedImageBase64 = result?.data?.image ?? result?.data?.Image;
            } else {
                const result = await surveyReportService.uploadSurveyImage({
                    file, projectId, cardId, templateId, sectionId, imageId: 0, replaceImage,
                });
                uploadedImageBase64 = result?.data?.image ?? result?.data?.Image;
            }

            if (uploadedImageBase64) {
                const newImageUrl = `data:image/jpeg;base64,${uploadedImageBase64}`;
                
                // Sync condition back to the server to prevent it from resetting to null
                const conditionValue = tank.conditionId ? tank.conditionId : 0;
                if (useTankSectionApis) {
                    await surveyReportService.updateTankCardCurrentCondition({
                        templateId, projectId, sectionId, cardNumber: cardId, value: conditionValue, cardName,
                    });
                } else if (useNewNormalSectionApis) {
                    await surveyReportService.updateGenericCurrentCondition({
                        templateId, projectId, sectionId, cardId, value: conditionValue, cardName,
                    });
                } else {
                    await surveyReportService.updateCurrentCondition({
                        templateId, projectId, sectionId, cardId, value: conditionValue,
                    });
                }

                setTanks(prev => prev.map(t => t.id === tankId ? {
                    ...t,
                    hasImage: true,
                    imageUrl: newImageUrl,
                    imageName: file.name
                } : t));
                refreshCurrentReport();
            }
        } catch (err) {
            console.error(err);
        }
    };

    const popup = usePopup();
    const saveErrorOpenRef = useRef(false);

    const handleSaveError = () => {
        if (!saveErrorOpenRef.current) {
            saveErrorOpenRef.current = true;
            popup.error("Your change could not be saved. Please try again.", "Save").finally(() => {
                saveErrorOpenRef.current = false;
            });
        }
    };
    const tooltipRef = useRef<HTMLDivElement>(null);

    const sideDialog = useContext(SideDialogManagerContext) as any;
    const id = _uniqueId("sideDialog_");

    const sections = useMemo(() => {
        if (!data) return [];

        if (activePart === "VMD") {
            return [
                { id: 'main-data', label: 'Vessel Main Data' },
                { id: 'survey-status', label: 'Survey Status' },
                { id: 'statutory', label: 'Statutory Certificates' }
            ];
        }

        const partData = data.reportPartGrid?.find(
            (p: any) =>
                p.name ===
                `Part ${activePart === "HI"
                    ? "H & I"
                    : activePart
                }`
        );

        if (!partData) return [];

        return partData.sectionsList.map((sec: any) => ({
            id: sec.sectionId,
            label: sec.sectionName
        }));

    }, [data, activePart]);

    const sectionLabel =
        sections.find(
            (s) =>
                String(s.id) === String(activeSection)
        )?.label || "";

    const sectionIndex = sections.findIndex(
        (s) =>
            String(s.id) === String(activeSection)
    );
    const gradingData = useMemo(() => {
        if (!reportData) return { rows: [], options: [] };

        const matchedSubSection = findMatchedSubSection(
            normalizedSections,
            activeSection,
            sectionLabel
        );

        if (!matchedSubSection) return { rows: [], options: [] };

        return {
            rows: matchedSubSection.grading || [],
            options: matchedSubSection.gradingLabelName || []
        };

    }, [normalizedSections, activeSection, sectionLabel]);

    useEffect(() => {
        if (!reportData) return;

        const matchedSubSection = findMatchedSubSection(
            normalizedSections,
            activeSection,
            sectionLabel
        );

        if (!matchedSubSection) {
            setNotes("");
            return;
        }

        setNotes(
            matchedSubSection.sectionNotes ||
            matchedSubSection.notes ||
            ""
        );

    }, [reportData, activeSection, normalizedSections, sectionLabel]);

    const matchedSubSection = useMemo(
        () => findMatchedSubSection(normalizedSections, activeSection, sectionLabel),
        [normalizedSections, activeSection, sectionLabel]
    );

    const activeDescriptionOptions = useMemo(() => {
        return getSubSectionDescriptions(matchedSubSection).map((d: any) => ({
            text: d.description,
            value: String(d.id),
        }));
    }, [matchedSubSection]);

    const descriptionoptions = useMemo(() => {
        if (!reportData) return [];
        const opts = normalizedSections.flatMap((section: any) => {
            return (section.subSections || []).flatMap((sub: any) => {
                const sectionIds = [sub.id, sub.sectionId, sub.sectionMappingId, section.id].filter(Boolean).map(String); const sectionId = sectionIds[0];
                const sectionNameStr = sub.subSectionName || sub.sectionName || sub.name || section.sectionName || section.name || "";
                return getSubSectionDescriptions(sub).map((d: any) => ({
                    text: d.description,
                    value: String(d.id),
                    sectionId: String(sectionId),
                    sectionIds: sectionIds,
                    sectionName: String(sectionNameStr).trim().toLowerCase()
                }));
            });
        });
        return opts;

    }, [normalizedSections, reportData]);

    const fileNameOptions = useMemo(() => {
        if (!reportData) return [];
        const opts = normalizedSections.flatMap((section: any) => {
            return (section.subSections || []).flatMap((sub: any) => {
                const sectionIds = [sub.id, sub.sectionId, sub.sectionMappingId, section.id].filter(Boolean).map(String); const sectionId = sectionIds[0];
                const cards = (sub.imageCards || []);
                return cards
                    .filter((card: any) => {
                        const matchedTank = tanks.find(
                            (t: any) =>
                                t.cardId === card.id &&
                                String(t.sectionId) === String(sectionId)
                        );
                        return !matchedTank?.hasImage;
                    })
                    .map((card: any) => {
                        const sectionNameStr = sub.subSectionName || sub.sectionName || sub.name || section.sectionName || section.name || "";
                        return {
                            text: card.fileName,
                            value: card.id,
                            currentCondition: card.currentCondition,
                            sectionId: sectionId,
                            sectionIds: sectionIds,
                            sectionName: String(sectionNameStr).trim().toLowerCase()
                        };
                    });
            });
        });
        return opts;

    }, [normalizedSections, activeSection, tanks, reportData]);

    const conditionOptions = useMemo(() => {
        const matchedSubSection = findMatchedSubSection(
            normalizedSections,
            activeSection,
            sectionLabel
        );

        const conditions =
            matchedSubSection?.currentConditions ||
            reportData?.template?.currentConditions ||
            [];

        if (!conditions?.length) return [];

        return conditions.map((c: any) => ({
            text: c.name,
            value: String(c.id)
        }));

    }, [reportData, normalizedSections, activeSection, sectionLabel]);


    const handleNext = () => {

        if (activePart === "VMD") {

            const vmdSections = [
                "main-data",
                "survey-status",
                "statutory"
            ];

            const currentIndex =
                vmdSections.indexOf(activeSection);


            if (currentIndex < vmdSections.length - 1) {

                onNavigate(
                    "VMD",
                    vmdSections[currentIndex + 1]
                );

                return;
            }


            const firstPart =
                data?.reportPartGrid?.find(
                    (p: any) => p.name === "Part A"
                );

            const firstSection =
                firstPart?.sectionsList?.[0];

            if (firstSection) {

                onNavigate(
                    "A",
                    firstSection.sectionId
                );
            }

            return;
        }


        const currentPart =
            data?.reportPartGrid?.find(
                (p: any) =>
                    p.name ===
                    `Part ${activePart === "HI"
                        ? "H & I"
                        : activePart
                    }`
            );

        if (!currentPart) return;

        const currentSections =
            currentPart.sectionsList || [];

        const currentIndex =
            currentSections.findIndex(
                (s: any) =>
                    String(s.sectionId) ===
                    String(activeSection)
            );

        if (currentIndex < currentSections.length - 1) {

            const nextSection =
                currentSections[currentIndex + 1];

            onNavigate(
                activePart,
                nextSection.sectionId
            );

            return;
        }

        const allParts =
            data?.reportPartGrid || [];

        const currentPartIndex =
            allParts.findIndex(
                (p: any) =>
                    p.name ===
                    `Part ${activePart === "HI"
                        ? "H & I"
                        : activePart
                    }`
            );

        if (currentPartIndex < allParts.length - 1) {

            const nextPart =
                allParts[currentPartIndex + 1];

            const nextSection =
                nextPart.sectionsList?.[0];

            if (nextSection) {

                const nextPartName =
                    nextPart.name === "Part H & I"
                        ? "HI"
                        : nextPart.name.replace(
                            "Part ",
                            ""
                        );

                onNavigate(
                    nextPartName,
                    nextSection.sectionId
                );
            }
        }
    };

    const handlePrevious = () => {


        if (activePart === "VMD") {

            const vmdSections = [
                "main-data",
                "survey-status",
                "statutory"
            ];

            const currentIndex =
                vmdSections.indexOf(activeSection);


            if (currentIndex > 0) {

                onNavigate(
                    "VMD",
                    vmdSections[currentIndex - 1]
                );
            }

            return;
        }
        const currentPart =
            data?.reportPartGrid?.find(
                (p: any) =>
                    p.name ===
                    `Part ${activePart === "HI"
                        ? "H & I"
                        : activePart
                    }`
            );

        if (!currentPart) return;

        const currentSections =
            currentPart.sectionsList || [];

        const currentIndex =
            currentSections.findIndex(
                (s: any) =>
                    String(s.sectionId) ===
                    String(activeSection)
            );


        if (currentIndex > 0) {

            const previousSection =
                currentSections[currentIndex - 1];

            onNavigate(
                activePart,
                previousSection.sectionId
            );

            return;
        }


        const allParts =
            data?.reportPartGrid || [];

        const currentPartIndex =
            allParts.findIndex(
                (p: any) =>
                    p.name ===
                    `Part ${activePart === "HI"
                        ? "H & I"
                        : activePart
                    }`
            );


        if (currentPartIndex <= 0) {
            onNavigate(
                "VMD",
                "main-data"
            );
            return;
        }

        const previousPart =
            allParts[currentPartIndex - 1];

        const previousSections =
            previousPart.sectionsList || [];

        const lastSection =
            previousSections[
            previousSections.length - 1
            ];

        if (lastSection) {

            const previousPartName =
                previousPart.name === "Part H & I"
                    ? "HI"
                    : previousPart.name.replace(
                        "Part ",
                        ""
                    );

            onNavigate(
                previousPartName,
                lastSection.sectionId
            );
        }
    };

    const handleSelect = async (
        row: any,
        optionValue: any,
        checked: boolean
    ) => {

        let current: any[] = grades[row.id] || [];


        setGradingErrors(prev => ({
            ...prev,
            [row.id]: ""
        }));

        const isNA =
            optionValue === "N/A";


        if (isNA) {

            let updated =
                checked ? ["N/A"] : [];

            setGrades((prev: any) => ({
                ...prev,
                [row.id]: updated
            }));

            setGradingErrors(prev => ({
                ...prev,
                [row.id]: ""
            }));

            try {

                const choicesToUpdate = gradingData.options.map((opt: any) => ({
                    choiceName: String(opt),
                    value: opt === "N/A" ? checked : false
                }));

                await surveyReportService.updateGradingCondition({
                    projectId,
                    templateId: reportData.template.id,
                    sectionId: activeSection,
                    gradingId: row.id,
                    choices: choicesToUpdate
                });

            } catch (err) {
                handleSaveError();
            }

            return;
        }

        current =
            current.filter(v => v !== "N/A");

        let updated: any[] = [];

        if (checked) {

            updated = [...current, optionValue];

        } else {

            updated = current.filter(
                v => v !== optionValue
            );
        }

        updated = [...new Set(updated)];

        updated.sort((a: number, b: number) => a - b);


        if (
            updated.includes(5) &&
            updated.length > 1
        ) {

            setGradingErrors(prev => ({
                ...prev,
                [row.id]:
                    "Not examined cannot be selected"
            }));

            return;
        }

        if (updated.length > 2) {

            setGradingErrors(prev => ({
                ...prev,
                [row.id]:
                    "Adjacent grading only allowed"
            }));

            return;
        }

        if (
            updated.length === 2 &&
            Math.abs(updated[1] - updated[0]) !== 1
        ) {

            setGradingErrors(prev => ({
                ...prev,
                [row.id]:
                    "Adjacent grading only allowed"
            }));

            return;
        }

        setGradingErrors(prev => ({
            ...prev,
            [row.id]: ""
        }));

        setGrades((prev: any) => ({
            ...prev,
            [row.id]: updated
        }));

        try {

            const choicesToUpdate = gradingData.options.map((opt: any) => ({
                choiceName: String(opt),
                value: opt === "N/A" ? false : updated.includes(Number(opt))
            }));

            await surveyReportService.updateGradingCondition({
                projectId,
                templateId: reportData.template.id,
                sectionId: activeSection,
                gradingId: row.id,
                choices: choicesToUpdate
            });

        } catch (err) {
            handleSaveError();
        }
    };

    useEffect(() => {
        if (!reportData) return;

        const matchedSubSection = findMatchedSubSection(
            normalizedSections,
            activeSection,
            sectionLabel
        );

        if (!matchedSubSection) {
            setTanks([]);
            return;
        }

        const cards = matchedSubSection.imageCards || [];
        const projectCards = matchedSubSection.projectCards || [];

        const toImageSrc = (src: string | null | undefined) => {
            if (!src) return null;
            return src.startsWith("data:") ? src : `data:image/jpeg;base64,${src}`;
        };

        const imageMap: Record<number, string> = {};
        projectCards?.forEach((p: any) => {
            const src = toImageSrc(p.src ?? p.Src);
            const cardKey = p.cardId ?? p.cardNumber;
            if (cardKey && src) {
                imageMap[cardKey] = src;
            }
        });
        cards.forEach((card: any) => {
            const src = toImageSrc(card.src);
            const cardKey = card.cardNumber ?? card.id;
            if (cardKey && src && !imageMap[cardKey]) {
                imageMap[cardKey] = src;
            }
        });

        const mappedTanks = cards.map((card: any) => {
            const cardKey = card.cardNumber ?? card.id;
            const projectImage = projectCards?.find(
                (p: any) => String(p.cardId ?? p.cardNumber) === String(cardKey)
            );

            const selectedConditionId =
                projectImage?.currentCondition ||
                projectImage?.conditionId ||
                "";

            const selectedCondition = (
                matchedSubSection.currentConditions ||
                reportData?.template?.currentConditions ||
                []
            )?.find((c: any) => String(c.id) === String(selectedConditionId));

            const descriptionObj = getSubSectionDescriptions(matchedSubSection).find(
                (d: any) =>
                    String(d.id) === String(projectImage?.descriptionId || card.descriptionId)
            );

            return {
                id: card.fileName || card.cardName || String(cardKey),
                cardId: cardKey,
                hasImage: !!imageMap[cardKey],
                selected: false,
                active: false,
                imageUrl: imageMap[cardKey] || null,
                currentCondition: card.currentCondition,
                descriptionId: projectImage?.descriptionId || card.descriptionId || "",
                descriptionText: descriptionObj?.description || descriptionObj?.text || "",
                additionalDescription: projectImage?.additionalDescription || card.additionalDescription || "",
                selectedCondition: selectedCondition?.name || "",
                imageName: card.fileName || card.cardName || "",
                imageId: projectImage?.imageId || 0,
                templateId: reportData.template.id,
                sectionId: activeSection,
                conditionId: selectedConditionId || "",
            };
        });
        setTanks(mappedTanks);

    }, [reportData, activeSection, normalizedSections, sectionLabel, isNewProject]);

    const toggleActive = (id: string) => {
        setTanks(prev => {
            const updatedTanks = prev.map(t => ({
                ...t,
                active: t.id === id
            }));
            const selectedTank = updatedTanks.find(t => t.id === id);
            setActiveTank(selectedTank || null);
            return updatedTanks;
        });
    };

    const slides = tanks
        .filter(t => t.hasImage && t.imageUrl)
        .map(t => ({
            src: t.imageUrl,
            title: t.id,
            description: `FileName : ${t.selectedCondition || ((t.currentCondition == 0 || t.currentCondition == "0" || t.currentCondition == false || t.currentCondition == "false" || t.currentCondition == null) ? "Current Condition" : "")}${t.descriptionText ? `\nDescription : ${t.descriptionText}` : ""}`
        }));

    const getTankClass = (tank: TankImage): string => {
        if (tank.active) return "tank-image tank-active";
        if (tank.selected) return "tank-image tank-selected";
        return "tank-image tank-notselected";
    };

    const getIconColor = (tank: TankImage): string => {
        if (tank.active) return "#2f4ea0";
        if (tank.hasImage) return "#4caf50";
        return "#b0b0b0";
    };

    useEffect(() => {
        if (!projectId) return;

        surveyReportService.GetBulkUploadSectionDropdown(projectId)
            .then(res => {
                const mapped = (res.data as any || []).map((item: any) => ({
                    text: item.name,
                    value: item.id
                }));

                setReportSectionOptions(mapped);
            })
            .catch(err => console.error("Section dropdown error", err));

    }, [projectId]);

    const handleClick = () => {
        if (isBulkUploading) return;
        let filteredReportSectionOptions = reportSectionOptions;
        if (!isNewProject) {
            filteredReportSectionOptions = reportSectionOptions.filter((opt: any) => opt.value?.startsWith(`${reportData.template.id}:`));
        } else {
            filteredReportSectionOptions = normalizedSections.flatMap((sec: any) =>
                (sec.subSections || []).map((sub: any) => {
                    const matchedPart = findPartInGrid(data?.reportPartGrid, activePart);
                    let resolvedPartName = "";
                    if (matchedPart) {
                        resolvedPartName = getPartNameFromGrid(matchedPart);
                    } else {
                        resolvedPartName = activePart;
                    }
                    
                    const partName = reportData?.template?.TemplateTitle || reportData?.template?.name || resolvedPartName;
                    
                    return {
                        text: `${partName} : ${sub.subSectionName}`,
                        value: sub.id
                    };
                })
            );
        }

        sideDialog.addDialog({
            id,
            component: <BulkImageUpload
                id={id}
                data={reportData}
                fileNameOptions={fileNameOptions}
                descriptionOptions={descriptionoptions}
                reportSectionOptions={filteredReportSectionOptions}
                conditionOptions={conditionOptions}
                projectId={projectId}
                templateId={reportData.template.id}
                onUploadSuccess={refreshCurrentReport}
                isNewProject={isNewProject}
            />
        });
    };

    useEffect(() => {
        if (!reportData) return;
        const matchedSubSection = findMatchedSubSection(
            normalizedSections,
            activeSection,
            sectionLabel
        );

        if (!matchedSubSection) {
            setGrades({});
            setGradingErrors({});
            return;
        }

        const mapped: any = {};
        gradingData.rows.forEach((row: any) => {
            const selected = row.checkbox
                ?.filter((c: any) => c.value === true)
                .map((c: any) => c.name === "N/A" ? "N/A" : Number(c.name));
            mapped[row.id] = selected || [];
            mapped[`${row.id}_comment`] = row.value || "";
        });

        setGrades(mapped);
        setGradingErrors({});

    }, [reportData, activeSection, normalizedSections, sectionLabel, gradingData.rows]);

    useEffect(() => {
        setPanelOpen(false);
        setActiveTank(null);
    }, [activeSection]);

    const uploadedCount =
        tanks.filter(t => t.hasImage).length;

    const totalCount =
        tanks.length;

    const confirmDeleteImage = (tankToDelete: any) => {
        if (!tankToDelete) return;

        popup.confirmAsync({
            title: "Delete Image",
            message: "Are you sure you want to delete this image?",
            confirmButtonText: "Yes",
            cancelButtonText: "Cancel",
            danger: true,
            onConfirm: async () => {
                const isTankSection = toArray(
                    reportData?.tankReportView?.sections ?? reportData?.template?.tankReportView?.sections
                ).some((tank: any) =>
                    [tank?.sectionMappingId, tank?.sectionId, tank?.id]
                        .filter(Boolean)
                        .map(String)
                        .map(s => s.trim().toLowerCase())
                        .includes(String(tankToDelete.sectionId).trim().toLowerCase())
                );

                if (isTankSection) {
                    await surveyReportService.deleteTankCardImage({
                        templateId: tankToDelete.templateId ?? 0,
                        projectId: projectId,
                        sectionId: tankToDelete.sectionId,
                        cardNumber: tankToDelete.cardId,
                        imageId: Number(tankToDelete.imageId ?? 0),
                    });
                } else if (isNewProject) {
                    await surveyReportService.deleteGenericImage(
                        tankToDelete.templateId ?? 0,
                        projectId,
                        tankToDelete.sectionId,
                        tankToDelete.cardId,
                        Number(tankToDelete.imageId ?? 0)
                    );
                } else {
                    await surveyReportService.deleteImage({
                        templateId: tankToDelete.templateId,
                        projectId: projectId,
                        sectionId: tankToDelete.sectionId,
                        cardId: tankToDelete.cardId,
                        imageId: tankToDelete.imageId,
                        replaceImage: false
                    });
                }

                setTanks(prev =>
                    prev.map(t =>
                        t.id === tankToDelete.id
                            ? {
                                ...t,
                                hasImage: false,
                                imageUrl: null,
                                imageId: 0,
                                descriptionId: "",
                                descriptionText: "",
                                additionalDescription: "",
                                selectedCondition: ""
                            }
                            : t
                    )
                );

                setActiveTank(prev =>
                    prev && prev.id === tankToDelete.id
                        ? {
                            ...prev,
                            hasImage: false,
                            imageUrl: null,
                            imageId: 0,
                            descriptionId: "",
                            descriptionText: "",
                            additionalDescription: "",
                            selectedCondition: ""
                        }
                        : prev
                );

                popup.success("Image has been deleted successfully.", "Delete Image");
            }
        }).catch((err: any) => popup.error(err?.response?.data || err?.message || "Unable to delete the image. Please try again.", "Delete Image"));
    };

    const closePanel = () => {
        setPanelOpen(false);

    };

    const togglePanel = () => {
        if (panelOpen) {
            closePanel();
        } else {
            setPanelOpen(true);
        }
    };

    const GRADE_ENTRIES: GradeEntry[] = [
        {
            grade: "Grade 1",
            description:
                "Condition superior. Items examined and/or measured and found with only superficial reductions from as new or current rule scantlings. No maintenance or repair required.",
        },
        {
            grade: "Grade 2",
            description:
                "Condition above average. Items examined and/or measured and found to have deficiencies of minor nature not requiring correction or repairs and/or found to have thickness significantly above Class limits.",
        },
        {
            grade: "Grade 3",
            description:
                "Condition average. Items examined and/or measured and either found to have a deficiency which does not require immediate corrective action, or found to have thicknesses, although generally above Class renewal levels, with substantial corrosion.",
        },
        {
            grade: "Grade 4",
            description:
                "Condition below average. Items examined and/or measured and either found to have a deficiency or deficiencies which may affect the ship's potential to remain in Class, or found to have, in some areas, thicknesses, which are below the Class renewal levels.",
        },
        {
            grade: "Grade 5",
            description:
                "Conditions which could not be determined due to many factors (tank not gas free, etc.)",
        },
    ];

    const EVALUATION_TEXT =
        "To arrive at an unbiased evaluation of the vessel's condition, the survey was carried out using this check sheet together with a grading system ranging from very good to unsatisfactory with the survey being done by surveyors acting independently of each other. Upon completion of CAP Survey by the attending CAP Surveyors, a grade was agreed upon and assigned to each item with results reported herein.";

    if (loading) {
        return <AppLoader inline />;
    }

    return (
        <div className="report-content">
            <div className="project-header">
                <div className="section-title p-2 p-md-3 pe-5 pe-md-3 border-bottom d-flex flex-column flex-md-row align-items-start align-items-md-center justify-content-between gap-md-3 position-relative">
                    <div className="d-md-none position-absolute" style={{ right: '54px', top: '12px', zIndex: 10 }}>
                        <button className='info-tooltip shadow-sm'
                            onClick={() => setVisible((prev) => !prev)}
                            aria-label="Show grading system information"
                            aria-expanded={visible}
                            style={{ width: '32px', height: '32px', borderRadius: '6px', borderRight: '1px solid #dee2e6', padding: 0 }}> <Icon.InfoCircle size={16} /> </button>
                    </div>
                    <div className="d-flex flex-row gap-3 w-100 w-md-auto justify-content-between justify-content-md-start">
                        <div className="d-flex flex-column">
                            <h5>{sectionLabel}</h5>
                            <div className="project-meta tx-14">
                                PART {getPartDisplayLabel(activePart)}
                                <span className="px-1"><Icon.CircleFill size={3} /></span>
                                {sectionIndex + 1} of {sections.length} sections
                            </div>
                        </div>
                    </div>
                    <div className="section-actions w-100 w-md-auto d-flex justify-content-end mt-2 mt-md-0">
                        <div className="d-flex flex-wrap gap-2 align-items-center justify-content-end w-100">
                            <div className="d-none d-md-block"><button className='info-tooltip'
                                onClick={() => setVisible((prev) => !prev)}
                                aria-label="Show grading system information"
                                aria-expanded={visible}> <Icon.InfoCircle /> </button></div>
                            {(activePart === "B" || activePart === "C" || activePart === "D") && (
                                <div>
                                    <Button
                                        variant={BUTTONS.SECONDARY}
                                        onClick={() => navigate('/app/Configuration/ManageTanks?ProjectId=' + projectId + '&imoNumber=' + (data?.report?.vessel?.imoNumber || data?.report?.vessel?.ImoNumber || data?.imonumber || data?.imoNumber || ''))}
                                    >
                                        Go to Manage Tanks
                                    </Button>
                                </div>
                            )}
                            <div>
                                <Button
                                    variant={BUTTONS.SECONDARY}
                                    onClick={handlePrevious}
                                >
                                    Previous
                                </Button>
                            </div>
                            <div>
                                <Button
                                    onClick={handleNext}
                                >
                                    Next
                                </Button>
                            </div>
                        </div>

                    </div>
                </div>
            </div>
            {visible && (
                <div ref={tooltipRef} role="tooltip" className='tooltip-panel'>
                    <div className='hint-wrapper'>
                        <h5 className='hint-title d-flex justify-content-between align-items-center'>
                            Evaluation Method

                            <button onClick={() => setVisible(false)} aria-label="Close grading info" className='hint-close-btn' >
                                <Icon.XLg size={32} className='tx-32' />
                            </button>
                        </h5>
                        <p className='hint-content'>
                            {EVALUATION_TEXT}
                        </p>
                    </div>

                    <div className='hint-wrapper'>
                        <h5 className='hint-title'>
                            Grading System
                        </h5>
                        <table className='table table-bordered'>
                            <tbody>
                                {GRADE_ENTRIES.map((entry) => (
                                    <tr key={entry.grade} >
                                        <td className='text-nowrap _600'>
                                            {entry.grade}
                                        </td>
                                        <td>
                                            {entry.description}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            <div className="report-wrapper" style={{ position: 'relative', display: 'flex' }}>
                <div className={`main-report-data ${panelOpen ? 'panel-open' : ''}`}>
                    <h3 className="form-group-title">Select Grades</h3>
                    <div className="row">
                        <div className="col-md-12">
                            <table className="table tbl-grading-table">

                                <thead>
                                    <tr>
                                        <th className="row-label"> </th>
                                        <th className="row-label text-center">Grading</th>
                                        {gradingData.options.map((opt: number) => (
                                            <th key={String(opt)} >
                                                {opt}
                                            </th>
                                        ))}
                                    </tr>
                                </thead>
                                <tbody>
                                    {gradingData.rows.map((row: any) => {
                                        const rowId = row.id || row.name;
                                        {
                                            gradingErrors[row.id] && (
                                                <tr>
                                                    <td colSpan={8}>
                                                        <div
                                                            style={{
                                                                color: "red",
                                                                fontSize: "12px",
                                                                padding: "6px 0"
                                                            }}
                                                        >
                                                            {gradingErrors[row.id]}
                                                        </div>
                                                    </td>
                                                </tr>
                                            )
                                        }
                                        return (
                                            <React.Fragment key={rowId}>
                                                <tr>
                                                    <td className="row-label">
                                                        {row.name}
                                                    </td>
                                                    <td>
                                                        <Input
                                                            placeholder=" "
                                                            value={
                                                                grades[`${rowId}_comment`]
                                                                ?? row.value
                                                                ?? ""
                                                            }
                                                            onChange={async (val: string) => {

                                                                setGrades(prev => ({
                                                                    ...prev,
                                                                    [`${rowId}_comment`]: val
                                                                }));

                                                                try {

                                                                    await surveyReportService.updateGradingNotes({
                                                                        projectId: projectId,
                                                                        templateId: reportData.template.id,
                                                                        sectionId: activeSection,
                                                                        gradingId: row.id,
                                                                        value: val
                                                                    });

                                                                } catch (err) {
                                                                    handleSaveError();
                                                                }
                                                            }}
                                                            maxLength={20}
                                                            disabled={isOfflineProject}
                                                        />
                                                    </td>

                                                    {gradingData.options.map((opt: any) => {

                                                        const optionValue =
                                                            opt === "N/A"
                                                                ? "N/A"
                                                                : Number(opt);

                                                        return (

                                                            <td key={String(opt)}>

                                                                <CheckboxInput
                                                                    checked={
                                                                        grades[row.id]?.includes(optionValue)
                                                                    }
                                                                    onChange={(checked: boolean) =>
                                                                        handleSelect(
                                                                            row,
                                                                            optionValue,
                                                                            checked
                                                                        )
                                                                    }
                                                                    disabled={isOfflineProject}
                                                                />
                                                            </td>
                                                        );
                                                    })}
                                                </tr>
                                                {gradingErrors[row.id] && (
                                                    <tr>
                                                        <td
                                                            colSpan={
                                                                gradingData.options.length + 2
                                                            }
                                                        >
                                                            <div
                                                                style={{
                                                                    color: "red",
                                                                    fontSize: "12px",
                                                                    paddingTop: "4px",
                                                                    paddingBottom: "6px"
                                                                }}
                                                            >
                                                                {gradingErrors[row.id]}
                                                            </div>
                                                        </td>
                                                    </tr>
                                                )}
                                            </React.Fragment>
                                        );
                                    })}
                                </tbody>

                            </table>
                        </div>
                        <div className="col-md-12">
                            <Label name="Notes" bold />
                            <div className='mt-4'>
                                <RichTextEditor
                                    key={`${reportData?.template?.id}-${projectId}-${activeSection}`}
                                    id={`${reportData?.template?.id}-${projectId}-${activeSection}-remarks-input`}
                                    value={notes}
                                    readOnly={isOfflineProject}
                                    onChange={async (value) => {
                                        if (isOfflineProject) return;
                                        setNotes(value);
                                        try {
                                            await surveyReportService.updateSectionNotes({
                                                projectId: projectId,
                                                templateId: reportData.template.id,
                                                sectionId: activeSection,
                                                value: value
                                            });
                                        } catch (err) {
                                            handleSaveError();
                                        }
                                    }}
                                    placeholder="Enter Notes here..."
                                />
                            </div>
                        </div>
                    </div>
                    <Divider className="divider-1" />

                    <div className="row">
                        <div className="col-md-12">
                            <div className="section-title d-flex flex-wrap justify-content-between align-items-center gap-3">
                                <div className="d-flex flex-wrap align-items-baseline gap-2">
                                    <h3 className="form-group-title mb-0 text-nowrap">Upload Tank Images</h3>
                                    <span className="text-small text-nowrap">{uploadedCount} of {totalCount} tanks have images</span>
                                </div>
                                <div className="section-actions d-flex gap-3 align-items-center w-100 w-md-auto justify-content-between mt-2 mt-md-0">
                                    <div className="d-flex gap-1 p-1 rounded">
                                        <Switch
                                            links={['Grid', 'List']}
                                            onChange={(selected: string) => setViewMode(selected.toUpperCase() as "GRID" | "LIST")}
                                            selected={viewMode === 'GRID' ? 'Grid' : 'List'}
                                        />
                                    </div>
                                    <Button onClick={handleClick} disabled={isOfflineProject || isBulkUploading}>Bulk Update</Button>
                                </div>
                            </div>
                        </div>

                    </div>

                    <div className="row mb-2">
                        <div className="col-md-4 mb-2 mb-md-0">
                            <AbsSearch
                                handleClear={() => setTankSearchVal("")}
                                value={tankSearchVal}
                                handleChange={(e: any) => setTankSearchVal(e.target.value)}
                                handleSearch={() => {}}
                                placeholder="Search by Tank ID"
                                searchApplied={tankSearchVal !== ""}
                            />
                        </div>
                        <div className="col-md-8 d-flex justify-content-center justify-content-md-end align-items-center">
                            <div className="d-flex flex-wrap justify-content-center gap-2 p-1">
                                <span 
                                    role="button"
                                    className={`image-status-badge  ${filterType === 'ALL' ? 'is-selected' : ''}`} 
                                    onClick={() => setFilterType('ALL')}
                                >
                                    All - {totalCount}
                                </span>
                                <span 
                                    role="button"
                                    className={`image-status-badge with-image ${filterType === 'WITH_IMAGE' ? 'is-selected' : ''}`} 
                                    onClick={() => setFilterType('WITH_IMAGE')}
                                >
                                    With Image - {uploadedCount}
                                </span>
                                <span 
                                    role="button"
                                    className={`image-status-badge without-image ${filterType === 'MISSING' ? 'is-selected' : ''}`} 
                                    onClick={() => setFilterType('MISSING')}
                                >
                                    Missing - {totalCount - uploadedCount}
                                </span>
                            </div>
                        </div>
                    </div>

                    {(() => {
                        let filtered = filterType === "ALL" 
                            ? tanks 
                            : filterType === "WITH_IMAGE" 
                                ? tanks.filter(t => t.hasImage)
                                : tanks.filter(t => !t.hasImage);
                                
                        if (tankSearchVal) {
                            filtered = filtered.filter(t => t.id.toLowerCase().includes(tankSearchVal.toLowerCase()));
                        }
                        
                        if (viewMode === 'LIST') {
                            return (
                                <ReportContentListView 
                                    loading={loading}
                                    tanks={filtered}
                                    conditionOptions={conditionOptions}
                                    descriptionOptions={activeDescriptionOptions}
                                    isOfflineProject={isOfflineProject}
                                    onViewImage={(tankId) => {
                                        const index = slides.findIndex((s) => s.title === tankId);
                                        if (index >= 0) {
                                            setLightboxIndex(index);
                                            setLightboxOpen(true);
                                        }
                                    }}
                                    onDeleteImage={(tankId) => {
                                        const tk = tanks.find(t => t.id === tankId);
                                        if (tk) {
                                            confirmDeleteImage(tk);
                                        }
                                    }}
                                    onConditionChange={handleConditionChange}
                                    onDescriptionChange={handleDescriptionChange}
                                    onAdditionalDescriptionChange={handleAdditionalDescriptionChange}
                                    onImageUpload={handleImageUpload}
                                />
                            );
                        }

                        return (
                            <div className='tank-grid'  >
                                {filtered.map((tank) => (
                            <div
                                key={tank.id}
                                onClick={() => {
                                    if (!isOfflineProject) {
                                        toggleActive(tank.id);
                                    }
                                }}
                                className={getTankClass(tank)}
                            >
                                <div className="image-area">
                                    {tank.hasImage ? (
                                        <> <img src={tank.imageUrl} alt="Tank Image" />
                                            <div className="image-overlay">
                                                <button className="overlay-btn" title="Zoom" onClick={(e) => {
                                                    e.stopPropagation();
                                                    const idx = slides.findIndex(s => s.title === tank.id);
                                                    setLightboxIndex(idx);
                                                    setLightboxOpen(true);
                                                }}>
                                                    <Icon.ZoomIn size={20} />
                                                </button>
                                                {!isOfflineProject && (
                                                    <>
                                                        <button
                                                            className="overlay-btn"
                                                            title="Edit"
                                                            onClick={(e) => {

                                                                e.stopPropagation();

                                                                setActiveTank(tank);

                                                                setPanelOpen(true);

                                                                setTanks(prev =>
                                                                    prev.map(t => ({
                                                                        ...t,
                                                                        active: t.id === tank.id
                                                                    }))
                                                                );
                                                            }}
                                                        >
                                                            <Icon.Pencil size={20} />
                                                        </button>
                                                        <button
                                                            className="overlay-btn"
                                                            title="Remove"
                                                            onClick={(e) => {
                                                                e.stopPropagation();
                                                                confirmDeleteImage(tank);
                                                            }}
                                                        >
                                                            <Icon.Trash2 size={20} />
                                                        </button>
                                                    </>
                                                )}
                                            </div>
                                        </>
                                    ) : (
                                        <> <Icon.Image size={64} color={getIconColor(tank)} />
                                            {!isOfflineProject && (
                                                <div className="image-overlay">
                                                    <button
                                                        className="overlay-btn"
                                                        title="Add"
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            setActiveTank(tank);
                                                            setPanelOpen(true);
                                                            setTanks(prev =>
                                                                prev.map(t => ({
                                                                    ...t,
                                                                    active: t.id === tank.id
                                                                }))
                                                            );
                                                        }}
                                                    >
                                                        <Icon.Plus size={20} />
                                                    </button>
                                                </div>
                                            )}
                                        </>
                                    )}
                                </div>

                                {/* Footer */}
                                <div className="tank-footer p-2 w-100" style={{ overflow: 'hidden' }}>
                                    <div className='d-flex flex-column text-start small w-100'>
                                        <div className="d-flex align-items-center justify-content-between">
                                            <div className="tank-id fw-bold me-1 flex-shrink-0">
                                                {tank.id}
                                            </div>
                                            {tank.hasImage && (
                                                <div className="text-muted text-truncate" title={tank.selectedCondition || ((tank.currentCondition == 0 || tank.currentCondition == "0" || tank.currentCondition == false || tank.currentCondition == "false" || tank.currentCondition == null) ? "Current Condition" : "")}>
                                                    {tank.selectedCondition || ((tank.currentCondition == 0 || tank.currentCondition == "0" || tank.currentCondition == false || tank.currentCondition == "false" || tank.currentCondition == null) ? "Current Condition" : "")}
                                                </div>
                                            )}
                                        </div>
                                        {tank.hasImage && (
                                            <div className="text-muted text-truncate w-100" title={tank.descriptionText || ""}>
                                                {tank.descriptionText || ""}
                                            </div>
                                        )}
                                    </div>
                                </div>
                            </div>
                        ))}
                            </div>
                        );
                    })()}
                    <Lightbox
                        open={lightboxOpen}
                        close={() => setLightboxOpen(false)}
                        index={lightboxIndex}
                        slides={slides}
                        plugins={[Zoom, Thumbnails, Captions]}
                        zoom={{
                            maxZoomPixelRatio: 3,
                            zoomInMultiplier: 2,
                            doubleTapDelay: 300,
                        }}
                        thumbnails={{
                            position: "bottom",
                            width: 80,
                            height: 156,
                            border: 2,
                            borderRadius: 6,
                            gap: 8,
                        }}
                        captions={{
                            showToggle: true,
                            descriptionTextAlign: "center",
                        }}
                        carousel={{
                            finite: false,
                            preload: 2,
                        }}
                        styles={{
                            container: { backgroundColor: "rgba(0,0,0,0.85)" },
                        }}
                    />
                </div>
                {!isOfflineProject && (
                    <>
                        <button
                            onClick={togglePanel}
                            aria-label={panelOpen ? "Collapse upload panel" : "Expand upload panel"}
                            className={`collapse-btn ${panelOpen ? 'panel-open' : ''}`}
                        >
                            {panelOpen
                                ? <Icon.ChevronRight size={14} />
                                : <Icon.ChevronLeft size={14} />
                            }
                        </button>

                        <div className={`tank-form ${panelOpen ? 'panel-open' : ''}`}>
                            <UploadTankImage
                                activeTank={activeTank}
                                projectId={projectId}
                                tanks={tanks}
                                onNavigateTank={(tank: any) => {
                                    setActiveTank(tank);

                                    setTanks(prev =>
                                        prev.map(t => ({
                                            ...t,
                                            active: t.id === tank.id
                                        }))
                                    );
                                }}

                                onUploadSuccess={(data) => {
                                    setTanks(prev =>
                                        prev.map(t =>
                                            t.id === data.tankId
                                                ? {
                                                    ...t,
                                                    hasImage: true,
                                                    imageUrl: data.imageUrl,
                                                    descriptionId: data.descriptionId,
                                                    descriptionText: data.descriptionText,
                                                    additionalDescription: data.additionalDescription,
                                                    selectedCondition: data.selectedCondition,
                                                    selected: false,
                                                    active: false
                                                }
                                                : t
                                        )
                                    );
                                }}
                                onImageDeleted={(tankId) => {
                                    const clearedTank = {
                                        hasImage: false,
                                        imageUrl: null,
                                        imageId: 0
                                    };

                                    setTanks(prev =>
                                        prev.map(t =>
                                            t.id === tankId
                                                ? { ...t, ...clearedTank }
                                                : t
                                        )
                                    );

                                    setActiveTank(prev =>
                                        prev && prev.id === tankId
                                            ? { ...prev, ...clearedTank }
                                            : prev
                                    );
                                }}
                                descriptionoptions={activeDescriptionOptions}
                                conditionOptions={conditionOptions}
                                uploadedCount={uploadedCount}
                                totalCount={totalCount}
                                onClose={closePanel}
                                reportData={reportData}
                                isNewProject={isNewProject} />
                        </div>
                    </>
                )}
            </div>
        </div>



    );
};

export default ReportContent;