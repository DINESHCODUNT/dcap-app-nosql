import React, { useState, useRef, useEffect, useMemo } from "react";
import { CheckboxInput } from "customer_portal-ui-shared";

export type DropdownWithSearchOption = { label: string; value: string };

type GroupedOption = { label: string; options: DropdownWithSearchOption[] };

type DropdownWithSearchProps = {
    options?: DropdownWithSearchOption[];
    placeholder?: string;
    onSelect: (vals: DropdownWithSearchOption[]) => void;
    selectedValue?: DropdownWithSearchOption[];
    searchPlaceholder?: string;
    isMulti?: boolean;
    isGrouped?: boolean;
    isSearchable?: boolean;
    disabled?: boolean;
    hasMore?: boolean;
    onLoadMore?: () => void;
    loadingMore?: boolean;
    loadedCount?: number;
    totalCount?: number;
    onSearchChange?: (searchText: string) => void;
};

const DropdownWithSearch: React.FC<DropdownWithSearchProps> = ({
    options = [],
    placeholder = "Select an option",
    onSelect,
    selectedValue,
    searchPlaceholder = "Search...",
    isMulti = false,
    isGrouped = false,
    isSearchable = true,
    disabled = false,
    hasMore = false,
    onLoadMore,
    loadingMore = false,
    loadedCount,
    totalCount,
    onSearchChange,
}) => {
    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const [searchValue, setSearchValue] = useState("");
    const dropdownRef = useRef<HTMLDivElement | null>(null);

    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (
                dropdownRef.current &&
                event.target instanceof Node &&
                !dropdownRef.current.contains(event.target)
            ) {
                setIsDropdownOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    const normalised: GroupedOption[] = useMemo(
        () => (isGrouped ? (options as unknown as GroupedOption[]) : [{ label: "", options }]),
        [isGrouped, options]
    );

    const filteredOptions = useMemo(
        () =>
            normalised.map((group) => ({
                ...group,
                options:
                    isSearchable && searchValue && !onSearchChange
                        ? group.options.filter((option) =>
                            option.label.toLowerCase().includes(searchValue.toLowerCase())
                        )
                        : group.options,
            })),
        [normalised, isSearchable, searchValue, onSearchChange]
    );

    const isSelected = (option: DropdownWithSearchOption): boolean => {
        if (!selectedValue) return false;
        if (isMulti) return selectedValue.some((item) => item.value === option.value);
        return selectedValue[0]?.value === option.value;
    };

    const handleSelect = (option: DropdownWithSearchOption) => {
        if (disabled) return;
        if (isMulti) {
            const updated = isSelected(option)
                ? (selectedValue || []).filter((item) => item.value !== option.value)
                : [...(selectedValue || []), option];
            onSelect(updated);
        } else {
            onSelect([option]);
            setIsDropdownOpen(false);
        }
        setSearchValue("");
    };

    const getDisplayValue = (): string => {
        if (!selectedValue || selectedValue.length === 0) return placeholder;
        if (isMulti) {
            if (selectedValue.length > 3) return `${selectedValue.length} selected`;
            return selectedValue.map((item) => item.label).join(", ");
        }
        return selectedValue[0]?.label ?? placeholder;
    };

    return (
        <div className="dd-search" ref={dropdownRef}>
            <div
                className="dd-control"
                onClick={() => !disabled && setIsDropdownOpen(!isDropdownOpen)}
                style={disabled ? { cursor: "not-allowed", opacity: 0.65 } : undefined}
            >
                <span>{getDisplayValue()}</span>
                <span className="dd-arrow">{isDropdownOpen ? "▲" : "▼"}</span>
            </div>

            {isDropdownOpen && !disabled && (
                <div className="dd-open">
                    {isSearchable && (
                        <div className="dd-search-container">
                            <input
                                type="text"
                                placeholder={searchPlaceholder}
                                value={searchValue}
                                onChange={(e) => {
                                    const next = e.target.value;
                                    setSearchValue(next);
                                    onSearchChange?.(next);
                                }}
                                className="dd-input-search"
                            />
                        </div>
                    )}

                    <div className="dd-options-scroll">
                        {filteredOptions.map((group, gi) => (
                            <div key={group.label || String(gi)}>
                                {isGrouped && <div className="dd-group-label">{group.label}</div>}

                                {group.options.length > 0 ? (
                                    group.options.map((option) => (
                                        <div
                                            key={option.value}
                                            className={isSelected(option) ? "dd-option-active" : "dd-option"}
                                            role="button"
                                            tabIndex={0}
                                            onKeyDown={(e) => {
                                                if (e.key === "Enter" || e.key === " ") {
                                                    e.preventDefault();
                                                    handleSelect(option);
                                                }
                                            }}
                                            onClick={() => handleSelect(option)}
                                        >
                                            {isMulti ? (
                                                <label
                                                    className="dd-option-label"
                                                    onClick={(e) => {
                                                        e.preventDefault();
                                                        e.stopPropagation();
                                                        handleSelect(option);
                                                    }}
                                                >
                                                    <CheckboxInput
                                                        label={option.label}
                                                        checked={isSelected(option)}
                                                        onChange={() => { }}
                                                    />
                                                </label>
                                            ) : (
                                                <span>
                                                    {option.label}
                                                </span>
                                            )}
                                        </div>
                                    ))
                                ) : (
                                    <div className="dd-no-data">No options found</div>
                                )}
                            </div>
                        ))}
                    </div>

                    {(hasMore || (loadedCount != null && totalCount != null && totalCount > 0)) && (
                        <div
                            className="dd-load-more-footer"
                            onClick={(e) => e.stopPropagation()}
                            onMouseDown={(e) => e.stopPropagation()}
                        >
                            {loadedCount != null && totalCount != null && (
                                <span className="dd-records-summary">
                                    1-{loadedCount.toLocaleString()} of {totalCount.toLocaleString()} records
                                    {!hasMore && !onSearchChange ? ". Use search to filter." : "."}
                                </span>
                            )}
                            {hasMore && onLoadMore && (
                                <span
                                    role="button"
                                    tabIndex={0}
                                    className="dd-load-more-btn"
                                    aria-disabled={loadingMore}
                                    onKeyDown={(e) => {
                                        if (!loadingMore && (e.key === "Enter" || e.key === " ")) {
                                            e.preventDefault();
                                            onLoadMore();
                                        }
                                    }}
                                    onClick={() => {
                                        if (!loadingMore) onLoadMore();
                                    }}
                                >
                                    {loadingMore ? "Loading..." : "Load More"}
                                </span>
                            )}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export default DropdownWithSearch;
