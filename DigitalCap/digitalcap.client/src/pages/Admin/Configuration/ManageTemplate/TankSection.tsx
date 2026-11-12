import React, { useState, useEffect } from "react";
import { Button, BUTTONS, CheckboxInput } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import * as reportConfigService from "../../../../service/ReportConfigService";
import { usePopup } from "../../../../Common/PopupProvider";
import { resultMessage } from "../../../../Common/UtilityMessage";

type Props = {
    tankSections: any[];
    vesselTypeId: number;
    selectedPartId: number;
    selectedPartName: string;
    onRefresh: () => void;
};

const TankSection: React.FC<Props> = ({
    tankSections,
    vesselTypeId,
    selectedPartId,
    selectedPartName,
    onRefresh
}) => {
    const popup = usePopup();

    const [editId, setEditId] = useState<number | null>(null);
    const [row, setRow] = useState<any | null>(null);
    const [pageSize, setPageSize] = useState(5);
    const [currentPage, setCurrentPage] = useState(1);

    const totalPages = Math.ceil(tankSections.length / pageSize);

    const paginatedTankSections = tankSections.slice(
        (currentPage - 1) * pageSize,
        currentPage * pageSize
    );
    const saveTankSection = async () => {
        if (!row) return;

        const payload = {
            reportParts: [],
            normalSectionMappings: [],
            tankSectionMappings: [
                {
                    vesselTypeId: vesselTypeId,
                    partName: selectedPartName,
                    vesselTypePartMappingId: selectedPartId,

                    tankTypeId: row.tankTypeId,
                    placeholderCount: Number(row.placeholderCount),
                    fileNameCount: Number(row.fileNameCount),
                    totalCards: Number(row.placeholderCount || 0) + Number(row.fileNameCount || 0),

                    isMapped: row.isActive,
                    isActive: row.isActive,
                    isDeleted: false
                }
            ]
        };

        try {
            await reportConfigService.saveReportTemplateConfig(
                vesselTypeId,
                payload as any
            );

            popup.success(resultMessage("Tank Section", "Saved"), "Tank Section");
            onRefresh();
            setEditId(null);
            setRow(null);
        } catch (err: any) {
            popup.error(err?.response?.data || err?.message || "Unable to save tank section. Please try again.", "Tank Section");
        }
    };
    useEffect(() => {
        setCurrentPage(1);
    }, [tankSections]);

    return (
        <div className="form-area">
            <table className="table table-bordered custom-table">
                <thead>
                    <tr>
                        <th>Tank Type</th>
                        <th>Total Conditions</th>
                        <th>Current Count</th>
                        <th>Selectable Count</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>

                <tbody>
                    {tankSections.length === 0 && (
                        <tr>
                            <td colSpan={6} className="text-center">
                                No Records Found
                            </td>
                        </tr>
                    )}
                    {paginatedTankSections.map(t =>
                        editId === t.tankTypeId ? (
                            /* ========== EDIT MODE ========== */
                            <tr key={t.tankTypeId}>
                                <td>{t.tankType}</td>

                                <td>{Number(row.placeholderCount || 0) + Number(row.fileNameCount || 0)}</td>

                                <td>
                                    {/* NATIVE INPUT � IMPORTANT */}
                                    <input
                                        type="number"
                                        min={0}
                                        step={1}
                                        className="form-control"
                                        value={row.placeholderCount}
                                        onChange={e => {
                                            const val = e.target.value;
                                            setRow(prev =>
                                                prev
                                                    ? {
                                                        ...prev,
                                                        placeholderCount: val === '' ? '' : Number(val)
                                                    }
                                                    : prev
                                            );
                                        }}

                                    />
                                </td>

                                <td>
                                    {/* NATIVE INPUT � IMPORTANT */}
                                    <input
                                        type="number"
                                        min={0}
                                        step={1}
                                        className="form-control"
                                        value={row.fileNameCount}
                                        onChange={e => {
                                            const val = e.target.value;
                                            setRow(prev =>
                                                prev
                                                    ? {
                                                        ...prev,
                                                        fileNameCount: val === '' ? '' : Number(val)
                                                    }
                                                    : prev
                                            );
                                        }}

                                    />
                                </td>

                                <td>
                                    <CheckboxInput
                                        label=" "
                                        checked={row.isActive}
                                        onChange={(v: boolean) =>
                                            setRow({
                                                ...row,
                                                isMapped: v,
                                                isActive: v
                                            })
                                        }
                                    />
                                </td>

                                <td className="d-flex gap-2">
                                    <Button
                                        variant={BUTTONS.TABLEDOWNLOAD}
                                        startIcon={<Icon.Check />}
                                        onClick={saveTankSection}
                                    />
                                    <Button
                                        variant={BUTTONS.TABLEDOWNLOAD}
                                        startIcon={<Icon.X />}
                                        onClick={() => {
                                            setEditId(null);
                                            setRow(null);
                                        }}
                                    />
                                </td>
                            </tr>
                        ) : (
                            /* ========== VIEW MODE ========== */
                            <tr key={t.tankTypeId}>
                                <td>{t.tankType}</td>
                                <td>{t.totalCards}</td>
                                <td>{t.placeholderCount}</td>
                                <td>{t.fileNameCount}</td>
                                <td>
                                    <CheckboxInput label=" " checked={t.isActive} disabled />
                                </td>
                                <td>
                                    <Button
                                        variant={BUTTONS.TABLEDOWNLOAD}
                                        startIcon={<Icon.PencilFill />}
                                        onClick={() => {
                                            setEditId(t.tankTypeId);
                                            setRow({ ...t });
                                        }}
                                    />
                                </td>
                            </tr>
                        )
                    )}
                </tbody>
            </table>
            {selectedPartId > 0 && tankSections.length > 0 && (
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
                            {tankSections.length === 0 ? 0 : (currentPage - 1) * pageSize + 1}-
                            {Math.min(currentPage * pageSize, tankSections.length)} of {tankSections.length}
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
        </div>
    );
};

export default TankSection;
