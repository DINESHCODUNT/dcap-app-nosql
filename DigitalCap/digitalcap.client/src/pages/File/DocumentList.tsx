import React, { forwardRef, useCallback } from "react";
import { AbsSearch } from "customer_portal-ui-shared";
import CommonAttachment, { type CommonAttachmentHandle } from "./CommonAttachment";

export type { CommonAttachmentHandle };

export interface DocumentListProps {
    projectId?: number;
    taskId?: number | null;
    addInternalDocumentsOnly?: boolean;
}

const DocumentList = forwardRef<CommonAttachmentHandle, DocumentListProps>(function DocumentList(
    { projectId, taskId, addInternalDocumentsOnly },
    ref
) {
    const [sval, updatesVal] = React.useState<string>("");
    const [searchApplied, updateSearchApplied] = React.useState<string | null>(null);
    const [filterResetKey, setFilterResetKey] = React.useState(0);
    const [activeTab, setActiveTab] = React.useState<'abs' | 'vessel'>('abs');

    const handleClearFilter = useCallback((e?: React.MouseEvent) => {
        e?.preventDefault();
        updatesVal("");
        updateSearchApplied(null);
        setFilterResetKey((k) => k + 1);
    }, []);

    const onChangeSearchInput = useCallback((value: string) => {
        updatesVal(value);
        if (value.trim() === "") {
            updateSearchApplied(null);
        } else {
            updateSearchApplied(value);
        }
    }, []);

    return (
        <>
            <div className="section-title">
                <h5 className='_600'>Documents</h5>
            </div>

            <ul className="nav nav-tabs document-grid-tabs mb-3">
                <li className="nav-item">
                    <button
                        className={`nav-link ${activeTab === 'abs' ? 'active' : ''}`}
                        onClick={() => setActiveTab('abs')}
                    >
                        ABS Documents
                    </button>
                </li>
                <li className="nav-item">
                    <button
                        className={`nav-link ${activeTab === 'vessel' ? 'active' : ''}`}
                        onClick={() => setActiveTab('vessel')}
                    >
                        Vessel Documents
                    </button>
                </li>
            </ul>

            <div className="d-flex justify-content-start align-items-center gap-2 mb-3 mt-2">


                        <AbsSearch
                            handleClear={() => handleClearFilter()}
                            value={sval}
                            handleChange={(e: { target: { value: string } }) =>
                                onChangeSearchInput(e.target.value)
                            }
                            handleSearch={() => {
                                updateSearchApplied(sval === "" ? null : sval);
                            }}
                            placeholder="Search"
                            searchApplied={searchApplied !== null}
                        />
                        <a
                            href="#"
                            className='text-link text-nowrap _500'
                            onClick={handleClearFilter}
                        >
                            Clear Filter
                        </a>


            </div>
            <div className="attachment-wrapper">
                <CommonAttachment
                    ref={ref}
                    projectId={projectId}
                    taskId={taskId}
                    addInternalDocumentsOnly={addInternalDocumentsOnly}
                    searchQuery={searchApplied ?? ""}
                    filterResetKey={filterResetKey}
                    activeTab={activeTab}
                />
            </div>
        </>
    );
});

export default DocumentList;
