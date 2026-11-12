import React, { useState, useEffect, useContext, useRef, useCallback, useMemo } from 'react';
import { Button, BUTTONS, Divider, Input, DatePickerInput, AbsSelect, RadioInput, CheckboxInput, SideDialog, Label, SideDialogManagerContext } from "customer_portal-ui-shared";
import * as Icon from 'react-bootstrap-icons';
import * as tankService from '../../service/TankService';
import * as projectService from '../../service/ProjectService';
import * as classService from '../../service/ClassService';
import * as portService from '../../service/PortService';
import { usePopup } from '../../Common/PopupProvider';
import { resultMessage } from '../../Common/UtilityMessage';
import DropdownWithSearch, { type DropdownWithSearchOption } from '../../Common/DropdownWithSearch';
import { isEmptyDate } from '../../utils/dateTime';
import AppLoader from '../../Common/AppLoader';
import { toFriendlyErrorMessage } from '../../utils/errorMessage';

type FormData = {
    project_name: string;
    cap_cordinator: string;
    cap_type: string;
    cap_scope: string;
    class_number: string;
    imo_number: string;
    vessel_name: string;
    classification: string[];
    builder: string;
    hull_number: string;
    length_overall: string;
    month_build: string;
    year_build: string;
    vessel_type: string;
    copying_vessel_id: string;
    contract_sister_exist: boolean;
    sis_imo_number: string;
    sis_vessel_name: string;
    planned_drydock_location: string;
    potential_drydock_date: string;
    conjunction_with_ssh_or_ihs: boolean;
    start_date: string;
    end_date: string;
    first_visit_date: string;
    last_visit_date: string;
    cap_lead: string;
    cap_team: string;
    contact_name: string;
    contact_email: string;
    contact_phone: string;
    client_profile: string;
    wcn_number: string;
    company_name: string;
    company_address: string;
    company_email: string;
    company_system_url: string;
    contract_client: boolean;
    agreement_quote_number: string;
    proposal_rev_no: string;
    agreement_submitted_date: string;
    agreement_signed_date: string;
    agreement_owner: string;
    cap_contract_value: string;
    crm_opportunity_no: string;
    project_id: string;
    expense_markup: string;
    survey_day_rate_to_client: string;
    survey_day_rate_from_abs: string;
    sed_rate_1: string;
    sed_rate_2: string;
    PIDNumber: string;
};

type SelectOption = {
    text: string;
    value: string | number;
};

function selectOptionsToDropdownOptions(opts: SelectOption[]): DropdownWithSearchOption[] {
    return opts.map((o) => ({ label: String(o.text), value: String(o.value) }));
}

function tokensToClassificationOptionIds(
    values: string[],
    options: SelectOption[],
    idToName: Map<string, string>,
    nameToId: Map<string, string>
): string[] {
    const out: string[] = [];
    const seen = new Set<string>();
    for (const raw of values) {
        const t = String(raw).trim();
        if (!t) continue;
        let id: string | undefined;
        if (idToName.has(t)) id = t;
        else id = nameToId.get(t.toLowerCase());
        if (!id) {
            const opt = options.find(
                (o) =>
                    String(o.value) === t ||
                    String(o.text).trim().toLowerCase() === t.toLowerCase()
            );
            if (opt) id = String(opt.value);
        }
        if (id && !seen.has(id)) {
            seen.add(id);
            out.push(id);
        }
    }
    return out;
}

function classificationIdsToNamesForApi(
    ids: string[],
    idToName: Map<string, string>,
    options: SelectOption[]
): string[] {
    const out: string[] = [];
    const seen = new Set<string>();
    for (const raw of ids) {
        const id = String(raw).trim();
        if (!id) continue;
        let name = idToName.get(id);
        if (!name) {
            const opt = options.find(
                (o) =>
                    String(o.value) === id ||
                    String(o.text).trim().toLowerCase() === id.toLowerCase()
            );
            name = opt ? String(opt.text).trim() : id;
        }
        if (name && !seen.has(name)) {
            seen.add(name);
            out.push(name);
        }
    }
    return out;
}

const INITIAL_FORM_DATA: FormData = {
    project_name: "",
    cap_cordinator: "",
    cap_type: "",
    cap_scope: "",
    class_number: "",
    imo_number: "",
    vessel_name: "",
    classification: [],
    builder: "",
    hull_number: "",
    length_overall: "",
    month_build: "",
    year_build: "",
    vessel_type: "",
    copying_vessel_id: "",
    contract_sister_exist: false,
    sis_imo_number: "",
    sis_vessel_name: "",
    planned_drydock_location: "",
    potential_drydock_date: "",
    conjunction_with_ssh_or_ihs: false,
    start_date: "",
    end_date: "",
    first_visit_date: "",
    last_visit_date: "",
    cap_lead: "",
    cap_team: "",
    contact_name: "",
    contact_email: "",
    contact_phone: "",
    client_profile: "",
    wcn_number: "",
    company_name: "",
    company_address: "",
    company_email: "",
    company_system_url: "",
    contract_client: true,
    agreement_quote_number: "",
    proposal_rev_no: "",
    agreement_submitted_date: "",
    agreement_signed_date: "",
    agreement_owner: "",
    cap_contract_value: "",
    crm_opportunity_no: "OPP-",
    project_id: "",
    expense_markup: "",
    survey_day_rate_to_client: "",
    survey_day_rate_from_abs: "",
    sed_rate_1: "",
    sed_rate_2: "",
    PIDNumber: "",
};

