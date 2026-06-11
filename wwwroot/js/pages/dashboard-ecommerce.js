/**
 * Template Name: Paces - Admin & Dashboard Template
 * By (Author): Coderthemes
 * Module/App (File Name): Ecommerce Dashboard
 */

// Clock
let clockInterval = setInterval(() => {
    document.getElementById("clock-widget").innerText = new Date().toLocaleTimeString()
}, 1000)

window.addEventListener("beforeunload", () => {
    clearInterval(clockInterval)
})

//
// Store Performance Analytics
//
new CustomApexChart({
    selector: "#total-sales-chart",
    options: () => ({
        chart: {
            height: 210,
            type: "donut",
        },
        legend: {
            show: false,
        },
        stroke: {
            width: 0,
        },

        plotOptions: {
            pie: {
                donut: {
                    size: "75%",
                    labels: {
                        show: true,
                        total: {
                            showAlways: true,
                            show: true,
                            formatter: function (w) {
                                return (
                                    w.globals.seriesTotals.reduce((a, b) => {
                                        return a + b
                                    }, 0) + "k"
                                )
                            },
                        },
                    },
                },
            },
        },
        series: [44, 55, 41],
        labels: ["Direct", "Affiliate", "Sponsored"],
        colors: [theme("chart-primary"), theme("chart-gamma"), theme("chart-gray")],
        dataLabels: {
            enabled: false,
        },
        responsive: [
            {
                breakpoint: 480,
                options: {
                    chart: {
                        width: 180,
                    },
                },
            },
        ],
    }),
})

//
// Weekly Performance Insights
//
new CustomApexChart({
    selector: "#weekly-performance-chart",
    options: () => ({
        series: [
            {
                data: [
                    {
                        x: "Mon",
                        y: [28, 45],
                    },
                    {
                        x: "Tue",
                        y: [32, 41],
                    },
                    {
                        x: "Wed",
                        y: [29, 78],
                    },
                    {
                        x: "Thu",
                        y: [30, 46],
                    },
                    {
                        x: "Fri",
                        y: [35, 41],
                    },
                    {
                        x: "Sat",
                        y: [45, 65],
                    },
                    {
                        x: "Sun",
                        y: [41, 56],
                    },
                ],
            },
        ],
        chart: {
            height: 247,
            type: "rangeBar",
            toolbar: {
                show: false,
            },
            zoom: {
                enabled: false,
            },
        },
        plotOptions: {
            bar: {
                horizontal: true,
                isDumbbell: true,
                dumbbellColors: [[theme("chart-primary"), theme("chart-primary")]],
            },
        },
        legend: {
            show: false,
        },
        fill: {
            type: "gradient",
            gradient: {
                gradientToColors: [theme("chart-primary"), theme("chart-gamma"), theme("chart-gray")],
                inverseColors: false,
                stops: [0, 100],
            },
        },
        xaxis: {
            type: "string",
            axisBorder: {
                show: false,
            },
        },
        yaxis: {
            axisBorder: {
                show: false,
            },
            labels: {
                offsetX: 10,
            },
        },
        grid: {
            borderColor: [theme("chart-order-color")],
            padding: {
                top: -20,
                right: 0,
                bottom: -10,
                left: 0,
            },
        },
    }),
})

