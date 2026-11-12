export const CIRCLE_E = '\u24BA';
export const MALTESE = '\u2720';

export const formatClassNotationsForDisplay = (notationStr: string | undefined | null): string => {
    if (!notationStr) return '';

    let formatted = notationStr;

    formatted = formatted.replace(/\*/g, MALTESE);

    if (formatted.includes('Circle E')) {
        formatted = formatted
            .replace(/Circle E\*/g, CIRCLE_E + '*')
            .replace(/Circle E/g, CIRCLE_E);
    }

    return formatted;
};

export const formatClassNotationsForSave = (displayStr: string | undefined | null): string => {
    if (!displayStr) return '';

    let raw = displayStr;

    raw = raw.replace(/\u2720/g, '*');

    raw = raw
        .replace(/\u24BA\*/g, 'Circle E*')
        .replace(/\u24BA/g, 'Circle E');

    return raw;
};
