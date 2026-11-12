import React, { useEffect, useRef } from "react";
import ABSLogo from '../../assets/images/ABSLogo.svg';
import { Label, Input, Divider, AbsSelect } from "customer_portal-ui-shared";
import NotApplicableDateInput from "../../Common/NotApplicableDateInput";
import RichTextEditor from "../../Common/RichTextEditor";
import AppLoader from '../../Common/AppLoader';
import { updateReport } from "../../service/SurveyReportService";
import { formatClassNotationsForDisplay, formatClassNotationsForSave } from "../../utils/classNotationFormatter";
import { parseVisitDate, formatVisitDateForSave, isEmptyDate } from "../../utils/dateTime";
import { NOT_APPLICABLE_DATE, isNotApplicableDate } from "../../utils/notApplicableDate";
import { usePopup } from "../../Common/PopupProvider";

type FormData = {
    report_no: string;
    portdate: Date;
    vessel_name: string;
    name_prefix: string;
    vessel_type: string;
    year_built: string;
    year_built_by: string;
    hull_no: string;
    flag: string;
    port: string;
    port_of_registry: string;
    official_no: string;
    call_sign: string;
    classed: string;
    class_no: string;
    class_notations: string;
    additional_class_notations: string;
    other_notations: string;
    previous_names: string;
    owners: string;
    managers: string;
    length: string;
    breadth: string;
    depth: string;
    summer_draft: string;
    deadweight: string;
    gross_tons: string;
    net_tons: string;
    propulsion_type: string;
    propulsion_value: string;
    kw: string;
    propulsion_manufacturer: string;
    gear_manufacturer: string;
    imoNumber: string;
    firstVisitDate: Date | "" | null;
    lastVisitDate: Date | "" | null;
};

interface Props {
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
}

