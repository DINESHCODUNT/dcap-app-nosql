export const FileDataType = {
    Empty: 0,
    ClientDeliverables: 9,
    InternalDocuments: 10,
    InternalInvoicing: 11,
    VesselDocuments: 12,
} as const;


export function isClientDeliverablesType(dataType: number | null | undefined): boolean {
    return dataType === FileDataType.ClientDeliverables;
}

export function parseFileDataType(raw: unknown): number | null {
    if (raw == null || raw === "") return null;
    if (typeof raw === "number" && Number.isFinite(raw)) {
        return raw;
    }
    if (typeof raw === "object" && !Array.isArray(raw)) {
        const o = raw as Record<string, unknown>;
        const nested =
            parseFileDataType(o.value) ??
            parseFileDataType(o.Value) ??
            parseFileDataType(o.name) ??
            parseFileDataType(o.Name) ??
            parseFileDataType(o.text) ??
            parseFileDataType(o.Text) ??
            parseFileDataType(o.data) ??
            parseFileDataType(o.Data);
        if (nested != null) return nested;
    }
    const s = String(raw).trim();
    if (!s || s === "[object Object]") return null;
    const asNum = Number(s);
    if (Number.isFinite(asNum) && /^\d+$/.test(s.replace(/\s/g, ""))) {
        return asNum;
    }
    const key = s.toLowerCase().replace(/[\s_-]+/g, "");
    const byName: Record<string, number> = {
        empty: FileDataType.Empty,
        clientdeliverables: FileDataType.ClientDeliverables,
        internaldocuments: FileDataType.InternalDocuments,
        internalinvoicing: FileDataType.InternalInvoicing,
        vesseldocuments: FileDataType.VesselDocuments,
    };
    if (key in byName) return byName[key]!;
    return null;
}

export function getFileDataTypeDisplayName(dt: number): string | null {
    switch (dt) {
        case FileDataType.ClientDeliverables:
            return "Client Deliverables";
        case FileDataType.InternalDocuments:
            return "Internal Documents";
        case FileDataType.InternalInvoicing:
            return "Internal Invoicing";
        case FileDataType.VesselDocuments:
            return "Vessel Documents";
        case FileDataType.Empty:
            return "Empty";
        default:
            return null;
    }
}

export function inferFileDataTypeLoose(text: string): number | null {
    const key = text.toLowerCase().replace(/[\s_-]+/g, "");
    if (!key) return null;
    if (key.includes("clientdeliverable") || (key.includes("client") && key.includes("deliverable"))) {
        return FileDataType.ClientDeliverables;
    }
    if (key.includes("internalinvoic")) {
        return FileDataType.InternalInvoicing;
    }
    if (key.includes("internaldocument")) {
        return FileDataType.InternalDocuments;
    }
    if (key.includes("vesseldocument")) {
        return FileDataType.VesselDocuments;
    }
    return null;
}
