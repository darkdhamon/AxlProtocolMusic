function getSelectionParts(textarea) {
    const value = textarea.value ?? "";
    const start = textarea.selectionStart ?? value.length;
    const end = textarea.selectionEnd ?? value.length;

    return {
        value,
        start,
        end,
        selected: value.slice(start, end)
    };
}

function updateTextarea(textarea, newValue, start, end) {
    textarea.value = newValue;
    textarea.focus();
    textarea.setSelectionRange(start, end);
    textarea.dispatchEvent(new Event("input", { bubbles: true }));
}

export function applyMarkdownFormat(textarea, format) {
    const parts = getSelectionParts(textarea);
    const selected = parts.selected || "text";
    let replacement = selected;
    let selectionStart = parts.start;
    let selectionEnd = parts.end;

    switch (format) {
        case "heading":
            replacement = `## ${selected}`;
            break;
        case "bold":
            replacement = `**${selected}**`;
            selectionStart += 2;
            selectionEnd = selectionStart + selected.length;
            break;
        case "italic":
            replacement = `*${selected}*`;
            selectionStart += 1;
            selectionEnd = selectionStart + selected.length;
            break;
        case "quote":
            replacement = `> ${selected}`;
            break;
        case "unordered-list":
            replacement = `- ${selected}`;
            break;
        case "link":
            replacement = `[${selected}](https://example.com)`;
            selectionStart += selected.length + 3;
            selectionEnd = selectionStart + 19;
            break;
        default:
            return;
    }

    const newValue = `${parts.value.slice(0, parts.start)}${replacement}${parts.value.slice(parts.end)}`;

    if (format === "heading" || format === "quote" || format === "unordered-list") {
        selectionStart = parts.start;
        selectionEnd = parts.start + replacement.length;
    }

    updateTextarea(textarea, newValue, selectionStart, selectionEnd);
}
