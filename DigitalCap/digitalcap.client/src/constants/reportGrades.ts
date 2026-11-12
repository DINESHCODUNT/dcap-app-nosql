import type { ReportDetailsViewModel } from "../service/ProjectService";
export const FINAL_GRADE_VALUES = [1, 2, 3, 4, 5] as const;

export type FinalGradeValue = (typeof FINAL_GRADE_VALUES)[number];

export const SUB_GRADE_OPTIONS: { value: number; label: string }[] = [
    { value: 1, label: "A+" },
    { value: 2, label: "A" },
    { value: 3, label: "A-" },
    { value: 4, label: "B+" },
    { value: 5, label: "B" },
    { value: 6, label: "B-" },
    { value: 7, label: "C+" },
    { value: 8, label: "C" },
    { value: 9, label: "C-" },
    { value: 10, label: "D" },
];

export function getFinalGradeLabel(v: number | null | undefined): string {
    if (v === undefined || v == null) return "-";
    if (v < 1 || v > 5) return "-";
    return String(v);
}
export function getSubGradeLabel(v: number | null | undefined): string {
    if (v === undefined || v == null || v < 1 || v > 10) return "-";
    return SUB_GRADE_OPTIONS.find((g) => g.value === v)?.label ?? "-";
}
export function getSubGradeTextColor(v: number | null | undefined): string {
    const label = getSubGradeLabel(v);
    if (label === "-") return "#6c757d";
    if (["A+", "A", "A-", "B+"].includes(label)) return "#28a745";
    if (["B", "B-", "C+"].includes(label)) return "#fd7e14";
    if (["D"].includes(label)) return "#dc3545";
    return "#fd7e14";
}

export function getFinalGradeTextColor(v: number | null | undefined): string {
    if (v == null) return "#6c757d";
    if (v <= 2) return "#28a745";
    if (v <= 4) return "#fd7e14";
    return "#dc3545";
}

export function subGradeBarColorClass(v: number | null | undefined): string {
    const label = getSubGradeLabel(v);
    if (label === "-") return "bar-gray";
    if (["A+", "A", "A-", "B+"].includes(label)) return "bar-green";
    if (["D"].includes(label)) return "bar-red";
    return "bar-orange";
}
export function subGradeBarWidthPct(v: number | null | undefined): number {
    if (v === undefined || v == null || v < 1 || v > 10) return 0;
    return Math.min(100, Math.round(((11 - v) / 10) * 100));
}

export function finalGradeBarWidthPct(v: number | null | undefined): number {
    if (v === undefined || v === null) return 0;
    if (typeof v !== "number" || Number.isNaN(v) || !Number.isFinite(v)) return 0;
    if (v < 1 || v > 5) return 0;
    return Math.min(100, Math.round(((6 - v) / 5) * 100));
}

export function finalGradeBarColorClass(v: number | null | undefined): string {
    if (v === undefined || v === null || v < 1 || v > 5) return "bar-gray";
    if (v <= 2) return "bar-green";
    if (v <= 4) return "bar-orange";
    return "bar-red";
}


export const SUB_GRADE_ROWS: {
    key: keyof Pick<
        ReportDetailsViewModel,
        | "structuralGrade"
        | "fatigueGrade"
        | "renewalGrade"
        | "materialGrade"
        | "gaugingGrade"
        | "hullGirderStrength"
    >;
    label: string;
}[] = [
        { key: "structuralGrade", label: "Structural Grade" },
        { key: "fatigueGrade", label: "Fatigue Grade" },
        { key: "renewalGrade", label: "Renewal Grade" },
        { key: "materialGrade", label: "Material Grade" },
        { key: "gaugingGrade", label: "Gauging Grade" },
        { key: "hullGirderStrength", label: "Hull Girder Strength" },
    ];

