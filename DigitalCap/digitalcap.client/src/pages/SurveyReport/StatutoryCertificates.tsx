import React, { useState, useEffect, useRef } from "react";
import moment from "moment";
import ABSLogo from '../../assets/images/ABSLogo.svg';
import { Label, Input, AbsSelect } from "customer_portal-ui-shared";
import NotApplicableDateInput from "../../Common/NotApplicableDateInput";
import { updateReport } from "../../service/SurveyReportService";
import { parseVisitDate, formatVisitDateForSave, isEmptyDate, DISPLAY_DATE_FORMAT } from "../../utils/dateTime";
import { NOT_APPLICABLE_DATE, isNotApplicableDate } from "../../utils/notApplicableDate";
import { usePopup } from "../../Common/PopupProvider";

interface CertField {
    id: string;
    label: string;
    date: string;

}

interface CertSection {
    badge: string;
    name: string;
    color: string;
    fields: CertField[];
}

const STATUTORY_REPORT_PARTS: Record<string, number> = {
    report_no: 1,
    port: 2,
    portdate: 2,
    name_prefix: 90,
    loadline: 40,
    "safety-construction": 41,
    "safety-equipment": 42,
    "safety-radio": 43,
    iopp: 44,
    iapp: 45,
    ispp: 46,
    iee: 47,
    "cargo-annual": 48,
    "cargo-retest": 49,
    iafs: 50,
    ibwm: 51,
    ism: 52,
    isps: 53,
    mlc: 54,
    "gas-carrier-fitness": 74,
    lastVisitDate: 72,
    firstVisitDate: 73,
};

type FormData = {
    report_no: string;
    portdate: Date | null;
    vessel_name: string;
    name_prefix: string;
    imoNumber: string;
    flag: string;
    port: string;
};

const CERT_SECTIONS: CertSection[] = [
    {
        badge: 'A',
        name: 'Load Line',
        color: '#3b5bdb',
        fields: [
            { id: 'loadline', label: 'Loadline Certificate Valid To', date: '' },
        ],
    },
    {
        badge: 'B',
        name: 'SOLAS',
        color: '#0ca678',
        fields: [
            { id: 'safety-construction', label: 'Safety Construction Certificate Valid To', date: '' },
            { id: 'safety-equipment', label: 'Safety Equipment Certificate Valid To', date: '' },
            { id: 'safety-radio', label: 'Safety Radio Certificate Valid To', date: '' },
        ],
    },
    {
        badge: 'C',
        name: 'MARPOL',
        color: '#e67700',
        fields: [
            { id: 'iopp', label: 'IOPP Certificate Valid To', date: '' },
            { id: 'iapp', label: 'IAPP Certificate Valid To', date: '' },
            { id: 'ispp', label: 'ISPP (Sewage) Certificate Valid To', date: '' },
            { id: 'iee', label: 'IEE (Energy Efficiency) Certificate Issued Date', date: '' },
        ],
    },
    {
        badge: 'D',
        name: 'Cargo Gear',
        color: '#9c36b5',
        fields: [
            { id: 'cargo-annual', label: 'Cargo Gear Annual Certificate Issued On', date: '' },
            { id: 'cargo-retest', label: 'Cargo Gear Retesting Certificate Valid To', date: '' },
        ],
    },
    {
        badge: 'E',
        name: 'Other',
        color: '#c92a2a',
        fields: [
            { id: 'iafs', label: 'IAFS Certificate Issued Date', date: '' },
            { id: 'ibwm', label: 'IBWM (Ballast Water) Certificate Valid To', date: '' },
            { id: 'ism', label: 'ISM Certificate Valid To', date: '' },
            { id: 'isps', label: 'ISPS Certificate Valid To', date: '' },
            { id: 'mlc', label: 'Maritime Labour Certificate Valid To', date: '' },
        ],
    },
];