const VesselMainData: React.FC<Props> = ({ reportData, isOfflineProject, onVesselFieldChange }) => {
    const popup = usePopup();
    const saveErrorOpenRef = useRef(false);

    const [formData, setFormData] = React.useState<FormData>({
        report_no: "",
        portdate: new Date(),
        vessel_name: "",
        name_prefix: "",
        vessel_type: "",
        year_built: "",
        year_built_by: "",
        hull_no: "",
        flag: "",
        port: "",
        port_of_registry: "",
        official_no: "",
        call_sign: "",
        classed: "",
        class_no: "",
        class_notations: "",
        additional_class_notations: "",
        other_notations: "",
        previous_names: "",
        owners: "",
        managers: "",
        length: "",
        breadth: "",
        depth: "",
        summer_draft: "",
        deadweight: "",
        gross_tons: "",
        net_tons: "",
        propulsion_type: "",
        propulsion_value: "",
        kw: "",
        propulsion_manufacturer: "",
        gear_manufacturer: "",
        imoNumber: "",
        firstVisitDate: "",
        lastVisitDate: ""
    });

    const [portdate, setportDate] = React.useState(new Date());
    const [loading, setLoading] = React.useState(!reportData);
    const REPORT_PARTS: Record<string, number> = {
        report_no: 1,
        port: 2,
        portdate: 2,
        name_prefix: 90,
        vessel_name: 4,
        vessel_type: 5,
        year_built: 6,
        year_built_by: 7,
        hull_no: 8,
        flag: 9,
        port_of_registry: 10,
        official_no: 11,
        call_sign: 12,
        classed: 13,
        class_no: 14,
        class_notations: 15,
        additional_class_notations: 16,
        other_notations: 17,
        previous_names: 18,
        owners: 19,
        managers: 20,
        length: 21,
        breadth: 22,
        depth: 23,
        summer_draft: 24,
        deadweight: 25,
        gross_tons: 26,
        net_tons: 27,
        propulsion_type: 28,
        kw: 29,
        propulsion_value: 30,
        propulsion_manufacturer: 31,
        gear_manufacturer: 32,
        firstVisitDate: 73,
        lastVisitDate: 72
    };
    useEffect(() => {

        if (!reportData) {
            setLoading(true);
            return;
        }

        const vessel = reportData?.vessel ?? reportData?.report?.vessel ?? {};
        setFormData({
            report_no:
                reportData?.reportNo ??
                reportData?.report?.reportNo ??
                "",

            portdate:
                !isEmptyDate(vessel.portDate)
                    ? new Date(vessel.portDate)
                    : new Date(),

            vessel_name: vessel.vesselName || "",
            name_prefix: vessel.selectedPrefix || "",

            vessel_type: vessel.vesselType || "",
            year_built: vessel.yearBuilt?.toString() || "",
            year_built_by: vessel.builtBy || "",

            hull_no: vessel.hullNo || "",

            flag: vessel.flagName || "",
            port: reportData?.reportPort ?? reportData?.report?.reportPort ?? "",
            port_of_registry: vessel.homeport || "",

            official_no: vessel.officalNumber || "",
            call_sign: vessel.callSign || "",

            classed: vessel.classed || "",
            class_no: vessel.classNo || "",

            class_notations: formatClassNotationsForDisplay(vessel.classNotation || ""),
            additional_class_notations: formatClassNotationsForDisplay(vessel.machinery || ""),

            other_notations: formatClassNotationsForDisplay(vessel.other || ""),
            previous_names: vessel.previousName || "",

            owners: vessel.ownerName || "",
            managers: vessel.manager || "",

            length: vessel.length?.toString() || "",
            breadth: vessel.breadth?.toString() || "",
            depth: vessel.depth?.toString() || "",

            summer_draft: vessel.draft?.toString() || "",

            deadweight: vessel.deadweight || "",
            gross_tons: vessel.grossTons?.toString() || "",
            net_tons: vessel.netTons?.toString() || "",

            propulsion_type: vessel.propulsionType || "",
            propulsion_value: vessel.shaftRPM?.toString() || "",
            kw: vessel.kw || "",

            propulsion_manufacturer: vessel.propulsionManufacturer || "",
            gear_manufacturer: vessel.gearManufacturer || "",
            imoNumber: vessel.imoNumber || "",
            firstVisitDate: isNotApplicableDate(vessel.firstVisitDate)
                ? NOT_APPLICABLE_DATE
                : (parseVisitDate(vessel.firstVisitDate) || ""),
            lastVisitDate: isNotApplicableDate(vessel.lastVisitDate)
                ? NOT_APPLICABLE_DATE
                : (parseVisitDate(vessel.lastVisitDate) || ""),
        });
        if (!isEmptyDate(vessel.portDate)) {
            setportDate(new Date(vessel.portDate));
        }

        setLoading(false);

    }, [reportData]);

    const getEditorHtml = (editor: any) => {
        if (!editor) return "";
        const text = typeof editor.getText === "function" ? editor.getText().trim() : "";
        if (!text) return "";
        return typeof editor.getHTML === "function" ? editor.getHTML() : "";
    };

    const visitDateValue = (d: any): Date | null => {
        if (!d || !(d instanceof Date) || isNaN(d.getTime()) || isNotApplicableDate(d)) return null;
        return d;
    };

    const handleFieldUpdate = async (
        field: keyof FormData,
        value: any
    ) => {

        const updatedFormData = {
            ...formData,
            [field]: value,
        };

        setFormData(updatedFormData);

        let saveValue = value;
        if (["class_notations", "additional_class_notations", "other_notations"].includes(field)) {
            saveValue = formatClassNotationsForSave(value);
        } else if (value instanceof Date) {
            saveValue = formatVisitDateForSave(value);
        } else if ((field === "firstVisitDate" || field === "lastVisitDate") && !value) {
            saveValue = null;
        }

        onVesselFieldChange?.(field, saveValue);

        try {

            await updateReport({
                projectId: reportData?.projectId,
                templateId: reportData?.templateId || 1,
                reportPart: REPORT_PARTS[field],
                value: saveValue,
                imonumber: Number(updatedFormData.imoNumber),
                hullNo: Number(updatedFormData.hull_no) || 1,
                surveyDate:
                    portdate instanceof Date
                        ? portdate.toISOString()
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

    if (loading) {
        return <AppLoader inline />;
    }
    return (
        <div className="report-content">
            <div className="project-header">
                <div className="section-title p-3 pe-5 border-bottom">
                    <h5>Vessel Main Data - {formData.vessel_name}</h5>
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
                                        value={formData.report_no || ""}
                                        onChange={(value: string) => {
                                            handleFieldUpdate("report_no", value);
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
                                            handleFieldUpdate("port", value);
                                        }}
                                        disabled={isOfflineProject}
                                    />
                                    <NotApplicableDateInput
                                        onChange={(value: any) => {
                                            const firstVal = visitDateValue(formData.firstVisitDate);
                                            const lastVal = visitDateValue(value);
                                            if (firstVal && lastVal && firstVal > lastVal) {
                                                handleFieldUpdate("lastVisitDate", null);
                                                return;
                                            }
                                            handleFieldUpdate("lastVisitDate", value);
                                        }}
                                        value={formData.lastVisitDate instanceof Date ? formData.lastVisitDate : null}
                                        minDate={visitDateValue(formData.firstVisitDate) ?? undefined}
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
                                            handleFieldUpdate("name_prefix", String(selected?.[0] || ""))
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
                                        placeholder="Select"
                                        disabled={isOfflineProject}
                                    />
                                </span>
                                <input
                                    type="text"
                                    className="form-control inline-input text-uppercase fw-bold text-dark mx-2"
                                    style={{ display: "inline-block", padding: "2px 5px", height: "30px", width: `${Math.max(formData.vessel_name?.length || 10, 10)}ch` }}
                                    value={formData.vessel_name || ""}
                                    onChange={(e) => {
                                        handleFieldUpdate("vessel_name", e.target.value);
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
                                            onChange={(value: any) => {
                                                const firstVal = visitDateValue(value);
                                                const lastVal = visitDateValue(formData.lastVisitDate);
                                                if (firstVal && lastVal && firstVal > lastVal) {
                                                    handleFieldUpdate("lastVisitDate", null);
                                                }
                                                handleFieldUpdate("firstVisitDate", value);
                                            }}
                                            value={formData.firstVisitDate instanceof Date ? formData.firstVisitDate : null}
                                            maxDate={visitDateValue(formData.lastVisitDate) ?? undefined}
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
                                <h3 className="form-group-title">Vessel's Primary  Information</h3>
                                <div className="row">
                                    <div className="col-md-2 form-group">
                                        <Label name="Type Of Vessel" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Vessel Type"
                                            value={formData.vessel_type}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("vessel_type", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Year Built" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Year Built"
                                            value={formData.year_built}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("year_built", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name=" Built by" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Builder"
                                            value={formData.year_built_by}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("year_built_by", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Hull No" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Hull No"
                                            value={formData.hull_no}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("hull_no", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Flag" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Flag"
                                            value={formData.flag}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("flag", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Port of Registry" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Port of Registry"
                                            value={formData.port_of_registry}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("port_of_registry", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Official No" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Official No"
                                            value={formData.official_no}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("official_no", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Call Sign" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Call Sign"
                                            value={formData.call_sign}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("call_sign", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Classed" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Classed"
                                            value={formData.classed}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("classed", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Class No" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Class No"
                                            value={formData.class_no}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("class_no", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                </div>
                                <Divider width="100%" />
                                <h3 className="form-group-title">Vessel's Additional  Information</h3>
                                <div className="row">
                                    <div className="col-md-6 form-group">
                                        <Label name="Class Notations" bold />
                                        <RichTextEditor
                                            id="ClassNotation"
                                            placeholder="Enter Class Notations"
                                            value={formData.class_notations || ""}
                                            onChange={(value: string) =>
                                                setFormData(prev => ({ ...prev, class_notations: value }))
                                            }
                                            onBlur={(_range, _source, editor) =>
                                                handleFieldUpdate("class_notations", getEditorHtml(editor))
                                            }
                                            readOnly={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-6 form-group">
                                        <Label name="Additional Notations" bold />
                                        <RichTextEditor
                                            id="AdditionalNotations"
                                            placeholder="Enter Additional Class Notations"
                                            value={formData.additional_class_notations || ""}
                                            onChange={(value: string) =>
                                                setFormData(prev => ({ ...prev, additional_class_notations: value }))
                                            }
                                            onBlur={(_range, _source, editor) =>
                                                handleFieldUpdate("additional_class_notations", getEditorHtml(editor))
                                            }
                                            readOnly={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-6 form-group">
                                        <Label name=" Other" bold />
                                        <RichTextEditor
                                            id="OtherField"
                                            placeholder="Enter Other Notations"
                                            value={formData.other_notations || ""}
                                            onChange={(value: string) =>
                                                setFormData(prev => ({ ...prev, other_notations: value }))
                                            }
                                            onBlur={(_range, _source, editor) =>
                                                handleFieldUpdate("other_notations", getEditorHtml(editor))
                                            }
                                            readOnly={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-6 form-group">
                                        <Label name="Previous Names" bold />
                                        <RichTextEditor
                                            id="PreviousNames"
                                            placeholder="Enter Previous Names"
                                            value={formData.previous_names || ""}
                                            onChange={(value: string) =>
                                                setFormData(prev => ({ ...prev, previous_names: value }))
                                            }
                                            onBlur={(_range, _source, editor) =>
                                                handleFieldUpdate("previous_names", getEditorHtml(editor))
                                            }
                                            readOnly={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-6 form-group">
                                        <Label name="Owners" bold />
                                        <RichTextEditor
                                            id="OwnersField"
                                            placeholder="Enter Owners"
                                            value={formData.owners || ""}
                                            onChange={(value: string) =>
                                                setFormData(prev => ({ ...prev, owners: value }))
                                            }
                                            onBlur={(_range, _source, editor) =>
                                                handleFieldUpdate("owners", getEditorHtml(editor))
                                            }
                                            readOnly={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-6 form-group">
                                        <Label name="Managers" bold />
                                        <RichTextEditor
                                            id="ManagersField"
                                            placeholder="Enter Managers"
                                            value={formData.managers || ""}
                                            onChange={(value: string) =>
                                                setFormData(prev => ({ ...prev, managers: value }))
                                            }
                                            onBlur={(_range, _source, editor) =>
                                                handleFieldUpdate("managers", getEditorHtml(editor))
                                            }
                                            readOnly={isOfflineProject}
                                        />
                                    </div>
                                </div>
                                <Divider width="100%" />
                                <h3 className="form-group-title">Main Dimensions</h3>
                                <div className="row">
                                    <div className="col-md-2 form-group">
                                        <Label name="Length (m)" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Length"
                                            value={formData.length}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("length", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Breadth (m)" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Breadth"
                                            value={formData.breadth}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("breadth", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Depth (m)" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Depth"
                                            value={formData.depth}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("depth", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Summer Draft (m)" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Summer Draft"
                                            value={formData.summer_draft}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("summer_draft", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Deadweight" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Dead weight"
                                            value={formData.deadweight}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("deadweight", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Gross Tons" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Gross Tons"
                                            value={formData.gross_tons}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("gross_tons", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-2 form-group">
                                        <Label name="Net Tons" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Net Tons"
                                            value={formData.net_tons}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("net_tons", value)
                                            }
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                </div>
                                <Divider width="100%" />
                                <h3 className="form-group-title">Propulsion Details</h3>
                                <div className="row">
                                    <div className="col-md-6 form-group">
                                        <Label name="Propulsion Type" bold />

                                        <div className="d-flex align-items-center my-2 gap-2">
                                            <Input
                                                label=""
                                                placeholder="Enter Propulsion Type"
                                                value={formData.propulsion_type}
                                                onChange={(value: string) =>
                                                    handleFieldUpdate("propulsion_type", value)
                                                }
                                                maxLength={100}
                                                disabled={isOfflineProject}
                                            />
                                            <Input
                                                label=""
                                                placeholder="Enter Propulsion Value"
                                                value={formData.propulsion_value}
                                                onChange={(value: string) =>
                                                    handleFieldUpdate("propulsion_value", value)
                                                }
                                                maxLength={5}
                                                disabled={isOfflineProject}
                                            />
                                            <span className="text-muted fw-semibold">KE at</span>
                                            <Input
                                                placeholder="Enter KW"
                                                value={formData.kw}
                                                onChange={(value: string) =>
                                                    handleFieldUpdate("kw", value)
                                                }
                                                maxLength={25}
                                                disabled={isOfflineProject}
                                            />
                                            <span className="text-muted fw-semibold">Shaft RPM
                                            </span>
                                        </div>
                                    </div>

                                    <div className="col-md-3 form-group">
                                        <Label name="Propulsion Manufacturer" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Propulsion Manufacturer"
                                            value={formData.propulsion_manufacturer}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("propulsion_manufacturer", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
                                    </div>
                                    <div className="col-md-3 form-group">
                                        <Label name="Gear Manufacturer" bold />
                                        <Input
                                            label=""
                                            placeholder="Enter Gear Manufacturer"
                                            value={formData.gear_manufacturer}
                                            onChange={(value: string) =>
                                                handleFieldUpdate("gear_manufacturer", value)
                                            }
                                            maxLength={100}
                                            disabled={isOfflineProject}
                                        />
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

export default VesselMainData;
