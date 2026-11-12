export const HI_PART_ID = 'HI';
export const HI_PART_NAME = 'Part H & I';
export const HI_DEFAULT_SECTIONS = [{ id: 'summary', label: 'H & I' }];

export const getPartGridName = (partId: string) =>
    partId === HI_PART_ID ? HI_PART_NAME : `Part ${partId}`;

export const getPartNameFromGrid = (part: any) =>
    (part?.name ?? part?.partName ?? '').trim();

export const findPartInGrid = (grid: any[] | undefined, partId: string) =>
    grid?.find((part: any) => getPartNameFromGrid(part) === getPartGridName(partId));

export const getPartDisplayLabel = (partId: string): string => {
    if (partId === "VMD") return "Vessel Main Data";
    if (partId === HI_PART_ID) return "H & I";
    return partId;
};

export const getPartSectionsTitle = (partId: string): string => {
    if (partId === "VMD") return "Vessel Main Data";
    return `Part ${getPartDisplayLabel(partId)} Sections`;
};
