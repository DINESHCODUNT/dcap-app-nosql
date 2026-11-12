export const NOT_APPLICABLE_DATE_TEXT = "N/A";
export const NOT_APPLICABLE_DATE = new Date(1900, 0, 1);
export const DATE_ENTRY_ERROR = "Enter a date as dd-MMM-yyyy or N/A.";

const SHORT_MONTHS = [
    "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
];

const MONTH_NAMES: Record<string, number> = {
    jan: 0, january: 0,
    feb: 1, february: 1,
    mar: 2, march: 2,
    apr: 3, april: 3,
    may: 4, jun: 5, june: 5,
    jul: 6, july: 6,
    aug: 7, august: 7,
    sep: 8, sept: 8, september: 8,
    oct: 9, october: 9,
    nov: 10, november: 10,
    dec: 11, december: 11
};

export const isNotApplicableText = (text: string | null | undefined): boolean => {
    return /^\s*n\s*\/?\s*a\s*$/i.test(text || "");
};

export const isNotApplicableDate = (value: Date | string | null | undefined): boolean => {
    if (!value) return false;
    if (value instanceof Date) {
        return (
            !isNaN(value.getTime()) &&
            value.getFullYear() === 1900 &&
            value.getMonth() === 0 &&
            value.getDate() === 1
        );
    }
    if (typeof value === "string") {
        const s = value.trim();
        if (isNotApplicableText(s)) return true;
        if (/^1900-01-01\b/.test(s)) return true;
        if (/^01-01-1900\b/.test(s)) return true;
        if (/^01-Jan-1900\b/i.test(s)) return true;
    }
    return false;
};

export const parseDateEntry = (text: string | null | undefined): Date | null => {
    if (!text || typeof text !== "string") return null;
    const s = text.trim();
    if (!s) return null;

    // Pattern 1: dd-MMM-yyyy or d-MMM-yyyy (e.g. 05-Nov-2026, 5 Nov 2026)
    const nameMatch = s.match(/^(\d{1,2})[-/. ]([A-Za-z]{3,9})[-/. ,]+(\d{4})$/);
    if (nameMatch) {
        const day = parseInt(nameMatch[1], 10);
        const mStr = nameMatch[2].toLowerCase();
        const year = parseInt(nameMatch[3], 10);
        if (mStr in MONTH_NAMES) {
            const month = MONTH_NAMES[mStr];
            const d = new Date(year, month, day);
            if (d.getFullYear() === year && d.getMonth() === month && d.getDate() === day) {
                return d;
            }
        }
        return null;
    }

    // Pattern 2: day-first numeric (dd-MM-yyyy, d-M-yyyy, dd/MM/yyyy, dd.MM.yyyy)
    const numMatch = s.match(/^(\d{1,2})[-/.](\d{1,2})[-/.](\d{4})$/);
    if (numMatch) {
        const day = parseInt(numMatch[1], 10);
        const month = parseInt(numMatch[2], 10) - 1;
        const year = parseInt(numMatch[3], 10);
        if (month >= 0 && month <= 11) {
            const d = new Date(year, month, day);
            if (d.getFullYear() === year && d.getMonth() === month && d.getDate() === day) {
                return d;
            }
        }
        return null;
    }

    // Pattern 3: year-first numeric (yyyy-MM-dd)
    const isoMatch = s.match(/^(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})$/);
    if (isoMatch) {
        const year = parseInt(isoMatch[1], 10);
        const month = parseInt(isoMatch[2], 10) - 1;
        const day = parseInt(isoMatch[3], 10);
        if (month >= 0 && month <= 11) {
            const d = new Date(year, month, day);
            if (d.getFullYear() === year && d.getMonth() === month && d.getDate() === day) {
                return d;
            }
        }
        return null;
    }

    return null;
};

export const isValidDateText = (text: string | null | undefined): boolean => {
    if (!text || !text.trim() || isNotApplicableText(text)) return true;
    return parseDateEntry(text) !== null;
};

export const formatDateForDisplay = (date: Date | null | undefined): string => {
    if (!date || !(date instanceof Date) || isNaN(date.getTime())) {
        return "";
    }
    if (isNotApplicableDate(date)) {
        return NOT_APPLICABLE_DATE_TEXT;
    }
    const day = String(date.getDate()).padStart(2, "0");
    const month = SHORT_MONTHS[date.getMonth()];
    const year = date.getFullYear();
    return `${day}-${month}-${year}`;
};
