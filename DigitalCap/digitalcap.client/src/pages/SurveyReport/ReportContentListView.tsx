import React from 'react';
import { AbsReactTable, AbsSelect, Button, BUTTONS, TABLE } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";

interface TankImage {
    id: string;
    hasImage: boolean;
    imageUrl: string;
    selectedCondition: string;
    conditionId: string;
    descriptionId: string;
    descriptionText: string;
    additionalDescription?: string;
    currentCondition?: any;
}

interface ReportContentListViewProps {
    loading?: boolean;
    tanks: TankImage[];
    conditionOptions: any[];
    descriptionOptions: any[];
    isOfflineProject?: boolean;
    onViewImage: (tankId: string) => void;
    onDeleteImage: (tankId: string) => void;
    onConditionChange?: (tankId: string, value: any) => void;
    onDescriptionChange?: (tankId: string, value: any) => void;
    onAdditionalDescriptionChange?: (tankId: string, value: string) => void;
    onImageUpload?: (tankId: string, file: File) => void;
}

const ReportContentListView: React.FC<ReportContentListViewProps> = ({
    loading = false,
    tanks,
    conditionOptions,
    descriptionOptions,
    isOfflineProject = false,
    onViewImage,
    onDeleteImage,
    onConditionChange,
    onDescriptionChange,
    onAdditionalDescriptionChange,
    onImageUpload
}) => {

    const columns = [
        {
            Header: 'Image',
            accessor: 'image',
            Cell: ({ row }: any) => {
                const tank = row.original;
                return tank.hasImage ? (
                    <img src={tank.imageUrl} alt={tank.id} className="object-fit-cover rounded" width="60" height="40" />
                ) : (
                    <div className="d-flex align-items-center justify-content-center bg-light border rounded" style={{ width: 60, height: 40, borderStyle: 'dashed' }}>
                        <Icon.Image color="#ccc" size={20} />
                    </div>
                );
            }
        },
        {
            Header: 'Tank',
            accessor: 'tank',
            Cell: ({ row }: any) => (
                <span className="fw-bold text-primary">{row.original.id}</span>
            )
        },
        {
            Header: 'File Name',
            accessor: 'condition',
            Cell: ({ row }: any) => {
                const tank = row.original;
                const isSelectableCondition = tank?.currentCondition;
                const isCurrentCondition = isSelectableCondition === 0 || isSelectableCondition === "0" || isSelectableCondition === false || isSelectableCondition === "false";
                
                if (isCurrentCondition) {
                    return (
                        <AbsSelect
                            mult={false}
                            options={[{ text: "Current Condition", value: "0" }]}
                            selected={["0"]}
                            onChange={() => {}}
                            disabled={true}
                        />
                    );
                } else {
                    return (
                        <AbsSelect
                            mult={false}
                            options={conditionOptions.map((c: any) => ({ ...c, value: String(c.value) }))}
                            selected={tank.conditionId ? [String(tank.conditionId)] : []}
                            onChange={(selected: any) => {
                                const val = typeof selected?.[0] === "object" ? String(selected?.[0]?.value) : String(selected?.[0] || "");
                                if (val !== undefined && val !== null) onConditionChange?.(tank.id, val);
                            }}
                            placeholder="Select File Name"
                            disabled={isOfflineProject}
                        />
                    );
                }
            }
        },
        {
            Header: 'Description',
            accessor: 'description',
            Cell: ({ row }: any) => {
                const tank = row.original;
                const isOthers = tank.descriptionText === "Others" || descriptionOptions.find((d: any) => String(d.value) === String(tank.descriptionId))?.text === "Others";
                
                return (
                    <div className="d-flex gap-2 align-items-center">
                        <div className={isOthers ? "w-50" : "w-100"}>
                            <AbsSelect
                                mult={false}
                                options={descriptionOptions.map((d: any) => ({ ...d, value: String(d.value) }))}
                                selected={tank.descriptionId ? [String(tank.descriptionId)] : []}
                                onChange={(selected: any) => {
                                    const val = typeof selected?.[0] === "object" ? String(selected?.[0]?.value) : String(selected?.[0] || "");
                                    if (val) onDescriptionChange?.(tank.id, val);
                                }}
                                placeholder="Select description"
                                disabled={isOfflineProject}
                            />
                        </div>
                        {isOthers && (
                            <div className="w-50">
                                <input
                                    type="text"
                                    className="form-control form-control-sm"
                                    defaultValue={tank.additionalDescription || ""}
                                    placeholder="Additional Description"
                                    disabled={isOfflineProject}
                                    onBlur={(e: React.FocusEvent<HTMLInputElement>) => {
                                        if (onAdditionalDescriptionChange) {
                                            onAdditionalDescriptionChange(tank.id, e.target.value);
                                        }
                                    }}
                                />
                            </div>
                        )}
                    </div>
                );
            }
        },
        {
            Header: 'Status',
            accessor: 'status',
            Cell: ({ row }: any) => {
                const tank = row.original;
                return tank.hasImage ? (
                    <span className="tank-status-badge with-image">
                        Image uploaded
                    </span>
                ) : (
                    <span className="tank-status-badge without-image">
                        Image missing
                    </span>
                );
            }
        },
        {
            Header: 'Action',
            accessor: 'actions',
            Cell: ({ row }: any) => {
                const tank = row.original;

                const handleFileClick = () => {
                    if (isOfflineProject) return;
                    const input = document.getElementById(`file-upload-${tank.id}`);
                    if (input) input.click();
                };

                const handleFileChange = (e: any) => {
                    if (e.target.files && e.target.files.length > 0) {
                        onImageUpload?.(tank.id, e.target.files[0]);
                        // Reset input so the same file can be selected again if needed
                        e.target.value = null;
                    }
                };

                if (isOfflineProject) {
                    return tank.hasImage ? (
                        <div className="d-flex align-items-center gap-2">
                            <Icon.Eye size={18} className="cursor-pointer text-secondary" onClick={() => onViewImage(tank.id)} title="View" />
                        </div>
                    ) : (
                        <span className="text-muted small">-</span>
                    );
                }

                return (
                    <>
                        <input 
                            type="file" 
                            id={`file-upload-${tank.id}`} 
                            hidden 
                            accept="image/*"
                            onChange={handleFileChange} 
                        />
                        {tank.hasImage ? (
                            <div className="d-flex align-items-center gap-3">
                                <Icon.Eye size={18} className="cursor-pointer text-secondary" onClick={() => onViewImage(tank.id)} />
                                <span className="cursor-pointer text-primary d-flex align-items-center gap-1 fw-medium" onClick={handleFileClick}>
                                    <Icon.ArrowCounterclockwise size={16} /> Replace
                                </span>
                                <Icon.Trash size={18} className="cursor-pointer text-danger" onClick={() => onDeleteImage(tank.id)} />
                            </div>
                        ) : (
                            <Button variant={BUTTONS.TERTIARY} startIcon={<Icon.Upload />} onClick={handleFileClick}>
                                Upload image
                            </Button>
                        )}
                    </>
                );
            }
        }
    ];

    return (
        <div className="tbl-tank-list-wrapper">
            <AbsReactTable
                loading={loading}
                data={tanks}
                columns={columns}
                pagination={false}
                expand={false}
                allowColumnsShowHide={false}
                tableType={TABLE.TYPE.BASIC}
                variant={TABLE.VARIANT.SIMPLE}
            />
        </div>
    );
};

export default ReportContentListView;