//
// Sales Report Chart
//
new CustomApexChart({
    selector: "#sales-report-chart",
    options: () => ({
        series: [
            {
                name: "Total Revenue",
                type: "area",
                data: [21, 21, 21, 35, 35, 35, 44, 44, 44, 54, 54, 54, 48, 48, 76, 76, 95, 95, 76, 76, 32, 32, 46, 48, 48],
            },
            {
                name: "Orders",
                type: "line",
                data: [40, 40, 40, 50, 50, 35, 27, 27, 27, 15, 15, 27, 27, 36, 36, 33, 33, 34, 35, 33, 50, 50, 55, 55, 55],
            },
        ],
        chart: {
            type: "line",
            height: 359,
            toolbar: {
                show: false,
            },
            offsetX: 0,
        },
        stroke: {
            width: [3, 2],
            curve: "smooth",
            dashArray: [0, 8],
        },
        colors: [theme("chart-secondary"), theme("chart-alpha")],
        grid: {
            strokeDashArray: 7,
        },
        zoom: {
            enabled: false,
        },
        xaxis: {
            type: "string",
            axisBorder: {
                show: false,
            },
            labels: {
                offsetY: 2,
            },
        },
        yaxis: {
            tickAmount: 4,
            min: 0,
            max: 100,
            labels: {
                show: true,
                formatter: function (value) {
                    return value + "k"
                },
                offsetX: -10,
            },
            axisBorder: {
                show: false,
            },
        },
        dataLabels: {
            enabled: false,
        },
        markers: {
            size: 0,
            style: "hollow",
        },
        tooltip: {
            x: {
                format: "dd MMM yyyy",
            },
            y: {
                formatter: function (val) {
                    return "$" + val + "k"
                },
            },
        },
        fill: {
            opacity: [1, 0.5],
            type: ["gradient", "solid"],
            gradient: {
                type: "vertical",
                //   shadeIntensity: 1,
                inverseColors: false,
                opacityFrom: 0.5,
                opacityTo: 0,
                stops: [0, 70],
            },
        },
        legend: {
            offsetY: 15,
        },
    }),
})

class VectorMap {
    currentResizeHandler = null

    init() {
        this.initWorldMarkerLine()
    }

    initVectorMap(selector, options = {}) {
        let element = null

        if (selector instanceof Element) {
            element = selector
        } else {
            element = document.querySelector(selector)
        }

        if (element) {
            const svgElement = element.querySelector("svg")

            if (svgElement) {
                const mapInstance = element.jsVectorMap

                if (mapInstance && typeof mapInstance.destroy === "function") {
                    mapInstance.destroy()
                } else {
                    // Fallback for cases where the instance is missing but the SVG remains.
                    element.innerHTML = ""
                }
            }

            if (this.currentResizeHandler) {
                window.removeEventListener("resize", this.currentResizeHandler)
                this.currentResizeHandler = null
            }

            const map = new jsVectorMap({
                selector: element,
                ...options,
            })

            const debouncedUpdate = debounce(() => {
                map.updateSize()
            }, 200)

            this.currentResizeHandler = debouncedUpdate

            window.addEventListener("resize", this.currentResizeHandler)
        }
    }

    initWorldMarkerLine() {
        this.initVectorMap("#revenue-by-location", {
            map: "world_merc",
            zoomOnScroll: false,
            zoomButtons: false,
            markers: [
                { name: "Australia", coords: [-25.2744, 133.7751] },
                { name: "India", coords: [20.5937, 78.9629] },
                { name: "Japan", coords: [36.2048, 138.2529] },
                { name: "South Africa", coords: [-30.5595, 22.9375] },
                { name: "Germany", coords: [51.1657, 10.4515] },
                { name: "United Kingdom", coords: [55.3781, -3.436] },
                { name: "Mexico", coords: [23.6345, -102.5528] },
                { name: "Argentina", coords: [-38.4161, -63.6167] },
                { name: "Saudi Arabia", coords: [23.8859, 45.0792] },
                { name: "Indonesia", coords: [-0.7893, 113.9213] },
            ],
            lines: [
                { from: "India", to: "Australia" },
                { from: "Japan", to: "Germany" },
                { from: "Mexico", to: "United Kingdom" },
                { from: "Argentina", to: "South Africa" },
                { from: "Saudi Arabia", to: "India" },
                { from: "Indonesia", to: "Japan" },
                { from: "United Kingdom", to: "Germany" },
                { from: "Australia", to: "Indonesia" },
            ],
            regionStyle: {
                initial: {
                    stroke: "#aab9d14d",
                    strokeWidth: 0.25,
                    fill: "#aab9d14d",
                    fillOpacity: 1,
                },
            },
            markerStyle: {
                initial: { fill: theme("secondary") },
                selected: { fill: theme("secondary") },
            },
            lineStyle: {
                animation: true,
                strokeDasharray: "1 2 3 4 5 6",
            },
        })
    }
}

document.addEventListener("DOMContentLoaded", function (e) {
    new VectorMap().init()
})

// Observe theme changes
const mapObserver = new MutationObserver(() => {
    new VectorMap().init()
})

mapObserver.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ["data-skin", "data-bs-theme"],
})