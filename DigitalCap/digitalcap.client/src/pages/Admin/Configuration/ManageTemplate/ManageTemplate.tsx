import React, { useState, useEffect, useMemo, useRef } from 'react';
import { BreadcrumbBar, Button, Tab, BUTTONS, Input, CheckboxInput, Label, AbsSelect, } from "customer_portal-ui-shared";
import * as Icon from 'react-bootstrap-icons'
import { DragDropContext, Droppable, Draggable } from "@hello-pangea/dnd";;
import GeneralSection from './GeneralSection';
import TankSection from './TankSection';
import * as reportConfigService from '../../../../service/ReportConfigService';
import * as gradingService from '../../../../service/GradingService';
import { usePopup } from '../../../../Common/PopupProvider';
import { resultMessage } from '../../../../Common/UtilityMessage';
import DropdownWithSearch from '../../../../Common/DropdownWithSearch';
import AppLoader from '../../../../Common/AppLoader';

type FormData = {
    vesselTypeId: number | "";
    partId: number | "";
};

const EXCLUDED_PART_NAMES = [
    "Vessel Main Data",
    "Part H&I",
    "Part H",
    "Part I",
    "VesselMainData"
] as const;

const DISALLOWED_PART_NAME_MESSAGE =
    "Given Part Name is not allowed,Should be avoid (Vessel Main data and PART H&I)!";

function normalizePartNameCompare(name: string): string {
    return name.trim().toLowerCase();
}

function isReservedPartName(rawName: string): boolean {
    const trimmed = rawName.trim();
    if (!trimmed) return false;
    const lower = normalizePartNameCompare(trimmed);
    if (EXCLUDED_PART_NAMES.some((ex) => ex.toLowerCase() === lower)) {
        return true;
    }
    const upper = trimmed.toUpperCase();
    return upper === "PART H" || upper === "PART I";
}

function sortPartsBySequence(partsList: any[]): any[] {
    return [...partsList].sort((a, b) => {
        const da = Number(a.sequenceNo) || 0;
        const db = Number(b.sequenceNo) || 0;
        if (da !== db) return da - db;
        return (Number(a.partId) || 0) - (Number(b.partId) || 0);
    });
}

