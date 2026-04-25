
console.log("Upload.js loaded");
function showToast(message, type = "error") {
    const container = document.getElementById("toastContainer");
    if (!container) return;

    const toast = document.createElement("div");
    toast.className = `toast ${type}`;

    toast.innerHTML = `
        <span>${message}</span>
        <button class="toast-close">&times;</button>
    `;

    container.appendChild(toast);

    // Auto remove after 3 sec
    setTimeout(() => {
        toast.style.animation = "toastOut 0.25s ease forwards";
        setTimeout(() => toast.remove(), 250);
    }, 3000);

    // Manual close
    toast.querySelector(".toast-close").addEventListener("click", () => {
        toast.remove();
    });
}/**
 * Upload.js  —  Skinalyze
 * Handles drag-and-drop, file browse, preview, remove, and smooth scroll to results.
 */
document.addEventListener("DOMContentLoaded", function () {
   
    /* ── Element refs ───────────────────────────────── */
    const dropzone = document.getElementById("dropzone");
    const fileInput = document.getElementById("fileInput");
    const dropContent = document.getElementById("dropzoneContent");
    const previewState = document.getElementById("previewState");
    const previewImg = document.getElementById("previewImg");
    const removeImg = document.getElementById("removeImg");
    const clearBtn = document.getElementById("clearBtn");
    const uploadForm = document.querySelector("form[enctype='multipart/form-data']");

    if (!dropzone) return; // not on upload page

    /* ── 1. Prevent Browser Default Drag Behaviors ──── */
    // Prevents the browser from opening the file in a new tab on accidental drops
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropzone.addEventListener(eventName, e => {
            e.preventDefault();
            e.stopPropagation();
        });
    });

    /* ── 2. Click to Browse (Fixed Double-Click) ────── */
    dropzone.addEventListener("click", (e) => {
        // Prevent the file dialog from double-triggering if the user clicked 
        // the label, the input itself, or the remove button.
        if (e.target.closest('#removeImg') || e.target.closest('label') || e.target.closest('input')) {
            return;
        }
        fileInput.click();
    });

    /* ── 3. Show Preview ────────────────────────────── */
    function showPreview(file) {
        if (!file || !file.type.startsWith("image/")) return;
        const reader = new FileReader();
        reader.onload = (e) => {
            previewImg.src = e.target.result;
            dropContent.style.display = "none";
            previewState.style.display = "block";
        };
        reader.readAsDataURL(file);
    }
    const hasResult = document.getElementById("hasResult")?.value === "true";

    if (hasResult) {
        clearPreview();
    }
    /* ── 4. Clear / Reset ───────────────────────────── */
    function clearPreview(e) {
        if (e) {
            e.preventDefault();
            e.stopPropagation();
        }
        previewImg.src = "";
        fileInput.value = "";
        previewState.style.display = "none";
        dropContent.style.display = "flex";
    }

    /* ── 5. Input & Drop Events ─────────────────────── */
    fileInput.addEventListener("change", function () {
        if (this.files && this.files[0]) showPreview(this.files[0]);
    });

    dropzone.addEventListener("dragover", () => dropzone.classList.add("drag-over"));
    dropzone.addEventListener("dragleave", () => dropzone.classList.remove("drag-over"));
    dropzone.addEventListener("drop", (e) => {
        dropzone.classList.remove("drag-over");
        const file = e.dataTransfer.files[0];
        if (file && file.type.startsWith("image/")) {
            fileInput.files = e.dataTransfer.files;
            showPreview(file);
        }
    });

    removeImg?.addEventListener("click", clearPreview);
    clearBtn?.addEventListener("click", clearPreview);

    /* ── 6. Robust Smooth Scroll to Results ─────────── */
    uploadForm?.addEventListener("submit", (e) => {
        console.log("Submit triggered");
        console.log("Files:", fileInput.files.length);

        if (!fileInput.files.length) {
            console.log("Validation failed → showing toast");
            e.preventDefault();
            showToast("Please select an image first.");
            return;
        }
   

        // Store scroll flag
        sessionStorage.setItem("scrollToResult", "1");

        // Loading state
        const btn = document.querySelector(".btn-upload-pill");
        if (btn) {
            btn.disabled = true;
            btn.innerText = "Processing...";
        }

        // IMPORTANT: reset AFTER submit trigger
        setTimeout(() => {
            fileInput.value = "";
        }, 0);
    });

    if (sessionStorage.getItem("scrollToResult")) {
        sessionStorage.removeItem("scrollToResult");

        const doScroll = () => {
            const resultSection = document.getElementById("resultSection");

            // Only scroll down if actual results loaded. 
            // If validation failed, the placeholder will still be there, and it won't scroll down.
            const isPlaceholder = resultSection?.innerText.includes("Analysis results will appear here");

            if (resultSection && !isPlaceholder) {
                // A slight delay ensures the browser layout is fully calculated first
                setTimeout(() => {
                    resultSection.scrollIntoView({ behavior: "smooth", block: "start" });
                }, 150);
            }
        };

        // Catch the load correctly depending on the document's current state
        if (document.readyState === "complete") {
            doScroll();
        } else {
            window.addEventListener("load", doScroll);
        }
    }
});