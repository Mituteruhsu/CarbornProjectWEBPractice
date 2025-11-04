// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
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
                    chartInstance = new Chart(ctx, {
                        type: 'line', // 改成線圖
                        data: {
                            labels: data.labels,
                            datasets: [{
                                label: `最近 ${days} 日登入次數`,
                                data: data.counts,
                                fill: false,
                                borderColor: 'rgba(54, 162, 235, 1)',
                                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                                tension: 0, // 將 tension 設為 0，取消平滑曲線
                                pointRadius: 3
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false, // 讓 canvas 適應 card
                            plugins: {
                                legend: { display: true },
                                tooltip: { mode: 'index', intersect: false }
                            },
                            interaction: { mode: 'index', intersect: false },
                            scales: {
                                y: { beginAtZero: true, ticks: { precision: 0 } }
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