/**
 * Upload.js  —  Skinalyze
 * Handles drag-and-drop, file browse, preview, and remove.
 */

document.addEventListener("DOMContentLoaded", function () {
    const dropzone = document.getElementById("dropzone");
    const fileInput = document.getElementById("fileInput");
    const preview = document.getElementById("uploadPreview");
    const previewImg = document.getElementById("previewImg");
    const removeImg = document.getElementById("removeImg");

    if (!dropzone) return;   // not on upload page



    /* ── Show preview ───────────────────────────────── */
    function showPreview(file) {
        if (!file || !file.type.startsWith("image/")) return;
        const reader = new FileReader();
        reader.onload = (e) => {
            previewImg.src = e.target.result;
            dropzone.style.display = "none";
            preview.style.display = "flex";
        };
        reader.readAsDataURL(file);
    }

    /* ── File input change ──────────────────────────── */
    fileInput.addEventListener("change", function () {
        if (this.files && this.files[0]) showPreview(this.files[0]);
    });

    /* ── Drag & Drop ────────────────────────────────── */
    dropzone.addEventListener("dragover", (e) => {
        e.preventDefault();
        dropzone.classList.add("dragover");
    });

    dropzone.addEventListener("dragleave", () => {
        dropzone.classList.remove("dragover");
    });

    dropzone.addEventListener("drop", (e) => {
        e.preventDefault();
        dropzone.classList.remove("dragover");
        const file = e.dataTransfer.files[0];
        if (file) showPreview(file);
    });

    /* ── Remove image ───────────────────────────────── */
    removeImg.addEventListener("click", function () {
        previewImg.src = "";
        fileInput.value = "";
        preview.style.display = "none";
        dropzone.style.display = "flex";
    });
});