const StatutoryCertificates: React.FC<{
    data: any;
    reportData: any;
    isOfflineProject?: boolean;
    onPortChange?: (newPort: string) => void;
    onReportNoChange?: (newReportNo: string) => void;
    onReportDateChange?: (newDate: any) => void;
    onLastVisitDateChange?: (newDate: any) => void;
    onFirstVisitDateChange?: (newDate: any) => void;
    onVesselFieldChange?: (field: string, value: any) => void;
    onVesselNameChange?: (vesselName: string) => void;
}> = ({ reportData, isOfflineProject = false, onVesselFieldChange }) => {
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

    const [formData, setFormData] = useState<FormData>({
        report_no: "",
        portdate: null,
        vessel_name: "",
        name_prefix: "",
        imoNumber: "",
        flag: "",
        port: "",
    });
    const [portdate, setPortDate] = React.useState<Date | null>(null);
    const [lastVisitDate, setLastVisitDate] = React.useState<Date | "">("");
    const [firstVisitDate, setFirstVisitDate] = React.useState<Date | "">("");
    const [dates, setDates] = useState<Record<string, string>>(
        () => Object.fromEntries(
            CERT_SECTIONS.flatMap((s) => s.fields.map((f) => [f.id, f.date]))
        )
    );
    const parseDate = (dateStr: string | any): Date | null => {
        if (isNotApplicableDate(dateStr)) return NOT_APPLICABLE_DATE;
        if (isEmptyDate(dateStr)) return null;
        const d = new Date(dateStr);
        return isNaN(d.getTime()) ? null : d;
    };

    useEffect(() => {
        if (!reportData) return;

        const vessel = reportData.vessel || reportData.report.vessel || {};
        const exp = reportData.statutoryCertificatesExpirationDates || reportData.report.statutoryCertificatesExpirationDates || {};
        const iss = reportData.statutoryCertificatesIssuedDates || reportData.report.statutoryCertificatesIssuedDates || {};

        setFormData({
            report_no: reportData?.reportNo ?? vessel.reportNo ?? "",
            portdate: !isEmptyDate(vessel.portDate) ? new Date(vessel.portDate) : null,
            vessel_name: vessel.vesselName || "",
            name_prefix: vessel.selectedPrefix || "",
            imoNumber: vessel.imoNumber || "",
            flag: vessel.flagName || "",
            port: reportData?.reportPort ?? reportData?.report?.reportPort ?? "",
        });

        if (!isEmptyDate(vessel.portDate)) {
            setPortDate(new Date(vessel.portDate));
        }

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

        setDates({
            loadline: exp.llliDate,

            "safety-construction": exp.safetyConDate,
            "safety-equipment": exp.safetyEquipmentDate,
            "safety-radio": exp.safetyRadioDate,

            iopp: exp.ioppDate,
            iapp: exp.iapDate,
            ispp: exp.isppSewageDate,
            iee: iss.ieeDate,

            "cargo-annual": iss.cargoGearAnnualCertificate,
            "cargo-retest": exp.cargoGearReTestDate,

            iafs: iss.iafsDate,
            ibwm: exp.ibwmDate,
            ism: exp.ismDate,
            isps: exp.ispsDate,
            mlc: exp.maritimeLabourDate,
        });

    }, [reportData]);
    const getDateStatus = (dateStr: string) => {
        if (isEmptyDate(dateStr) || isNotApplicableDate(dateStr)) {
            return { color: 'transparent', status: '' };
        }
        const today = new Date();
        const date = new Date(dateStr);

        const diffTime = date.getTime() - today.getTime();
        const diffDays = diffTime / (1000 * 60 * 60 * 24);

        if (diffDays < 0) {
            return { color: '#e03131', status: 'Expired' };
        } else if (diffDays <= 90) {
            return { color: '#f08c00', status: 'Expiring Soon' };
        } else {
            return { color: '#2f9e44', status: 'Valid' };
        }
    };

    const visitDateValue = (d: any): Date | null => {
        if (!d || !(d instanceof Date) || isNaN(d.getTime()) || isNotApplicableDate(d)) return null;
        return d;
    };

    const handleVisitDateChange = async (
        field: "lastVisitDate" | "firstVisitDate",
        date: Date | null
    ) => {
        if (field === "lastVisitDate") {
            setLastVisitDate(date ?? "");
        } else {
            setFirstVisitDate(date ?? "");
        }

        try {
            const formatted = date ? formatVisitDateForSave(date) : null;
            onVesselFieldChange?.(field, formatted);

            const surveyDateSource =
                lastVisitDate instanceof Date ? lastVisitDate : portdate;

            await updateReport({
                projectId: reportData?.projectId,
                templateId: reportData?.templateId || 0,
                reportPart: STATUTORY_REPORT_PARTS[field],
                value: formatted,
                imonumber: Number(formData.imoNumber),
                surveyDate:
                    surveyDateSource instanceof Date && !isNotApplicableDate(surveyDateSource)
                        ? surveyDateSource.toISOString()
                        : "",
            });

        } catch (error) {
            handleSaveError();
        }
    };

    const formatDate = (date: Date): string => {
        return moment(date).locale("en").format(DISPLAY_DATE_FORMAT);
    };
    const handleDateChange = async (
        id: string,
        val: Date | null
    ): Promise<void> => {

        const formattedDate = val
            ? (isNotApplicableDate(val) ? "1900-01-01" : formatDate(val))
            : null;

        setDates((prev) => ({
            ...prev,
            [id]: formattedDate ?? ""
        }));

        try {

            await updateReport({
                projectId: reportData?.projectId,
                templateId: reportData?.templateId || 0,
                reportPart: STATUTORY_REPORT_PARTS[id],
                value: formattedDate,
                imonumber: Number(formData.imoNumber),
                surveyDate:
                    portdate instanceof Date && !isNotApplicableDate(portdate)
                        ? portdate.toISOString()
                        : "",
            });

        } catch (error) {
            handleSaveError();
        }
    };
    return (
        <>
            <div className="report-content">
                <div className="project-header">
                    <div className="section-title p-3 pe-5 border-bottom">
                        <h5>Statutory Certificates  -  {formData.vessel_name}</h5>
                    </div>
                </div>
                <div className="report-area">
                    <div className="report-header ">
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
                                                setFormData(prev => ({
                                                    ...prev,
                                                    report_no: value
                                                }));
                                                updateReport({
                                                    projectId: reportData?.projectId,
                                                    templateId: reportData?.templateId || 0,
                                                    reportPart: STATUTORY_REPORT_PARTS.report_no,
                                                    value: value,
                                                    imonumber: Number(formData.imoNumber),
                                                    surveyDate:
                                                        portdate instanceof Date
                                                            ? portdate.toISOString()
                                                            : "",
                                                }).catch(handleSaveError);
                                                onVesselFieldChange?.("report_no", value);
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
                                                updateReport({
                                                    projectId: reportData?.projectId,
                                                    templateId: reportData?.templateId || 0,
                                                    reportPart: STATUTORY_REPORT_PARTS.port,
                                                    value: value,
                                                    imonumber: Number(formData.imoNumber),
                                                    surveyDate:
                                                        portdate instanceof Date
                                                            ? portdate.toISOString()
                                                            : "",
                                                }).catch(handleSaveError);
                                                onVesselFieldChange?.("port", value);
                                            }}
                                            disabled={isOfflineProject}
                                        />
                                        <NotApplicableDateInput
                                            onChange={(date: any) => {
                                                const firstVal = visitDateValue(firstVisitDate);
                                                const lastVal = visitDateValue(date);
                                                if (firstVal && lastVal && firstVal > lastVal) {
                                                    setLastVisitDate("");
                                                    handleVisitDateChange("lastVisitDate", null);
                                                    return;
                                                }
                                                handleVisitDateChange("lastVisitDate", date);
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

                                                updateReport({
                                                    projectId: reportData?.projectId,
                                                    templateId: reportData?.templateId || 0,
                                                    reportPart: STATUTORY_REPORT_PARTS.name_prefix,
                                                    value: String(selectedValue),
                                                    imonumber: Number(formData.imoNumber),
                                                    surveyDate:
                                                        portdate instanceof Date
                                                            ? portdate.toISOString()
                                                            : "",
                                                }).catch(handleSaveError);
                                                onVesselFieldChange?.("name_prefix", String(selectedValue));
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
                                            setFormData((prev: any) => ({ ...prev, vessel_name: e.target.value }));
                                            updateReport({
                                                projectId: reportData?.projectId,
                                                templateId: reportData?.templateId || 0,
                                                reportPart: 4,
                                                value: e.target.value,
                                                imonumber: Number(formData.imoNumber),
                                                surveyDate:
                                                    portdate instanceof Date
                                                        ? portdate.toISOString()
                                                        : "",
                                            }).catch(handleSaveError);
                                            onVesselFieldChange?.("vessel_name", e.target.value);
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
                                                        handleVisitDateChange("lastVisitDate", null);
                                                    }
                                                    handleVisitDateChange("firstVisitDate", date);
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
                    <div className="report-formdata card-area">
                        <h3 className="form-group-title">Statutory Certificates</h3>
                        <div className="row">
                            <div className="col-md-8">
                                <div className="sc-wrapper ">
                                    <div className="sc-timeline">
                                        {CERT_SECTIONS.map((sec, secIdx) => {
                                            const isLast = secIdx === CERT_SECTIONS.length - 1;

                                            return (
                                                <div
                                                    key={sec.badge}
                                                    className="sc-section"
                                                    style={{ marginBottom: isLast ? 0 : '24px' }}
                                                >
                                                    <div className="sc-section-label">
                                                        <span className="sc-badge"  >
                                                            {sec.badge}
                                                        </span>
                                                        <span className="sc-section-name">{sec.name}</span>
                                                    </div>

                                                    <div className="sc-card">
                                                        {sec.fields.map((field) => {
                                                            const currentDate = dates[field.id] ?? field.date;
                                                            const { color, status } = getDateStatus(currentDate);

                                                            return (
                                                                <div
                                                                    key={field.id}
                                                                    className="sc-field flex-column flex-md-row align-items-start align-items-md-center"
                                                                >
                                                                    <div className="d-flex gap-2 justify-content-start align-items-center w-100 w-md-auto mb-2 mb-md-0"> 
                                                                        <span
                                                                            className="sc-bullet"
                                                                            style={{ background: color }}
                                                                        />
                                                                        <span className="sc-field-label" style={{ whiteSpace: "normal", flexShrink: 1 }}>
                                                                            {field.label}
                                                                        </span>
                                                                    </div>
                                                                    <div className="d-flex justify-content-between justify-content-md-end align-items-center gap-3 w-100 w-md-auto">
                                                                        <div>
                                                                            <span className="flex-shrink-0 fw-bold" style={{ color }}>{status ? `(${status})` : ""}</span>
                                                                        </div>
                                                                        <div>
                                                                            <NotApplicableDateInput
                                                                                value={parseDate(currentDate)}
                                                                                onChange={(val: Date | null) => handleDateChange(field.id, val)}
                                                                                disabled={isOfflineProject}
                                                                            />
                                                                        </div>

                                                                    </div>
                                                                </div>
                                                            );
                                                        })}
                                                    </div>
                                                </div>
                                            );
                                        })}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
};

export default StatutoryCertificates;
