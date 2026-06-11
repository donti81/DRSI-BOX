/**
 * Template Name: Paces - Admin & Dashboard Template
 * By (Author): Coderthemes
 * Module/App (File Name): Form Fileupload
 */

FilePond.registerPlugin(FilePondPluginImagePreview, FilePondPluginImageExifOrientation, FilePondPluginFileValidateSize, FilePondPluginFileEncode)

function getCsrfToken() {
    return document.querySelector('meta[name="csrf-token"]')?.getAttribute("content") ?? ""
}

class FileUpload {
    constructor() {
        this.init()
    }

    init() {
        if (typeof Dropzone === "undefined") {
            console.warn("Dropzone is not loaded.")
            return
        }

        Dropzone.autoDiscover = false

        const dropzones = document.querySelectorAll('[data-plugin="dropzone"]')
        if (dropzones) {
            dropzones.forEach((dropzoneEl) => {
                const actionUrl = dropzoneEl.getAttribute("action") || "/"
                const previewContainer = dropzoneEl.dataset.previewsContainer
                const uploadPreviewTemplate = dropzoneEl.dataset.uploadPreviewTemplate

                const options = {
                    url: actionUrl,
                    acceptedFiles: "image/*",
                    headers: {
                        RequestVerificationToken: getCsrfToken(),
                    },
                }

                if (previewContainer) {
                    options.previewsContainer = previewContainer
                }

                if (uploadPreviewTemplate) {
                    const template = document.querySelector(uploadPreviewTemplate)
                    if (template) {
                        options.previewTemplate = template.innerHTML
                    }
                }

                options.sending = (file, xhr, formData) => {
                    formData.append("folder", document.getElementById("folder-select")?.value ?? "")
                }

                try {
                    new Dropzone(dropzoneEl, options)
                } catch (e) {
                    console.error("Dropzone initialization failed:", e)
                }
            })
        }
    }
}

