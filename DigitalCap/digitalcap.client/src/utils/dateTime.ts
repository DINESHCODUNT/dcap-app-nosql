import moment from "moment";

export const DISPLAY_DATE_FORMAT = "DD-MMM-YYYY";
export const DISPLAY_DATETIME_FORMAT = "DD-MMM-YYYY hh:mm A";

export function formatDisplayDate(value: string | Date | null | undefined): string {
    if (!value) return "";
    const m = moment(value);
    if (!m.isValid()) return "";
    return m.locale("en").format(DISPLAY_DATE_FORMAT);
}

export function formatDisplayDateTime(value: string | Date | null | undefined): string {
    if (!value) return "";
    const m = moment(value);
    if (!m.isValid()) return "";
    return m.locale("en").format(DISPLAY_DATETIME_FORMAT);
}

export function parseApiDateTime(value: string | Date | null | undefined): Date {
    if (!value) return new Date(NaN);
    if (value instanceof Date) return value;

    const trimmed = value.trim();
    if (!trimmed) return new Date(NaN);

    if (/[zZ]$/.test(trimmed) || /[+-]\d{2}:\d{2}$/.test(trimmed)) {
        return new Date(trimmed);
    }

    const normalized = trimmed.replace(/(\.\d{3})\d+$/, "$1");
    return new Date(`${normalized}Z`);
}

export const isEmptyDate = (value: any): boolean => {
    if (value === null || value === undefined || value === "") return true;
    if (typeof value === "string" && /^\d{2}-\d{2}-\d{4}$/.test(value.trim())) {
        const year = Number(value.trim().split("-")[2]);
        return isNaN(year) || year <= 1901;
    }
    const d = new Date(value);
    if (isNaN(d.getTime())) return true;
    return d.getFullYear() <= 1901;
};

export function parseVisitDate(value: string | null | undefined): Date | "" {
    if (isEmptyDate(value)) {
        return "";
    }

    if (/^\d{2}-\d{2}-\d{4}$/.test(value)) {
        const [day, month, year] = value.split("-").map(Number);
        return new Date(year, month - 1, day);
    }

    const date = new Date(value);
    return isNaN(date.getTime()) ? "" : date;
}

export function formatVisitDateForSave(date: Date): string {
    const day = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const year = date.getFullYear();
    return `${day}-${month}-${year}`;
}

export function getRelativeTime(value: string | Date | null | undefined): string {
    const date = parseApiDateTime(value);
    const diff = Date.now() - date.getTime();

    if (!Number.isFinite(diff) || diff < 0) return "Just now";

    const minutes = Math.floor(diff / (1000 * 60));
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));

    if (days > 0) return `${days} day${days > 1 ? "s" : ""} ago`;
    if (hours > 0) return `${hours} hour${hours > 1 ? "s" : ""} ago`;
    if (minutes > 0) return `${minutes} minute${minutes > 1 ? "s" : ""} ago`;

    return "Just now";
}
