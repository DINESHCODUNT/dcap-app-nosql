import React, { useEffect, useState } from "react";
import { Button, BUTTONS, Input } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import * as reportConfigService from "../../../../service/ReportConfigService";
import { usePopup } from '../../../../Common/PopupProvider';
import { resultMessage } from '../../../../Common/UtilityMessage';

type Section = {
    id: number;
    section_name: string;
    subheader_name: string;
    current_count: number | string;
    selectable_count: number | string;
    total_conditions?: number | string;
};

type Props = {
    generalSections?: Section[];
    vesselTypeId: number;
    partId: number;
    partName: string;
    onRefresh: () => void;
    onAddNewSection?: React.MutableRefObject<(() => void) | null>;
    onPartIdError?: (error: string) => void;
    error?: boolean;
    setError?: React.Dispatch<
        React.SetStateAction<boolean>
    >;
};
const EMPTY_FORM: Section = {
    id: 0,
    section_name: "",
    subheader_name: "",
    current_count: 0,
    selectable_count: 0,
    total_conditions: 0
};

function newSectionTotalDisplay(current: number | string, selectable: number | string): number {
    return Number(current) + Number(selectable);
}

const GeneralSection: React.FC<Props> = ({
    generalSections = [],
    vesselTypeId,
    partId,
    partName,
    onRefresh,
    onAddNewSection,
    onPartIdError,
    setError
}) => {
    const popup = usePopup();
    const [sections, setSections] = useState<Section[]>([]);
    const [editId, setEditId] = useState<number | null>(null);
    const [formData, setFormData] = useState<Section>(EMPTY_FORM);
    const [sectionNameError, setSectionNameError] = useState<string>("");
    const [currentCountError, setCurrentCountError] = useState<string>("");

    const [pageSize, setPageSize] = useState(5);
    const [currentPage, setCurrentPage] = useState(1);

    const totalPages = Math.ceil(sections.length / pageSize);
    const paginatedSections = sections.slice(
        (currentPage - 1) * pageSize,
        currentPage * pageSize
    );
    useEffect(() => {
        setSections(generalSections);
        setEditId(null);
        setFormData(EMPTY_FORM);
    }, [generalSections]);

    useEffect(() => {
        if (onAddNewSection) {
            onAddNewSection.current = () => {
                setEditId(0);
                setFormData(EMPTY_FORM);
            };
        }
        return () => {
            if (onAddNewSection) {
                onAddNewSection.current = null;
            }
        };
    }, [onAddNewSection]);

    const saveSection = async () => {
        setSectionNameError("");
        setCurrentCountError("");

        if (!vesselTypeId || vesselTypeId === 0) {
            if (setError) setError(true);
            return;
        }

        if (!partId || partId === 0) {
            if (onPartIdError) {
                onPartIdError("Please Select Part");
            }
            return;
        }

        if (onPartIdError) {
            onPartIdError("");
        }

        if (!formData.section_name || formData.section_name.trim() === "") {
            setSectionNameError("Section Name Required");
            return;
        }

        const isDuplicate = sections.some(
            sec =>
                sec.section_name.toLowerCase().trim() === formData.section_name.toLowerCase().trim() &&
                sec.id !== editId
        );

        if (isDuplicate) {
            setSectionNameError("Section Name already exists");
            return;
        }


        if (Number(formData.current_count) === 0) {
            setCurrentCountError("Count is not a valid");
            return;
        }

        if (!vesselTypeId || !partId) return;

        const totalCardsForSave =
            Number(formData.current_count) +
            Number(formData.selectable_count);

        const payload = {
            reportParts: [],
            tankSectionMappings: [],
            normalSectionMappings: [
                {
                    vesselTypeNormalSectionMappingId:
                        editId === 0 ? null : editId,

                    vesselTypeId: Number(vesselTypeId),
                    vesselTypePartMappingId: Number(partId),
                    partName: partName,

                    sectionName: formData.section_name,
                    subHeader: formData.subheader_name,

                    placeholderCount: Number(formData.current_count),
                    fileNameCount: Number(formData.selectable_count),
                    totalCards: totalCardsForSave,

                    isActive: true,
                    isDeleted: false
                }
            ]
        };

        const savedName = formData.section_name;
        const isCreate = editId === 0;

        try {
            await reportConfigService.saveReportTemplateConfig(
                Number(vesselTypeId),
                payload as any
            );

            setEditId(null);
            setFormData(EMPTY_FORM);
            setSectionNameError("");
            setCurrentCountError("");
            onRefresh();
            popup.success({
                title: isCreate ? "Create Section" : "Edit Section",
                message: resultMessage(savedName || "Section", isCreate ? "Created" : "Updated")
            });
        } catch (err: any) {
            popup.error({ message: err?.message || `Failed to ${isCreate ? 'create' : 'update'} section.` });
        }
    };

    const onEdit = (sec: Section) => {
        setEditId(sec.id);
        setFormData({
            ...sec,
            total_conditions:
                sec.total_conditions ??
                newSectionTotalDisplay(sec.current_count, sec.selectable_count)
        });
    };

    const onCancel = () => {
        setEditId(null);
        setFormData(EMPTY_FORM);
        setSectionNameError("");
        setCurrentCountError("");
    };

    const handleDeleteSection = async (sec: Section) => {
        if (!sec || !vesselTypeId || !partId) return;

        const confirmed = await popup.confirm({
            title: "Delete Section",
            message: `Are you sure you want to delete section ${sec.section_name}?`,
            confirmText: "Delete",
            danger: true
        });
        if (!confirmed) return;

        const payload = {
            reportParts: [],
            tankSectionMappings: [],
            normalSectionMappings: [
                {
                    vesselTypeNormalSectionMappingId: sec.id,
                    vesselTypeId: Number(vesselTypeId),
                    vesselTypePartMappingId: Number(partId),
                    partName: partName,

                    sectionName: sec.section_name,
                    subHeader: sec.subheader_name,

                    placeholderCount: Number(sec.current_count),
                    fileNameCount: Number(sec.selectable_count),
                    totalCards: Number(
                        sec.total_conditions ??
                        newSectionTotalDisplay(
                            sec.current_count,
                            sec.selectable_count
                        )
                    ),

                    isActive: true,
                    isDeleted: true
                }
            ]
        };

        try {
            await reportConfigService.saveReportTemplateConfig(
                Number(vesselTypeId),
                payload as any
            );
            onRefresh();
            popup.success({
                title: "Delete Section",
                message: resultMessage(sec.section_name || "Section", "Deleted")
            });
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to delete section." });
        }
    };



    return (
        <>
            <div className="form-area">
                <table className="table table-bordered custom-table">
                    <thead>
                        <tr>
                            <th>Section Name</th>
                            <th>Subheading</th>
                            <th>Total Conditions</th>
                            <th>Current Count</th>
                            <th>Selectable Count</th>
                            <th>Actions</th>
                        </tr>
                    </thead>

                    <tbody>
                        {sections.length === 0 && editId !== 0 && (
                            <tr>
                                <td colSpan={6} className="text-center">
                                    No Records Found
                                </td>
                            </tr>
                        )}
                        {editId === 0 && (
                            <tr>
                                <td>
                                    <Input
                                        autoFocus={true}
                                        value={formData.section_name}
                                        placeholder="Enter Section Name"
                                        onChange={(v: string) => {
                                            setFormData({
                                                ...formData,
                                                section_name: v
                                            });
                                            if (sectionNameError) {
                                                setSectionNameError("");
                                            }
                                        }}
                                        maxLength={55}
                                    />
                                    {sectionNameError && (
                                        <div><label style={{ color: 'red' }}>{sectionNameError}</label></div>
                                    )}
                                </td>

                                <td>
                                    <Input
                                        autoFocus={false}
                                        value={formData.subheader_name}
                                        placeholder="Enter Subheading"
                                        onChange={(v: string) =>
                                            setFormData({
                                                ...formData,
                                                subheader_name: v
                                            })
                                        }
                                    />
                                </td>

                                <td>
                                    {Number(formData.current_count || 0) + Number(formData.selectable_count || 0)}
                                </td>

                                <td>
                                    <input
                                        type="number"
                                        min={0}
                                        step={1}
                                        className="form-control"
                                        value={formData.current_count}
                                        onChange={e => {
                                            const val = e.target.value;
                                            setFormData({
                                                ...formData,
                                                current_count: val === '' ? '' : Number(val)
                                            });
                                            if (currentCountError) {
                                                setCurrentCountError("");
                                            }
                                        }}
                                    />
                                    {currentCountError && (
                                        <div><label style={{ color: 'red' }}>{currentCountError}</label></div>
                                    )}
                                </td>

                                <td>
                                    <input
                                        type="number"
                                        min={0}
                                        step={1}
                                        className="form-control"
                                        value={formData.selectable_count}
                                        onChange={e => {
                                            const val = e.target.value;
                                            setFormData({
                                                ...formData,
                                                selectable_count: val === '' ? '' : Number(val)
                                            });
                                        }}
                                    />
                                </td>

                                <td className="d-flex gap-2">
                                    <Button
                                        variant={BUTTONS.PRIMARY}
                                        onClick={saveSection}
                                    >
                                        Save
                                    </Button>
                                    <Button
                                        variant={BUTTONS.SECONDARY}
                                        onClick={onCancel}
                                    >
                                        Cancel
                                    </Button>
                                </td>
                            </tr>
                        )}

                        {paginatedSections.map(sec =>
                            editId === sec.id ? (
                                <tr key={sec.id}>
                                    <td>
                                        <Input
                                            autoFocus={true}
                                            value={formData.section_name}
                                            onChange={(v: string) => {
                                                setFormData({
                                                    ...formData,
                                                    section_name: v
                                                });
                                                if (sectionNameError) {
                                                    setSectionNameError("");
                                                }
                                            }}
                                        />
                                        {sectionNameError && (
                                            <div><label style={{ color: 'red' }}>{sectionNameError}</label></div>
                                        )}
                                    </td>

                                    <td>
                                        <Input
                                            value={formData.subheader_name}
                                            onChange={(v: string) =>
                                                setFormData({
                                                    ...formData,
                                                    subheader_name: v
                                                })
                                            }
                                        />
                                    </td>

                                    <td>
                                        {Number(formData.current_count || 0) + Number(formData.selectable_count || 0)}
                                    </td>

                                    <td>
                                        <input
                                            type="number"
                                            min={0}
                                            step={1}
                                            className="form-control"
                                            value={formData.current_count}
                                            onChange={e => {
                                                const val = e.target.value;
                                                setFormData({
                                                    ...formData,
                                                    current_count: val === '' ? '' : Number(val)
                                                });
                                                if (currentCountError) {
                                                    setCurrentCountError("");
                                                }
                                            }}
                                        />
                                        {currentCountError && (
                                            <div><label style={{ color: 'red' }}>{currentCountError}</label></div>
                                        )}
                                    </td>

                                    <td>
                                        <input
                                            type="number"
                                            min={0}
                                            step={1}
                                            className="form-control"
                                            value={formData.selectable_count}
                                            onChange={e => {
                                                const val = e.target.value;
                                                setFormData({
                                                    ...formData,
                                                    selectable_count: val === '' ? '' : Number(val)
                                                });
                                            }}
                                        />
                                    </td>

                                    <td className="d-flex gap-2">
                                        <Button
                                            variant={BUTTONS.TABLEDOWNLOAD}
                                            onClick={saveSection}
                                            startIcon={
                                                <Icon.Check />
                                            }
                                        >
                                        </Button>
                                        <Button
                                            variant={BUTTONS.TABLEDOWNLOAD}
                                            onClick={onCancel}
                                            startIcon={
                                                <Icon.X />
                                            }
                                        >
                                        </Button>
                                    </td>
                                </tr>
                            ) : (
                                <tr key={sec.id}>
                                    <td>{sec.section_name}</td>
                                    <td>{sec.subheader_name}</td>
                                    <td>
                                        {Number(
                                            sec.total_conditions ??
                                            newSectionTotalDisplay(
                                                sec.current_count,
                                                sec.selectable_count
                                            )
                                        )}
                                    </td>
                                    <td>{sec.current_count}</td>
                                    <td>{sec.selectable_count}</td>
                                    <td className="d-flex gap-2">
                                        <Button variant={BUTTONS.TABLEDOWNLOAD}
                                            startIcon={
                                                <Icon.PencilFill />
                                            }
                                            onClick={() => onEdit(sec)}
                                        />
                                        <Button variant={BUTTONS.TABLEDOWNLOAD}
                                            startIcon={
                                                <Icon.TrashFill />
                                            }
                                            onClick={() => handleDeleteSection(sec)}
                                        />
                                    </td>
                                </tr>
                            )
                        )}
                    </tbody>
                </table>
            </div>

            {partId > 0 && sections.length > 0 && (
                <div className="d-flex justify-content-center align-items-center mt-3 gap-3 flex-wrap" style={{ fontSize: '14px', color: '#00335b', fontWeight: '500' }}>
                    <div className="d-flex align-items-center gap-2">
                        <span>Items per page:</span>
                        <select
                            className="form-select form-select-sm"
                            style={{ width: '70px', backgroundColor: '#f4f5f6', border: 'none', fontWeight: 'bold', color: '#00335b', cursor: 'pointer' }}
                            value={pageSize}
                            onChange={(e) => {
                                setPageSize(Number(e.target.value));
                                setCurrentPage(1);
                            }}
                        >
                            <option value={5}>5</option>
                            <option value={10}>10</option>
                            <option value={15}>15</option>
                            <option value={20}>20</option>
                        </select>
                    </div>

                    <div className="d-flex align-items-center gap-3">
                        <Icon.ChevronBarLeft
                            size={20}
                            style={{ cursor: currentPage > 1 ? 'pointer' : 'not-allowed', opacity: currentPage > 1 ? 1 : 0.4 }}
                            onClick={() => currentPage > 1 && setCurrentPage(1)}
                        />
                        <Icon.ChevronLeft
                            size={20}
                            style={{ cursor: currentPage > 1 ? 'pointer' : 'not-allowed', opacity: currentPage > 1 ? 1 : 0.4 }}
                            onClick={() => currentPage > 1 && setCurrentPage(prev => prev - 1)}
                        />

                        <span style={{ minWidth: '80px', textAlign: 'center', fontWeight: '400' }}>
                            {sections.length === 0 ? 0 : (currentPage - 1) * pageSize + 1}-
                            {Math.min(currentPage * pageSize, sections.length)} of {sections.length}
                        </span>

                        <Icon.ChevronRight
                            size={20}
                            style={{ cursor: currentPage < totalPages ? 'pointer' : 'not-allowed', opacity: currentPage < totalPages ? 1 : 0.4 }}
                            onClick={() => currentPage < totalPages && setCurrentPage(prev => prev + 1)}
                        />
                        <Icon.ChevronBarRight
                            size={20}
                            style={{ cursor: currentPage < totalPages ? 'pointer' : 'not-allowed', opacity: currentPage < totalPages ? 1 : 0.4 }}
                            onClick={() => currentPage < totalPages && setCurrentPage(totalPages)}
                        />
                    </div>
                </div>
            )}
        </>
    );
};

export default GeneralSection;