const ManageTemplate: React.FC = () => {
    const popup = usePopup();
    const addNewSectionRef = useRef<(() => void) | null>(null);
    const tbodyRef = useRef<HTMLTableSectionElement | null>(null);
    const [formData, setFormData] = useState<FormData>({
        vesselTypeId: "",
        partId: ""
    });
    type SelectOption = {
        text: string;
        value: number | string;
    };
    const [parts, setParts] = useState<any[]>([]);
    const [normalSections, setNormalSections] = useState<any[]>([]);
    const [tankSections, setTankSections] = useState<any[]>([]);
    const [partOptions, setPartOptions] = useState<SelectOption[]>([]);
    const [activeTab, setActiveTab] = React.useState<number>(0);
    const [vesselOptions, setVesselOptions] = useState<SelectOption[]>([]);
    const [showAddRow, setShowAddRow] = useState(false);
    const [editPartId, setEditPartId] = useState<number | null>(null);
    const [editPart, setEditPart] = useState<any>(null);
    const [selectedPartName, setSelectedPartName] = useState<string>("");
    const [newPart, setNewPart] = useState({
        sequenceNo: "",
        partName: "",
        isActive: false
    });
    const [error, setError] = React.useState<boolean>(false);
    const [partNameError, setPartNameError] = useState<string>("");
    const [partIdError, setPartIdError] = useState<string>("");

    const handleTabClick = (tabIndex: number) => {
        setActiveTab(tabIndex);
    };

    useEffect(() => {
        gradingService.getVesselType().then(res => {
            setVesselOptions(
                res.data.data.map((v: any) => ({
                    text: v.vesselType,
                    value: v.id
                }))
            );
        });
    }, []);

    const [partsLoading, setPartsLoading] = useState<boolean>(false);
    const [sectionsLoading, setSectionsLoading] = useState<boolean>(false);
    const partsRequestIdRef = useRef<number>(0);
    const sectionsRequestIdRef = useRef<number>(0);

    const onVesselTypeChange = async (vesselTypeId: number, silent: boolean = false) => {
        if (!vesselTypeId) return;

        const currentRequestId = ++partsRequestIdRef.current;
        if (!silent) {
            setParts([]);
            setShowAddRow(false);
            setPartsLoading(true);
        }

        try {
            const res = await reportConfigService.getReportPartsByVesselType(vesselTypeId);
            if (partsRequestIdRef.current !== currentRequestId) return;
            const sorted = sortPartsBySequence(res.data ?? []);

            setParts(sorted);
            const partOptions = sorted.map((p: any) => ({
                text: p.partName,
                value: p.partId
            }));

            setPartOptions(partOptions);

        } catch (err) {
        } finally {
            if (!silent && partsRequestIdRef.current === currentRequestId) {
                setPartsLoading(false);
            }
        }
    };
    const orderedParts = useMemo(() => sortPartsBySequence(parts), [parts]);

    const saveNewPart = async () => {
        if (!formData.vesselTypeId) {
            setError(true);
            return;
        }
        if (!newPart.partName || newPart.partName.trim() === "") {
            setPartNameError("Part Name is Required");
            return;
        }

        const nameNorm = normalizePartNameCompare(newPart.partName);
        const isDuplicate = orderedParts.some((p) => normalizePartNameCompare(p.partName ?? "") === nameNorm);
        if (isDuplicate) {
            setPartNameError("Duplicate Part Name is not allowed");
            return;
        }

        if (isReservedPartName(newPart.partName)) {
            setPartNameError(DISALLOWED_PART_NAME_MESSAGE);
            return;
        }

        setPartNameError("");

        const payload = {
            reportParts: [{
                sequenceNo: orderedParts.length + 1,
                vesselTypeId: formData.vesselTypeId,
                partName: newPart.partName,
                isActive: newPart.isActive,
                vesselTypePartMappingId: 0
            }]
        };

        const savedPartName = newPart.partName;
        try {
            await reportConfigService.saveReportTemplateConfig(
                Number(formData.vesselTypeId),
                payload as any
            );

            setShowAddRow(false);
            setNewPart({ sequenceNo: "", partName: "", isActive: true });
            setPartNameError("");
            onVesselTypeChange(Number(formData.vesselTypeId), true);
            popup.success({
                title: "Create Part",
                message: resultMessage(savedPartName || "Part", "Created")
            });
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to create part." });
        }
    };

    const updatePart = async () => {
        if (!editPart) return;
        if (!editPart.partName || editPart.partName.trim() === "") {
            setPartNameError("Part Name is Required");
            return;
        }

        const editNorm = normalizePartNameCompare(editPart.partName);
        const isDuplicate = orderedParts.some(
            (p) =>
                p.partId !== editPart.partId &&
                normalizePartNameCompare(p.partName ?? "") === editNorm
        );
        if (isDuplicate) {
            setPartNameError("Duplicate Part Name is not allowed");
            return;
        }

        if (isReservedPartName(editPart.partName)) {
            setPartNameError(DISALLOWED_PART_NAME_MESSAGE);
            return;
        }

        setPartNameError("");

        const seqIndex = orderedParts.findIndex((p) => p.partId === editPart.partId);
        const sequenceNo = seqIndex >= 0 ? seqIndex + 1 : Number(editPart.sequenceNo);

        const payload = {
            reportParts: [{
                vesselTypePartMappingId: editPart.partId,
                vesselTypeId: Number(formData.vesselTypeId),
                partName: editPart.partName,
                sequenceNo,
                isActive: editPart.isActive,
                isDeleted: false
            }]
        };

        const updatedPartName = editPart.partName;
        try {
            await reportConfigService.saveReportTemplateConfig(
                Number(formData.vesselTypeId),
                payload
            );

            setEditPartId(null);
            setPartNameError("");
            onVesselTypeChange(Number(formData.vesselTypeId), true);
            popup.success({
                title: "Edit Part",
                message: resultMessage(updatedPartName || "Part", "Updated")
            });
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to update part." });
        }
    };

    const handleDeletePart = async (part: any) => {
        if (!part || !formData.vesselTypeId) return;

        const confirmed = await popup.confirm({
            title: "Delete Part",
            message: `Are you sure you want to delete ${part?.partName} ?`,
            confirmText: "Delete",
            danger: true
        });
        if (!confirmed) return;

        const deletedId = part.partId;
        const remaining = orderedParts.filter((p) => p.partId !== deletedId);

        const reportParts = remaining.map((p: any, index: number) => ({
            vesselTypePartMappingId: p.partId,
            vesselTypeId: Number(formData.vesselTypeId),
            partName: p.partName,
            sequenceNo: index + 1,
            isActive: !!p.isActive,
            isDeleted: false
        }));

        reportParts.push({
            vesselTypePartMappingId: deletedId,
            vesselTypeId: Number(formData.vesselTypeId),
            partName: part.partName,
            sequenceNo: Number(part.sequenceNo),
            isActive: part.isActive,
            isDeleted: true
        });

        const payload = { reportParts };

        try {
            await reportConfigService.saveReportTemplateConfig(
                Number(formData.vesselTypeId),
                payload
            );

            onVesselTypeChange(Number(formData.vesselTypeId), true);
            popup.success({
                title: "Delete Part",
                message: resultMessage(part.partName || "Part", "Deleted")
            });
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to delete part." });
        }
    };

    const onPartChange = async (partId: number) => {
        setFormData(prev => ({ ...prev, partId }));
        const currentRequestId = ++sectionsRequestIdRef.current;
        setSectionsLoading(true);

        try {
            const res =
                await reportConfigService.getSectionNamesByPartId(
                    Number(formData.vesselTypeId),
                    partId
                );
            if (sectionsRequestIdRef.current !== currentRequestId) return;

            const mappedSections = (res.data.normalSections ?? []).map((s: any) => ({
                id: s.sectionId,
                section_name: s.sectionName,
                subheader_name: s.subHeader ?? "",
                current_count: Number(s.placeholderCount),
                selectable_count: Number(s.fileNameCount),
                total_conditions: Number(
                    s.totalCards ?? Number(s.placeholderCount) + Number(s.fileNameCount)
                )
            }));

            setNormalSections(mappedSections);

            setTankSections(res.data.tanksSections ?? []);
        } catch (err) {
        } finally {
            if (sectionsRequestIdRef.current === currentRequestId) {
                setSectionsLoading(false);
            }
        }
    };

    const refreshTankSections = async () => {
        if (!formData.vesselTypeId || !formData.partId) return;

        try {
            const res =
                await reportConfigService.getSectionNamesByPartId(
                    Number(formData.vesselTypeId),
                    Number(formData.partId)
                );

            setTankSections(res.data.tanksSections ?? []);


            const mapped = (res.data.normalSections ?? []).map((s: any) => ({
                id: s.sectionId,
                section_name: s.sectionName,
                subheader_name: s.subHeader ?? "",
                current_count: Number(s.placeholderCount),
                selectable_count: Number(s.fileNameCount),
                total_conditions: Number(
                    s.totalCards ?? Number(s.placeholderCount) + Number(s.fileNameCount)
                )
            }));

            setNormalSections(mapped);

        } catch (err) {
        }
    };
    const handleDragEnd = async (result: any) => {
        if (!result.destination) return;
        if (editPartId !== null || showAddRow) return;

        const items = Array.from(orderedParts);
        const [moved] = items.splice(result.source.index, 1);
        items.splice(result.destination.index, 0, moved);

        const updated = items.map((item, index) => ({
            ...item,
            sequenceNo: index + 1
        }));

        setParts(updated);

        const payload = {
            reportParts: updated.map((item) => ({
                vesselTypePartMappingId: item.partId,
                vesselTypeId: Number(formData.vesselTypeId),
                partName: item.partName,
                sequenceNo: item.sequenceNo,
                isActive: item.isActive,
                isDeleted: false
            }))
        };

        try {
            await reportConfigService.saveReportTemplateConfig(
                Number(formData.vesselTypeId),
                payload
            );

            onVesselTypeChange(Number(formData.vesselTypeId), true);
        } catch (err: any) {
            popup.error(err?.response?.data || err?.message || "Unable to save the section order. Please try again.", "Manage Template");
        }
    };

    return (
        <>
            <div className="page-title">
                <BreadcrumbBar
                    pageName="Manage Template"
                    parentPages={[
                        {
                            name: "Project Configuration",
                            link: ""
                        }
                    ]}>
                </BreadcrumbBar>
            </div>
            <div className="page-content p-4">
                <div className="content-body">
                    <div className='section-header'>
                        <h5 className='form-title'>Part  Configuration</h5>
                        {/* <Button variant={BUTTONS.PRIMARY} onClick={() => { "" }}>Add New Section</Button> */}
                    </div>
                    <div className="row">
                        <div className="col-md-4 form-group">
                            <Label name="Vessel Type" bold />
                            <AbsSelect
                                mult={false}
                                options={vesselOptions}
                                selected={
                                    formData.vesselTypeId
                                        ? [String(formData.vesselTypeId)]
                                        : []
                                }
                                onChange={(selected: any) => {
                                    if (!selected || !selected.length) return;

                                    const id = Number(selected[0]);
                                    if (isNaN(id)) return;

                                    setFormData(prev => ({ ...prev, vesselTypeId: id }));
                                    onVesselTypeChange(id);
                                }}
                                placeholder="Select a Vessel Type"
                            />
                            {(error && (!formData.vesselTypeId)) ?
                                <div><label style={{ color: 'red' }}>Please Select VesselType</label></div> :
                                <></>
                            }
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-md-6">
                            <div className="card mb-3">
                                <div className="card-header d-flex justify-content-between align-items-center">
                                    <h5 className='form-title mb-0'>Manage Parts</h5>
                                    <Button
                                        variant={BUTTONS.PRIMARY}
                                        disabled={!formData.vesselTypeId}
                                        onClick={() => {
                                            setNewPart({
                                                sequenceNo: "1",
                                                partName: "",
                                                isActive: false
                                            });
                                            setShowAddRow(true);
                                            setEditPartId(null);
                                            setPartNameError("");
                                        }}                                    >
                                        Add New Part
                                    </Button>
                                </div>
                                <div className="card-body">

                                    <div className="form-area">
                                        {partsLoading ? (
                                            <AppLoader inline />
                                        ) : (
                                            <DragDropContext onDragEnd={handleDragEnd}>
                                            <Droppable droppableId="parts">
                                                {(provided) => (
                                                    <table className='table table-bordered custom-table'>

                                                        <thead>
                                                            <tr>
                                                                <th style={{ width: "50px", minWidth: "50px" }}></th> {/* drag */}
                                                                <th style={{ width: "150px", minWidth: "150px" }}>Sequence Number</th>
                                                                <th>Part Name</th>
                                                                <th style={{ width: "100px", minWidth: "100px" }}>Active</th>
                                                                <th style={{ width: "120px", minWidth: "120px" }}>Actions</th>
                                                            </tr>
                                                        </thead>

                                                        <tbody
                                                            ref={(el) => {
                                                                provided.innerRef(el);
                                                                tbodyRef.current = el;
                                                            }}
                                                            {...provided.droppableProps}
                                                        >

                                                            {showAddRow && (
                                                                <tr>
                                                                    <td></td>
                                                                    <td style={{ width: "150px", minWidth: "150px" }}></td>
                                                                    <td>
                                                                        <Input
                                                                            value={newPart.partName}
                                                                            placeholder="Enter Part Name"
                                                                            onChange={(v: string) => {
                                                                                setNewPart(p => ({ ...p, partName: v }));
                                                                                if (partNameError) setPartNameError("");
                                                                            }}
                                                                        />
                                                                        {partNameError && showAddRow && (
                                                                            <span style={{ color: 'red', fontSize: '12px' }}>{partNameError}</span>
                                                                        )}
                                                                    </td>
                                                                    <td style={{ width: "100px", minWidth: "100px" }}>
                                                                        <CheckboxInput
                                                                            label=" "
                                                                            checked={newPart.isActive}
                                                                            onChange={(v: boolean) =>
                                                                                setNewPart(p => ({ ...p, isActive: v }))
                                                                            }
                                                                        />
                                                                    </td>
                                                                    <td className="d-flex gap-2 flex-nowrap" style={{ width: "120px", minWidth: "120px" }}>
                                                                        <Button variant={BUTTONS.PRIMARY} onClick={saveNewPart} startIcon={<Icon.Check />} />
                                                                        <Button variant={BUTTONS.SECONDARY} onClick={() => { setShowAddRow(false); setPartNameError(""); }} startIcon={<Icon.X />} />
                                                                    </td>
                                                                </tr>
                                                            )}

                                                            {/* DRAGGABLE ROWS */}
                                                            {!showAddRow && orderedParts.length === 0 && (
                                                                <tr>
                                                                    <td colSpan={5} className="text-center">
                                                                        No Records Found
                                                                    </td>
                                                                </tr>
                                                            )}

                                                            {/* DRAGGABLE ROWS */}
                                                            {orderedParts.map((part, index) => (
                                                                <Draggable key={part.partId} draggableId={String(part.partId)} index={index}>
                                                                    {(provided, snapshot) => (

                                                                        editPartId === part.partId ? (
                                                                            <tr ref={provided.innerRef}>
                                                                                <td></td>
                                                                                <td>{index + 1}</td>
                                                                                <td>
                                                                                    <Input
                                                                                        value={editPart.partName}
                                                                                        placeholder="Enter Part Name"
                                                                                        onChange={(v: string) => {
                                                                                            setEditPart({ ...editPart, partName: v });
                                                                                            if (partNameError) setPartNameError("");
                                                                                        }}
                                                                                    />
                                                                                    {partNameError && editPartId === part.partId && (
                                                                                        <span style={{ color: 'red', fontSize: '12px' }}>{partNameError}</span>
                                                                                    )}
                                                                                </td>
                                                                                <td>
                                                                                    <CheckboxInput
                                                                                        label=" "
                                                                                        checked={editPart.isActive}
                                                                                        onChange={(v: boolean) =>
                                                                                            setEditPart({ ...editPart, isActive: v })
                                                                                        }
                                                                                    />
                                                                                </td>

                                                                                <td className="d-flex gap-2 flex-nowrap">
                                                                                    <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={updatePart} startIcon={<Icon.Check />} />
                                                                                    <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={() => { setEditPartId(null); setPartNameError(""); }} startIcon={<Icon.X />} />
                                                                                </td>
                                                                            </tr>
                                                                        ) : (
                                                                            <tr
                                                                                ref={provided.innerRef}
                                                                                {...provided.draggableProps}
                                                                                style={{
                                                                                    ...provided.draggableProps.style,
                                                                                    display: snapshot.isDragging ? "flex" : "",
                                                                                    ...(snapshot.isDragging && provided.draggableProps.style?.transform
                                                                                        ? {
                                                                                            transform: (() => {
                                                                                                const t = provided.draggableProps.style.transform;
                                                                                                if (!t) return "";
                                                                                                const parts = t.split(',');
                                                                                                if (parts.length > 1) {
                                                                                                    let y = parseFloat(parts[1]);
                                                                                                    if (tbodyRef.current) {
                                                                                                        const tbodyRect = tbodyRef.current.getBoundingClientRect();
                                                                                                        const s = provided.draggableProps.style as any;
                                                                                                        const itemTop = parseFloat(s.top);
                                                                                                        const itemHeight = parseFloat(s.height);
                                                                                                        if (!isNaN(itemTop) && !isNaN(itemHeight)) {
                                                                                                            const maxUp = tbodyRect.top - itemTop;
                                                                                                            const maxDown = tbodyRect.bottom - (itemTop + itemHeight);
                                                                                                            if (y < maxUp) y = maxUp;
                                                                                                            if (y > maxDown) y = maxDown;
                                                                                                        }
                                                                                                    }
                                                                                                    return `translate(0px, ${y}px)`;
                                                                                                }
                                                                                                return t;
                                                                                            })()
                                                                                        }
                                                                                        : {}),
                                                                                }}
                                                                            >

                                                                                {/* DRAG HANDLE */}
                                                                                <td {...provided.dragHandleProps} style={{ cursor: "grab", width: "50px", minWidth: "50px" }}>
                                                                                    <Icon.List />
                                                                                </td>

                                                                                <td style={{ width: "150px", minWidth: "150px" }}>{index + 1}</td>
                                                                                <td style={{ flex: snapshot.isDragging ? 1 : "auto" }}>{part.partName}</td>
                                                                                <td style={{ width: "100px", minWidth: "100px" }}>
                                                                                    <CheckboxInput label=" " checked={part.isActive} />
                                                                                </td>

                                                                                <td className="d-flex gap-2 flex-nowrap" style={{ width: "120px", minWidth: "120px" }}>
                                                                                    <Button
                                                                                        variant={BUTTONS.TABLEDOWNLOAD}
                                                                                        startIcon={<Icon.PencilFill />}
                                                                                        onClick={() => {
                                                                                            setEditPartId(part.partId);
                                                                                            setEditPart({ ...part });
                                                                                            setShowAddRow(false);
                                                                                            setPartNameError("");
                                                                                        }}
                                                                                    />
                                                                                    <Button
                                                                                        variant={BUTTONS.TABLEDOWNLOAD}
                                                                                        startIcon={<Icon.TrashFill />}
                                                                                        onClick={() => handleDeletePart(part)}
                                                                                    />
                                                                                </td>
                                                                            </tr>
                                                                        )
                                                                    )}
                                                                </Draggable>
                                                            ))}

                                                            {provided.placeholder}
                                                        </tbody>
                                                    </table>
                                                )}
                                            </Droppable>
                                        </DragDropContext>
                                        )}
                                    </div>
                                </div>
                            </div>

                        </div>
                    </div>
                </div>
                <div className="content-body">
                    <div className='section-header'>
                        <h5 className='form-title'>Manage Sections</h5>

                    </div>
                    <div className="row">

                        <div className="col-md-4 form-group">
                            <Label name="Select Part" bold />
                            <DropdownWithSearch
                                isMulti={false}
                                isSearchable={false}
                                placeholder="Select a Part"
                                options={partOptions.map((p: any) => ({ label: p.text, value: String(p.value) }))}
                                selectedValue={
                                    formData.partId
                                        ? (() => {
                                            const part = partOptions.find(p => Number(p.value) === formData.partId);
                                            return part ? [{ label: part.text, value: String(formData.partId) }] : [];
                                        })()
                                        : []
                                }
                                onSelect={(selected: any[]) => {
                                    if (!selected || !selected.length) return;

                                    const partId = Number(selected[0].value);

                                    setSelectedPartName(selected[0].label);
                                    setPartIdError("");

                                    onPartChange(partId)
                                }}
                            />
                            {partIdError && (
                                <div><label style={{ color: 'red' }}>{partIdError}</label></div>
                            )}
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-md-8">
                            <div className="card mb-3">
                                <div className="card-header d-flex justify-content-between align-items-center">
                                    <h5 className='form-title mb-0'> {selectedPartName}</h5>
                                    {activeTab === 0 && (
                                        <Button
                                            variant={BUTTONS.PRIMARY}
                                            disabled={!formData.partId}
                                            onClick={() => {
                                                if (addNewSectionRef.current) {
                                                    addNewSectionRef.current();
                                                }
                                            }}
                                        >
                                            Add New Section
                                        </Button>
                                    )}
                                </div>
                                <div className="card-body">
                                    {sectionsLoading ? (
                                        <AppLoader inline />
                                    ) : (
                                        <Tab
                                        activeTab={activeTab}
                                        onTabChange={handleTabClick}
                                        tabData={[
                                            {
                                                content: (
                                                    <>
                                                        <GeneralSection
                                                            generalSections={normalSections}
                                                            vesselTypeId={Number(formData.vesselTypeId)}
                                                            partId={Number(formData.partId)}
                                                            partName={selectedPartName}
                                                            onRefresh={refreshTankSections}
                                                            onAddNewSection={addNewSectionRef}
                                                            onPartIdError={setPartIdError}
                                                            error={error}
                                                            setError={setError}
                                                        />

                                                    </>
                                                ),
                                                header: 'General '
                                            },
                                            {
                                                content: (
                                                    <>
                                                        <TankSection tankSections={tankSections}
                                                            vesselTypeId={Number(formData.vesselTypeId)}
                                                            selectedPartId={Number(formData.partId)}
                                                            selectedPartName={selectedPartName}
                                                            onRefresh={refreshTankSections}
                                                        />
                                                    </>
                                                ),
                                                header: 'Tank Section'
                                            }
                                        ]}
                                    />
                                    )}
                                </div>
                            </div>
                        </div>
                    </div>

                </div>
            </div>
        </>
    );
};

export default ManageTemplate;