const CreateProject: React.FC<{ id: string; projectId?: number; onSuccess?: () => void }> = ({ id, projectId, onSuccess }) => {

    const [selected, setSelected] = React.useState<number>(2);
    const [tankselected, setTankSelected] = React.useState<number>(1);
    const [conjection, setConjection] = React.useState<number>(1);
    const [fatigue, setFatigue] = React.useState<number>(1);
    const [drydockdate, setDryDockDate] = React.useState<Date | null>(null);
    const [gaugingstartdate, setGaugingStartDate] = React.useState<Date | null>(null);
    const [gaugingenddate, setGaugingEndDate] = React.useState<Date | null>(null);
    const [surveystartdate, setSurveyStartDate] = React.useState<Date | null>(null);
    const [surveyenddate, setSurveyEndDate] = React.useState<Date | null>(null);
    const [agreementDate, setAgreementDate] = React.useState<Date | null>(null);
    const [agreementSignedDate, setAgreementSignedDate] = React.useState<Date | null>(null);
    const [vesselTypeOptions, setVesselTypeOptions] = useState<SelectOption[]>([]);
    const [clientProfileOptions, setClientProfileOptions] = useState<SelectOption[]>([]);
    const [wcnOptions, setWcnOptions] = useState<SelectOption[]>([]);
    const [capLeadOptions, setCapLeadOptions] = useState<SelectOption[]>([]);
    const [capTeamOptions, setCapTeamOptions] = useState<SelectOption[]>([]);
    const [existingProjectOptions, setExistingProjectOptions] = useState<SelectOption[]>([]);
    const [classSocietyOptions, setClassSocietyOptions] = useState<SelectOption[]>([]);
    const classSocietyIdToNameRef = useRef<Map<string, string>>(new Map());
    const classSocietyNameToIdRef = useRef<Map<string, string>>(new Map());
    const [drydockLocationOptions, setDrydockLocationOptions] = useState<SelectOption[]>([]);
    const [drydockPageSize] = useState<number>(100);
    const [drydockCurrentPage, setDrydockCurrentPage] = useState<number>(1);
    const [drydockHasMore, setDrydockHasMore] = useState<boolean>(true);
    const [loadingDrydock, setLoadingDrydock] = useState<boolean>(false);
    const [allDrydockData, setAllDrydockData] = useState<any[]>([]);
    const [totalDrydockRecords, setTotalDrydockRecords] = useState<number>(0);
    const drydockSearchDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
    const [monthBuiltOptions, setMonthBuiltOptions] = useState<SelectOption[]>([]);
    const [agreementOwnerOptions, setAgreementOwnerOptions] = useState<SelectOption[]>([]);
    const [capRegionOptions, setCapRegionOptions] = useState<SelectOption[]>([]);
    const [capTypeOptions, setCapTypeOptions] = useState<SelectOption[]>([]);
    const popup = usePopup();
    const [capScopeOptions, setCapScopeOptions] = useState<SelectOption[]>([]);
    const [formErrors, setFormErrors] = useState<{ [key: string]: string }>({});
    const [isSaving, setIsSaving] = useState<boolean>(false);
    const sideDialog = useContext(SideDialogManagerContext);
    const [showDuplicateWarning, setShowDuplicateWarning] = useState<string | undefined>();
    const imoLookupCounterRef = useRef<number>(0);
    const [projectLoading, setProjectLoading] = useState<boolean>(!!projectId);
    const [projectLoadError, setProjectLoadError] = useState<string | null>(null);
    const [formData, setFormData] = React.useState<FormData>({ ...INITIAL_FORM_DATA });
    const updateForm = (field: keyof FormData, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        setFormErrors(prev => {
            const copy = { ...prev };
            delete copy[field];
            return copy;
        });
    };
    const loadVesselTypes = async () => {
        try {
            const res = await tankService.getShipType();
            const data = res.data?.data || res.data || [];
            const options = data.map((v: any) => ({
                text: v.vesselType || v.VesselType || v.name || String(v),
                value: String(v.id || v.Id || v.value || v)
            }));
            setVesselTypeOptions(options);
        } catch (error) {
            setVesselTypeOptions([]);
        }
    };

    const loadClientProfiles = async () => {
        try {
            const res = await projectService.getAllClients(false);
            let data: any[] = [];
            if (res.data?.data) {
                data = Array.isArray(res.data.data) ? res.data.data : [];
            } else if (Array.isArray(res.data)) {
                data = res.data;
            }
            const options = data.map((client: any) => ({
                text: client.companyName,
                value: String(client.id)
            }));
            setClientProfileOptions(options);
        } catch (error) {
            setClientProfileOptions([]);
        }
    };

    const loadWcnNumbers = async () => {
        try {
            const res = await projectService.getAllClients(true);
            let data: any[] = [];
            if (res.data?.data) {
                data = Array.isArray(res.data.data) ? res.data.data : [];
            } else if (Array.isArray(res.data)) {
                data = res.data;
            }
            const wcnOpts = data
                .filter((client: any) => client.wcn || client.WCN || client.wcnNumber)
                .map((client: any) => ({
                    text: client.wcn || client.WCN || client.wcnNumber || "",
                    value: String(client.wcn || client.WCN || client.wcnNumber || "")
                }));
            setWcnOptions(wcnOpts);
        } catch (error) {
            setWcnOptions([]);
        }
    };

    const loadCapLead = async () => {
        try {
            const res = await projectService.getAllUsers();
            let data: any[] = [];
            if (res.data?.data) {
                data = Array.isArray(res.data.data) ? res.data.data : [];
            } else if (Array.isArray(res.data)) {
                data = res.data;
            }
            const options = data.map((user: any) => ({
                text: user.userName || user.email,
                value: String(user.userName)
            }));
            setCapLeadOptions(options);
        } catch (error) {
            setCapLeadOptions([]);
        }
    };

    const loadCapTeam = async () => {
        try {
            const res = await projectService.getAllSurveyors();
            let data: any[] = [];
            if (res.data?.data) {
                data = Array.isArray(res.data.data) ? res.data.data : [];
            } else if (Array.isArray(res.data)) {
                data = res.data;
            }
            const options = data.map((user: any) => {
                if (typeof user === 'string') {
                    return { text: user, value: user };
                }
                return {
                    text: user.userName || user.email || `${user.firstName || ''} ${user.lastName || ''}`.trim() || String(user),
                    value: String(user.userName || user.email || user.aspNetUserId || user.id || user.Id || "")
                };
            });
            setCapTeamOptions(options);
        } catch (error) {
            setCapTeamOptions([]);
        }
    };

    const loadExistingProjects = async () => {
        try {
            const res = await tankService.getVesselTypeList();
            const data = res.data?.data || res.data || [];
            const options = data.map((project: any) => {
                const combinedDescription = project.combinedDescription || project.CombinedDescription || "";
                const id = project.id || project.ID || project.id || "";
                return {
                    text: combinedDescription || `${project.imo || ""} / ${project.vesselName || ""} / ${project.projectName || ""} / ${project.projectId || ""}`,
                    value: String(id)
                };
            });
            setExistingProjectOptions(options);
        } catch (error) {
            setExistingProjectOptions([]);
        }
    };

    const loadClassSociety = useCallback(() => {
        classService.searchClass()
            .then((res) => {
                let raw: any[] = [];
                if (Array.isArray(res.data)) raw = res.data;
                else if (res.data?.data && Array.isArray(res.data.data)) raw = res.data.data;

                const idToName = new Map<string, string>();
                const nameToId = new Map<string, string>();
                const opts: SelectOption[] = [];
                const seen = new Set<string>();

                raw.forEach((v: any) => {
                    const name = String(v?.name ?? v?.Name ?? "").trim();
                    const idRaw = v?.id ?? v?.Id;
                    if (!name) return;
                    const idStr = idRaw != null && idRaw !== "" ? String(idRaw).trim() : "";
                    const value = idStr || name;
                    if (idStr) {
                        idToName.set(idStr, name);
                        nameToId.set(name.toLowerCase(), idStr);
                    }
                    if (!seen.has(name.toLowerCase())) {
                        seen.add(name.toLowerCase());
                        opts.push({ text: name, value: value });
                    }
                });
                classSocietyIdToNameRef.current = idToName;
                classSocietyNameToIdRef.current = nameToId;
                setClassSocietyOptions(opts);
            })
            .catch(() => {
                classSocietyIdToNameRef.current = new Map();
                classSocietyNameToIdRef.current = new Map();
                setClassSocietyOptions([]);
            });
    }, []);

    const mapPortDataToOptions = (data: any[]): SelectOption[] =>
        data
            .filter((item: any) => {
                if (!item || typeof item !== 'object') return false;
                const portName = item.portName || item.PortName || item.name;
                return portName && typeof portName === 'string' && portName.trim() !== '';
            })
            .map((item: any) => ({
                text: item.portName || item.PortName || item.name || '',
                value: String(item.portId || item.PortId || item.id || item.value || '')
            }));

    const parsePortSearchResponse = (res: any): any[] => {
        if (res.data?.data) {
            return Array.isArray(res.data.data) ? res.data.data : [];
        }
        if (Array.isArray(res.data)) {
            return res.data;
        }
        return [];
    };

    const loadDrydockLocation = async (searchText: string = "", append: boolean = false) => {
        setLoadingDrydock(true);
        try {
            const res = await portService.searchPort(searchText, { logic: "and" });
            const data = parsePortSearchResponse(res);

            if (!append) {
                setAllDrydockData(data);
                setTotalDrydockRecords(data.length);
            }

            const startIndex = append ? drydockLocationOptions.length : 0;
            const endIndex = startIndex + drydockPageSize;
            const paginatedData = data.slice(startIndex, endIndex);
            setDrydockHasMore(endIndex < data.length);

            const options = mapPortDataToOptions(paginatedData);

            if (append) {
                setDrydockLocationOptions(prev => [...prev, ...options]);
            } else {
                setDrydockLocationOptions(options);
                setDrydockCurrentPage(1);
            }
        } catch (error) {
            if (!append) {
                setDrydockLocationOptions([]);
                setAllDrydockData([]);
                setTotalDrydockRecords(0);
            }
        } finally {
            setLoadingDrydock(false);
        }
    };

    const handleDrydockSearchChange = (searchText: string) => {
        if (drydockSearchDebounceRef.current) {
            clearTimeout(drydockSearchDebounceRef.current);
        }
        drydockSearchDebounceRef.current = setTimeout(() => {
            loadDrydockLocation(searchText, false);
        }, 300);
    };

    useEffect(() => {
        loadDrydockLocation("", false);
        return () => {
            if (drydockSearchDebounceRef.current) {
                clearTimeout(drydockSearchDebounceRef.current);
            }
        };
    }, []);

    const loadMoreDrydockLocations = () => {
        if (!loadingDrydock && drydockHasMore) {
            const nextPage = drydockCurrentPage + 1;
            setDrydockCurrentPage(nextPage);
            const startIndex = drydockLocationOptions.length;
            const endIndex = startIndex + drydockPageSize;
            const paginatedData = allDrydockData.slice(startIndex, endIndex);
            setDrydockHasMore(endIndex < allDrydockData.length);

            const options = mapPortDataToOptions(paginatedData);

            setDrydockLocationOptions(prev => [...prev, ...options]);
        }
    };

    const loadMonthBuilt = () => {
        const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        setMonthBuiltOptions(months.map((month, index) => ({ text: month, value: String(index + 1) })));
    };

    const loadAgreementOwner = () => {
        const owners = ["ABS Group", "American Bureau of Shipping"];
        setAgreementOwnerOptions(owners.map((owner, index) => ({ text: owner, value: String(index + 1) })));
    };

    const loadCapRegions = () => {
        const regions = ["Americas/Europe", "Greece", "Middle East", "Singapore"];
        setCapRegionOptions(regions.map((region) => ({ text: region, value: region })));
    };

    const loadCapTypes = () => {
        const types = ["Initial Assessment", "Initial Assessment Sister", "Reassessment"];
        setCapTypeOptions(types.map((type) => ({ text: type, value: type })));
    };

    const loadCapScopes = () => {
        const scopes = ["Hull & Machinery", "Hull Only", "Machinery Only", "Pre-CAP", "Other"];
        setCapScopeOptions(scopes.map((scope) => ({ text: scope, value: scope })));
    };

    useEffect(() => {
        loadCapRegions();
        loadCapTypes();
        loadCapScopes();
        loadMonthBuilt();
        loadAgreementOwner();

        const loadAllDropdowns = async () => {
            try {
                await Promise.all([
                    loadVesselTypes(),
                    loadClientProfiles(),
                    loadWcnNumbers(),
                    loadCapLead(),
                    loadCapTeam(),
                    loadExistingProjects(),
                    loadClassSociety()
                ]);
            } catch (error) {
            }
        };
        loadAllDropdowns();
    }, []);

    useEffect(() => {
        if (projectId && monthBuiltOptions.length > 0 && formData.month_build) {
            setFormData(prev => ({ ...prev, month_build: String(prev.month_build) }));
        }
    }, [projectId, monthBuiltOptions.length]);

    const loadClientDetails = async (value: string, isWcn: boolean) => {

        try {
            const res = await projectService.getAllClients(isWcn);

            const clients = res.data.data;

            if (!clients || clients.length === 0) return;

            const client = clients.find((x: any) =>
                isWcn
                    ? String(x.wcn).trim() === String(value).trim()
                    : String(x.id).trim() === String(value).trim()
            );

            if (!client) {
                setFormData(prev => ({
                    ...prev,
                    company_name: "",
                    company_address: "",
                    company_email: "",
                    company_system_url: ""
                }));
                return;
            }

            setFormData(prev => ({
                ...prev,
                company_name: client.companyName || "",
                company_address: client.companyAddress || "",
                company_email: client.companyBillingEmail || "",
                company_system_url: client.companyBillingSystemUrl || ""
            }));
        } catch (err) {
        }
    };

    useEffect(() => {
        const loadProjectData = async () => {
            if (projectId) {
                setProjectLoading(true);
                setProjectLoadError(null);
                try {
                    const response = await projectService.getProjectById(projectId);
                    if (response.data && response.data.projectDetails) {
                        const projectData = response.data.projectDetails;

                        const getCapTypeString = (capType: number): string => {
                            switch (capType) {
                                case 0: return "Initial Assessment";
                                case 1: return "Initial Assessment";
                                case 2: return "Initial Assessment Sister";
                                case 3: return "Reassessment";
                                default: return "";
                            }
                        };

                        const filterSecondSurveyor = (secondSurveyor: any): string => {
                            if (!secondSurveyor) return '';
                            if (Array.isArray(secondSurveyor)) {
                                return secondSurveyor.filter((item: any) => item && String(item).trim() !== '').join(",");
                            }
                            return String(secondSurveyor);
                        };

                        const mappedFormData: FormData = {
                            project_name: projectData.name || '',
                            cap_cordinator: projectData.capRegion || '',
                            cap_type: projectData.capType !== undefined ? getCapTypeString(projectData.capType) : '',
                            cap_scope: projectData.capScope || '',
                            class_number: projectData.absClassID || '',
                            imo_number: projectData.imo ? String(projectData.imo) : '',
                            vessel_name: projectData.vesselName || '',
                            classification: tokensToClassificationOptionIds(
                                Array.isArray(projectData.classSociety)
                                    ? projectData.classSociety
                                    : (typeof projectData.classSociety === "string"
                                        ? projectData.classSociety.split(",")
                                        : []),
                                classSocietyOptions,
                                classSocietyIdToNameRef.current,
                                classSocietyNameToIdRef.current
                            ),
                            builder: projectData.builder || '',
                            hull_number: projectData.hullNumber || '',
                            length_overall: projectData.lengthOverall ? String(projectData.lengthOverall) : '',
                            month_build: projectData.monthBuilt != null ? String(projectData.monthBuilt) : '',
                            year_build: projectData.yearBuilt ? String(projectData.yearBuilt) : '',
                            vessel_type: projectData.shipType ? String(projectData.shipType) : '',
                            contract_sister_exist: projectData.sisterVessel || false,
                            sis_imo_number: projectData.sisterVesselImoNumber || '',
                            sis_vessel_name: projectData.sisterVesselName || '',
                            planned_drydock_location: projectData.drydockLocation ? String(projectData.drydockLocation) : '',
                            potential_drydock_date: !isEmptyDate(projectData.potentialDrydockDate) ? projectData.potentialDrydockDate : '',
                            conjunction_with_ssh_or_ihs: projectData.specialHull || false,
                            start_date: projectData.drydockStart || '',
                            end_date: projectData.drydockEnd || '',
                            first_visit_date: projectData.surveyFirstVisit || '',
                            last_visit_date: projectData.surveyLastVisit || '',
                            cap_lead: projectData.leadSurveyor ? String(projectData.leadSurveyor) : '',
                            cap_team: filterSecondSurveyor(projectData.secondSurveyor),
                            contact_name: projectData.pocName || '',
                            contact_email: projectData.pocEmail || '',
                            contact_phone: projectData.pocPhone || '',
                            client_profile: projectData.clientProfileId || '',
                            wcn_number: projectData.wcn || '',
                            company_name: projectData.companyName || '',
                            company_address: projectData.companyAddress || '',
                            company_email: projectData.companyBillingEmail || '',
                            company_system_url: projectData.companyBillingSystemUrl || '',
                            contract_client: projectData.billSameAsCapClient || true,
                            agreement_quote_number: projectData.agreementNumber || '',
                            proposal_rev_no: projectData.proposalRev ? String(projectData.proposalRev) : '',
                            agreement_submitted_date: projectData.agreementDate || '',
                            agreement_signed_date: projectData.agreementSignedDate || '',
                            agreement_owner: projectData.agreementOwner || '',
                            cap_contract_value: projectData.capContractValue ? String(projectData.capContractValue) : '',
                            crm_opportunity_no: projectData.crmOpportunityNumber || '',
                            project_id: projectData.id ? String(projectData.id) : '',
                            expense_markup: projectData.expenseMarkup ? String(projectData.expenseMarkup) : '',
                            survey_day_rate_to_client: projectData.surveyDayrateClient ? String(projectData.surveyDayrateClient) : '',
                            survey_day_rate_from_abs: projectData.surveyDayrateAbs ? String(projectData.surveyDayrateAbs) : '',
                            sed_rate_1: projectData.sedRate1 ? String(projectData.sedRate1) : '',
                            sed_rate_2: projectData.sedRate2 ? String(projectData.sedRate2) : '',
                            copying_vessel_id: projectData.copyingVesselID ? String(projectData.copyingVesselID) : '',
                            PIDNumber: projectData.pidNumber ? String(projectData.pidNumber) : '',
                        };

                        setFormData(mappedFormData);

                        if (!isEmptyDate(projectData.potentialDrydockDate)) setDryDockDate(new Date(projectData.potentialDrydockDate));
                        if (projectData.drydockStart) setGaugingStartDate(new Date(projectData.drydockStart));
                        if (projectData.drydockEnd) setGaugingEndDate(new Date(projectData.drydockEnd));
                        if (projectData.surveyFirstVisit) setSurveyStartDate(new Date(projectData.surveyFirstVisit));
                        if (projectData.surveyLastVisit) setSurveyEndDate(new Date(projectData.surveyLastVisit));
                        if (projectData.agreementDate) setAgreementDate(new Date(projectData.agreementDate));
                        if (projectData.agreementSignedDate) setAgreementSignedDate(new Date(projectData.agreementSignedDate));

                        if (projectData.specialHull !== undefined) setConjection(projectData.specialHull ? 1 : 2);
                        if (projectData.fatigue !== undefined) setFatigue(projectData.fatigue ? 1 : 2);
                        if (projectData.isDefaultTank !== undefined) setTankSelected(projectData.isDefaultTank ? 1 : 2);
                        if (projectData.sisterVessel !== undefined) setSelected(projectData.sisterVessel ? 1 : 2);
                    } else {
                        setProjectLoadError("Failed to load project details. Please close and try again.");
                    }
                } catch (error: any) {
                    setProjectLoadError(toFriendlyErrorMessage(
                        error?.response?.data?.message ?? error?.message,
                        "Failed to load project details. Please close and try again."
                    ));
                } finally {
                    setProjectLoading(false);
                }
            }
        };
        loadProjectData();
    }, [projectId]);

    useEffect(() => {
        if (projectId && capTeamOptions.length > 0 && formData.cap_team) {
            const capTeamValues: string[] = typeof formData.cap_team === 'string'
                ? formData.cap_team.split(",").map((v: string) => v.trim()).filter((v: string) => v !== "")
                : Array.isArray(formData.cap_team)
                    ? (formData.cap_team as any[]).map((v: any) => String(v).trim()).filter((v: string) => v !== "")
                    : [];

            if (capTeamValues.length > 0) {
                const matchedValues = capTeamValues.map(val => {
                    const valTrimmed = String(val).trim();
                    const matchedOption = capTeamOptions.find(opt => {
                        const optValue = String(opt.value || '').trim();
                        const optText = String(opt.text || '').trim().toLowerCase();
                        return optValue === valTrimmed || optText === valTrimmed.toLowerCase();
                    });
                    return matchedOption ? String(matchedOption.value) : valTrimmed;
                }).filter((v: string) => v !== '');

                const newCapTeam = matchedValues.join(",");
                if (newCapTeam !== formData.cap_team) {
                    setFormData(prev => ({ ...prev, cap_team: newCapTeam }));
                }
            }
        }
    }, [projectId, capTeamOptions.length, formData.cap_team]);

    const classificationSelectedIds = useMemo(
        () => tokensToClassificationOptionIds(
            formData.classification,
            classSocietyOptions,
            classSocietyIdToNameRef.current,
            classSocietyNameToIdRef.current
        ),
        [formData.classification, classSocietyOptions]
    );

    const classificationDropdownOptions = useMemo(
        () => selectOptionsToDropdownOptions(classSocietyOptions),
        [classSocietyOptions]
    );

    const classificationDropdownSelected = useMemo((): DropdownWithSearchOption[] => {
        return classificationSelectedIds.map((id) => {
            const opt = classSocietyOptions.find((o) => String(o.value) === String(id));
            return opt
                ? { label: String(opt.text), value: String(opt.value) }
                : { label: String(id), value: String(id) };
        });
    }, [classificationSelectedIds, classSocietyOptions]);

    const capTeamDropdownOptions = useMemo(
        () => selectOptionsToDropdownOptions(capTeamOptions),
        [capTeamOptions]
    );

    const drydockDropdownOptions = useMemo(
        () => selectOptionsToDropdownOptions(drydockLocationOptions),
        [drydockLocationOptions]
    );

    const drydockDropdownSelected = useMemo((): DropdownWithSearchOption[] => {
        const val = formData.planned_drydock_location?.trim();
        if (!val) return [];
        const opt = drydockLocationOptions.find((o) => String(o.value) === val);
        return opt
            ? [{ label: String(opt.text), value: String(opt.value) }]
            : [{ label: val, value: val }];
    }, [formData.planned_drydock_location, drydockLocationOptions]);

    const capTeamDropdownSelected = useMemo((): DropdownWithSearchOption[] => {
        if (!formData.cap_team || capTeamOptions.length === 0) return [];

        const capTeamValues: string[] =
            typeof formData.cap_team === "string"
                ? formData.cap_team.split(",").map((v: string) => v.trim()).filter((v: string) => v !== "")
                : Array.isArray(formData.cap_team)
                    ? (formData.cap_team as unknown[]).map((v) => String(v).trim()).filter((v: string) => v !== "")
                    : [];

        if (capTeamValues.length === 0) return [];

        const matchedValues = capTeamValues
            .map((val) => {
                const valTrimmed = String(val).trim();
                let matchedOption = capTeamOptions.find((opt) => String(opt.value).trim() === valTrimmed);
                if (!matchedOption) {
                    matchedOption = capTeamOptions.find((opt) => {
                        const optText = String(opt.text || "").trim().toLowerCase();
                        const optValue = String(opt.value || "").trim().toLowerCase();
                        return optText === valTrimmed.toLowerCase() || optValue === valTrimmed.toLowerCase();
                    });
                }
                return matchedOption ? String(matchedOption.value) : null;
            })
            .filter((v): v is string => v !== null);

        return matchedValues.map((value) => {
            const opt = capTeamOptions.find((o) => String(o.value) === value);
            return opt
                ? { label: String(opt.text), value: String(opt.value) }
                : { label: value, value };
        });
    }, [formData.cap_team, capTeamOptions]);


    const validateForm = (): boolean => {
        const errors: { [key: string]: string } = {};

        if (!formData.project_name?.trim()) errors.project_name = "Please enter Project Name";
        if (!formData.cap_cordinator) errors.cap_cordinator = "Please select CAP Coordinator";
        if (!formData.cap_type) errors.cap_type = "Please select CAP Type";
        if (!formData.cap_scope) errors.cap_scope = "Please select CAP Scope";

        if (!formData.class_number?.trim() && !formData.imo_number?.trim()) {
            errors.imo_number = "Please enter IMO Number";
        } else if (formData.imo_number?.trim() && !/^\d{7}$/.test(formData.imo_number)) {
            errors.imo_number = "IMO must be a seven-digit number.";
        }

        if (!formData.vessel_name?.trim()) errors.vessel_name = "Please enter Vessel Name";
        if (!formData.classification || formData.classification.length === 0) errors.classification = "Please Select Classification";
        if (!formData.vessel_type) errors.vessel_type = "Please enter Vessel Type";

        if (selected === 1) {
            if (!formData.sis_imo_number?.trim()) {
                errors.sis_imo_number = "Sister IMO Number is required when Contract Sister Exist is Yes";
            } else if (!/^\d{7}$/.test(formData.sis_imo_number)) {
                errors.sis_imo_number = "IMO must be a seven-digit number.";
            }
            if (!formData.sis_vessel_name?.trim()) errors.sis_vessel_name = "Sister Vessel Name is required when Contract Sister Exist is Yes";
        }

        if (tankselected === 2) {
            if (!formData.copying_vessel_id || formData.copying_vessel_id === "0") {
                errors.copying_vessel_id = "Project Copied is required.";
            }
        }

        if (!formData.client_profile) errors.client_profile = "Please enter Client Profile";
        if (!formData.company_name?.trim()) errors.company_name = "Please enter Company Name";
        if (!formData.company_address?.trim()) errors.company_address = "Please enter Company Address";
        if (!formData.agreement_quote_number?.trim()) errors.agreement_quote_number = "Please enter Agreement / Quote Number";

        if (!formData.proposal_rev_no?.trim()) {
            errors.proposal_rev_no = "Please enter Proposal Rev. No.";
        } else if (!/^\d{1,2}$/.test(formData.proposal_rev_no)) {
            errors.proposal_rev_no = "Proposal Rev. must be one or two digits";
        }

        if (!agreementDate) {
            errors.agreement_submitted_date = "Please enter Agreement Submitted Date";
        } else if (agreementDate.getFullYear() < 1950) {
            errors.agreement_submitted_date = "Agreement Submitted Date must be on or after 1950.";
        }

        if (!agreementSignedDate) {
            errors.agreement_signed_date = "Please enter Agreement Signed Date";
        } else if (agreementSignedDate.getFullYear() < 1950) {
            errors.agreement_signed_date = "Agreement Signed Date must be on or after 1950.";
        }

        if (agreementDate && agreementSignedDate && agreementSignedDate < agreementDate) {
            errors.agreement_submitted_date = "Agreement Submitted Date must be on or before Agreement Signed Date.";
        }

        if (gaugingstartdate && gaugingenddate && gaugingenddate <= gaugingstartdate) {
            errors.start_date = "Start must be on or before End";
        }

        if (surveystartdate && surveyenddate && surveyenddate <= surveystartdate) {
            errors.first_visit_date = "First Visit Date must be on or before Last Visit Date";
        }

        if (!formData.agreement_owner) errors.agreement_owner = "Please select Agreement Owner";
        if (!formData.cap_contract_value?.trim()) errors.cap_contract_value = "Please enter CAP Contract Value";

        if (formData.contact_email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.contact_email)) {
            errors.contact_email = "Please enter a valid email address";
        }
        if (formData.company_email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.company_email)) {
            errors.company_email = "Please enter a valid email address";
        }

        if (formData.PIDNumber?.trim() && !/^\d{7}$/.test(formData.PIDNumber)) {
            errors.PIDNumber = "Project ID must be 7 digits.";
        }

        if (formData.expense_markup?.trim() && !/^\d{1,2}$/.test(formData.expense_markup)) {
            errors.expense_markup = "Expense Markup must be one or two digits.";
        }

        if (formData.crm_opportunity_no?.trim() && formData.crm_opportunity_no !== "OPP-" && !/^OPP-\d{10}$/.test(formData.crm_opportunity_no)) {
            errors.crm_opportunity_no = "Opportunity Number must be ten digits.";
        }

        if (formData.month_build?.trim()) {
            const m = parseInt(formData.month_build);
            if (isNaN(m) || m < 1 || m > 12) {
                errors.month_build = "Month must be between 1 and 12.";
            }
        }

        if (formData.year_build?.trim() && !/^\d{4}$/.test(formData.year_build)) {
            errors.year_build = "Year must have 4 digits.";
        }

        setFormErrors(errors);
        return Object.keys(errors).length === 0;
    };


    const handleCreate = async () => {
        if (!validateForm()) {
            return;
        }
        setIsSaving(true);
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 30000);
        try {

            const formatDate = (date: Date | "" | null | undefined): string => {
                if (!date) return "";
                return new Date(date).toISOString().split('T')[0];
            };

            const getCapTypeEnum = (capTypeString: string): number | null => {
                switch (capTypeString) {
                    case "Initial Assessment": return 1;
                    case "Initial Assessment Sister": return 2;
                    case "Reassessment": return 3;
                    default: return null;
                }
            };

            const currentUser = localStorage.getItem("user");
            let userName = "";
            try {
                if (currentUser) {
                    const userObj = JSON.parse(currentUser);
                    userName = userObj?.name || userObj?.username || userObj?.userName || "";
                }
            } catch (e) { }

            const classSocietyNames = classificationIdsToNamesForApi(
                Array.isArray(formData.classification) ? formData.classification : [],
                classSocietyIdToNameRef.current,
                classSocietyOptions
            );

            const payload: any = {
                ...(projectId && { ID: projectId }),
                Name: formData.project_name,
                CapRegion: formData.cap_cordinator,
                CapType: formData.cap_type ? getCapTypeEnum(formData.cap_type) : null,
                CapScope: formData.cap_scope,
                AbsClassID: formData.class_number || "",
                IMO: formData.imo_number ? parseInt(formData.imo_number) : null,
                VesselName: formData.vessel_name,
                ClassSociety: classSocietyNames,
                Builder: formData.builder || "",
                HullNumber: formData.hull_number || "",
                LengthOverall: formData.length_overall ? parseFloat(formData.length_overall) : null,
                MonthBuilt: formData.month_build || null,
                YearBuilt: formData.year_build ? parseInt(formData.year_build) : null,
                ShipType: formData.vessel_type ? parseInt(formData.vessel_type) : null,
                SisterVessel: formData.contract_sister_exist,
                SisterVesselImoNumber: formData.sis_imo_number || "",
                SisterVesselName: formData.sis_vessel_name || "",
                DrydockLocation: formData.planned_drydock_location ? parseInt(formData.planned_drydock_location) : null,
                PotentialDrydockDate: formatDate(drydockdate) || null,
                SpecialHull: conjection === 1,
                Fatigue: fatigue === 1,
                DrydockStart: formatDate(gaugingstartdate) || null,
                DrydockEnd: formatDate(gaugingenddate) || null,
                SurveyFirstVisit: formatDate(surveystartdate) || null,
                SurveyLastVisit: formatDate(surveyenddate) || null,
                LeadSurveyor: formData.cap_lead || "",
                SecondSurveyor: formData.cap_team
                    ? (typeof formData.cap_team === 'string'
                        ? formData.cap_team.split(",").filter((v: string) => v.trim() !== "")
                        : Array.isArray(formData.cap_team)
                            ? (formData.cap_team as any[]).filter((v: any) => v && String(v).trim() !== "")
                            : [])
                    : [],
                PocName: formData.contact_name || "",
                PocEmail: formData.contact_email || "",
                PocPhone: formData.contact_phone || "",
                ClientProfileId: formData.client_profile || null,
                WCN: formData.wcn_number || "",
                CompanyName: formData.company_name || null,
                CompanyAddress: formData.company_address || null,
                CompanyBillingEmail: formData.company_email || "",
                CompanyBillingSystemUrl: formData.company_system_url || "",
                BillSameAsCapClient: formData.contract_client,
                AgreementNumber: formData.agreement_quote_number || null,
                ProposalRev: formData.proposal_rev_no ? parseInt(formData.proposal_rev_no) : null,
                AgreementDate: formatDate(agreementDate) || null,
                AgreementSignedDate: formatDate(agreementSignedDate) || null,
                AgreementOwner: formData.agreement_owner || null,
                CapContractValue: formData.cap_contract_value && !isNaN(parseFloat(formData.cap_contract_value))
                    ? parseFloat(formData.cap_contract_value)
                    : null,
                CRMOpportunityNumber: formData.crm_opportunity_no || "",
                PIDNumber: formData.PIDNumber || null,
                ExpenseMarkup: formData.expense_markup ? parseInt(formData.expense_markup) : null,
                SurveyDayrateClient: formData.survey_day_rate_to_client ? parseFloat(formData.survey_day_rate_to_client) : null,
                SurveyDayrateAbs: formData.survey_day_rate_from_abs ? parseFloat(formData.survey_day_rate_from_abs) : null,
                SedRate1: formData.sed_rate_1 ? parseFloat(formData.sed_rate_1) : null,
                SedRate2: formData.sed_rate_2 ? parseFloat(formData.sed_rate_2) : null,
                IsDefaultTank: tankselected === 1,
                CopyingVesselID: tankselected === 2 && formData.copying_vessel_id
                    ? parseInt(formData.copying_vessel_id)
                    : null,
                CapScopeOther: "",
                ClassSocietyOther: "",
                BillToWCN: "",
                BillToCompanyName: "",
                BillToCompanyAddress: "",
                BillToCompanyEmail: "",
                BillToCompanyBillingUrl: "",
                CreatedBy: userName,
                LastModifiedBy: userName,
                StatusChangedBy: "",
                ProjectStatusName: "",
                DrydockLocationName: "",
                CertificateGrade: "",
                ProjectComments: "",
                VesselType: "",
                TankVesselType: "",
                LeadSurveyorEID: "",
                SecondSurveyorEID: "",
                ThirdSurveyor: "",
                ThirdSurveyorEID: "",
                FourthSurveyor: "",
                FourthSurveyorEID: ""
            };

            Object.keys(payload).forEach(key => {
                if (payload[key] === undefined && key !== 'ID') delete payload[key];
            });

            const response = projectId
                ? await projectService.updateProject(payload)
                : await projectService.createProject(payload);
            clearTimeout(timeoutId);

            const isEdit = !!projectId;
            const projectName = formData.project_name || formData.vessel_name || "Project";

            if (response.data) {
                window.dispatchEvent(new Event('refreshProjectList'));
                onSuccess?.();
                if (sideDialog && typeof sideDialog === 'object' && 'removeDialog' in sideDialog) {
                    (sideDialog as any).removeDialog(id);
                }
                setConjection(1);
                setFatigue(1);
                setFormData({ ...INITIAL_FORM_DATA });
                setSelected(2);
                setTankSelected(1);
                setShowDuplicateWarning(undefined);
                setFormErrors({});
                popup.success({
                    title: isEdit ? "Edit Project" : "Create Project",
                    message: resultMessage(projectName, isEdit ? "Updated" : "Created")
                });
            } else {
                const submitErr = isEdit ? "Failed to update project. Please try again." : "Failed to create project. Please try again.";
                setFormErrors({ submit: submitErr });
                popup.error({ message: submitErr });
            }
        } catch (error: any) {
            clearTimeout(timeoutId);
            const isTimeout = error?.name === 'AbortError' || error?.code === 'ECONNABORTED';
            const errorMessage = isTimeout
                ? "The request timed out. Please try again or contact support."
                : error?.response?.data?.message ||
                error?.message ||
                "An error occurred while saving the project.";
            setFormErrors({ submit: errorMessage });
            popup.error({ message: errorMessage });
        } finally {
            setIsSaving(false);
        }
    };

    const handleCancel = () => {
        if (sideDialog && typeof sideDialog === 'object' && 'removeDialog' in sideDialog) {
            (sideDialog as any).removeDialog(id);
        }
    };

    const resetImoState = () => {
        imoLookupCounterRef.current += 1;
        setShowDuplicateWarning(undefined);
        setTankSelected(1);
        setFormData(prev => ({ ...prev, copying_vessel_id: "" }));
        setFormErrors(prev => {
            if (!prev.copying_vessel_id) return prev;
            const next = { ...prev };
            delete next.copying_vessel_id;
            return next;
        });
    };

    const handleImoLookup = async (imo: string) => {
        const trimmed = imo?.trim();
        if (!trimmed || trimmed === "0" || trimmed.length !== 7 || !/^\d{7}$/.test(trimmed)) {
            resetImoState();
            return;
        }

        const requestId = ++imoLookupCounterRef.current;
        try {
            const res = await projectService.lookupByImo(trimmed);
            if (imoLookupCounterRef.current !== requestId) return;

            const data = res?.data;
            if (!data) {
                resetImoState();
                return;
            }

            if (data.result === true && data.project) {
                setShowDuplicateWarning("true");
                setTankSelected(2);

                const projValue = String(data.project.value ?? "");
                const projText = String(data.project.text ?? data.project.label ?? projValue);

                setExistingProjectOptions(prev => {
                    if (prev.some(o => String(o.value) === projValue)) return prev;
                    return [{ text: projText, value: projValue }, ...prev];
                });

                setFormData(prev => {
                    const next: FormData = {
                        ...prev,
                        copying_vessel_id: projValue,
                    };
                    if (data.vesseldetails) {
                        const vessel = data.vesseldetails;
                        // digital-cap rule: keep existing vessel name if user already typed one
                        if (!prev.vessel_name?.trim() && vessel.vesselName) {
                            next.vessel_name = vessel.vesselName;
                        }
                        if (vessel.builder) next.builder = vessel.builder;
                        if (vessel.hullNumber) next.hull_number = vessel.hullNumber;
                        if (vessel.lengthOverall != null && String(vessel.lengthOverall) !== "" && String(vessel.lengthOverall) !== "0") {
                            next.length_overall = String(vessel.lengthOverall);
                        }
                        if (vessel.monthBuilt != null && String(vessel.monthBuilt) !== "") {
                            next.month_build = String(vessel.monthBuilt);
                        }
                        if (vessel.yearBuilt != null && String(vessel.yearBuilt) !== "") {
                            next.year_build = String(vessel.yearBuilt);
                        }
                        if (vessel.absClassID) next.class_number = vessel.absClassID;
                        if (vessel.imo) next.imo_number = vessel.imo;
                        if (vessel.type != null && String(vessel.type) !== "") {
                            next.vessel_type = String(vessel.type);
                        }
                    }
                    return next;
                });

                setFormErrors(prev => {
                    if (!prev.copying_vessel_id) return prev;
                    const n = { ...prev };
                    delete n.copying_vessel_id;
                    return n;
                });
            } else {
                resetImoState();
            }
        } catch (err) {
            if (imoLookupCounterRef.current === requestId) {
                resetImoState();
            }
        }
    };

    const handleClassNumberLookup = async (classNumber: string) => {
        const trimmed = classNumber?.trim();
        if (!trimmed || !!projectId) return;

        try {
            const res = await projectService.lookupByClassNumber(trimmed);
            const payload = res.data?.data ?? res.data;
            const vessel = payload?.data ?? payload;

            if (!vessel || res.data?.isSuccess === false) return;

            setFormData(prev => ({
                ...prev,
                class_number: vessel.absClassID || vessel.AbsClassID || trimmed,
                vessel_name: vessel.vesselName || vessel.VesselName || prev.vessel_name,
                builder: vessel.builder || vessel.Builder || "",
                hull_number: vessel.hullNumber || vessel.HullNumber || "",
                length_overall: vessel.lengthOverall != null
                    ? String(vessel.lengthOverall)
                    : (vessel.LengthOverall != null ? String(vessel.LengthOverall) : ""),
                month_build: vessel.monthBuilt != null
                    ? String(vessel.monthBuilt)
                    : (vessel.MonthBuilt != null ? String(vessel.MonthBuilt) : ""),
                year_build: vessel.yearBuilt != null
                    ? String(vessel.yearBuilt)
                    : (vessel.YearBuilt != null ? String(vessel.YearBuilt) : ""),
                imo_number: vessel.imo || vessel.IMO || prev.imo_number,
                vessel_type: vessel.type != null
                    ? String(vessel.type)
                    : (vessel.Type != null ? String(vessel.Type) : ""),
            }));

            const returnedImo = vessel.imo || vessel.IMO;
            if (returnedImo) {
                handleImoLookup(String(returnedImo));
            } else {
                resetImoState();
            }
        } catch (err) {
        }
    };
    const isCopyLocked = !!showDuplicateWarning;

    return (
        <>
            <style>{`
                .currency-input-wrapper {
                    position: relative;
                    display: flex;
                    align-items: center;
                    width: 100%;
                }
                .currency-input-wrapper .currency-symbol {
                    position: absolute;
                    left: 12px;
                    z-index: 5;
                    color: #495057;
                    font-weight: 500;
                }
                .currency-input-wrapper input {
                    padding-left: 24px !important;
                }
            `}</style>
            <SideDialog
                id={id}
                confirmOnClose={false}
                className="right-side-dialog"
                showWarningMinimize={() => false}
                title={
                    <div className='side-panel-title'>
                        <h5>{projectId ? "Edit Project" : "Create Project"}</h5>
                        <div className="side-header-actions">
                            <Button className="close-button" onClick={handleCancel}><Icon.X /></Button>
                        </div>
                    </div>
                }
                minimizedTitle={projectId ? "Edit Project" : "Create Project"}
            >
                {() => (
                    <>
                        {projectId && projectLoading ? (
                            <AppLoader inline />
                        ) : projectId && projectLoadError ? (
                            <>
                                <div className="alert alert-danger" role="alert">
                                    {projectLoadError}
                                </div>
                                <div className="col-md-12 form-group">
                                    <div className="d-flex gap-2">
                                        <Button
                                            variant={BUTTONS.SECONDARY}
                                            onClick={handleCancel}
                                        >
                                            Cancel
                                        </Button>
                                    </div>
                                </div>
                            </>
                        ) : (
                            <>
                                {/* ── Project Information ── */}
                        <h3 className="form-group-title">Project Information</h3>
                        <div className="row">

                            {/* Project Name — FIX: removed label="" prop that caused "l is not a function" error */}
                            <div className="col-md-6 form-group">
                                <Label name="Project Name" bold required />
                                <Input
                                    placeholder="Enter Project Name"
                                    value={formData.project_name}
                                    onChange={(value: string) => updateForm("project_name", value || "")}
                                />
                                {formErrors.project_name && <span className="text-danger">{formErrors.project_name}</span>}
                            </div>

                            {/* CAP Coordinator — FIX: use updateForm */}
                            <div className="col-md-6 form-group">
                                <Label name="CAP Coordinator" bold required />
                                <AbsSelect
                                    key={`cap-coordinator-${projectId || 'new'}-${capRegionOptions.length}-${formData.cap_cordinator}`}
                                    mult={false}
                                    onChange={(selected: string[] | any) => updateForm("cap_cordinator", (selected && selected[0]) || "")}
                                    options={capRegionOptions}
                                    selected={formData.cap_cordinator?.trim() ? [String(formData.cap_cordinator)] : []}
                                    placeholder="Select a CAP Coordinator"
                                />
                                {formErrors.cap_cordinator && <span className="text-danger">{formErrors.cap_cordinator}</span>}
                            </div>

                            {/* CAP Type — FIX: use updateForm */}
                            <div className="col-md-6 form-group">
                                <Label name="CAP Type" bold required />
                                <AbsSelect
                                    key={`cap-type-${projectId || 'new'}-${capTypeOptions.length}-${formData.cap_type}`}
                                    mult={false}
                                    onChange={(selected: string[] | any) => updateForm("cap_type", (selected && selected[0]) || "")}
                                    options={capTypeOptions}
                                    selected={formData.cap_type?.trim() ? [String(formData.cap_type)] : []}
                                    placeholder="Select a CAP Type"
                                />
                                {formErrors.cap_type && <span className="text-danger">{formErrors.cap_type}</span>}
                            </div>

                            {/* CAP Scope — FIX: use updateForm */}
                            <div className="col-md-6 form-group">
                                <Label name="CAP Scope" bold required />
                                <AbsSelect
                                    key={`cap-scope-${projectId || 'new'}-${capScopeOptions.length}-${formData.cap_scope}`}
                                    mult={false}
                                    onChange={(selected: string[] | any) => updateForm("cap_scope", (selected && selected[0]) || "")}
                                    options={capScopeOptions}
                                    selected={formData.cap_scope?.trim() ? [String(formData.cap_scope)] : []}
                                    placeholder="Select a CAP Scope"
                                />
                                {formErrors.cap_scope && <span className="text-danger">{formErrors.cap_scope}</span>}
                            </div>
                        </div>

                        <Divider width="100%" />

                        {showDuplicateWarning && (
                            <div className="alert alert-warning">
                                ⚠ A Project with this IMO Number already exists. Tanks are being copied from latest project.
                            </div>
                        )}

                        {/* ── Vessel Primary Information ── */}
                        <h3 className="form-group-title">Vessel Primary Information</h3>
                        <div className="row">

                            <div className="col-md-5 form-group search-control">
                                <Label name="ABS Class Number" bold />

                                <div
                                    className="search-input-wrapper"
                                    onClick={(e: React.MouseEvent<HTMLDivElement>) => {
                                        const target = e.target as HTMLElement;
                                        if (
                                            target &&
                                            target.tagName !== 'INPUT' &&
                                            !target.closest('.clear-field-icon')
                                        ) {
                                            void handleClassNumberLookup(formData.class_number);
                                        }
                                    }}
                                    style={{ cursor: 'pointer' }}
                                >
                                    <Input
                                        withSearch
                                        placeholder="Enter ABS Class Number"
                                        value={formData.class_number}
                                        onChange={(value: string) => {
                                            if (/^\d*$/.test(value)) {
                                                updateForm("class_number", value);
                                            }
                                        }}
                                        onKeyDown={(e: React.KeyboardEvent<HTMLInputElement>) => {
                                            if (e.key === "Enter") {
                                                e.preventDefault();
                                                void handleClassNumberLookup(formData.class_number);
                                            }
                                        }}
                                        disabled={!!projectId}
                                    />
                                </div>
                            </div>

                            <div className="col-md-2">
                                <div className="d-flex align-items-center my-2">
                                    <hr className="flex-grow-1 center-divider" />
                                    <span className="px-3 text-muted fw-semibold">OR</span>
                                    <hr className="flex-grow-1 center-divider" />
                                </div>
                            </div>

                            {/* IMO Number — FIX: use updateForm so error clears on type */}
                            <div className="col-md-5 form-group search-control">
                                <Label name="IMO Number (For Non-ABS Vessels)" bold required />
                                <Input
                                    placeholder="Enter IMO Number"
                                    value={formData.imo_number}
                                    onChange={(value: string) => {
                                        if (/^\d*$/.test(value)) {
                                            updateForm("imo_number", value);
                                            if (value.length === 7) {
                                                handleImoLookup(value);
                                            } else {
                                                resetImoState();
                                            }
                                        }
                                    }}
                                    maxLength={7}
                                    disabled={!!projectId}
                                />
                                {formErrors.imo_number && <span className="text-danger">{formErrors.imo_number}</span>}
                            </div>

                            {/* Vessel Name — FIX: use updateForm */}
                            <div className="col-md-6 form-group">
                                <Label name="Vessel Name" bold required />
                                <Input
                                    placeholder="Enter Vessel Name"
                                    value={formData.vessel_name}
                                    onChange={(value: string) => updateForm("vessel_name", value)}
                                    disabled={!!projectId}
                                />
                                {formErrors.vessel_name && <span className="text-danger">{formErrors.vessel_name}</span>}
                            </div>

                            {/* Classification — FIX: clear error on select */}
                            <div className="col-md-6 form-group">
                                <Label name="Classification" bold required />
                                <DropdownWithSearch
                                    options={classificationDropdownOptions}
                                    placeholder="Select a Classification"
                                    isMulti={true}
                                    isSearchable={true}
                                    selectedValue={classificationDropdownSelected}
                                    onSelect={(vals) => {
                                        setFormData(prev => ({ ...prev, classification: vals.map(v => v.value) }));
                                        setFormErrors(prev => { const c = { ...prev }; delete c.classification; return c; });
                                    }}
                                    disabled={!!projectId}
                                />
                                {formErrors.classification && <span className="text-danger">{formErrors.classification}</span>}
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Builder" bold />
                                <Input
                                    placeholder="Enter Builder Name"
                                    value={formData.builder}
                                    onChange={(value: string) => updateForm("builder", value)}
                                    disabled={!!projectId}
                                />
                            </div>

                            <div className="col-md-3 form-group">
                                <Label name="Hull Number" bold />
                                <Input
                                    placeholder="Enter Hull Number"
                                    value={formData.hull_number}
                                    onChange={(value: string) => {
                                        if (/^\d*$/.test(value)) {
                                            updateForm("hull_number", value);
                                        }
                                    }}
                                    disabled={!!projectId}
                                />
                            </div>

                            <div className="col-md-3 form-group">
                                <Label name="Length Overall" bold />
                                <Input
                                    placeholder="Enter Length Overall"
                                    value={formData.length_overall}
                                    onChange={(value: string) => updateForm("length_overall", value.replace(/[^0-9.]/g, ''))}
                                    disabled={!!projectId}
                                />
                            </div>

                            <div className="col-md-3 form-group">
                                <Label name="Month Built" bold />
                                <AbsSelect
                                    key={`month-built-${monthBuiltOptions.length}-${formData.month_build}`}
                                    mult={false}
                                    onChange={(selected: string[] | any) => updateForm("month_build", (selected && selected[0]) || "")}
                                    options={monthBuiltOptions}
                                    selected={formData.month_build ? [String(formData.month_build)] : []}
                                    placeholder="Select Month Built"
                                    disabled={!!projectId}
                                />
                                {formErrors.month_build && <span className="text-danger">{formErrors.month_build}</span>}
                            </div>

                            <div className="col-md-3 form-group">
                                <Label name="Year Built" bold />
                                <Input
                                    placeholder="Enter Year Built"
                                    value={formData.year_build}
                                    onChange={(value: string) => {
                                        if (/^\d{0,4}$/.test(value)) {
                                            updateForm("year_build", value);
                                        }
                                    }}
                                    disabled={!!projectId}
                                />
                                {formErrors.year_build && <span className="text-danger">{formErrors.year_build}</span>}
                            </div>
                        </div>

                        <Divider width="100%" />

                        {/* ── Vessel Additional Information ── */}
                        <h3 className="form-group-title">Vessel Additional Information</h3>
                        <div className="row">

                            {/* Vessel Type — FIX: use updateForm */}
                            <div className="col-md-6 form-group">
                                <Label name="Select a Vessel Type" bold required />
                                <AbsSelect
                                    key={`vessel-type-${projectId || 'new'}-${vesselTypeOptions.length}-${formData.vessel_type}`}
                                    mult={false}
                                    onChange={(selected: string[] | any) => updateForm("vessel_type", (selected && selected[0]) || "")}
                                    options={vesselTypeOptions}
                                    selected={formData.vessel_type?.trim() ? [String(formData.vessel_type)] : []}
                                    placeholder="Select a Vessel Type"
                                    search={true}
                                    disabled={!!projectId}
                                />
                                {formErrors.vessel_type && <span className="text-danger">{formErrors.vessel_type}</span>}
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Does a Contract Sister Exist?" bold />
                                <div className='d-flex justify-content-start gap-3'>
                                    <RadioInput
                                        label="Yes"
                                        checked={selected === 1}
                                        onChange={() => {
                                            setSelected(1);
                                            updateForm("contract_sister_exist", true);
                                        }}
                                        disabled={!!projectId}
                                    />
                                    <RadioInput
                                        label="No"
                                        checked={selected === 2}
                                        onChange={() => {
                                            setSelected(2);
                                            setFormData(prev => ({
                                                ...prev,
                                                contract_sister_exist: false,
                                                sis_imo_number: "",
                                                sis_vessel_name: ""
                                            }));
                                            setFormErrors(prev => {
                                                const c = { ...prev };
                                                delete c.sis_imo_number;
                                                delete c.sis_vessel_name;
                                                return c;
                                            });
                                        }}
                                        disabled={!!projectId}
                                    />
                                </div>
                            </div>

                            {selected === 1 && (
                                <>
                                    {/* Sister IMO — FIX: use updateForm */}
                                    <div className="col-md-6 form-group search-control">
                                        <Label name="Sister IMO Number" bold />
                                        <Input
                                            withSearch
                                            placeholder="Enter Sister IMO Number"
                                            value={formData.sis_imo_number}
                                            onChange={(value: string) => updateForm("sis_imo_number", value)}
                                            disabled={!!projectId}
                                            maxLength={7}
                                        />
                                        {formErrors.sis_imo_number && <span className="text-danger">{formErrors.sis_imo_number}</span>}
                                    </div>

                                    {/* Sister Vessel Name — FIX: use updateForm */}
                                    <div className="col-md-6 form-group">
                                        <Label name="Sister Vessel Name" bold />
                                        <Input
                                            placeholder="Enter Sister Vessel Name"
                                            value={formData.sis_vessel_name}
                                            onChange={(value: string) => updateForm("sis_vessel_name", value)}
                                            disabled={!!projectId}
                                        />
                                        {formErrors.sis_vessel_name && <span className="text-danger">{formErrors.sis_vessel_name}</span>}
                                    </div>
                                </>
                            )}

                            <div className="col-md-6 form-group">
                                <Label name="Select Tank" bold />
                                <div className='d-flex justify-content-start gap-3'>
                                    <RadioInput
                                        label="Default Tanks Project"
                                        checked={tankselected === 1}
                                        onChange={() => {
                                            setTankSelected(1);
                                            setFormData(prev => ({ ...prev, copying_vessel_id: "" }));
                                        }}
                                        disabled={!!projectId || isCopyLocked}
                                    />
                                    <RadioInput
                                        label="Copy Tank from Existing CAP"
                                        checked={tankselected === 2}
                                        onChange={() => setTankSelected(2)}
                                        disabled={!!projectId || isCopyLocked}
                                    />
                                </div>
                            </div>

                            {tankselected === 2 && (
                                <div className="col-md-6 form-group">
                                    <Label name="Project Copied" bold required />
                                    <Label name="IMO Number/ Vessel Name/ Project Name/ Project ID" />
                                    <AbsSelect
                                        mult={false}
                                        onChange={(selected) => updateForm("copying_vessel_id", selected?.[0] || "")}
                                        selected={formData.copying_vessel_id ? [formData.copying_vessel_id] : []}
                                        options={existingProjectOptions}
                                        placeholder="Select an Existing Project"
                                        search={true}
                                        disabled={!!projectId || isCopyLocked}
                                    />
                                    {formErrors.copying_vessel_id && <span className="text-danger">{formErrors.copying_vessel_id}</span>}
                                </div>
                            )}
                        </div>

                        <Divider width="100%" />

                        {/* ── Survey ── */}
                        <h3 className="form-group-title">Survey</h3>
                        <div className="row">

                            <div className="col-md-6 form-group">
                                <Label name="Planned Drydock Location" bold />
                                <DropdownWithSearch
                                    options={drydockDropdownOptions}
                                    placeholder="Select a Planned Drydock Location"
                                    isMulti={false}
                                    isSearchable={true}
                                    selectedValue={drydockDropdownSelected}
                                    onSelect={(vals) => updateForm("planned_drydock_location", vals[0]?.value || "")}
                                    onSearchChange={handleDrydockSearchChange}
                                    hasMore={drydockHasMore}
                                    onLoadMore={loadMoreDrydockLocations}
                                    loadingMore={loadingDrydock}
                                    loadedCount={drydockLocationOptions.length}
                                    totalCount={totalDrydockRecords}
                                />
                            </div>

                            <div className="col-md-6 form-group search-control">
                                <Label name="Potential Drydock Date" bold />
                                <DatePickerInput
                                    autoFocus={false}
                                    value={drydockdate || ""}
                                    onChange={(val: Date) => setDryDockDate(val)}
                                />
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="CAP in conjunction with SSH or IHS?" bold required />
                                <div className='d-flex justify-content-start gap-3'>
                                    <RadioInput label="Yes" checked={conjection === 1} onChange={() => setConjection(1)} />
                                    <RadioInput label="No" checked={conjection === 2} onChange={() => setConjection(2)} />
                                </div>
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="CAP requires Fatigue Analysis?" bold required />
                                <div className='d-flex justify-content-start gap-3'>
                                    <RadioInput label="Yes" checked={fatigue === 1} onChange={() => setFatigue(1)} />
                                    <RadioInput label="No" checked={fatigue === 2} onChange={() => setFatigue(2)} />
                                </div>
                            </div>
                        </div>

                        <Divider className="divider-1" />

                        {/* ── Survey and Planned Gauge Dates ── */}
                        <div className="row">
                            <div className="col-md-6">
                                <h3 className="form-group-title">Planned Gauging Dates</h3>
                            </div>
                            <div className="col-md-6">
                                <h3 className="form-group-title">Survey Dates</h3>
                            </div>
                        </div>
                        <div className="row">
                            <div className="col-md-3 form-group search-control">
                                <Label name="Start Date" bold />
                                <DatePickerInput
                                    autoFocus={false}
                                    value={gaugingstartdate || ""}
                                    onChange={(val: Date) => {
                                        setGaugingStartDate(val);
                                        setFormErrors(prev => ({ ...prev, start_date: "" }));
                                    }}
                                />
                            </div>
                            <div className="col-md-3 form-group search-control">
                                <Label name="End Date" bold />
                                <DatePickerInput
                                    autoFocus={false}
                                    value={gaugingenddate || ""}
                                    onChange={(val: Date) => {
                                        setGaugingEndDate(val);
                                        setFormErrors(prev => ({ ...prev, start_date: "" }));
                                    }}
                                />
                                {formErrors.start_date && <span className="text-danger">{formErrors.start_date}</span>}
                            </div>
                            <div className="col-md-3 form-group search-control">
                                <Label name="First Visit Date" bold />
                                <DatePickerInput
                                    autoFocus={false}
                                    value={surveystartdate || ""}
                                    onChange={(val: Date) => {
                                        setSurveyStartDate(val);
                                        setFormErrors(prev => ({ ...prev, first_visit_date: "" }));
                                    }}
                                />
                            </div>
                            <div className="col-md-3 form-group search-control">
                                <Label name="Last Visit Date" bold />
                                <DatePickerInput
                                    autoFocus={false}
                                    value={surveyenddate || ""}
                                    onChange={(val: Date) => {
                                        setSurveyEndDate(val);
                                        setFormErrors(prev => ({ ...prev, first_visit_date: "" }));
                                    }}
                                />
                                {formErrors.first_visit_date && <span className="text-danger">{formErrors.first_visit_date}</span>}
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Assigned CAP Lead" bold />
                                <AbsSelect
                                    key={`cap-lead-${projectId || 'new'}-${capLeadOptions.length}-${formData.cap_lead}`}
                                    mult={false}
                                    onChange={(selected: string[]) => updateForm("cap_lead", (selected && selected[0]) || "")}
                                    options={capLeadOptions}
                                    selected={formData.cap_lead ? [String(formData.cap_lead)] : []}
                                    placeholder="Select a CAP Lead"
                                />
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Assigned CAP Team" bold />
                                <DropdownWithSearch
                                    options={capTeamDropdownOptions}
                                    placeholder="Select a CAP Team"
                                    isMulti={true}
                                    isSearchable={true}
                                    selectedValue={capTeamDropdownSelected}
                                    onSelect={(vals) => {
                                        setFormData(prev => ({
                                            ...prev,
                                            cap_team: vals.map(v => v.value).join(",") || "",
                                        }));
                                    }}
                                />
                            </div>
                        </div>

                        <Divider width="100%" />

                        {/* ── Client ── */}
                        <h3 className="form-group-title">Client</h3>
                        <div className="row">

                            <div className="col-md-6 form-group">
                                <Label name="Point of Contact Name" bold />
                                <Input
                                    placeholder="Enter Point of Contact Name"
                                    value={formData.contact_name}
                                    onChange={(value: string) => updateForm("contact_name", value)}
                                />
                            </div>

                            <div className="col-md-6 form-group search-control">
                                <Label name="Point of Contact Email" bold />
                                <Input
                                    placeholder="Enter Point of Contact Email"
                                    value={formData.contact_email}
                                    onChange={(value: string) => updateForm("contact_email", value)}
                                />
                                {formErrors.contact_email && <span className="text-danger">{formErrors.contact_email}</span>}
                            </div>

                            <div className="col-md-6 form-group search-control">
                                <Label name="Point of Contact Number" bold />
                                <Input
                                    placeholder="Enter Point of Contact Number"
                                    value={formData.contact_phone}
                                    onChange={(value: string) => updateForm("contact_phone", value.replace(/[^0-9+\-\s()]/g, ''))}
                                />
                            </div>
                            <div className="col-md-6 form-group">
                                <Label name="Client Profile" bold required />

                                <AbsSelect
                                    key={`client-profile-${projectId || 'new'}-${clientProfileOptions.length}-${formData.client_profile}`}
                                    mult={false}
                                    options={clientProfileOptions}
                                    selected={formData.client_profile ? [formData.client_profile] : []}
                                    onChange={(selected: string[]) => {
                                        const value = selected?.[0] || "";

                                        updateForm("client_profile", value);
                                        loadClientDetails(value, false);
                                    }}
                                    placeholder="Select a Client Profile"
                                />

                                {formErrors.client_profile && (
                                    <span className="text-danger">{formErrors.client_profile}</span>
                                )}
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="WCN Number" bold />

                                <AbsSelect
                                    key={`wcn-number-${projectId || 'new'}-${wcnOptions.length}-${formData.wcn_number}`}
                                    mult={false}
                                    options={wcnOptions}
                                    selected={formData.wcn_number ? [formData.wcn_number] : []}
                                    onChange={(selected: string[]) => {
                                        const value = selected?.[0] || "";

                                        updateForm("wcn_number", value);
                                        loadClientDetails(value, true);
                                    }}
                                    placeholder="Select a WCN Number"
                                />

                                {formErrors.wcn_number && (
                                    <span className="text-danger">{formErrors.wcn_number}</span>
                                )}
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Company Name" bold required />
                                <div className="form-control-plaintext">
                                    {formData.company_name || ""}
                                </div>
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Company Address" bold required />
                                <div className="form-control-plaintext">
                                    {formData.company_address || ""}
                                </div>
                            </div>
                            <div className="col-md-6 form-group">
                                <Label name="Company Billing Email" bold />
                                <div className="form-control-plaintext">
                                    {formData.company_email || ""}
                                </div>
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Company Billing System URL" bold />
                                <div className="form-control-plaintext">
                                    {formData.company_system_url || ""}
                                </div>
                            </div>

                            {/* Company Name 
                            <div className="col-md-6 form-group">
                                <Label name="Company Name" bold />
                                <Input
                                    placeholder="Enter Company Name"
                                    value={formData.company_name}
                                    onChange={(value: string) => updateForm("company_name", value)}
                                    disabled={formData.contract_client}
                                />
                            </div>*/}

                            {/* Company Address
                            <div className="col-md-6 form-group">
                                <Label name="Company Address" bold />
                                <Input
                                    placeholder="Enter Company Address"
                                    value={formData.company_address}
                                    onChange={(value: string) => updateForm("company_address", value)}
                                    disabled={formData.contract_client}
                                />
                            </div> */}

                            <div className="col-md-6 form-group">
                                <CheckboxInput
                                    label="Same as Contract Client?"
                                    checked={formData.contract_client}
                                    onChange={(checked: boolean) => updateForm("contract_client", checked)}
                                />
                            </div>
                        </div>

                        <Divider width="100%" />

                        {/* ── Agreement ── */}
                        <h3 className="form-group-title">Agreement</h3>
                        <div className="row">

                            <div className="col-md-6 form-group">
                                <Label name="Agreement / Quote Number" bold required />
                                <Input
                                    placeholder="Enter Agreement / Quote Number"
                                    value={formData.agreement_quote_number}
                                    onChange={(value: string) => {
                                        if (/^\d*$/.test(value)) {
                                            updateForm("agreement_quote_number", value);
                                        }
                                    }}
                                />
                                {formErrors.agreement_quote_number && <span className="text-danger">{formErrors.agreement_quote_number}</span>}
                            </div>

                            {/* Proposal Rev No — FIX: use updateForm */}
                            <div className="col-md-6 form-group search-control">
                                <Label name="Proposal Rev. No." bold required />
                                <Input
                                    placeholder="Enter Proposal Rev. No."
                                    value={formData.proposal_rev_no}
                                    maxLength={2}
                                    onChange={(value: string) => {
                                        if (/^\d*$/.test(value)) updateForm("proposal_rev_no", value);
                                    }}
                                />
                                {formErrors.proposal_rev_no && <span className="text-danger">{formErrors.proposal_rev_no}</span>}
                            </div>

                            <div className="col-md-6 form-group search-control">
                                <Label name="Agreement Submitted Date" bold required />
                                <DatePickerInput
                                    autoFocus={false}
                                    value={agreementDate || ""}
                                    onChange={(val: Date) => {
                                        setAgreementDate(val);
                                        setFormErrors(prev => ({ ...prev, agreement_submitted_date: "" }));
                                    }}
                                />

                                {formErrors.agreement_submitted_date && <span className="text-danger">{formErrors.agreement_submitted_date}</span>}
                            </div>

                            <div className="col-md-6 form-group search-control">
                                <Label name="Agreement Signed Date" bold required />
                                <DatePickerInput
                                    autoFocus={false}
                                    value={agreementSignedDate || ""}
                                    onChange={(val: Date) => {
                                        setAgreementSignedDate(val);
                                        setFormErrors(prev => ({ ...prev, agreement_signed_date: "" }));
                                    }}
                                />
                                {formErrors.agreement_signed_date && <span className="text-danger">{formErrors.agreement_signed_date}</span>}
                            </div>

                            {/* Agreement Owner — FIX: use updateForm */}
                            <div className="col-md-6 form-group">
                                <Label name="Agreement Owner" bold required />
                                <AbsSelect
                                    key={`agreement-owner-${projectId || 'new'}-${agreementOwnerOptions.length}-${formData.agreement_owner}`}
                                    mult={false}
                                    onChange={(selected: string[] | any) => updateForm("agreement_owner", (selected && selected[0]) || "")}
                                    options={agreementOwnerOptions}
                                    selected={formData.agreement_owner?.trim() ? [String(formData.agreement_owner)] : []}
                                    placeholder="Select an Agreement Owner"
                                />
                                {formErrors.agreement_owner && <span className="text-danger">{formErrors.agreement_owner}</span>}
                            </div>

                            {/* CAP Contract Value — FIX: use updateForm */}
                            <div className="col-md-6 form-group">
                                <Label name="CAP Contract Value" bold required />
                                <div className="currency-input-wrapper">
                                    <span className="currency-symbol">$</span>
                                    <Input
                                        placeholder="Enter CAP Contract Value"
                                        value={formData.cap_contract_value}
                                        onChange={(value: string) => updateForm("cap_contract_value", value.replace(/[^0-9.]/g, ''))}
                                    />
                                </div>
                                {formErrors.cap_contract_value && <span className="text-danger">{formErrors.cap_contract_value}</span>}
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="CRM Opportunity No." bold />
                                <Input
                                    placeholder="Enter CRM Opportunity No."
                                    value={formData.crm_opportunity_no}
                                    onChange={(value: string) => {
                                        if (!value.startsWith("OPP-")) return;
                                        const digits = value.replace("OPP-", "");
                                        if (/^\d{0,10}$/.test(digits)) {
                                            updateForm("crm_opportunity_no", "OPP-" + digits);
                                        }
                                    }}
                                />
                                {formErrors.crm_opportunity_no && <span className="text-danger">{formErrors.crm_opportunity_no}</span>}
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Project ID" bold />
                                <Input
                                    placeholder="Enter Project ID"
                                    value={formData.PIDNumber}
                                    onChange={(value: string) => {
                                        if (/^\d*$/.test(value)) updateForm("PIDNumber", value);
                                    }}
                                    maxLength={7}
                                />
                                {formErrors.PIDNumber && <span className="text-danger">{formErrors.PIDNumber}</span>}
                            </div>
                        </div>

                        <Divider width="100%" />

                        {/* ── Invoicing ── */}
                        <h3 className="form-group-title">Invoicing</h3>
                        <div className="row">
                            <div className="col-md-6 form-group">
                                <Label name="Expense % Markup" bold />
                                <Input
                                    placeholder="Enter Expense % Markup"
                                    value={formData.expense_markup}
                                    onChange={(value: string) => {
                                        if (/^\d*$/.test(value)) {
                                            updateForm("expense_markup", value);
                                        }
                                    }}
                                />
                                {formErrors.expense_markup && <span className="text-danger">{formErrors.expense_markup}</span>}
                            </div>
                            <div className="col-md-6 form-group">
                                <Label name="Survey Day Rate to Client" bold />
                                <div className="currency-input-wrapper">
                                    <span className="currency-symbol">$</span>
                                    <Input
                                        placeholder="Enter Survey Day Rate to Client"
                                        value={formData.survey_day_rate_to_client}
                                        onChange={(value: string) => updateForm("survey_day_rate_to_client", value.replace(/[^0-9.]/g, ''))}
                                    />
                                </div>
                            </div>
                            <div className="col-md-6 form-group">
                                <Label name="Survey Day Rate from ABS" bold />
                                <div className="currency-input-wrapper">
                                    <span className="currency-symbol">$</span>
                                    <Input
                                        placeholder="Enter Survey Day Rate from ABS"
                                        value={formData.survey_day_rate_from_abs}
                                        onChange={(value: string) => updateForm("survey_day_rate_from_abs", value.replace(/[^0-9.]/g, ''))}
                                    />
                                </div>
                            </div>
                            <div className="col-md-3 form-group">
                                <Label name="SED Rate 1" bold />
                                <div className="currency-input-wrapper">
                                    <span className="currency-symbol">$</span>
                                    <Input
                                        placeholder="Enter SED Rate 1"
                                        value={formData.sed_rate_1}
                                        onChange={(value: string) => updateForm("sed_rate_1", value.replace(/[^0-9.]/g, ''))}
                                    />
                                </div>
                            </div>
                            <div className="col-md-3 form-group">
                                <Label name="SED Rate 2" bold />
                                <div className="currency-input-wrapper">
                                    <span className="currency-symbol">$</span>
                                    <Input
                                        placeholder="Enter SED Rate 2"
                                        value={formData.sed_rate_2}
                                        onChange={(value: string) => updateForm("sed_rate_2", value.replace(/[^0-9.]/g, ''))}
                                    />
                                </div>
                            </div>
                        </div>

                        {/* Submit error */}
                        {formErrors.submit && (
                            <div className="col-md-12 form-group">
                                <div className="alert alert-danger" role="alert">
                                    {formErrors.submit}
                                </div>
                            </div>
                        )}

                        {/* Action buttons */}
                        <div className="col-md-12 form-group">
                            <div className="d-flex gap-2">
                                <Button
                                    variant={BUTTONS.PRIMARY}
                                    onClick={handleCreate}
                                    disabled={isSaving}
                                >
                                    {isSaving ? "Saving..." : (projectId ? "Update" : "Save")}
                                </Button>
                                <Button
                                    variant={BUTTONS.SECONDARY}
                                    onClick={handleCancel}
                                    disabled={isSaving}
                                >
                                    Cancel
                                </Button>
                            </div>
                        </div>
                            </>
                        )}
                    </>
                )}
            </SideDialog>
        </>
    );
};

export default CreateProject;
