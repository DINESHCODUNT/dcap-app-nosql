import React, { useState, useEffect, useMemo, useRef } from 'react';
import { BUTTONS, Button, Input, Label, AbsSelect } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import FormAttachment from "./Attachment";
import * as surveyReportService from '../../service/SurveyReportService';
import { usePopup } from '../../Common/PopupProvider';

type FormData = {
    description: string;
    adddescription: string;
    condition: string;
};

interface Tank {
    id: string;
    currentCondition: boolean | number | string;
    cardId?: string | number;
    imageUrl?: string;
    imageId?: string | number;
    uploadedFileName?: string;
    imageName?: string;
    descriptionId?: string | number;
    additionalDescription?: string;
    conditionId?: string | number;
    templateId?: string | number;
    sectionId?: string | number;
    selectedCondition?: string | number
}

interface Props {
    activeTank: Tank | null;
    projectId: number;
    tanks: any[];
    onNavigateTank: (tank: any) => void;
    onUploadSuccess: (data: any) => void;
    onImageDeleted?: (tankId: string) => void;
    onClose: () => void;
    descriptionoptions: any[];
    conditionOptions: any[];
    uploadedCount: number;
    totalCount: number;
    reportData: any;
    isNewProject?: boolean;
}

const UploadTankImage: React.FC<Props> = ({ activeTank, projectId, tanks, onNavigateTank, onUploadSuccess, onImageDeleted, onClose,
    descriptionoptions, conditionOptions, uploadedCount, totalCount, reportData, isNewProject = false }) => {
    const [formData, setFormData] = useState<FormData>({
        description: "",
        adddescription: "",
        condition: "",
    });

    const isSelectableCondition = activeTank?.currentCondition;
    const isCurrentCondition =
        isSelectableCondition === 0 ||
        isSelectableCondition === "0" ||
        isSelectableCondition === false ||
        isSelectableCondition === "false";
    const [selectedFile, setSelectedFile] = useState<File | null>(null);

    const [localPreview, setLocalPreview] = useState<string | null>(null);

    useEffect(() => {
        if (!selectedFile) {
            setLocalPreview(null);
            return;
        }
        const objectUrl = URL.createObjectURL(selectedFile);
        setLocalPreview(objectUrl);

        return () => {
            URL.revokeObjectURL(objectUrl);
        };
    }, [selectedFile]);

    const displayImageUrl = localPreview || activeTank?.imageUrl;

    const imageName =
        activeTank?.uploadedFileName ||
        activeTank?.imageName ||
        activeTank?.id;

    const selectedDescriptionText =
        descriptionoptions?.find(
            (opt: any) =>
                String(opt.value) ===
                String(formData.description)
        )?.text;

    const popup = usePopup();
    const saveErrorOpenRef = useRef(false);
    const [clearFile, setClearFile] = useState(false);
    const [error, setError] = useState(false);
    const [clearingImage, setClearingImage] = useState(false);

    const isEmptyId = (value: unknown) => {
        if (value == null || value === "") return true;
        const normalized = String(value).trim().toLowerCase();
        return normalized === "0" || normalized === "00000000-0000-0000-0000-000000000000";
    };

    const reportRoot = reportData?.report ?? reportData;

    const tankSections = useMemo(() => {
        const sections =
            reportRoot?.tankReportView?.sections ??
            reportRoot?.template?.tankReportView?.sections ??
            [];

        return Array.isArray(sections) ? sections : [];
    }, [reportRoot]);

    const normalSubSections = useMemo(() => {
        const subSections =
            reportRoot?.template?.sections?.subSections ??
            reportRoot?.template?.sections?.SubSections ??
            [];

        return Array.isArray(subSections) ? subSections : [];
    }, [reportRoot]);

    const collectSectionIds = (item: any) =>
        [item?.sectionMappingId, item?.sectionId, item?.id]
            .filter((id) => !isEmptyId(id))
            .map((id) => String(id).trim().toLowerCase());

    const isTankSection = useMemo(() => {
        if (!reportData || !activeTank?.sectionId) return false;

        const activeSectionId = String(activeTank.sectionId).trim().toLowerCase();
        return tankSections.some((tank: any) =>
            collectSectionIds(tank).includes(activeSectionId)
        );
    }, [reportData, activeTank?.sectionId, tankSections]);

    const resolveSectionId = (tank: Tank) => {
        const sectionId = String(tank.sectionId ?? "").trim();
        if (!isNewProject || !reportData || isEmptyId(sectionId)) {
            return sectionId;
        }

        const normalizedSectionId = sectionId.toLowerCase();

        if (isTankSection) {
            const matchedTank = tankSections.find((tSection: any) =>
                collectSectionIds(tSection).includes(normalizedSectionId)
            );
            const resolvedTankId = matchedTank?.id ?? matchedTank?.sectionId;
            return !isEmptyId(resolvedTankId) ? String(resolvedTankId) : sectionId;
        }

        const matchedSubSection = normalSubSections.find((sub: any) =>
            collectSectionIds(sub).includes(normalizedSectionId)
        );

        const mappedSectionId =
            matchedSubSection?.sectionMappingId ??
            matchedSubSection?.sectionId ??
            matchedSubSection?.id;

        return !isEmptyId(mappedSectionId) ? String(mappedSectionId) : sectionId;
    };

    const useTankSectionApis = isTankSection;
    const useNewNormalSectionApis = isNewProject && !isTankSection;

    const resolveCardName = (tank: Tank) =>
        String(tank.imageName || tank.id || "").trim();

    useEffect(() => {
        if (!activeTank) return;
        if (!descriptionoptions?.length) return;

        setFormData({
            description: activeTank.descriptionId
                ? String(activeTank.descriptionId)
                : "",
            adddescription: activeTank.additionalDescription || "",
            condition:
                activeTank.conditionId != null && activeTank.conditionId !== ""
                    ? String(activeTank.conditionId)
                    : "",
        });

        setError(false);
        setSelectedFile(null);
    }, [activeTank, descriptionoptions]);

    const handleUpload = async (file: File | null) => {

        setError(true);

        if (!activeTank) return;

        if (
            !formData.description ||
            formData.description === ""
        ) {
            return;
        }

        if (
            selectedDescriptionText === "Others" &&
            (
                !formData.adddescription ||
                formData.adddescription.trim() === ""
            )
        ) {
            return;
        }

        if (!isCurrentCondition && !formData.condition) {
            return;
        }

        if (!file && !activeTank.imageUrl) {
            return;
        }

        const tank = activeTank;
        const templateId = tank.templateId ?? 0;
        const sectionId = resolveSectionId(tank);
        const cardId = Number(tank.cardId ?? 0);
        const cardName = resolveCardName(tank);

        try {
            let imageUrl = tank.imageUrl || null;

            if (file) {
                const replaceImage = !!activeTank.imageUrl;
                if (useTankSectionApis) {
                    const result = await surveyReportService.uploadTankCardImage({
                        file,
                        projectId,
                        cardNumber: cardId,
                        templateId,
                        sectionId,
                        imageId: 0,
                        replaceImage,
                        cardName,
                    });

                    const uploadedImage =
                        result?.data?.image ?? result?.data?.Image;

                    imageUrl = uploadedImage
                        ? `data:image/jpeg;base64,${uploadedImage}`
                        : imageUrl;
                } else if (useNewNormalSectionApis) {
                    const result = await surveyReportService.uploadGenericSurveyImage({
                        file,
                        projectId,
                        cardId,
                        templateId,
                        sectionId,
                        imageId: 0,
                        replaceImage,
                        cardName,
                    });

                    const uploadedImage =
                        result?.data?.image ?? result?.data?.Image;

                    imageUrl = uploadedImage
                        ? `data:image/jpeg;base64,${uploadedImage}`
                        : imageUrl;
                } else {
                    const result = await surveyReportService.uploadSurveyImage({
                        file,
                        projectId,
                        cardId,
                        templateId,
                        sectionId,
                        imageId: 0,
                        replaceImage,
                    });

                    const uploadedImage =
                        result?.data?.image ?? result?.data?.Image;

                    imageUrl = uploadedImage
                        ? `data:image/jpeg;base64,${uploadedImage}`
                        : imageUrl;
                }
            }

            if (formData.description) {
                if (useTankSectionApis) {
                    await surveyReportService.updateTankImageDescriptionDropdownCard({
                        templateId,
                        projectId,
                        sectionId,
                        cardNumber: cardId,
                        value: Number(formData.description),
                        cardName,
                    });
                } else if (useNewNormalSectionApis) {
                    await surveyReportService.updateGenericImageDescriptionDropdownCard({
                        templateId,
                        projectId,
                        sectionId,
                        cardId,
                        value: Number(formData.description),
                        cardName,
                    });
                } else {
                    await surveyReportService.updateImageDescriptionDropdownCard({
                        templateId,
                        projectId,
                        sectionId,
                        cardId,
                        value: formData.description,
                    });
                }
            }

            if (
                selectedDescriptionText === "Others" &&
                formData.adddescription
            ) {
                if (useTankSectionApis) {
                    await surveyReportService.updateTankCardAdditionalDescription({
                        templateId,
                        projectId,
                        sectionId,
                        cardNumber: cardId,
                        value: formData.adddescription,
                        cardName,
                    });
                } else if (useNewNormalSectionApis) {
                    await surveyReportService.updateGenericAdditionalDescription({
                        templateId,
                        projectId,
                        sectionId,
                        cardId,
                        value: formData.adddescription,
                        cardName,
                    });
                } else {
                    await surveyReportService.updateAdditionalDescription({
                        templateId,
                        projectId,
                        sectionId,
                        cardId,
                        value: formData.adddescription
                    });
                }
            }

            if (formData.condition !== null) {
                if (useTankSectionApis) {
                    await surveyReportService.updateTankCardCurrentCondition({
                        templateId,
                        projectId,
                        sectionId,
                        cardNumber: cardId,
                        value: formData.condition ? formData.condition : 0,
                        cardName,
                    });
                } else if (useNewNormalSectionApis) {
                    await surveyReportService.updateGenericCurrentCondition({
                        templateId,
                        projectId,
                        sectionId,
                        cardId,
                        value: formData.condition ? formData.condition : 0,
                        cardName,
                    });
                } else {
                    await surveyReportService.updateCurrentCondition({
                        templateId,
                        projectId,
                        sectionId,
                        cardId,
                        value: formData.condition ? formData.condition : 0
                    });
                }
            }

            const selectedConditionText = conditionOptions?.find(
                (opt: any) => String(opt.value) === String(formData.condition)
            )?.text || formData.condition;

            onUploadSuccess({

                tankId: tank.id,

                imageUrl: imageUrl,

                imageName: file?.name || tank.imageName || "",

                descriptionId: formData.description,
                
                descriptionText: selectedDescriptionText,

                additionalDescription:
                    formData.adddescription,

                selectedCondition: selectedConditionText
            });

            setSelectedFile(null);
            setError(false);

            if (file) {
                setClearFile(true);

                setTimeout(() => {

                    setClearFile(false);

                }, 100);
            }

            if (file) {
                await popup.success("File has been uploaded successfully.", "Upload File");
            }
            onClose();
        } catch (err: any) {
            if (file) {
                popup.error(err?.response?.data || err?.message || "Failed to upload file.", "Upload File");
            } else {
                if (!saveErrorOpenRef.current) {
                    saveErrorOpenRef.current = true;
                    popup.error("Your change could not be saved. Please try again.", "Save").finally(() => {
                        saveErrorOpenRef.current = false;
                    });
                }
            }
        }
    };


    const currentIndex = tanks.findIndex(
        (t: any) => t.id === activeTank?.id
    );

    const hasPrevious = currentIndex > 0;
    const hasNext = currentIndex < tanks.length - 1;

    const handleClearImage = async () => {
        if (!activeTank || clearingImage) return;

        const hasSavedImage = !!activeTank.imageUrl;

        if (!hasSavedImage && !selectedFile) return;

        try {
            if (hasSavedImage) {
                setClearingImage(true);

                const sectionId = resolveSectionId(activeTank);
                const cardId = Number(activeTank.cardId ?? 0);

                if (useTankSectionApis) {
                    await surveyReportService.deleteTankCardImage({
                        templateId: activeTank.templateId ?? 0,
                        projectId,
                        sectionId,
                        cardNumber: cardId,
                        imageId: Number(activeTank.imageId ?? 0),
                    });
                } else if (useNewNormalSectionApis) {
                    await surveyReportService.deleteGenericImage(
                        activeTank.templateId ?? 0,
                        projectId,
                        sectionId,
                        cardId,
                        Number(activeTank.imageId ?? 0)
                    );
                } else {
                    await surveyReportService.deleteImage({
                        templateId: activeTank.templateId ?? 0,
                        projectId,
                        sectionId,
                        cardId,
                        imageId: activeTank.imageId ?? 0,
                        replaceImage: false
                    });
                }

                onImageDeleted?.(activeTank.id);
            }

            setSelectedFile(null);
            setError(false);
            setClearFile(true);
            setTimeout(() => {
                setClearFile(false);
            }, 100);
        } catch (err) {
        } finally {
            setClearingImage(false);
        }
    };

    const handlePreviousTank = () => {
        if (!hasPrevious) return;
        onNavigateTank(tanks[currentIndex - 1]);
    };

    const handleNextTank = () => {
        if (!hasNext) return;
        onNavigateTank(tanks[currentIndex + 1]);
    };
    return (
        <div className="upload-tank-image">
            <div className="project-header">
                <div className="section-title p-3 border-bottom">
                    <div className="d-flex flex-row gap-3">

                        <div className="d-flex flex-column">
                            <h5>Upload Tank Images {imageName || ""}</h5>
                            <div className="project-meta tx-14">
                                Images uploaded: {uploadedCount}/{totalCount}
                            </div>
                        </div>
                    </div>
                    <div className="section-actions">
                        <div className="d-flex flex-row gap-3">
                            <Button
                                variant={BUTTONS.TABLEDOWNLOAD}
                                startIcon={<Icon.ChevronLeft />}
                                onClick={handlePreviousTank}
                                disabled={!hasPrevious}
                            />
                            <Button
                                variant={BUTTONS.TABLEDOWNLOAD}
                                startIcon={<Icon.ChevronRight />}
                                onClick={handleNextTank}
                                disabled={!hasNext}
                            />
                        </div>
                    </div>
                </div>
            </div>
            <div className="row mt-4">
                <div className="col-md-12 form-group">
                    <Label name="Description" bold />
                    <AbsSelect
                        key={formData.description || "empty"}
                        mult={false}
                        onChange={(selected: any) =>
                            setFormData({
                                ...formData,
                                description:
                                    typeof selected?.[0] === "object"
                                        ? String(selected?.[0]?.value)
                                        : String(selected?.[0] || "")
                            })
                        }
                        options={descriptionoptions.map((x: any) => ({
                            ...x,
                            value: String(x.value)
                        }))}
                        selected={
                            formData.description
                                ? [String(formData.description)]
                                : []
                        }
                        placeholder="Select a Description"
                    />
                    {(error && (!formData.description || formData.description === "")) ?
                        <div>
                            <label style={{ color: 'red' }}>
                                Description Required
                            </label>
                        </div> : <></>
                    }
                </div>
                {selectedDescriptionText === "Others" && (
                    <div className="col-md-12 form-group">
                        <Label name="Additional Description" bold />
                        <Input
                            placeholder="Enter Additional Description"
                            value={formData.adddescription}
                            onChange={(value: string) =>
                                setFormData({ ...formData, adddescription: value })
                            }
                        />
                        {(error && selectedDescriptionText === "Others" && (!formData.adddescription || formData.adddescription.trim() === "")) ?
                            <div>
                                <label style={{ color: 'red' }}>
                                    Additional Description Required
                                </label>
                            </div> : <></>
                        }
                    </div>
                )}

                <div className="col-md-12 form-group">
                    <Label name="File Name" bold />

                    {!isCurrentCondition ? (
                        <AbsSelect
                            key={formData.condition || "empty"}
                            mult={false}
                            onChange={(selected: any) =>
                                setFormData({
                                    ...formData,
                                    condition:
                                        typeof selected?.[0] === "object"
                                            ? String(selected?.[0]?.value ?? "")
                                            : String(selected?.[0] || "")
                                })
                            }
                            options={conditionOptions.map((x: any) => ({
                                ...x,
                                value: String(x.value)
                            }))}
                            selected={
                                formData.condition
                                    ? [String(formData.condition)]
                                    : []
                            }
                            placeholder="Select File Name"
                        />
                    ) : (
                        <Input
                            value="Current Condition"
                            disabled
                        />
                    )}
                    {(error && !isCurrentCondition && !formData.condition) ?
                        <div>
                            <label style={{ color: 'red' }}>
                                File Name Required
                            </label>
                        </div> : <></>
                    }
                    {displayImageUrl && (
                        <img src={displayImageUrl}
                            alt={imageName || "Tank Image"} className="tank-image-placeholder img-thumbnail" />
                    )}
                </div>

                <div className="col-md-12 form-group">
                    <div className="d-flex flex-row justify-content-between gap-2">
                        <Label name="Attachments" bold />
                        <div
                            className="cursor-pointer"
                            onClick={handleClearImage}
                            role="button"
                        >
                            <Label name="Clear Image" labelColor="blue" bold />
                        </div>
                    </div>

                    <FormAttachment
                        onFileChange={(file: File) => {
                            setSelectedFile(file);
                            handleUpload(file);
                        }} clearFile={clearFile}
                    />
                    {(error && !selectedFile && !activeTank?.imageUrl) ?
                        <div>
                            <label style={{ color: 'red' }}>
                                File Required
                            </label>
                        </div> : <></>
                    }
                </div>
                <div className="col-md-12 form-group">
                    <div className="d-flex gap-2 justify-content-end">
                        <Button
                            variant={BUTTONS.PRIMARY}
                            onClick={() => handleUpload(selectedFile)}
                        >
                            Upload</Button>
                        <Button variant={BUTTONS.SECONDARY} onClick={onClose}>Cancel</Button>
                    </div>
                </div>

            </div>
        </div>

    );
};

export default UploadTankImage;