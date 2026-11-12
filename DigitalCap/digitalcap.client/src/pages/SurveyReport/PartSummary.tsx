import React, { useState, useEffect, useRef } from "react";

import { Label, Input, DatePickerInput, Divider, Card, RadioInput, CheckboxInput } from "customer_portal-ui-shared";
import RichTextEditor from "../../Common/RichTextEditor";
import { updateHIReport, updateGradingCondition, updateGradingNotes } from "../../service/SurveyReportService";
import { getPartDisplayLabel } from "./partUtils";
import { usePopup } from "../../Common/PopupProvider";
import { isEmptyDate } from "../../utils/dateTime";

type GradeOption = string | number;

interface PartSummaryProps {
    activePart: string;
    activeSection: string;
    data?: any;
    reportData?: any;
    isOfflineProject?: boolean;
}
type FormData = {
    report_no: string;
    orgname: string;
    certificate_no: string;
    expire_date: Date | null;
    accomodation: string;
    vessel_type: string;
    zonea: string;
    zoneb: string;
    zonec: string;
    maintanance: string;
    additional_comments: string;
    photographs: string;
    startDate: Date | null;
    endDate: Date | null;
    reportDate: string;
    remarks: string;
    survey_confirmed: string;
    class_recommendations: string;
    fatigue_analysis: string;
    under_remarks: string;
    modifications: string;
    plating_condition: string;
    bottom_plating: string;
    leadsurveyername: string;
    leademployee_no: string;
    surveyername: string;
    employee_no: string;
    visitDate: Date | null;
    individualA: string;
    individualB: string;
    individualC: string;
    individualD: string;
    individualE: string;
    individualF: string;
    operational_location: string;
}

const GRADE_OPTIONS: GradeOption[] = [1, 2, 3, 4, 5, 'N/A'];

