console.log("Upload.js loaded");

/* ══════════════════════════════════════════════════════════════
   Toast helper  (also used by Upload.cshtml inline script)
   ══════════════════════════════════════════════════════════════ */

function dismiss(toast) {
    toast.style.animation = "toastOut 0.25s ease forwards";
    setTimeout(() => toast.remove(), 250);
}

/* ══════════════════════════════════════════════════════════════
   Main Upload logic
   ══════════════════════════════════════════════════════════════ */
document.addEventListener("DOMContentLoaded", function () {

    /* ── Element refs ───────────────────────────────────────── */
    const dropzone = document.getElementById("dropzone");
    const fileInput = document.getElementById("fileInput");
    const dropContent = document.getElementById("dropzoneContent");
    const previewState = document.getElementById("previewState");
    const previewImg = document.getElementById("previewImg");
    const clearBtn = document.getElementById("clearBtn");
    const uploadForm = document.getElementById("uploadForm");
    const uploadBtn = document.getElementById("uploadBtn");
    const fileError = document.getElementById("fileError");

    const ALLOWED_TYPES = ["image/jpeg", "image/png"];
    const ALLOWED_EXT = [".jpg", ".jpeg", ".png", ".jfif"];
    const MAX_SIZE_MB = 10;
    const MAX_SIZE_B = MAX_SIZE_MB * 1024 * 1024;

    if (!dropzone) return; // not on upload page

    /* ── Prevent browser default drag behaviour ─────────────── */
    ["dragenter", "dragover", "dragleave", "drop"].forEach(evt => {
        dropzone.addEventListener(evt, e => { e.preventDefault(); e.stopPropagation(); });
        document.body.addEventListener(evt, e => { e.preventDefault(); e.stopPropagation(); });
    });

    /* ── Click to browse (no double-trigger) ────────────────── */
    dropzone.addEventListener("click", (e) => {
        if (e.target.closest("#removeImg") ||
            e.target.closest("label") ||
            e.target.closest("input")) return;
        fileInput.click();
    });

    /* ── File validation ────────────────────────────────────── */
    function validateFile(file) {
        if (!file) {
            showFieldError("Please select an image before uploading.");
            return false;
        }

        const ext = "." + file.name.split(".").pop().toLowerCase();

        if (!ALLOWED_TYPES.includes(file.type) && !ALLOWED_EXT.includes(ext)) {
            showFieldError("Invalid file type. Only .jpg, .png, .jpeg, .jfif are allowed.");
            showToast("Invalid file type. Please upload a JPG or PNG image.", "error");
            return false;
        }

        if (file.size > MAX_SIZE_B) {
            showFieldError(`File is too large (${(file.size / 1024 / 1024).toFixed(1)} MB). Max allowed: ${MAX_SIZE_MB} MB.`);
            showToast(`File exceeds ${MAX_SIZE_MB} MB limit. Please choose a smaller image.`, "error");
            return false;
        }

        clearFieldError();
        return true;
    }

    function showFieldError(msg) {
        if (!fileError) return;
        fileError.textContent = "⚠ " + msg;
        fileError.style.display = "block";
        dropzone.classList.add("dropzone-error");
    }

    function clearFieldError() {
        if (!fileError) return;
        fileError.style.display = "none";
        fileError.textContent = "";
        dropzone.classList.remove("dropzone-error");
    }

    /* ── Show preview ───────────────────────────────────────── */
    function showPreview(file) {
        if (!file) return;
        const reader = new FileReader();
        reader.onload = (e) => {
            previewImg.src = e.target.result;
            dropContent.style.display = "none";
            previewState.style.display = "block";
            clearFieldError();
        };
        reader.readAsDataURL(file);
    }

    /* ── Clear / reset ──────────────────────────────────────── */
    function clearPreview(e) {
        if (e) { e.preventDefault(); e.stopPropagation(); }
        previewImg.src = "";
        fileInput.value = "";
        previewState.style.display = "none";
        dropContent.style.display = "flex";
        clearFieldError();
    }

    /* Auto-clear if results already loaded */
    const hasResult = document.getElementById("hasResult")?.value === "true";
    if (hasResult) clearPreview();

    /* ── Input / drop events ────────────────────────────────── */
    fileInput.addEventListener("change", function () {
        const file = this.files && this.files[0];
        if (file && validateFile(file)) showPreview(file);
        else if (file) fileInput.value = "";
    });

    dropzone.addEventListener("dragover", () => dropzone.classList.add("drag-over"));
    dropzone.addEventListener("dragleave", () => dropzone.classList.remove("drag-over"));

    dropzone.addEventListener("drop", (e) => {
        dropzone.classList.remove("drag-over");
        const file = e.dataTransfer.files[0];
        if (!file) return;

        if (validateFile(file)) {
            // Assign dropped file to input
            const dt = new DataTransfer();
            dt.items.add(file);
            fileInput.files = dt.files;
            showPreview(file);
        }
    });

    clearBtn?.addEventListener("click", clearPreview);

    /* ── Form submit ────────────────────────────────────────── */
    uploadForm?.addEventListener("submit", (e) => {
        const file = fileInput.files && fileInput.files[0];

        if (!file) {
            e.preventDefault();
            showFieldError("Please select an image before uploading.");         
            return;
        }

        if (!validateFile(file)) {
            e.preventDefault();
            return;
        }

        // Success feedback
        sessionStorage.setItem("scrollToResult", "1");
        showToast("Uploading image for analysis…", "success");

        // Loading state
        if (uploadBtn) {
            uploadBtn.disabled = true;
            uploadBtn.innerHTML = `
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                     stroke-linecap="round" stroke-linejoin="round" width="15" height="15"
                     style="animation:spin 1s linear infinite">
                    <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4"/>
                </svg>
                Processing…
            `;
        }

        setTimeout(() => { fileInput.value = ""; }, 0);
    });

    /* ── Scroll to results after page reload ────────────────── */
    if (sessionStorage.getItem("scrollToResult")) {
        sessionStorage.removeItem("scrollToResult");

        const doScroll = () => {
            const resultSection = document.getElementById("resultSection");
            const isPlaceholder = resultSection?.innerText.includes("Analysis results will appear here");
            const hasError = document.querySelector(".alert.alert-danger");

            if (resultSection && !isPlaceholder) {
                setTimeout(() => {
                    resultSection.scrollIntoView({ behavior: "smooth", block: "start" });

                    // Show success toast if results loaded
                    if (!hasError) {
                        showToast("✅ Analysis complete! Results are ready below.", "success");
                    }
                }, 150);
            }
        };

        if (document.readyState === "complete") doScroll();
        else window.addEventListener("load", doScroll);
    }

});

/* CSS spin keyframe injected once */
(function () {
    const style = document.createElement("style");
    style.textContent = "@keyframes spin { to { transform: rotate(360deg); } }";
    document.head.appendChild(style);
})();