import React from "react";
import ReactQuill from "react-quill";
import "react-quill/dist/quill.snow.css";
import "./RichTextEditor.css";

// Access Quill instance from ReactQuill
const Quill = ReactQuill.Quill;

export const FONT_FAMILIES = [
    "arial",
    "courier-new",
    "georgia",
    "impact",
    "lucida-console",
    "tahoma",
    "times-new-roman",
    "trebuchet-ms",
    "verdana"
];

export const FONT_SIZES = [
    "8pt",
    "10pt",
    "12pt",
    "14pt",
    "18pt",
    "24pt",
    "36pt"
];

// Register custom font family whitelist using Quill's ClassAttributor (pure CSS classes, NO inline css)
const Font = Quill.import("formats/font");
Font.whitelist = FONT_FAMILIES;
Quill.register(Font, true);

// Register custom font size whitelist using Quill's ClassAttributor (pure CSS classes, NO inline css)
const Size = Quill.import("formats/size");
Size.whitelist = FONT_SIZES;
Quill.register(Size, true);

export interface RichTextEditorProps {
    id?: string;
    value?: string;
    onChange?: (value: string) => void;
    onBlur?: (previousRange: any, source: any, editor: any) => void;
    readOnly?: boolean;
    disabled?: boolean;
    placeholder?: string;
    className?: string;
    style?: React.CSSProperties;
}

export const RICH_TEXT_MODULES = {
    toolbar: [
        ["bold", "italic", "underline", "strike"],
        [{ align: "" }, { align: "center" }, { align: "right" }, { align: "justify" }],
        [{ list: "bullet" }, { list: "ordered" }],
        [{ indent: "-1" }, { indent: "+1" }],
        ["clean"],
        [{ header: [1, 2, 3, 4, 5, 6, false] }],
        [{ font: [false, ...FONT_FAMILIES] }],
        [{ size: [false, ...FONT_SIZES] }],
        [{ color: [] }, { background: [] }]
    ]
};

export const RICH_TEXT_FORMATS = [
    "bold",
    "italic",
    "underline",
    "strike",
    "align",
    "list",
    "indent",
    "clean",
    "header",
    "font",
    "size",
    "color",
    "background"
];

const RichTextEditor: React.FC<RichTextEditorProps> = ({
    id,
    value = "",
    onChange,
    onBlur,
    readOnly = false,
    disabled = false,
    placeholder,
    className = "",
    style
}) => {
    const isReadOnly = readOnly || disabled;
    const wrapperRef = React.useRef<HTMLDivElement | null>(null);

    const handleBlur = (previousRange: any, source: any, editor: any) => {
        setTimeout(() => {
            if (wrapperRef.current && wrapperRef.current.contains(document.activeElement)) {
                // Focus is still within toolbar or editor
                return;
            }
            onBlur?.(previousRange, source, editor);
        }, 150);
    };

    return (
        <div
            id={id}
            ref={wrapperRef}
            className={`rich-text-editor-wrapper ${isReadOnly ? "readonly-editor" : ""}`}
        >
            <ReactQuill
                theme="snow"
                className={`bg-white ${className}`}
                style={style}
                value={value || ""}
                onChange={onChange}
                onBlur={handleBlur}
                placeholder={placeholder}
                readOnly={isReadOnly}
                modules={RICH_TEXT_MODULES}
                formats={RICH_TEXT_FORMATS}
            />
        </div>
    );
};

export default RichTextEditor;
