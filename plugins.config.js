module.exports = [
    {
        vendorsJS: [
    "./node_modules/simplebar/dist/simplebar.min.js",
    "./node_modules/flatpickr/dist/flatpickr.min.js",
    "./node_modules/choices.js/public/assets/scripts/choices.min.js",
    "./node_modules/prismjs/prism.js",
    "./node_modules/prismjs/plugins/normalize-whitespace/prism-normalize-whitespace.min.js",
    "./node_modules/preline/preline.js"
],
        vendorCSS: [
    "./node_modules/flatpickr/dist/flatpickr.min.css",
    "./node_modules/choices.js/public/assets/styles/choices.min.css"
],
        
    },
    {
        name: "choices",
        assets: [
    "./node_modules/choices.js/public/assets/scripts/choices.min.js",
    "./node_modules/choices.js/public/assets/styles/choices.min.css"
],
        
        
    },
    {
        name: "apexcharts",
        assets: [
    "./node_modules/apexcharts/dist/apexcharts.min.js"
],
        
        
    },
    {
        name: "jsvectormap",
        assets: [
    "./node_modules/jsvectormap/dist/jsvectormap.min.js",
    "./node_modules/jsvectormap/dist/maps/world.js",
    "./node_modules/jsvectormap/dist/maps/world-merc.js",
    "./node_modules/jsvectormap/dist/jsvectormap.min.css"
],


    },
    {
        name: "dropzone",
        assets: [
    "./node_modules/dropzone/dist/dropzone-min.js",
    "./node_modules/dropzone/dist/dropzone.css"
],
    },
    {
        name: "filepond",
        assets: [
    "./node_modules/filepond/dist/filepond.min.js",
    "./node_modules/filepond/dist/filepond.min.css",
    "./node_modules/filepond-plugin-image-preview/dist/filepond-plugin-image-preview.min.js",
    "./node_modules/filepond-plugin-file-validate-size/dist/filepond-plugin-file-validate-size.min.js",
    "./node_modules/filepond-plugin-image-exif-orientation/dist/filepond-plugin-image-exif-orientation.min.js",
    "./node_modules/filepond-plugin-file-encode/dist/filepond-plugin-file-encode.min.js"
],
    },
    {
        name: "uppy",
        assets: [
    "./node_modules/uppy/dist/uppy.min.js",
    "./node_modules/uppy/dist/uppy.min.css"
],
    }
];