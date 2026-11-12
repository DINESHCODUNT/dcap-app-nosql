/** Maps form field names to reportData / vessel property keys. */
export const VESSEL_FIELD_MAP: Record<string, { reportKey?: string; vesselKey?: string }> = {
    report_no: { reportKey: "reportNo", vesselKey: "reportNo" },
    port: { reportKey: "reportPort" },
    name_prefix: { vesselKey: "selectedPrefix" },
    vessel_name: { vesselKey: "vesselName" },
    vessel_type: { vesselKey: "vesselType" },
    year_built: { vesselKey: "yearBuilt" },
    year_built_by: { vesselKey: "builtBy" },
    hull_no: { vesselKey: "hullNo" },
    flag: { vesselKey: "flagName" },
    port_of_registry: { vesselKey: "homeport" },
    official_no: { vesselKey: "officalNumber" },
    call_sign: { vesselKey: "callSign" },
    classed: { vesselKey: "classed" },
    class_no: { vesselKey: "classNo" },
    class_notations: { vesselKey: "classNotation" },
    additional_class_notations: { vesselKey: "machinery" },
    other_notations: { vesselKey: "other" },
    previous_names: { vesselKey: "previousName" },
    owners: { vesselKey: "ownerName" },
    managers: { vesselKey: "manager" },
    length: { vesselKey: "length" },
    breadth: { vesselKey: "breadth" },
    depth: { vesselKey: "depth" },
    summer_draft: { vesselKey: "draft" },
    deadweight: { vesselKey: "deadweight" },
    gross_tons: { vesselKey: "grossTons" },
    net_tons: { vesselKey: "netTons" },
    propulsion_type: { vesselKey: "propulsionType" },
    propulsion_value: { vesselKey: "shaftRPM" },
    kw: { vesselKey: "kw" },
    propulsion_manufacturer: { vesselKey: "propulsionManufacturer" },
    gear_manufacturer: { vesselKey: "gearManufacturer" },
    lastVisitDate: { vesselKey: "lastVisitDate" },
    firstVisitDate: { vesselKey: "firstVisitDate" },
};

export function applyVesselFieldUpdate(prev: any, field: string, value: any): any {
    const mapping = VESSEL_FIELD_MAP[field];
    if (!mapping || !prev) return prev;

    const updateVessel = (vessel: any) => {
        if (!vessel || !mapping.vesselKey) return vessel;
        return { ...vessel, [mapping.vesselKey]: value };
    };

    const next: any = { ...prev };

    if (mapping.reportKey) {
        next[mapping.reportKey] = value;
    }

    if (mapping.vesselKey) {
        next.vessel = updateVessel(next.vessel);
    }

    if (next.report) {
        next.report = { ...next.report };
        if (mapping.reportKey) {
            next.report[mapping.reportKey] = value;
        }
        if (mapping.vesselKey && next.report.vessel) {
            next.report.vessel = updateVessel(next.report.vessel);
        }
    }

    return next;
}
