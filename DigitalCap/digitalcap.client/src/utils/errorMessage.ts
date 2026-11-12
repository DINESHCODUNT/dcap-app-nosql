export const GENERIC_ERROR_MESSAGE = "Something went wrong. Please try again later.";

/** True when the text is not safe/useful to show a user: HTML pages, stack traces, exception/framework text, raw axios texts, very long text. */
export function isTechnicalErrorText(text: unknown): boolean {
    if (typeof text !== "string") return false;
    const t = text.trim();
    if (!t) return false;
    if (/<\s*(!doctype|html|head|body|title|center|hr|h1)\b/i.test(t)) return true;          // HTML error pages
    if (/\bat\s+[\w.<>]+\(|\bat\s+System\.|\bat\s+Microsoft\./.test(t)) return true;         // stack traces
    if (/(Exception|SqlException|NullReference|System\.|Microsoft\.|Npgsql|SQLite Error|stack trace)/i.test(t)) return true;
    if (/^Request failed with status code \d+$/i.test(t)) return true;                         // raw axios message
    if (/Gateway|Bad Gateway|Service Unavailable|Time-?out/i.test(t) && t.length < 200) return true;
    if (t.length > 300) return true;
    return false;
}

/** Returns the text if it is a normal user message, otherwise the generic message. */
export function toFriendlyErrorMessage(text: unknown, fallback: string = GENERIC_ERROR_MESSAGE): string {
    if (typeof text === "string" && text.trim() && !isTechnicalErrorText(text)) return text;
    return fallback;
}
