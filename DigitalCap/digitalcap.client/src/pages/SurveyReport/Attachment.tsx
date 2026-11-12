import Dropzone from 'react-dropzone'
import styled from 'styled-components'
import {
    DROP_CLICK_LABEL,
    SUPPORT_TYPE_LABEL_ALT,
    dropZoneStyle
} from '../File/constants'

const StyledTable = styled.div`
    thead tr th {
        font-weight: 600 !important;
        font-size: 0.9rem !important;
        color: #002a4e;
    }
`
const FormAttachment: React.FC<{
    onFileChange?: (file: File) => void; clearFile?: boolean;
}> = ({ onFileChange }) => {

    const handleFileDrop = (
        acceptedFiles: File[]
    ): void => {
        if (acceptedFiles?.length > 0) {
            onFileChange?.(acceptedFiles[0]);
        }
    }
    return (
        <>
            <StyledTable className='po-attachments'>

                <Dropzone
                    onDrop={handleFileDrop}
                    multiple
                >
                    {({ getRootProps, getInputProps }) => (
                        <div {...getRootProps({ style: dropZoneStyle })}>

                            <input {...getInputProps()} />

                            <div className='d-flex p-15 align-items-center flex-column justify-content-center'>
                                <h3 className='tx-16 _600'>
                                    {DROP_CLICK_LABEL}
                                </h3>

                                <p className='p-2 text-center'>
                                    {SUPPORT_TYPE_LABEL_ALT}
                                </p>
                            </div>

                        </div>
                    )}
                </Dropzone>

            </StyledTable>
        </>
    )
}

export default FormAttachment;