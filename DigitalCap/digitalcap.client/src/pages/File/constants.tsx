import { Input, AbsSelect, Icon, ICONS } from 'customer_portal-ui-shared'
import React, { useRef, useState } from 'react'
import { truncate } from 'lodash'
import styled from 'styled-components'


interface CellProps {
    row: {
        original: FileAttachment
    }
    cell: {
        value: string
    }
}

interface FileAttachment {
    file?: File
    description?: string
    attributes?: {
        description?: string
    }
}

interface ColumnDef {
    label: string
    accessor: string
    allowShowHide?: boolean
    Cell?: React.FC<CellProps>
}

const getCellValue = (
    value: string | undefined,
    defaultValue: string = 'N/A',
    length: number = 100
): string => (value ? truncate(value, { length }) : defaultValue)

export const mimiTypesAccepted: string[] = [
    'image/gif',
    'image/bmp',
    'image/jpeg',
    'image/png',
    'application/msword',
    'application/vnd.ms-excel',
    'application/vnd.ms-powerpoint',
    'text/plain',
    'application/pdf',
    'text/csv',
    'application/zip',
    'application/rtf',
    'application/acad',
    'image/vnd.dwg',
    'image/x-dwg',
    'application/vnd.ms-outlook',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'application/octet-stream',
    'application/x-zip-compressed',
    'multipart/x-zip',
    'application/vnd.openxmlformats-officedocument.presentationml.presentation',
]

export const dropZoneStyle: React.CSSProperties = {
    width: '100%',
    padding: 7,
    backgroundColor: '#EBEBE4',
    borderRadius: 5,
    border: '2px dashed #C7C7C7',
    wordWrap: 'break-word',
}

export const activeStyle: React.CSSProperties = {
    border: '2px dashed #fff',
    backgroundColor: 'rgba(225, 225, 225, 0.39)',
}

export const attributeMap: Record<string, string> = {
    survey: 'selectedSurvey',
    finding: 'selectedFinding',
    parts: 'selectedPart',
    desc: 'description',
}

export const SUPPORT_TYPE_LABEL: string =
    '(supported files type are "doc, docx, jpg, pdf, txt, xls, xlsx, png, jpeg, rtf, zip, dwg, gif, csv, ppt, bmp, pptx, msg")'

export const SUPPORT_TYPE_LABEL_ALT: string =
    '(supported files type are "jpg, png, jpeg, gif")'

export const DROP_CLICK_LABEL: string = 'Drop files here OR Click to upload'


const updateData = (_field: string, _row: any, _value: any) => { };

export const DescriptionCell: React.FC<CellProps> = ({ row: { original } }) => {
    const [desValue, setDesValue] = useState<string>('')
    const inputRef = useRef<HTMLInputElement>(null)

    return (
        <input
            className='new-text-input'
            ref={inputRef}
            onBlur={() => updateData('desc', original, desValue)}
            onKeyDown={(e: React.KeyboardEvent<HTMLInputElement>) => {
                if (e.key === 'Enter') {
                    updateData('desc', original, (e.target as HTMLInputElement).value)
                }
            }}
            value={desValue || original.attributes?.description || ''}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setDesValue(e.target.value)
            }
        />
    )
}
export const ProgressBar: React.FC<{ pct: number }> = ({ pct }) => (
    <div className="project-progress-wrap ">
        <div className="project-progress-track">
            <div className="project-progress-fill" style={{ width: `${pct}%` }} />
        </div>
        <span className="project-progress-pct">{pct}%</span>
    </div>
);


export const AttachmentsColumns: ColumnDef[] = [
    {
        label: 'File Name',
        accessor: 'file_name',
        allowShowHide: false,
        Cell: ({ row: { original } }: CellProps) => (
            <span className='d-flex align-items-center'>
                <Icon icon={ICONS.ATTACH_FILE} />
                {getCellValue(original.file?.name)}
            </span>
        ),
    },

    {
        label: 'File Type',
        accessor: 'filetype',
    },
    {
        label: 'Uploaded on',
        accessor: 'uploadedon',
    },


    {
        label: 'Uploaded by',
        accessor: 'uploadedby',
    },
    {
        label: 'Available for Client',
        accessor: 'availableclient',
    },

]

export const ImageAttachmentsColumns: ColumnDef[] = [
    {
        label: 'Tank ID',
        accessor: 'tank_id',
        allowShowHide: false,
    },
    {
        label: 'File Name',
        accessor: 'file_name',
        allowShowHide: false,
        Cell: ({ row: { original } }: CellProps) => (
            <span className='d-flex align-items-center'>
                <Icon icon={ICONS.ATTACH_FILE} />
                {getCellValue(original.file?.name)}
            </span>
        ),
    },

    {
        label: 'Description',
        accessor: 'description',
        Cell: () => (
            <div className="d-flex justify-content-start">
                <AbsSelect mult={false}

                    options={[
                        { text: "Description A", value: "1" },
                        { text: "Description B", value: "2" },
                    ]}
                    selected={("")}
                    placeholder="Select a Description "
                />
            </div>
        ),
    },
    {
        label: 'Additional Description',
        accessor: 'additional_description',
        Cell: () => (
            <div className="d-flex justify-content-start">
                <Input
                    label="Package Name (Optional)"
                    placeholder="Package Name"
                    value={"value"}
                    onChange={"setValue"}
                />
            </div>
        ),
    },


]


export const StyledDropDown = styled.span`
    &.disabled {
        .dropdown {
            pointer-events: none;
        }
    }
`