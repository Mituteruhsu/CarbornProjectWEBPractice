(() => {
    'use strict';

    document.addEventListener('DOMContentLoaded', () => {

        const ctx = document.getElementById('emissionChart').getContext('2d');
        const yearlyDataDiv = document.getElementById('yearlyData');
        const modeBtn = document.getElementById('modeToggle');

        // 解析資料
        const rawText = yearlyDataDiv.textContent.trim();
        const lines = rawText.split('\n').map(l => l.trim()).filter(l => l !== '');
        const targetEmission = parseFloat(lines[0].replace(/,/g, ''));
        const yearlyData = JSON.parse(lines.slice(1).join('\n'));

        const labels = yearlyData.map(x => x.Year);
        const actualEmissions = yearlyData.map(x => x.AverageAcrossYears);

        // 主題定義
        const lightTheme = {
            background: '#ffffff',
            text: '#1e293b',
            grid: 'rgba(0,0,0,0.1)',
            lineActual: '#16a34a',
            lineTarget: '#dc2626'
        };
        const darkTheme = {
            background: '#0f172a',
            text: '#f1f5f9',
            grid: 'rgba(255,255,255,0.15)',
            lineActual: '#22c55e',
            lineTarget: '#f87171'
        };

        let chart;

        function initChart(theme) {
            if (chart) chart.destroy();

            chart = new Chart(ctx, {
                type: 'line',
                data: {
                    labels,
                    datasets: [
                        {
                            label: '實際平均排放量 (噸)',
                            data: actualEmissions,
                            borderColor: theme.lineActual,
                            borderWidth: 3,
                            fill: false,
                            tension: 0.3,
                            pointBackgroundColor: theme.lineActual,
                            pointRadius: 5
                        },
                        {
                            label: '目標排放量 (噸)',
                            data: Array(actualEmissions.length).fill(targetEmission),
                            borderColor: theme.lineTarget,
                            borderWidth: 2,
                            borderDash: [6, 4],
                            fill: false,
                            pointRadius: 3
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { labels: { color: theme.text } },
                        title: {
                            display: true,
                            text: '年度碳排放趨勢',
                            color: theme.text,
                            font: { size: 18, weight: 'bold' }
                        }
                    },
                    scales: {
                        y: { ticks: { color: theme.text}, grid: { color: theme.grid } },
                        x: { ticks: { color: theme.text }, grid: { color: theme.grid } }
                    }
                }
            });
        }

        // 初始主題（依 body class 判斷）
        const isDark = document.body.classList.contains("dark-mode");
        initChart(isDark ? darkTheme : lightTheme);

        // 監聽 Dark/Light 切換按鈕
        modeBtn.addEventListener("click", () => {
            const nowDark = document.body.classList.contains("dark-mode");
            initChart(nowDark ? darkTheme : lightTheme);
        });

    });
})();


// Navbar + Sidebar Margin Shrink Sync
(function () {
    const navbar = document.querySelector(".navbar.spring-nav");
    const sidebar = document.querySelector(".spring-sidebar");

    const NAVBAR_MARGIN = 65;    // 初始 margin-top
    const NAVBAR_HEIGHT = 56;    // --navbar-height 設定值

    // ⭐ 函數：依 scroll 位置設定 navbar & sidebar
    function updateNavbarPosition() {
        if (window.scrollY > 0) {
            // ⬇️ 不是頁面頂端 → navbar 收起
            navbar.style.marginTop = "0px";
            sidebar.style.top = `${NAVBAR_HEIGHT}px`;
        } else {
            // ⬆️ 頁面頂端 → 還原 navbar margin
            navbar.style.marginTop = `${NAVBAR_MARGIN}px`;
            sidebar.style.top = `${NAVBAR_HEIGHT + NAVBAR_MARGIN}px`;
        }
    }

    // ⭐ 初始化偵測（使用者直接刷新停在中間時）
    updateNavbarPosition();

    // ⭐ 監聽滾動事件
    window.addEventListener("scroll", updateNavbarPosition);
})();