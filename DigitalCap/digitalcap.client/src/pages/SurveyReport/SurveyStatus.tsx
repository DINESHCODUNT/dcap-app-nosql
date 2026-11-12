import React, { useEffect, useRef } from "react";
import ABSLogo from '../../assets/images/ABSLogo.svg';
import { Label, Input, AbsSelect } from "customer_portal-ui-shared";
import NotApplicableDateInput from "../../Common/NotApplicableDateInput";
import { updateReport } from "../../service/SurveyReportService";
import { parseVisitDate, formatVisitDateForSave, isEmptyDate } from "../../utils/dateTime";
import { NOT_APPLICABLE_DATE, isNotApplicableDate } from "../../utils/notApplicableDate";
import { usePopup } from "../../Common/PopupProvider";

type FormData = {
    report_no: string;
    portdate: Date | null;
    surveydueDate: Date | null;
    drydockdueDate: Date | null;
    due: Date | null;
    surveydue: Date | null;
    boilersurveydue: Date | null;
    tailshaftsurveydue: Date | null;
    vessel_name: string;
    name_prefix: string;
    vessel_type: string;
    sshull: string;
    imoNumber: string;
    flag: string;
    port: string;
};

const SURVEY_REPORT_PARTS = {
    report_no: 1,
    port: 2,
    portdate: 2,
    name_prefix: 90,
    surveydueDate: 33,
    drydockdueDate: 34,
    sshull: 35,
    due: 36,
    surveydue: 37,
    boilersurveydue: 38,
    tailshaftsurveydue: 39,
    lastVisitDate: 72,
    firstVisitDate: 73,
    vessel_name: 4,
};
const parseDate = (value: any): Date | null => {
    if (isNotApplicableDate(value)) {
        return NOT_APPLICABLE_DATE;
    }
    if (isEmptyDate(value)) {
        return null;
    }

    const date = new Date(value);

    return isNaN(date.getTime()) ? null : date;
};
const SurveyStatus: React.FC<{
    data: any;
    reportData: any;
    isOfflineProject?: boolean;
    onPortChange?: (newPort: string) => void;
    onReportNoChange?: (newReportNo: string) => void;
    onReportDateChange?: (newDate: any) => void;
    onLastVisitDateChange?: (newDate: any) => void;
    onFirstVisitDateChange?: (newDate: any) => void;
    onVesselFieldChange?: (field: string, value: any) => void;
    onSurveyStatusFieldChange?: (field: string, value: any, specialHullNo?: number | string) => void;
    onVesselNameChange?: (vesselName: string) => void;
}> = ({ reportData, isOfflineProject = false, onVesselFieldChange, onSurveyStatusFieldChange }) => {
    const popup = usePopup();
    const saveErrorOpenRef = useRef(false);

    const [formData, setFormData] = React.useState<FormData>({
        report_no: "",
        portdate: null,
        surveydueDate: null,
        drydockdueDate: null,
        due: null,
        surveydue: null,
        boilersurveydue: null,
        tailshaftsurveydue: null,
        vessel_name: "",
        name_prefix: "",
        vessel_type: "",
        sshull: "",
        imoNumber: "",
        flag: "",
        port: "",
    });

    const [portdate, setportDate] = React.useState<Date | null>(null);
    const [lastVisitDate, setLastVisitDate] = React.useState<Date | "">("");
    const [firstVisitDate, setFirstVisitDate] = React.useState<Date | "">("");
    const [surveydueDate, setSurveyDueDate] = React.useState<Date | null>(null);
    const [drydockdueDate, setDrydockDueDate] = React.useState<Date | null>(null);
    const [due, setDue] = React.useState<Date | null>(null);
    const [surveydue, setSurveyDue] = React.useState<Date | null>(null);
    const [boilersurveydue, setBoilerSurveyDue] = React.useState<Date | null>(null);
    const [tailshaftsurveydue, setTailshaftSurveyDue] = React.useState<Date | null>(null);

    useEffect(() => {
        if (!reportData) return;

        const survey =
            reportData?.surveyStatus ??
            reportData?.report?.surveyStatus ??
            {};

        const vessel =
            reportData?.vessel ??
            reportData?.report?.vessel ??
            {};
        const specialHullNo = Number(survey.specialHullNo);

        const specialHullDueDate =
            specialHullNo >= 1 && specialHullNo <= 15
                ? survey[`specialContinuousHull${specialHullNo}`]
                : null;

        setFormData(prev => ({
            ...prev,
            report_no: reportData?.reportNo ?? vessel.reportNo ?? "",
            vessel_name: vessel.vesselName || "",
            vessel_type: vessel.vesselType || "",
            sshull: survey.specialHullNo?.toString() || "",
            imoNumber: vessel.imoNumber || "",
            flag: vessel.flagName || "",
            name_prefix: vessel.selectedPrefix || "",
            port: reportData?.reportPort ?? reportData?.report?.reportPort ?? "",
        }));

        setportDate(parseDate(reportData.reportDate));
        setLastVisitDate(
            isNotApplicableDate(vessel.lastVisitDate)
                ? NOT_APPLICABLE_DATE
                : parseVisitDate(vessel.lastVisitDate)
        );
        setFirstVisitDate(
            isNotApplicableDate(vessel.firstVisitDate)
                ? NOT_APPLICABLE_DATE
                : parseVisitDate(vessel.firstVisitDate)
        );
        setSurveyDueDate(parseDate(survey.classSurveyDate));
        setDrydockDueDate(parseDate(survey.dryDockDate));
        setDue(parseDate(specialHullDueDate));
        setSurveyDue(parseDate(survey.specialContinuousMachineryDate));
        setBoilerSurveyDue(parseDate(survey.boilerDate));
        setTailshaftSurveyDue(parseDate(survey.tailshaftDate));
    }, [reportData]);
    const formatDateForApi = (date: Date): string => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");

        return `${year}-${month}-${day}`;
    };

    const visitDateValue = (d: any): Date | null => {
        if (!d || !(d instanceof Date) || isNaN(d.getTime()) || isNotApplicableDate(d)) return null;
        return d;
    };
    const handleSurveyUpdate = async (
        field: keyof typeof SURVEY_REPORT_PARTS,
        value: any
    ) => {
        const updatedFormData = {
            ...formData,
            [field]: value,
        };

        setFormData(updatedFormData);

        try {
            const formattedValue =
                value instanceof Date
                    ? (
                        field === "lastVisitDate" ||
                        field === "firstVisitDate"
                    )
                        ? formatVisitDateForSave(value)
                        : formatDateForApi(value)
                    : value;

            const surveyDateSource =
                lastVisitDate instanceof Date
                    ? lastVisitDate
                    : portdate;

            const sharedHeaderFields = [
                "report_no",
                "port",
                "name_prefix",
                "vessel_name",
                "lastVisitDate",
                "firstVisitDate",
            ];

            if (sharedHeaderFields.includes(field)) {
                onVesselFieldChange?.(field, formattedValue);
            }

            const surveyStatusFields = [
                "surveydueDate",
                "drydockdueDate",
                "sshull",
                "due",
                "surveydue",
                "boilersurveydue",
                "tailshaftsurveydue",
            ];

            if (surveyStatusFields.includes(field)) {
                const hullNo = field === "sshull" ? value : updatedFormData.sshull;
                onSurveyStatusFieldChange?.(field, value, hullNo);
            }

            await updateReport({
                projectId: reportData?.projectId,
                templateId: reportData?.templateId || 1,
                reportPart: SURVEY_REPORT_PARTS[field],
                value: formattedValue,
                imonumber: Number(updatedFormData.imoNumber),

                surveyDate:
                    surveyDateSource instanceof Date && !isNotApplicableDate(surveyDateSource)
                        ? formatDateForApi(surveyDateSource)
                        : "",
            });

        } catch (error) {
            if (!saveErrorOpenRef.current) {
                saveErrorOpenRef.current = true;
                popup.error("Your change could not be saved. Please try again.", "Save").finally(() => {
                    saveErrorOpenRef.current = false;
                });
            }
        }
    };

    return (
        <div className="report-content">
            <div className="project-header">
                <div className="section-title p-3 pe-5 border-bottom">
                    <h5>Survey Status - {formData.vessel_name}</h5>
                </div>
            </div>
            <div className="report-area">
                <div className="report-header">
                    <div className="section-title d-flex flex-column flex-md-row align-items-start align-items-md-center gap-4 p-3 p-md-4">
                        <img src={ABSLogo} alt="Vessel Main Data" className="report-header-image mb-3 mb-md-0" />
                        <div className="report-header-fields">
                            <div className="report-header-row">
                                <div className="report-header-label">
                                    <Label name="Report No" bold inline />
                                </div>
                                <div>
                                    <Input
                                        placeholder="Enter Report No"
                                        value={formData.report_no}
                                        onChange={(value: string) => {
                                            setFormData(prev => ({ ...prev, report_no: value }));
                                            handleSurveyUpdate("report_no", value);
                                        }}
                                        disabled={isOfflineProject}
                                    />
                                </div>
                            </div>
                            <div className="report-header-row">
                                <div className="report-header-label">
                                    <Label name="Port/Date" bold inline />
                                </div>
                                <div className="d-flex flex-column flex-md-row gap-2">
                                    <Input
                                        placeholder="Enter Port"
                                        value={formData.port || ""}
                                        onChange={(value: string) => {
                                            setFormData((prev) => ({ ...prev, port: value }));
                                            handleSurveyUpdate("port", value);
                                        }}
                                        disabled={isOfflineProject}
                                    />
                                    <NotApplicableDateInput
                                        onChange={(date: any) => {
                                            const firstVal = visitDateValue(firstVisitDate);
                                            const lastVal = visitDateValue(date);
                                            if (firstVal && lastVal && firstVal > lastVal) {
                                                setLastVisitDate("");
                                                handleSurveyUpdate("lastVisitDate", null);
                                                return;
                                            }
                                            setLastVisitDate(date);
                                            handleSurveyUpdate("lastVisitDate", date);
                                        }}
                                        value={lastVisitDate instanceof Date ? lastVisitDate : null}
                                        minDate={visitDateValue(firstVisitDate) ?? undefined}
                                        disabled={isOfflineProject}
                                    />
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="report-meta">
                        <div className="inspection-statement">
                            <p className="_400">
                                <span className="text-uppercase _500 certify">This is to Certify</span><span className="certify">that the undersigned Surveyors to this Bureau did, at the request of the Owner's Representative, attend the</span>

                                <span className="inline-control certify">
                                    <AbsSelect
                                        key={String(formData.name_prefix)}
                                        mult={false}

                                        onChange={(selected: any) => {
                                            const selectedValue =
                                                selected?.[0]?.value ||
                                                selected?.[0] ||
                                                "";

                                            setFormData((prev: any) => ({
                                                ...prev,
                                                name_prefix: String(selectedValue)
                                            }));

                                            handleSurveyUpdate("name_prefix", String(selectedValue));
                                        }}
                                        options={[
                                            { text: "SS", value: "2" },
                                            { text: "M/V", value: "1" },
                                        ]}
                                        selected={
                                            formData.name_prefix
                                                ? [String(formData.name_prefix)]
                                                : []
                                        }
                                        placeholder="Select "
                                        disabled={isOfflineProject}
                                    />
                                </span>
                                <input
                                    type="text"
                                    className="form-control inline-input text-uppercase fw-bold text-dark mx-2"
                                    style={{ display: "inline-block", padding: "2px 5px", height: "30px", width: `${Math.max(formData.vessel_name?.length || 10, 10)}ch` }}
                                    value={formData.vessel_name || ""}
                                    onChange={(e) => {
                                        handleSurveyUpdate("vessel_name", e.target.value);
                                    }}
                                    disabled={isOfflineProject}
                                /><span> of </span>
                                <span className="text-uppercase _500 certify">
                                    {formData.flag}
                                </span>

                                <span className="text-uppercase _500 certify">
                                    IMO No. {formData.imoNumber}
                                </span>
                                <span className="certify">as the vessel lay afloat on drydock and underway on  </span>  <span className="inline-group certify">
                                    <span className="inline-control inline-control--date">
                                        <NotApplicableDateInput
                                            onChange={(date: any) => {
                                                const firstVal = visitDateValue(date);
                                                const lastVal = visitDateValue(lastVisitDate);
                                                if (firstVal && lastVal && firstVal > lastVal) {
                                                    setLastVisitDate("");
                                                    handleSurveyUpdate("lastVisitDate", null);
                                                }
                                                setFirstVisitDate(date);
                                                handleSurveyUpdate("firstVisitDate", date);
                                            }}
                                            value={firstVisitDate instanceof Date ? firstVisitDate : null}
                                            maxDate={visitDateValue(lastVisitDate) ?? undefined}
                                            disabled={isOfflineProject}
                                        />
                                    </span>
                                </span>
                                <span className="certify">and subsequent dates in order to examine and report upon the vessel relative to Condition Assessment Survey.</span>
                            </p>
                            <p className="mb-0 _400">For further particulars see report as follows</p>


                        </div>
                    </div>
                </div>
                <div className="report-formdata">
                    <div className="row">
                        <div className="col-md-12">
                            <div className="card-area">
                                <h3 className="form-group-title">Survey Status</h3>
                                <div className="row">
                                    <div className="col-md-3 form-group">
                                        <Label name="Annual Class Surveys Due" bold />
                                        <NotApplicableDateInput
                                            value={surveydueDate}
                                            onChange={(date: any) => {
                                                setSurveyDueDate(date);
                                                handleSurveyUpdate(
                                                    "surveydueDate",
                                                    date
                                                );
                                            }}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-3 form-group">
                                        <Label name="Drydocking Due" bold />
                                        <NotApplicableDateInput
                                            value={drydockdueDate}
                                            onChange={(date: any) => {
                                                setDrydockDueDate(date);
                                                handleSurveyUpdate(
                                                    "drydockdueDate",
                                                    date
                                                );
                                            }}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-3 form-group">
                                        <Label name="Special Survey Hull#" bold />
                                        <Input
                                            placeholder="Enter Hull#"
                                            value={formData.sshull}
                                            onChange={(value: string) => {

                                                const numericValue = Number(value);

                                                if (value === "") {
                                                    handleSurveyUpdate("sshull", value);
                                                    return;
                                                }

                                                if (numericValue >= 1 && numericValue <= 15) {

                                                    handleSurveyUpdate(
                                                        "sshull",
                                                        value
                                                    );

                                                }

                                            }}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-3 form-group">
                                        <Label name="Due" bold />
                                        <NotApplicableDateInput
                                            value={due}
                                            onChange={(date: any) => {
                                                setDue(date);
                                                handleSurveyUpdate(
                                                    "due",
                                                    date
                                                );
                                            }}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-3 form-group">
                                        <Label
                                            name="Special/Continuous Machinery Survey Due"
                                            bold
                                        />

                                        <NotApplicableDateInput
                                            value={surveydue}
                                            onChange={(date: any) => {
                                                setSurveyDue(date);
                                                handleSurveyUpdate(
                                                    "surveydue",
                                                    date
                                                );
                                            }}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-3 form-group">
                                        <Label name="Boiler Survey(s) Due" bold />
                                        <NotApplicableDateInput
                                            value={boilersurveydue}
                                            onChange={(date: any) => {
                                                setBoilerSurveyDue(date);
                                                handleSurveyUpdate(
                                                    "boilersurveydue",
                                                    date
                                                );
                                            }}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-3 form-group">
                                        <Label name="Tailshaft Survey Due" bold />
                                        <NotApplicableDateInput
                                            value={tailshaftsurveydue}
                                            onChange={(date: any) => {

                                                setTailshaftSurveyDue(date);

                                                handleSurveyUpdate(
                                                    "tailshaftsurveydue",
                                                    date
                                                );

                                            }}
                                            disabled={isOfflineProject}
                                        />
                                    </div>

                                    <div className="col-12 mt-3">
                                        <div className="note-box">
                                            <span className="fw-bold d-block text-dark small mb-1">NOTE:</span>
                                            <span className="text-muted small">
                                                Above should reflect dates applicable upon completion of any surveys at this attendance.
                                            </span>
                                        </div>
                                    </div>

                                </div>

                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default SurveyStatus;