document.addEventListener("DOMContentLoaded", () => {
    const folderSelectEl = document.getElementById("folder-select")
    if (folderSelectEl && folderSelectEl.tagName === "SELECT" && typeof Choices !== "undefined") {
        new Choices(folderSelectEl, { searchEnabled: true, itemSelectText: "", placeholder: true })
    }

    function getSelectedFolder() {
        return folderSelectEl?.value ?? ""
    }

    new FileUpload()

    if (typeof FilePond !== "undefined") {
        try {
            FilePond.registerPlugin(FilePondPluginImagePreview)
        } catch (e) {
            console.warn("FilePond plugins registration failed:", e)
        }

        const filepondServer = {
            process: {
                url: "/form/fileuploads?handler=Upload",
                method: "POST",
                headers: {
                    RequestVerificationToken: getCsrfToken(),
                },
                ondata: (fd) => {
                    fd.append("folder", document.getElementById("folder-select")?.value ?? "")
                    return fd
                },
            },
        }

        const multiInputs = document.querySelectorAll("input.filepond-input-multiple")
        multiInputs.forEach((input) => {
            FilePond.create(input, { server: filepondServer })
        })

        const circleInputs = document.querySelectorAll("input.filepond-input-circle")
        circleInputs.forEach((input) => {
            FilePond.create(input, {
                server: filepondServer,
                imageCropAspectRatio: "1:1",
                imageResizeTargetWidth: 200,
                imageResizeTargetHeight: 200,
                stylePanelLayout: "compact circle",
                styleLoadIndicatorPosition: "center bottom",
                styleProgressIndicatorPosition: "right bottom",
                styleButtonRemoveItemPosition: "left bottom",
                styleButtonProcessItemPosition: "right bottom",
                allowImagePreview: true,
                imagePreviewHeight: 100,
                labelIdle: `<svg  xmlns="http://www.w3.org/2000/svg"  width="32"  height="32"  viewBox="0 0 24 24"  fill="none"  stroke="currentColor"  stroke-width="2"  stroke-linecap="round"  stroke-linejoin="round" class="text-default-400"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M5 7h1a2 2 0 0 0 2 -2a1 1 0 0 1 1 -1h6a1 1 0 0 1 1 1a2 2 0 0 0 2 2h1a2 2 0 0 1 2 2v9a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-9a2 2 0 0 1 2 -2" /><path d="M9 13a3 3 0 1 0 6 0a3 3 0 0 0 -6 0" /></svg>`,
            })
        })
    } else {
        console.warn("FilePond is not loaded.")
    }

    // ── Uppy / TUS resumable upload ──────────────────────────────────────────
    if (typeof Uppy !== "undefined") {
        const { Uppy: UppyCore, Dashboard, Tus } = Uppy

        const folderRequiredMsg = document.getElementById("folder-required-msg")

        const uppy = new UppyCore({
            autoProceed: false,
            debug: false,
            onBeforeUpload: (files) => {
                const folder = getSelectedFolder()
                if (!folder) {
                    folderRequiredMsg?.classList.remove("hidden")
                    folderRequiredMsg?.scrollIntoView({ behavior: "smooth", block: "center" })
                    return false
                }
                folderRequiredMsg?.classList.add("hidden")
                uppy.setMeta({ folder })
                return files
            },
        })

        folderSelectEl?.addEventListener("change", () => {
            uppy.setMeta({ folder: folderSelectEl.value })
            if (folderSelectEl.value) folderRequiredMsg?.classList.add("hidden")
        })

        uppy.use(Dashboard, {
            inline: true,
            target: "#uppy-dashboard",
            proudlyDisplayPoweredByUppy: false,
            height: 300,
            showProgressDetails: true,
            singleFileMode: false,
            disableThumbnailGenerator: true,
            thumbnailWidth: 50,
        })

        uppy.use(Tus, {
            endpoint: "/tus",
            chunkSize: 10 * 1024 * 1024,
            retryDelays: [0, 1000, 3000, 5000],
            removeFingerprintOnSuccess: true,
        })

        // ── Session file list ────────────────────────────────────────────────
        const tusList = document.getElementById("tus-file-list")
        const tusItems = document.getElementById("tus-file-items")

        function formatBytes(bytes) {
            if (bytes < 1024) return bytes + " B"
            if (bytes < 1048576) return (bytes / 1024).toFixed(1) + " KB"
            return (bytes / 1048576).toFixed(1) + " MB"
        }

        function createFileRow(file) {
            const row = document.createElement("div")
            row.id = `tus-file-${file.id}`
            row.className = "flex items-center gap-3 p-3 border border-default-200 rounded-lg"
            row.innerHTML = `
                <i class="iconify tabler--file text-xl text-default-400 shrink-0"></i>
                <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium truncate">${file.name}</p>
                    <p class="text-xs text-default-400">${formatBytes(file.size)}</p>
                    <div class="mt-1.5 h-1.5 bg-default-100 rounded-full hidden" id="tus-bar-${file.id}">
                        <div class="h-full bg-primary rounded-full transition-all duration-200" style="width:0%" id="tus-fill-${file.id}"></div>
                    </div>
                </div>
                <span class="badge badge-soft-warning text-xs shrink-0" id="tus-status-${file.id}">V vrsti</span>
            `
            return row
        }

        // tracks successfully uploaded files for the notification form
        const uploadedFiles = new Map() // id -> { name, size }

        function updateNotificationCard() {
            const notifCard = document.getElementById("tus-notification-card")
            const files = [...uploadedFiles.values()]
            if (files.length === 0) { notifCard.classList.add("hidden"); return }

            notifCard.classList.remove("hidden")
            document.getElementById("notif-file-count").textContent = files.length
            document.getElementById("notif-total-size").textContent = formatBytes(files.reduce((s, f) => s + f.size, 0))
            document.getElementById("notif-folder").textContent = getSelectedFolder() || "—"

            const detail = document.getElementById("notif-file-detail")
            detail.innerHTML = files.map(f =>
                `<div class="flex items-center gap-2">
                    <i class="iconify tabler--file text-default-400 shrink-0"></i>
                    <span class="truncate flex-1">${f.name}</span>
                    <span class="text-default-400 shrink-0">${formatBytes(f.size)}</span>
                </div>`
            ).join("")
        }

        uppy.on("file-added", (file) => {
            tusList.classList.remove("hidden")
            tusItems.appendChild(createFileRow(file))
        })

        uppy.on("upload-progress", (file, progress) => {
            const pct = Math.round((progress.bytesUploaded / progress.bytesTotal) * 100)
            const bar = document.getElementById(`tus-bar-${file.id}`)
            const fill = document.getElementById(`tus-fill-${file.id}`)
            const status = document.getElementById(`tus-status-${file.id}`)
            if (bar) bar.classList.remove("hidden")
            if (fill) fill.style.width = pct + "%"
            if (status) { status.className = "badge badge-soft-info text-xs shrink-0"; status.textContent = pct + "%" }
        })

        uppy.on("upload-success", (file) => {
            const bar = document.getElementById(`tus-bar-${file.id}`)
            const fill = document.getElementById(`tus-fill-${file.id}`)
            const status = document.getElementById(`tus-status-${file.id}`)
            if (bar) bar.classList.add("hidden")
            if (fill) fill.style.width = "100%"
            if (status) { status.className = "badge badge-soft-success text-xs shrink-0"; status.textContent = "Naloženo" }
            uploadedFiles.set(file.id, { name: file.name, size: file.size })
            updateNotificationCard()
        })

        uppy.on("upload-error", (file) => {
            const bar = document.getElementById(`tus-bar-${file.id}`)
            const status = document.getElementById(`tus-status-${file.id}`)
            if (bar) bar.classList.add("hidden")
            if (status) { status.className = "badge badge-soft-danger text-xs shrink-0"; status.textContent = "Napaka" }
        })

        uppy.on("file-removed", (file) => {
            document.getElementById(`tus-file-${file.id}`)?.remove()
            if (!tusItems.children.length) tusList.classList.add("hidden")
            uploadedFiles.delete(file.id)
            updateNotificationCard()
        })

        // ── Notification send ────────────────────────────────────────────────
        document.getElementById("notif-send-btn")?.addEventListener("click", async () => {
            const btn = document.getElementById("notif-send-btn")
            const alert = document.getElementById("notif-alert")
            const files = [...uploadedFiles.values()]
            if (files.length === 0) return

            btn.disabled = true
            alert.className = "hidden"

            const recipientsEl = document.getElementById("notif-recipients")
            if (recipientsEl) {
                if (!recipientsEl.value.trim()) {
                    alert.className = "p-3 rounded-lg bg-danger/10 text-danger text-sm mb-4"
                    alert.textContent = "Vnesite vsaj en e-poštni naslov prejemnika."
                    btn.disabled = false
                    return
                }
                const emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
                const invalid = recipientsEl.value
                    .split(/[,\n\r]+/)
                    .map(e => e.trim())
                    .filter(e => e.length > 0 && !emailRe.test(e))
                if (invalid.length > 0) {
                    alert.className = "p-3 rounded-lg bg-danger/10 text-danger text-sm mb-4"
                    alert.textContent = "Neveljavni e-poštni naslovi: " + invalid.join(", ")
                    btn.disabled = false
                    return
                }
            }

            const fd = new FormData()
            fd.append("message", document.getElementById("notif-message").value)
            fd.append("folder", getSelectedFolder())
            fd.append("fileCount", files.length)
            fd.append("totalSize", files.reduce((s, f) => s + f.size, 0))
            fd.append("filesDetail", JSON.stringify(files))
            fd.append("recipients", recipientsEl?.value ?? "")

            try {
                const res = await fetch("/form/fileuploads?handler=SendNotification", {
                    method: "POST",
                    headers: { RequestVerificationToken: getCsrfToken() },
                    body: fd,
                })
                const json = await res.json()
                if (json.success) {
                    alert.className = "p-3 rounded-lg bg-success/10 text-success text-sm mb-4"
                    alert.textContent = "Obvestilo je bilo uspešno poslano."
                    document.getElementById("notif-message").value = ""
                } else {
                    alert.className = "p-3 rounded-lg bg-danger/10 text-danger text-sm mb-4"
                    alert.textContent = json.error ?? "Napaka pri pošiljanju."
                }
            } catch {
                alert.className = "p-3 rounded-lg bg-danger/10 text-danger text-sm mb-4"
                alert.textContent = "Napaka pri pošiljanju obvestila."
            } finally {
                btn.disabled = false
            }
        })
    } else {
        console.warn("Uppy is not loaded.")
    }
})
