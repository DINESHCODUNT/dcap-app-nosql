import React, { useState, useEffect, useRef } from "react";
import { DatePickerInput } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import {
    NOT_APPLICABLE_DATE,
    NOT_APPLICABLE_DATE_TEXT,
    DATE_ENTRY_ERROR,
    isNotApplicableDate,
    isNotApplicableText,
    isValidDateText,
    parseDateEntry,
    formatDateForDisplay,
} from "../utils/notApplicableDate";

export interface NotApplicableDateInputProps {
    value?: Date | null;
    onChange?: (value: Date | null) => void;
    disabled?: boolean;
    className?: string;
    id?: string;
    placeholder?: string;
    minDate?: Date;
    maxDate?: Date;
    [key: string]: any;
}

export interface NotApplicableDateTextInputProps {
    text: string;
    onTextChange: (val: string) => void;
    onCommit: (val: string) => void;
    placeholder?: string;
    disabled?: boolean;
    onClick?: React.MouseEventHandler<any>;
    onKeyDown?: React.KeyboardEventHandler<HTMLInputElement>;
    onBlur?: React.FocusEventHandler<HTMLInputElement>;
    onFocus?: React.FocusEventHandler<HTMLInputElement>;
    [key: string]: any;
}

const isSameDay = (d1: Date | null | undefined, d2: Date | null | undefined): boolean => {
    if (!d1 && !d2) return true;
    if (!d1 || !d2) return false;
    return (
        d1.getFullYear() === d2.getFullYear() &&
        d1.getMonth() === d2.getMonth() &&
        d1.getDate() === d2.getDate()
    );
};

export const NotApplicableDateTextInput = React.forwardRef<HTMLInputElement, NotApplicableDateTextInputProps>(
    (
        {
            text,
            onTextChange,
            onCommit,
            placeholder = "Select date or type N/A",
            disabled = false,
            onClick,
            onKeyDown,
            onBlur,
            onFocus,
            value: _injectedValue,
            onChange: _injectedOnChange,
            ...rest
        },
        ref
    ) => {
        return (
            <div
                className="not-applicable-date-inner"
                style={{ position: "relative", width: "100%", display: "inline-block" }}
            >
                <input
                    {...rest}
                    ref={ref}
                    type="text"
                    className="na-date-input"
                    value={text}
                    onChange={(e) => onTextChange(e.target.value)}
                    onBlur={(e) => {
                        onCommit(e.target.value);
                        onBlur?.(e);
                    }}
                    onKeyDown={(e) => {
                        if (e.key === "Enter") {
                            onCommit(e.currentTarget.value);
                        }
                        onKeyDown?.(e);
                    }}
                    onFocus={onFocus}
                    placeholder={placeholder}
                    disabled={disabled}
                    readOnly={disabled}
                />
                <span
                    onClick={disabled ? undefined : onClick}
                    style={{
                        position: "absolute",
                        right: "8px",
                        top: "50%",
                        transform: "translateY(-50%)",
                        cursor: disabled ? "not-allowed" : "pointer",
                        display: "flex",
                        alignItems: "center",
                        color: "#6c757d",
                        pointerEvents: disabled ? "none" : "auto",
                    }}
                    title="Open calendar"
                >
                    <Icon.Calendar3 size={15} />
                </span>
            </div>
        );
    }
);

NotApplicableDateTextInput.displayName = "NotApplicableDateTextInput";

const NotApplicableDateInput: React.FC<NotApplicableDateInputProps> = ({
    value,
    onChange,
    disabled = false,
    className = "",
    placeholder = "Select date or type N/A",
    minDate,
    maxDate,
    ...restProps
}) => {
    const [text, setText] = useState<string>(() => formatDateForDisplay(value));
    const [error, setError] = useState<string | null>(null);
    const lastValidRef = useRef<Date | null>(value ?? null);
    const errorTimerRef = useRef<any>(null);

    useEffect(() => {
        lastValidRef.current = value ?? null;
        setText(formatDateForDisplay(value));
    }, [value]);

    useEffect(() => {
        return () => {
            if (errorTimerRef.current) {
                clearTimeout(errorTimerRef.current);
            }
        };
    }, []);

    const clearError = () => {
        if (errorTimerRef.current) {
            clearTimeout(errorTimerRef.current);
            errorTimerRef.current = null;
        }
        setError(null);
    };

    const showError = (msg: string) => {
        clearError();
        setError(msg);
        errorTimerRef.current = setTimeout(() => {
            setError(null);
            errorTimerRef.current = null;
        }, 3000);
    };

    const handleTextChange = (val: string) => {
        setText(val);
        if (isValidDateText(val)) {
            clearError();
        }
    };

    const commitChange = (raw: string) => {
        const s = raw.trim();
        if (!s) {
            clearError();
            setText("");
            if (!isSameDay(lastValidRef.current, null)) {
                lastValidRef.current = null;
                onChange?.(null);
            }
            return;
        }

        if (isNotApplicableText(s)) {
            clearError();
            setText(NOT_APPLICABLE_DATE_TEXT);
            if (!isSameDay(lastValidRef.current, NOT_APPLICABLE_DATE)) {
                lastValidRef.current = NOT_APPLICABLE_DATE;
                onChange?.(NOT_APPLICABLE_DATE);
            }
            return;
        }

        const parsed = parseDateEntry(s);
        if (parsed) {
            clearError();
            setText(formatDateForDisplay(parsed));
            if (!isSameDay(lastValidRef.current, parsed)) {
                lastValidRef.current = parsed;
                onChange?.(parsed);
            }
            return;
        }

        // Invalid entry
        showError(DATE_ENTRY_ERROR);
        setText(formatDateForDisplay(lastValidRef.current));
    };

    const handlePickerChange = (date: Date | null) => {
        clearError();
        setText(formatDateForDisplay(date));
        if (!isSameDay(lastValidRef.current, date)) {
            lastValidRef.current = date;
            onChange?.(date);
        }
    };

    // Pass null to DatePickerInput when N/A so react-datepicker doesn't default to year 1900
    const pickerValue = isNotApplicableDate(value) ? null : (value ?? null);

    return (
        <div
            className={`not-applicable-date-input-container ${className}`.trim()}
            style={{ position: "relative", display: "inline-block", width: "100%" }}
        >
            <DatePickerInput
                {...restProps}
                value={pickerValue}
                onChange={handlePickerChange}
                disabled={disabled}
                customInput={
                    <NotApplicableDateTextInput
                        text={text}
                        onTextChange={handleTextChange}
                        onCommit={commitChange}
                        placeholder={placeholder}
                        disabled={disabled}
                    />
                }
                placeholderText={placeholder}
                minDate={minDate}
                maxDate={maxDate}
            />
            {error && (
                <span
                    className="text-danger date-validation-msg"
                    style={{
                        position: "absolute",
                        top: "100%",
                        left: 0,
                        whiteSpace: "nowrap",
                        zIndex: 20,
                        fontSize: "12px",
                        marginTop: "2px",
                    }}
                >
                    {error}
                </span>
            )}
        </div>
    );
};

export default NotApplicableDateInput;