const PartSummary: React.FC<PartSummaryProps> = ({ activePart, reportData, isOfflineProject }) => {
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

    const [formData, setFormData] = React.useState<FormData>({
        report_no: "",
        orgname: "",
        certificate_no: "",
        expire_date: null,

        accomodation: "",
        vessel_type: "Double Hull Crude Oil Carrier",
        zonea: "",
        zoneb: "",
        zonec: "",
        maintanance: "",
        additional_comments: "",
        photographs: "",
        startDate: null,
        endDate: null,
        reportDate: "",
        remarks: "",
        survey_confirmed: "",
        class_recommendations: "",
        fatigue_analysis: "",
        under_remarks: "",
        modifications: "",
        plating_condition: "",
        bottom_plating: "",
        leadsurveyername: "",
        leademployee_no: "",
        surveyername: "",
        employee_no: "",
        visitDate: null,
        individualA: "",
        individualB: "",
        individualC: "",
        individualD: "",
        individualE: "",
        individualF: "",
        operational_location: "",
    })

    const [grades, setGrades] =
        useState<Record<string, GradeOption[]>>({});
    const [gradingErrors, setGradingErrors] = useState<Record<string, string>>({});
    const isInitialLoad = React.useRef(true);

    useEffect(() => {

        const hiReport = reportData?.template?.handireport;

        if (!hiReport?.id) return;

        isInitialLoad.current = true;

        setFormData({

            report_no:
                hiReport.associatedReportNumber ?? "",

            orgname:
                hiReport.companyName ?? "",

            certificate_no:
                hiReport.certificateNumber ?? "",

            expire_date:
                !isEmptyDate(hiReport.expiryDate)
                    ? new Date(hiReport.expiryDate)
                    : null,

            accomodation:
                hiReport.accommodation ?? "",

            vessel_type:
                "Double Hull Crude Oil Carrier",

            zonea:
                hiReport.thicknessMeasurementA ?? "",

            zoneb:
                hiReport.thicknessMeasurementB ?? "",

            zonec:
                hiReport.thicknessMeasurementC ?? "",

            maintanance:
                hiReport.maintenanceExecuted ?? "",

            additional_comments:
                hiReport.additionalSurveyorComments ?? "",

            photographs:
                hiReport.photoGraphs ?? "",

            startDate:
                !isEmptyDate(hiReport.operationalTestStart)
                    ? new Date(hiReport.operationalTestStart)
                    : null,

            endDate:
                !isEmptyDate(hiReport.operationalTestFinish)
                    ? new Date(hiReport.operationalTestFinish)
                    : null,

            reportDate:
                hiReport.associatedReportDate ?? "",

            remarks:
                hiReport.remarks ?? "",

            survey_confirmed:
                hiReport.substantialCorrosionRemaining ?? "",

            class_recommendations:
                hiReport.statutoryDeficiencies ?? "",

            fatigue_analysis:
                hiReport.foundSatisfactory ?? "",

            under_remarks:
                hiReport.underRemarks ?? "",

            modifications:
                hiReport.underRemarks ?? "",

            plating_condition:
                hiReport.scouringOrPitting ?? "",

            bottom_plating:
                hiReport.bottomPlattingScouringOrPitting ?? "",

            leadsurveyername:
                hiReport.leadSurveyorName ?? "",

            leademployee_no:
                hiReport.leadSurveyorNumber ?? "",

            surveyername:
                hiReport.surveyorName ?? "",

            employee_no:
                hiReport.surveyorNumber ?? "",

            visitDate:
                !isEmptyDate(hiReport.lastVisitDate)
                    ? new Date(hiReport.lastVisitDate)
                    : null,

            individualA:
                hiReport.individualA ?? "",

            individualB:
                hiReport.individualB ?? "",

            individualC:
                hiReport.individualC ?? "",

            individualD:
                hiReport.individualD ?? "",

            individualE:
                hiReport.individualE ?? "",

            individualF:
                hiReport.individualF ?? "",

            operational_location:
                hiReport.operationalTestLocation ?? "",

        });

        setTimeout(() => {
            isInitialLoad.current = false;
        }, 100);

    }, [reportData?.template?.handireport?.id]);

    useEffect(() => {

    }, [formData]);

    const handleUpdateHIReport = async (updatedFormData = formData) => {

        const hiReport = reportData?.template?.handireport;
        if (!hiReport) return;

        try {

            const payload = {
                id: hiReport.id,
                projectId: hiReport.projectId,

                CompanyName: updatedFormData.orgname || "",
                CertificateNumber: updatedFormData.certificate_no || "",
                ExpiryDate: updatedFormData.expire_date,

                ThicknessMeasurementA: updatedFormData.zonea || "",
                ThicknessMeasurementB: updatedFormData.zoneb || "",
                ThicknessMeasurementC: updatedFormData.zonec || "",

                AdditionalSurveyorComments:
                    updatedFormData.additional_comments || "",

                MaintanceExecuted:
                    updatedFormData.maintanance || "",

                Accommodation:
                    updatedFormData.accomodation || "",

                PhotoGraphs:
                    updatedFormData.photographs || "",

                OperationalTestLocation:
                    updatedFormData.operational_location || "",

                OperationalTestStart:
                    updatedFormData.startDate,

                OperationalTestFinish:
                    updatedFormData.endDate,

                OptionalTestSatisFactory:
                    updatedFormData.survey_confirmed || "",

                AssociatedReportNumber:
                    updatedFormData.report_no || "",

                AssociatedReportDate:
                    updatedFormData.reportDate || "",

                Remarks:
                    updatedFormData.remarks || "",

                LeadSurveyorName:
                    updatedFormData.leadsurveyername || "",

                LeadSurveyorNumber:
                    updatedFormData.leademployee_no || "",

                SurveyorName:
                    updatedFormData.surveyername || "",

                SurveyorNumber:
                    updatedFormData.employee_no || "",

                LastVisitDate:
                    updatedFormData.visitDate,

                IndividualA:
                    updatedFormData.individualA || "",

                IndividualB:
                    updatedFormData.individualB || "",

                IndividualC:
                    updatedFormData.individualC || "",

                IndividualD:
                    updatedFormData.individualD || "",

                IndividualE:
                    updatedFormData.individualE || "",

                IndividualF:
                    updatedFormData.individualF || "",

                ScouringOrPitting:
                    updatedFormData.plating_condition || "",

                SubstantialCorrosionRemaining:
                    updatedFormData.class_recommendations || "",

                StatutoryDeficiencies:
                    updatedFormData.modifications || "",

                FoundSatisfactory:
                    updatedFormData.fatigue_analysis || "",

                UnderRemarks:
                    updatedFormData.under_remarks || "",

                BottomPlattingScouringOrPitting:
                    updatedFormData.bottom_plating || "",
            };
            await updateHIReport(payload);

        } catch (error) {
            handleSaveError();
        }
    };

    const debounceRef = React.useRef<any>(null);

    const handleChange = (field: string, value: any) => {
        setFormData(prev => {
            const updatedData = { ...prev, [field]: value };

            if (isInitialLoad.current) return updatedData;

            clearTimeout(debounceRef.current);
            debounceRef.current = setTimeout(() => {
                handleUpdateHIReport(updatedData);
            }, 800);

            return updatedData;
        });
    };

    const handleUpdateGradingCondition = async (gradingId: number, choices: { choiceName: string, value: boolean }[]) => {
        try {
            const payload = {
                templateId: reportData?.template?.id,
                projectId: reportData?.projectId,
                sectionId: reportData?.template?.handireport?.sectionId,
                gradingId,
                choices
            };
            await updateGradingCondition(payload);
        } catch (error) {
            handleSaveError();
        }
    };

    useEffect(() => {
        const grading = reportData?.template?.handireport?.grading;
        if (!grading?.length) return;

        const mapped: any = {};
        grading.forEach((row: any) => {
            const selectedValues = row.checkbox
                ?.filter((c: any) => c.value === true)
                ?.map((c: any) =>
                    c.name === "N/A"
                        ? "N/A"
                        : Number(c.name)
                ) || [];
            mapped[row.id] = selectedValues;
        });
        setGrades(mapped);
    }, [reportData?.template?.handireport]);

    return (
        <>
            <div className="report-content">
                <div className="project-header">
                    <div className="section-title p-3 pe-5 border-bottom">
                        <h5>Part - {getPartDisplayLabel(activePart)}</h5>
                    </div>
                </div>
                <div className="report-area">

                    <Card data={{
                        title: {
                            content:
                                <div className="d-flex justify-content-start gap-3 align-items-center">
                                    <span className="summary-badge">
                                        1
                                    </span>
                                    <h5 className="_600">Thickness Measurement</h5></div>
                        },
                        body: <>
                            <div className="report-meta p-0">
                                <div className="inspection-statement p-0">
                                    <p className="_400">
                                        <span className="certify">Taken by ABS recognized external specialists </span>

                                        <span className="inline-control-lg certify">
                                            <Input
                                                placeholder="Organization Name"
                                                value={formData.orgname}
                                                onChange={(value: string) =>
                                                    handleChange("orgname", value)
                                                }
                                                disabled={isOfflineProject}
                                            />
                                        </span>

                                        <span className="certify">Certificate No: </span><span className="inline-control certify">
                                            <Input
                                                placeholder="Certificate No"
                                                value={formData.certificate_no}
                                                onChange={(value: string) =>
                                                    handleChange("certificate_no", value)
                                                }
                                                disabled={isOfflineProject}
                                            />
                                        </span>
                                        <span className="certify">expiry date </span>
                                        <span className="inline-control inline-control--date">
                                            <DatePickerInput
                                                onChange={(date: any) =>
                                                    handleChange("expire_date", date)
                                                }
                                                value={formData.expire_date}
                                                disabled={isOfflineProject}
                                            />
                                        </span>
                                        <span className="certify">UTM submitted in way of the following transverse zones was carried out and completed in accordance with CAP Survey requirements:</span>
                                    </p>
                                    <div className="d-flex justify-content-start gap-3 mb-3">
                                        <span className="sec-badge"  >
                                            A
                                        </span>
                                        <div className="w-100">
                                            <Input
                                                placeholder="Zone A"
                                                value={formData.zonea}
                                                onChange={(value: string) =>
                                                    handleChange("zonea", value)
                                                }
                                                disabled={isOfflineProject}
                                            />
                                        </div>
                                    </div>
                                    <div className="d-flex justify-content-start gap-3 mb-3">
                                        <span className="sec-badge"  >
                                            B
                                        </span>
                                        <div className="w-100">
                                            <Input
                                                placeholder="Zone B"
                                                value={formData.zoneb}
                                                onChange={(value: string) =>
                                                    handleChange("zoneb", value)
                                                }
                                                disabled={isOfflineProject}
                                            />
                                        </div>
                                    </div>
                                    <div className="d-flex justify-content-start gap-3 mb-3">
                                        <span className="sec-badge"  >C </span>
                                        <div className="w-100">
                                            <Input
                                                placeholder="Zone C"
                                                value={formData.zonec}
                                                onChange={(value: string) =>
                                                    handleChange("zonec", value)
                                                }
                                                disabled={isOfflineProject}
                                            />
                                        </div>
                                    </div>

                                </div>
                            </div>
                        </>
                    }} />
                    <Card data={{
                        title: {
                            content:
                                <div className="d-flex justify-content-start gap-3 align-items-center">
                                    <span className="summary-badge">
                                        2
                                    </span>
                                    <h5 className="_600">Maintenance</h5></div>
                        },
                        body: <>
                            <div className="report-meta p-0">
                                <div className="d-flex justify-content-between gap-3 mb-3">
                                    <p>Onboard Maintenance appears organized and executed? </p>
                                    <div className="d-flex justify-content-start gap-3">
                                        <RadioInput
                                            label="Yes"
                                            checked={formData.maintanance === "Yes"}
                                            onChange={() =>
                                                handleChange("maintanance", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />
                                        <RadioInput
                                            label="No"
                                            checked={formData.maintanance === "No"}
                                            onChange={() =>
                                                handleChange("maintanance", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                </div>
                                <div className="form-group mb-0">
                                    <Label name="Additional Comments" bold />
                                    <div className="mt-3">
                                        <RichTextEditor
                                            id="AdditionalSurveyorComments"
                                            value={formData.additional_comments || ""}
                                            onChange={(value: string) =>
                                                handleChange("additional_comments", value)
                                            }
                                            placeholder="Enter Additional Comments..."
                                            readOnly={isOfflineProject}
                                        /></div>
                                </div>
                            </div>
                        </>
                    }} />
                    <h4 className="subtitle">Additional Surveyor Comments
                    </h4>
                    <Card data={{
                        title: {
                            content:
                                <div className="d-flex justify-content-start gap-3 align-items-center">
                                    <span className="summary-badge">
                                        3
                                    </span>
                                    <h5 className="_600">Accommodation</h5></div>
                        },
                        body: <>
                            <div className="report-meta p-0">
                                <div className="d-flex justify-content-between gap-3 mb-3">
                                    <p className="mb-0">Accommodations were surveyed for general condition, cleanliness, and habitability, including galley, living quarters, recreational areas and mess rooms. All found satisfactory.
                                    </p>
                                    <div className="d-flex justify-content-start gap-3">
                                        <RadioInput
                                            label="Yes"
                                            checked={formData.accomodation === "Yes"}
                                            onChange={() =>
                                                handleChange("accomodation", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="No"
                                            checked={formData.accomodation === "No"}
                                            onChange={() =>
                                                handleChange("accomodation", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="N/A"
                                            checked={formData.accomodation === "N/A"}
                                            onChange={() =>
                                                handleChange("accomodation", "N/A")
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                </div>

                            </div>
                        </>
                    }} />
                    <Card data={{
                        title: {
                            content:
                                <div className="d-flex justify-content-start gap-3 align-items-center">
                                    <span className="summary-badge">
                                        4
                                    </span>
                                    <h5 className="_600">Photographs</h5></div>
                        },
                        body: <>
                            <div className="report-meta p-0">
                                <div className="d-flex justify-content-between gap-3 mb-3">
                                    <p className="mb-0">Photographs taken as representative of the areas graded and are to be considered representative of vessel at this time
                                    </p>
                                    <div className="d-flex justify-content-start gap-3">

                                        <RadioInput
                                            label="Yes"
                                            checked={formData.photographs === "Yes"}
                                            onChange={() =>
                                                handleChange("photographs", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="No"
                                            checked={formData.photographs === "No"}
                                            onChange={() =>
                                                handleChange("photographs", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="N/A"
                                            checked={formData.photographs === "N/A"}
                                            onChange={() =>
                                                handleChange("photographs", "N/A")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                    </div>
                                </div>
                                {formData.photographs === "N/A" && (
                                    <div className="form-group mb-0">
                                        <div className="mt-3">
                                            <RichTextEditor
                                                id="AdditionalSurveyorCommentsNA"
                                                value={formData.additional_comments || ""}
                                                onChange={(value: string) =>
                                                    handleChange("additional_comments", value)
                                                }
                                                placeholder="Enter Additional Comments..."
                                                readOnly={isOfflineProject}
                                            />
                                        </div>
                                    </div>)}
                            </div>
                        </>
                    }} />
                    <Card data={{
                        title: {
                            content:
                                <div className="d-flex justify-content-start gap-3 align-items-center">
                                    <span className="summary-badge">
                                        5
                                    </span>
                                    <h5 className="_600">Operational Test</h5></div>
                        },
                        body: <>
                            <div className="report-meta p-0">
                                <p className="mb-0">Sea Trials Particulars:</p>
                                <div className="row mt-4">
                                    <div className="col-md-4 form-group">
                                        <Label name="Location" bold />
                                        <Input
                                            placeholder="Enter Location"
                                            value={formData.operational_location}
                                            onChange={(value: string) =>
                                                handleChange("operational_location", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Start Date" bold />
                                        <DatePickerInput
                                            onChange={(date: any) =>
                                                handleChange("startDate", date)
                                            }
                                            value={formData.startDate}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Finish Date" bold />
                                        <DatePickerInput
                                            onChange={(date: any) =>
                                                handleChange("endDate", date)
                                            }
                                            value={formData.endDate}
                                            disabled={isOfflineProject}
                                        />
                                    </div>

                                </div>


                            </div>
                        </>
                    }} />
                    <Card data={{
                        title: {
                            content:
                                <div className="d-flex justify-content-start gap-3 align-items-center">
                                    <span className="summary-badge">
                                        6
                                    </span>
                                    <h5 className="_600"> Class Survey Details</h5></div>
                        },
                        body: <>
                            <div className="report-meta p-0">

                                <div className="row mt-4">
                                    <div className="col-md-4 form-group">
                                        <Label name="Associated Report Number(s) " bold />
                                        <Input
                                            placeholder="Enter Report Number"
                                            value={formData.report_no}
                                            onChange={(value: string) =>
                                                handleChange("report_no", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-4 form-group">
                                        <Label name="Associated Report Date(s) " bold />
                                        <Input
                                            placeholder="Enter Report Date"
                                            value={formData.reportDate}
                                            onChange={(value: string) =>
                                                handleChange("reportDate", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>


                                </div>


                            </div>
                        </>
                    }} />
                    <Card data={{
                        title: {
                            content:
                                <div className="d-flex justify-content-start gap-3 align-items-center">
                                    <span className="summary-badge">
                                        7
                                    </span>
                                    <h5 className="_600"> Confirmation for Survey </h5>
                                </div>
                        },
                        body: <>
                            <div className="report-meta p-0">

                                <div className="d-flex justify-content-between gap-3 mb-3">
                                    <p className="mb-0">
                                        At the completion of the survey, it was confirmed that there were no areas of excessive or substantial corrosion remaining.
                                    </p>

                                    <div className="d-flex justify-content-start gap-3">

                                        <RadioInput
                                            label="Yes"
                                            checked={formData.survey_confirmed === "Yes"}
                                            onChange={() =>
                                                handleChange("survey_confirmed", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="No"
                                            checked={formData.survey_confirmed === "No"}
                                            onChange={() =>
                                                handleChange("survey_confirmed", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="N/A"
                                            checked={formData.survey_confirmed === "N/A"}
                                            onChange={() =>
                                                handleChange("survey_confirmed", "N/A")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                    </div>
                                </div>

                                <div className="d-flex justify-content-between gap-3 mb-3">

                                    <p className="mb-0">
                                        At the completion of the survey, it was confirmed that there were no outstanding Class recommendations or statutory deficiencies.
                                    </p>

                                    <div className="d-flex justify-content-start gap-3">

                                        <RadioInput
                                            label="Yes"
                                            checked={formData.class_recommendations === "Yes"}
                                            onChange={() =>
                                                handleChange("class_recommendations", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="No"
                                            checked={formData.class_recommendations === "No"}
                                            onChange={() =>
                                                handleChange("class_recommendations", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="N/A"
                                            checked={formData.class_recommendations === "N/A"}
                                            onChange={() =>
                                                handleChange("class_recommendations", "N/A")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                    </div>
                                </div>

                                <div className="d-flex justify-content-between gap-3 mb-3">

                                    <p className="mb-0">
                                        Fatigue and structurally sensitive areas identified in the SafeHull Fatigue Analysis were close-up examined and found satisfactory.
                                    </p>

                                    <div className="d-flex justify-content-start gap-3">

                                        <RadioInput
                                            label="Yes"
                                            checked={formData.fatigue_analysis === "Yes"}
                                            onChange={() =>
                                                handleChange("fatigue_analysis", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="No"
                                            checked={formData.fatigue_analysis === "No"}
                                            onChange={() =>
                                                handleChange("fatigue_analysis", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="N/A"
                                            checked={formData.fatigue_analysis === "N/A"}
                                            onChange={() =>
                                                handleChange("fatigue_analysis", "N/A")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                    </div>
                                </div>

                                <div className="d-flex justify-content-between gap-3 mb-3">

                                    <p className="mb-0">
                                        Modifications were carried out to the longitudinal connections as detailed under remarks.
                                    </p>

                                    <div className="d-flex justify-content-start gap-3">

                                        <RadioInput
                                            label="Yes"
                                            checked={formData.modifications === "Yes"}
                                            onChange={() =>
                                                handleChange("modifications", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="No"
                                            checked={formData.modifications === "No"}
                                            onChange={() =>
                                                handleChange("modifications", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="N/A"
                                            checked={formData.modifications === "N/A"}
                                            onChange={() =>
                                                handleChange("modifications", "N/A")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                    </div>
                                </div>

                                <div className="d-flex justify-content-between gap-3 mb-3">

                                    <p className="mb-0">
                                        Plating under bellmouths in ballast tanks was surveyed and found to be in acceptable condition without any significant scouring or pitting.
                                    </p>

                                    <div className="d-flex justify-content-start gap-3">

                                        <RadioInput
                                            label="Yes"
                                            checked={formData.plating_condition === "Yes"}
                                            onChange={() =>
                                                handleChange("plating_condition", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="No"
                                            checked={formData.plating_condition === "No"}
                                            onChange={() =>
                                                handleChange("plating_condition", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="N/A"
                                            checked={formData.plating_condition === "N/A"}
                                            onChange={() =>
                                                handleChange("plating_condition", "N/A")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                    </div>
                                </div>

                                <div className="d-flex justify-content-between gap-3 mb-3">

                                    <p className="mb-0">
                                        Bottom plating in way of cargo pump suction / bellmouths were examined and no significant scouring or pitting found.
                                    </p>

                                    <div className="d-flex justify-content-start gap-3">

                                        <RadioInput
                                            label="Yes"
                                            checked={formData.bottom_plating === "Yes"}
                                            onChange={() =>
                                                handleChange("bottom_plating", "Yes")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="No"
                                            checked={formData.bottom_plating === "No"}
                                            onChange={() =>
                                                handleChange("bottom_plating", "No")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                        <RadioInput
                                            label="N/A"
                                            checked={formData.bottom_plating === "N/A"}
                                            onChange={() =>
                                                handleChange("bottom_plating", "N/A")
                                            }
                                            disabled={isOfflineProject}
                                        />

                                    </div>
                                </div>

                            </div>
                            <div className="form-group mb-0">
                                <Label name="Remarks" bold />

                                <div className="mt-3">

                                    <RichTextEditor
                                        id="Remarks"
                                        value={formData.remarks || ""}
                                        onChange={(value: string) =>
                                            handleChange("remarks", value)
                                        }
                                        placeholder="Enter Additional Comments..."
                                        readOnly={isOfflineProject}
                                    />

                                </div>
                            </div>
                        </>
                    }} />

                    <Card data={{
                        title: {
                            content:
                                <div className="d-flex justify-content-start gap-3 align-items-center">

                                    <h5 className="_600"> Summary </h5>
                                </div>
                        },
                        body: <>
                            <div className="row">
                                <div className="col-md-12">
                                    <table className="table tbl-grading-table">

                                        <thead>
                                            <tr>
                                                <th className="row-label"> </th>
                                                <th className="row-label text-center">Grading</th>
                                                {GRADE_OPTIONS.map((opt) => (
                                                    <th key={String(opt)} >
                                                        {opt}
                                                    </th>
                                                ))}
                                            </tr>
                                        </thead>
                                        <tbody>

                                            {reportData?.template?.handireport?.grading?.map((grade: any) => (

                                                <React.Fragment key={grade.id}>

                                                    <tr>

                                                        <td className="row-label">
                                                            {grade.name}
                                                        </td>
                                                        <td>
                                                            <Input
                                                                placeholder=" "
                                                                value={
                                                                    grades[`${grade.id}_comment`] ??
                                                                    grade.value ??
                                                                    ""
                                                                }
                                                                onChange={(val: string) => {

                                                                    setGrades((prev: any) => ({
                                                                        ...prev,
                                                                        [`${grade.id}_comment`]: val
                                                                    }));

                                                                    updateGradingNotes({
                                                                        projectId: reportData?.projectId,
                                                                        templateId: reportData?.template?.id,
                                                                        sectionId: reportData?.template?.handireport?.sectionId,
                                                                        gradingId: grade.id,
                                                                        value: val
                                                                    }).catch(() => {
                                                                        handleSaveError();
                                                                    });
                                                                }}
                                                                maxLength={20}
                                                                disabled={isOfflineProject}
                                                            />
                                                        </td>
                                                        {grade.checkbox?.map((item: any) => (
                                                            <td key={item.id}>
                                                                <CheckboxInput
                                                                    checked={
                                                                        grades[grade.id]?.includes(
                                                                            item.name === "N/A"
                                                                                ? "N/A"
                                                                                : Number(item.name)
                                                                        ) || false
                                                                    }
                                                                    onChange={async (checked: boolean) => {

                                                                        const clickedValue =
                                                                            item.name === "N/A"
                                                                                ? "N/A"
                                                                                : Number(item.name);

                                                                        let current =
                                                                            grades[grade.id] || [];

                                                                        setGradingErrors(prev => ({
                                                                            ...prev,
                                                                            [grade.id]: ""
                                                                        }));

                                                                        if (clickedValue === "N/A") {

                                                                            const updated =
                                                                                checked ? ["N/A"] : [];

                                                                            setGrades((prev: any) => ({
                                                                                ...prev,
                                                                                [grade.id]: updated
                                                                            }));

                                                                            const choicesToUpdate = grade.checkbox.map((chk: any) => ({
                                                                                choiceName: chk.name,
                                                                                value: chk.name === "N/A" ? checked : false
                                                                            }));
                                                                            await handleUpdateGradingCondition(grade.id, choicesToUpdate);

                                                                            return;
                                                                        }

                                                                        current =
                                                                            current.filter((x: any) => x !== "N/A");

                                                                        let updated: any[] = [];

                                                                        if (checked) {

                                                                            updated = [...current, clickedValue];

                                                                        } else {

                                                                            updated =
                                                                                current.filter(
                                                                                    (x: any) => x !== clickedValue
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
                                                                                [grade.id]:
                                                                                    "Not examined cannot be selected"
                                                                            }));

                                                                            return;
                                                                        }

                                                                        if (updated.length > 2) {

                                                                            setGradingErrors(prev => ({
                                                                                ...prev,
                                                                                [grade.id]:
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
                                                                                [grade.id]:
                                                                                    "Adjacent grading only allowed"
                                                                            }));

                                                                            return;
                                                                        }

                                                                        setGradingErrors(prev => ({
                                                                            ...prev,
                                                                            [grade.id]: ""
                                                                        }));

                                                                        setGrades((prev: any) => ({
                                                                            ...prev,
                                                                            [grade.id]: updated
                                                                        }));

                                                                        const choicesToUpdate = grade.checkbox.map((chk: any) => ({
                                                                            choiceName: chk.name,
                                                                            value: chk.name === "N/A" ? false : updated.includes(Number(chk.name))
                                                                        }));
                                                                        await handleUpdateGradingCondition(grade.id, choicesToUpdate);
                                                                    }}
                                                                    disabled={isOfflineProject}
                                                                />
                                                            </td>
                                                        ))}
                                                    </tr>

                                                    {gradingErrors[grade.id] && (

                                                        <tr>

                                                            <td
                                                                colSpan={
                                                                    grade.checkbox.length + 2
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
                                                                    {gradingErrors[grade.id]}
                                                                </div>

                                                            </td>

                                                        </tr>
                                                    )}

                                                </React.Fragment>

                                            ))}

                                        </tbody>
                                    </table>
                                </div>

                            </div>
                            <Divider className="divider-1" />
                            <h3 className="form-group-title">
                                Survey Made Without Prejudice
                            </h3>

                            <div className="row">

                                <div className="col-md-4 form-group">

                                    <Label
                                        name="Lead Surveyor's Name"
                                        bold
                                    />

                                    <Input
                                        placeholder="Enter Lead Surveyor's Name"
                                        value={formData.leadsurveyername}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "leadsurveyername",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                                <div className="col-md-2 form-group">

                                    <Label
                                        name="Employee Number"
                                        bold
                                    />

                                    <Input
                                        placeholder="Enter Employee Number"
                                        value={formData.leademployee_no}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "leademployee_no",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                                <div className="col-md-2 form-group">

                                    <Label
                                        name="Last Visit Date(On board vessel)"
                                        bold
                                    />

                                    <DatePickerInput
                                        onChange={(date: any) =>
                                            handleChange(
                                                "visitDate",
                                                date
                                            )
                                        }
                                        value={formData.visitDate}
                                        disabled={isOfflineProject}
                                    />

                                </div>

                            </div>

                            <Divider className="divider-1" />

                            <h3 className="form-group-title">
                                Surveyor's Signature
                            </h3>

                            <div className="row">

                                <div className="col-md-4 form-group">

                                    <Label
                                        name="Lead Surveyor's Name"
                                        bold
                                    />

                                    <Input
                                        placeholder="Enter Surveyor's Name"
                                        value={formData.surveyername}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "surveyername",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                                <div className="col-md-2 form-group">

                                    <Label
                                        name="Employee Number"
                                        bold
                                    />

                                    <Input
                                        placeholder="Enter Employee Number"
                                        value={formData.employee_no}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "employee_no",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                            </div>

                            <Divider className="divider-1" />

                            <h3 className="form-group-title">
                                Other Individuals Participated in the Survey
                            </h3>

                            <div className="d-flex justify-content-start gap-3 mb-3">

                                <span className="sec-badge">
                                    A
                                </span>

                                <div className="w-100">

                                    <Input
                                        placeholder="User A"
                                        value={formData.individualA}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "individualA",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                            </div>

                            <div className="d-flex justify-content-start gap-3 mb-3">

                                <span className="sec-badge">
                                    B
                                </span>

                                <div className="w-100">

                                    <Input
                                        placeholder="User B"
                                        value={formData.individualB}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "individualB",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                            </div>

                            <div className="d-flex justify-content-start gap-3 mb-3">

                                <span className="sec-badge">
                                    C
                                </span>

                                <div className="w-100">

                                    <Input
                                        placeholder="User C"
                                        value={formData.individualC}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "individualC",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                            </div>

                            <div className="d-flex justify-content-start gap-3 mb-3">

                                <span className="sec-badge">
                                    D
                                </span>

                                <div className="w-100">

                                    <Input
                                        placeholder="User D"
                                        value={formData.individualD}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "individualD",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                            </div>

                            <div className="d-flex justify-content-start gap-3 mb-3">

                                <span className="sec-badge">
                                    E
                                </span>

                                <div className="w-100">

                                    <Input
                                        placeholder="User E"
                                        value={formData.individualE}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "individualE",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                            </div>

                            <div className="d-flex justify-content-start gap-3 mb-3">

                                <span className="sec-badge">
                                    F
                                </span>

                                <div className="w-100">

                                    <Input
                                        placeholder="User F"
                                        value={formData.individualF}

                                        onChange={(value: string) =>
                                            handleChange(
                                                "individualF",
                                                value
                                            )
                                        }
                                        disabled={isOfflineProject}
                                    />

                                </div>

                            </div>
                        </>
                    }} />
                </div>
            </div>
        </>
    );
}

export default PartSummary;

