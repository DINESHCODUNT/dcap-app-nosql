const SURVEY_STATUS_FIELD_MAP: Record<string, string> = {
    surveydueDate: "classSurveyDate",
    drydockdueDate: "dryDockDate",
    sshull: "specialHullNo",
    surveydue: "specialContinuousMachineryDate",
    boilersurveydue: "boilerDate",
    tailshaftsurveydue: "tailshaftDate",
};

const toStoredValue = (field: string, value: any): any => {
    if (value instanceof Date) {
        return value.toISOString();
    }
    if (field === "sshull" && value !== "" && value != null) {
        return Number(value);
    }
    return value;
};

const updateSurveyStatus = (
    survey: any,
    field: string,
    value: any,
    specialHullNo?: number | string
): any => {
    const updated = { ...(survey ?? {}) };
    const storedValue = toStoredValue(field, value);

    if (field === "due") {
        const hullNo = Number(specialHullNo);
        if (hullNo >= 1 && hullNo <= 15) {
            updated[`specialContinuousHull${hullNo}`] = storedValue;
        }
        return updated;
    }

    const apiKey = SURVEY_STATUS_FIELD_MAP[field];
    if (apiKey) {
        updated[apiKey] = storedValue;
    }

    return updated;
};

export function applySurveyStatusFieldUpdate(
    prev: any,
    field: string,
    value: any,
    specialHullNo?: number | string
): any {
    if (!prev) return prev;

    const next: any = {
        ...prev,
        surveyStatus: updateSurveyStatus(prev.surveyStatus, field, value, specialHullNo),
    };

    if (next.report) {
        next.report = {
            ...next.report,
            surveyStatus: updateSurveyStatus(
                next.report.surveyStatus,
                field,
                value,
                specialHullNo
            ),
        };
    }

    return next;
}
