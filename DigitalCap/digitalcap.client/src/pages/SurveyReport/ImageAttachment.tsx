import React, { useState, useRef } from 'react'
import Dropzone from 'react-dropzone'
import { AbsReactTable, TABLE, Input, AbsSelect, Button, BUTTONS } from 'customer_portal-ui-shared'
import styled from 'styled-components'
import { DROP_CLICK_LABEL, SUPPORT_TYPE_LABEL_ALT, dropZoneStyle } from '../File/constants'
import * as Icon from "react-bootstrap-icons";

const StyledTable = styled.div`
    thead tr th {
        font-weight: 600 !important;
        font-size: 0.9rem !important;
        color: #002a4e;
    }
`

const ImageAttachment: React.FC<{
    fileNameOptions: any,
    descriptionOptions: any,
    reportSectionOptions: any,
    conditionOptions: any,
    onChange: (data: any[]) => void,
    errors: any
}> = ({ fileNameOptions, descriptionOptions, reportSectionOptions, conditionOptions, onChange, errors }) => {

    const [attachmentList, setAttachmentList] = useState<any[]>([]);
    const attachmentListRef = useRef<any[]>([]);

    const handleFileDrop = (acceptedFiles: File[]) => {
        const allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif'];
        const validFiles = acceptedFiles.filter(file => {
            const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
            return allowedExtensions.includes(ext);
        });

        const newRows = validFiles.map((file) => ({
            id: `${file.name}_${Date.now()}_${Math.random()}`,
            file,
            fileName: "",
            condition: "",
            description: "",
            reportSection: "",
            displayName: file.name,
            previewUrl: URL.createObjectURL(file)
        }));

        setAttachmentList(prev => {
            const updated = [...prev, ...newRows];

            attachmentListRef.current = updated;
            onChange(updated);
            return updated;
        });
    };
    const handleRemove = (id: string) => {
        setAttachmentList(prev => {
            const item = prev.find(i => i.id === id);
            if (item?.previewUrl) {
                URL.revokeObjectURL(item.previewUrl);
            }
            const updated = prev.filter(item => item.id !== id);
            attachmentListRef.current = updated;
            onChange(updated);
            return updated;
        });
    };
    const columns = React.useMemo(() => [
        {
            label: 'Image',
            accessor: 'img',
            Cell: ({ row }: any) => {
                return (
                    <div className="d-flex flex-column align-items-center">
                        <img
                            src={row.original.previewUrl}
                            alt="Preview"
                            className="tank-image-placeholder img-thumbnail mb-1"
                            width="96"
                            height="96"
                            style={{ objectFit: 'cover' }}
                        />
                        <span className="small text-muted text-break" style={{ maxWidth: '96px', textAlign: 'center' }}>
                            {row.original.displayName}
                        </span>
                    </div>
                );
            }
        },
        {
            label: 'File Name',
            accessor: 'condition',
            Cell: ({ row }: any) => {
                const id = row.original.id;
                const selectedImageCardId = row.original.fileName;
                const selectedCardOpt = fileNameOptions.find((x: any) => String(x.value) === String(selectedImageCardId));
                
                if (!selectedImageCardId) {
                    return <Input disabled placeholder="Select Image Card" />;
                }

                const isSelectableCondition = selectedCardOpt?.currentCondition;
                const isCurrentCondition =
                    isSelectableCondition === 0 ||
                    isSelectableCondition === "0" ||
                    isSelectableCondition === false ||
                    isSelectableCondition === "false";

                if (isCurrentCondition) {
                    return <Input value="Current Condition" disabled />;
                } else {
                    return (
                        <>
                            <AbsSelect
                                key={`cond_${row.original.fileName || 'empty'}`}
                                mult={false}
                                options={conditionOptions.map((c: any) => ({ ...c, value: String(c.value) }))}
                                selected={row.original.condition ? [String(row.original.condition)] : []}
                                onChange={(selected: any) => {
                                    const value = typeof selected?.[0] === 'object' ? String(selected?.[0]?.value || "") : String(selected?.[0] || "");
                                    setAttachmentList(prev => {
                                        const updated = prev.map(item =>
                                            item.id === row.original.id
                                                ? { ...item, condition: value }
                                                : item
                                        );
                                        attachmentListRef.current = updated;
                                        onChange(updated);
                                        return updated;
                                    });
                                }}
                                placeholder="Select File Name"
                            />
                            {errors?.[`condition_${id}`] && !row.original.condition && (
                                <div className="text-danger small">
                                    {errors[`condition_${id}`]}
                                </div>
                            )}
                        </>
                    );
                }
            }
        },
        {
            label: 'Report Section',
            accessor: 'reportSection',
            Cell: ({ row }: any) => {
                const id = row.original.id;
                return (
                    <>
                        <AbsSelect
                            mult={false}
                            options={reportSectionOptions}
                            selected={row.original.reportSection ? [row.original.reportSection] : []}
                            onChange={(selected: any) => {
                                const value = selected?.[0];
                                setAttachmentList(prev => {
                                    const updated = prev.map(item =>
                                        item.id === row.original.id
                                            ? { ...item, reportSection: value, fileName: "", description: "", additionalDescription: "" }
                                            : item
                                    );
                                    attachmentListRef.current = updated;
                                    onChange(updated);
                                    return updated;
                                });
                            }}
                        />
                        {errors?.[`section_${id}`] && !row.original.reportSection && (
                            <div className="text-danger small">
                                {errors[`section_${id}`]}
                            </div>
                        )}
                    </>
                );
            }
        },
        {
            label: 'Image Card',
            accessor: 'file_name',
            Cell: ({ row }: any) => {
                const id = row.original.id;
                return (
                    <>
                        <AbsSelect
                            key={`file_${row.original.reportSection || 'empty'}`}
                            mult={false}
                            options={
                                fileNameOptions.filter((x: any) => {
                                    if (!row.original.reportSection) return false;

                                    const cleanSectionId = row.original.reportSection?.includes(":")
                                        ? row.original.reportSection.split(":")[1]
                                        : row.original.reportSection;

                                    let isMatch = x.sectionIds?.some((id: string) => String(id).trim().toLowerCase() === String(cleanSectionId).trim().toLowerCase());

                                    if (!isMatch && x.sectionName) {
                                        const selectedOption = reportSectionOptions.find((opt: any) => opt.value === row.original.reportSection);
                                        const selectedName = selectedOption ? String(selectedOption.text).split(":").pop()?.trim().toLowerCase() : "";
                                        if (selectedName && selectedName === x.sectionName) {
                                            isMatch = true;
                                        }
                                    }

                                    if (!isMatch) return false;

                                    const isAlreadyAssigned = attachmentListRef.current.some(item => {
                                        if (item.id === row.original.id) return false;

                                        const itemCleanSection = item.reportSection?.includes(":")
                                            ? item.reportSection.split(":")[1]
                                            : item.reportSection;

                                        const rowCleanSection = row.original.reportSection?.includes(":")
                                            ? row.original.reportSection.split(":")[1]
                                            : row.original.reportSection;

                                        return (
                                            String(item.fileName) === String(x.value) &&
                                            String(itemCleanSection) === String(rowCleanSection)
                                        );
                                    });

                                    return !isAlreadyAssigned;
                                })
                            }
                            selected={row.original.fileName ? [row.original.fileName] : []}
                            onChange={(selected: any) => {
                                const value = selected?.[0];
                                setAttachmentList(prev => {
                                    const updated = prev.map(item =>
                                        item.id === row.original.id
                                            ? { ...item, fileName: value, condition: "" }
                                            : item
                                    );
                                    attachmentListRef.current = updated;
                                    onChange(updated);
                                    return updated;
                                });
                            }}
                            placeholder="Select File Name"
                        />
                        {errors?.[`imageCard_${id}`] && !row.original.fileName && (
                            <div className="text-danger small">
                                {errors[`imageCard_${id}`]}
                            </div>
                        )}
                    </>
                );
            }
        },
        {
            label: 'Image Description',
            accessor: 'description',
            Cell: ({ row }: any) => {
                const id = row.original.id;
                return (
                    <>
                        <AbsSelect
                            key={`desc_${row.original.reportSection || 'empty'}`}
                            mult={false}
                            options={
                                descriptionOptions.filter((x: any) => {
                                    if (!row.original.reportSection) return false;

                                    const cleanSectionId = row.original.reportSection?.includes(":")
                                        ? row.original.reportSection.split(":")[1]
                                        : row.original.reportSection;

                                    let isMatch = x.sectionIds?.some((id: string) => String(id).trim().toLowerCase() === String(cleanSectionId).trim().toLowerCase());

                                    if (!isMatch && x.sectionName) {
                                        const selectedOption = reportSectionOptions.find((opt: any) => opt.value === row.original.reportSection);
                                        const selectedName = selectedOption ? String(selectedOption.text).split(":").pop()?.trim().toLowerCase() : "";
                                        if (selectedName && selectedName === x.sectionName) {
                                            isMatch = true;
                                        }
                                    }

                                    return isMatch;
                                })
                            }
                            selected={row.original.description ? [row.original.description] : []}
                            onChange={(selected: any) => {
                                const value = selected?.[0];
                                setAttachmentList(prev => {
                                    const updated = prev.map(item =>
                                        item.id === row.original.id
                                            ? { ...item, description: value, additionalDescription: "" }
                                            : item
                                    );
                                    attachmentListRef.current = updated;
                                    onChange(updated);
                                    return updated;
                                });
                            }}
                            placeholder="Select Description"
                        />
                        {errors?.[`description_${id}`] && !row.original.description && (
                            <div className="text-danger small">
                                {errors[`description_${id}`]}
                            </div>
                        )}
                        {descriptionOptions.find((d: any) => String(d.value) === String(row.original.description))?.text?.toLowerCase()?.includes("other") && (
                            <div className="mt-2">
                                <Input
                                    placeholder="Enter Additional Description"
                                    value={row.original.additionalDescription || ""}
                                    onChange={(e: any) => {
                                        const value = typeof e === 'string' ? e : e?.target?.value;
                                        setAttachmentList(prev => {
                                            const updated = prev.map(item =>
                                                item.id === row.original.id
                                                    ? { ...item, additionalDescription: value }
                                                    : item
                                            );
                                            attachmentListRef.current = updated;
                                            onChange(updated);
                                            return updated;
                                        });
                                    }}
                                />
                                {errors?.[`additionalDescription_${id}`] && !row.original.additionalDescription?.trim() && (
                                    <div className="text-danger small">
                                        {errors[`additionalDescription_${id}`]}
                                    </div>
                                )}
                            </div>
                        )}
                    </>
                );
            }
        },
        {
            label: 'Actions',
            accessor: 'actions',
            Cell: ({ row }: any) => (
                <div className="d-flex gap-2">
                    <Button
                        variant={BUTTONS.TABLEDOWNLOAD}
                        startIcon={<Icon.TrashFill />}
                        onClick={() => handleRemove(row.original.id)}
                    >

                    </Button>
                </div>
            )
        }
    ], [fileNameOptions, descriptionOptions, reportSectionOptions, errors]);

    return (
        <>
            <StyledTable className='po-attachments'>
                <Dropzone 
                    onDrop={handleFileDrop} 
                    multiple
                    accept={{
                        'image/jpeg': ['.jpg', '.jpeg'],
                        'image/png': ['.png'],
                        'image/gif': ['.gif']
                    }}
                >
                    {({ getRootProps, getInputProps }) => (
                        <div {...getRootProps({ style: dropZoneStyle })}>
                            <input {...getInputProps()} />
                            <div className='d-flex p-15 align-items-center flex-column justify-content-center'>
                                <h3 className='tx-16 _600'>{DROP_CLICK_LABEL}</h3>
                                <p className='p-2 text-center'>{SUPPORT_TYPE_LABEL_ALT}</p>
                            </div>
                        </div>
                    )}
                </Dropzone>
                <AbsReactTable
                    key={attachmentList.length}
                    loading={false}
                    columns={columns}
                    pagination={false}
                    data={attachmentList}
                    tableType={TABLE.TYPE.BASIC}
                    variant={TABLE.VARIANT.SIMPLE}
                />
            </StyledTable>
        </>
    );
};

export default ImageAttachment;