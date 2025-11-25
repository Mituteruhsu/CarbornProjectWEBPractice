// Sidebar Toggle (Top Drawer)
const sidebar = document.getElementById("sidebar");
const toggleBtn = document.getElementById("sidebarToggleBtn");

toggleBtn.addEventListener("click", () => {
    sidebar.classList.toggle("open");
});

// 手勢滑動偵測（手機版 Top Drawer）
let startY = 0;

document.addEventListener("touchstart", (e) => {
    startY = e.touches[0].clientY;
});

document.addEventListener("touchend", (e) => {
    let endY = e.changedTouches[0].clientY;
    let diff = endY - startY;

    // 往下滑 > 80px → 打開 sidebar
    if (diff > 80 && !sidebar.classList.contains("open")) {
        sidebar.classList.add("open");
    }

    // 往上滑 < -80px → 關閉 sidebar
    if (diff < -80 && sidebar.classList.contains("open")) {
        sidebar.classList.remove("open");
    }
});

// Dark / Light Mode
const modeToggle = document.getElementById("modeToggle");

modeToggle.addEventListener("click", () => {
    document.body.classList.toggle("dark-mode");
    document.body.classList.toggle("light-mode");

    localStorage.setItem("admin-mode",
        document.body.classList.contains("dark-mode") ? "dark" : "light");
    // 重新載入頁面
    location.reload();
});

// 初始化 Mode
const mode = localStorage.getItem("admin-mode");
if (mode === "dark") {
    document.body.classList.remove("light-mode");
    document.body.classList.add("dark-mode");
}

// Chart.js 初始化時用 getComputedStyle() 讀 CSS 變數
function getCssVariable(name) {
    return getComputedStyle(document.body).getPropertyValue(name).trim();
}

//< !--Chart.js -->
document.addEventListener("DOMContentLoaded", function () {
    // 等 Chart.js 被載入後再執行
    if (typeof Chart === 'undefined') {
        console.warn('Chart.js 尚未載入，延遲執行中...');
        const checkChart = setInterval(() => {
            if (typeof Chart !== 'undefined') {
                clearInterval(checkChart);
                initLoginTrendChart();
            }
        }, 200);
    } else {
        initLoginTrendChart();
    }
    function initLoginTrendChart() {
        const chartElement = document.getElementById('loginTrendChart');
        if (!chartElement) return;

        let chartInstance = null;  // 保存圖表實例
        let currentDays = parseInt(chartElement.dataset.url.match(/days=(\d+)/)?.[1]) || 7;

        const fetchAndRender = (days) => {
            const url = `/Home/GetLoginTrend?days=${days}`;
            fetch(url)
                .then(res => res.json())
                .then(data => {
                    if (chartInstance) chartInstance.destroy(); // 先銷毀舊圖表

                    const ctx = chartElement.getContext('2d');

                    // 取得顏色
                    const chartText = getCssVariable('--chart-text');
                    const chartGrid = getCssVariable('--chart-grid');
                    const chartLine = getCssVariable('--chart-line');
                    const chartLineBg = getCssVariable('--chart-line-bg');

                    chartInstance = new Chart(ctx, {
                        type: 'line', // 改成線圖
                        data: {
                            labels: data.labels,
                            datasets: [{
                                label: `最近 ${days} 日登入次數`,
                                data: data.counts,
                                fill: false,
                                borderColor: chartLine,
                                backgroundColor: chartLineBg,
                                tension: 0, // 將 tension 設為 0，取消平滑曲線
                                pointRadius: 3
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false, // 讓 canvas 適應 card
                            plugins: {
                                legend: {
                                    display: true,
                                    labels: { color: chartText }
                                },
                                tooltip: { mode: 'index', intersect: false }
                            },
                            interaction: { mode: 'index', intersect: false },
                            scales: {
                                x: {
                                    ticks: { color: chartText },
                                    grid: {color:chartGrid}
                                },
                                y: {
                                    beginAtZero: true,
                                    ticks: {
                                        precision: 0,
                                        color: chartText
                                    },
                                    grid: { color: chartGrid }
                                }
                            }
                        }
                    });
                })
                .catch(err => console.error('Chart.js 取得資料錯誤：', err));
        };

        // 初始渲染
        fetchAndRender(currentDays);

        // 點擊按鈕更新天數
        document.querySelectorAll('.trend-range-btn').forEach(btn => {
            btn.addEventListener('click', function () {
                const days = parseInt(this.getAttribute('data-days'), 10) || 7;
                if (days === currentDays) return;

                currentDays = days;

                // 切換按鈕 active 樣式
                document.querySelectorAll('.trend-range-btn').forEach(b => b.classList.remove('active'));
                this.classList.add('active');

                // 重新抓取資料並渲染
                fetchAndRender(currentDays);
            });
        });
    }
});
