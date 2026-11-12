export const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

export interface ConfigSection {
    SectionId: string;
    SectionName: string;
    RequiredInReport: boolean;
    IncludeInReport: boolean;
    TankTypeId: number;
    TankType: string;
    IsSubSection: boolean;
    ParentId: string;
}

// Normalise one section from GetAllSections / GetAllParts (camelCase or PascalCase).
export const toConfigSection = (s: any): ConfigSection => ({
    SectionId: String(s.sectionId ?? s.SectionId ?? ""),
    SectionName: s.sectionName ?? s.SectionName ?? "",
    RequiredInReport: !!(s.requiredInReport ?? s.RequiredInReport),
    IncludeInReport: (s.includeInReport ?? s.IncludeInReport) !== false,
    TankTypeId: Number(s.tankTypeId ?? s.TankTypeId ?? 0),
    TankType: s.tankType ?? s.TankType ?? "",
    IsSubSection: !!(s.isSubSection ?? s.IsSubSection),
    ParentId: String(s.parentId ?? s.ParentId ?? EMPTY_GUID),
});

// What the popup shows: normal sections, plus one entry per tank type (sub-sections hidden).
export function getDisplayGroups(sections: ConfigSection[]) {
    const visible = sections.filter(s => !s.IsSubSection);
    const normal = visible.filter(s => s.TankTypeId === 0);
    const tankTypes = Array.from(
        visible.filter(s => s.TankTypeId !== 0)
            .reduce((map, s) => map.set(s.TankTypeId, [...(map.get(s.TankTypeId) ?? []), s]), new Map<number, ConfigSection[]>())
            .entries()
    ).map(([tankTypeId, items]) => ({
        tankTypeId,
        tankType: items[0].TankType || `Tank Type ${tankTypeId}`,
        required: items[0].RequiredInReport,
        included: items[0].IncludeInReport,
    }));
    return { normal, tankTypes };
}

export const isSelected = (s: ConfigSection) => s.RequiredInReport || s.IncludeInReport;

// Sections to send: selected normal sections, all tanks of selected tank types,
// and sub-sections whose parent is selected (like digital-cap SaveSettings).
export function collectSelectedSections(sections: ConfigSection[]): ConfigSection[] {
    const visible = sections.filter(s => !s.IsSubSection && isSelected(s));
    const parentIds = new Set(visible.map(s => s.SectionId.toLowerCase()));
    const subs = sections.filter(s => s.IsSubSection && parentIds.has(s.ParentId.toLowerCase()));
    return [...visible, ...subs].map(s => ({ ...s, IncludeInReport: true }));
}

export const SELECT_AT_LEAST_ONE = "Please select at least one section.";
