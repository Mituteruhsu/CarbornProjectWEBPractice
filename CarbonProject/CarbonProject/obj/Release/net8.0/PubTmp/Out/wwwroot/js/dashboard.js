(() => {
    'use strict';

    document.addEventListener('DOMContentLoaded', () => {
        const toggleBtn = document.getElementById('bd-theme');
        const ctx = document.getElementById('emissionChart').getContext('2d');
        const yearlyDataDiv = document.getElementById('yearlyData');

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
                    plugins: {
                        legend: { labels: { color: theme.text } },
                        title: { display: true, text: '年度碳排放趨勢', color: theme.text, font: { size: 18, weight: 'bold' } }
                    },
                    scales: {
                        y: { ticks: { color: theme.text }, grid: { color: theme.grid } },
                        x: { ticks: { color: theme.text }, grid: { color: theme.grid } }
                    }
                }
            });

            // 更新所有 card-title 顏色
            document.querySelectorAll('.card-title').forEach(title => {
                title.style.color = theme.text;
            });
        }

        // 初始圖表
        initChart(lightTheme);

        // 監聽 aria-label 屬性變化
        const observer = new MutationObserver(() => {
            const label = toggleBtn.getAttribute('aria-label') || '';
            let theme;
            if (label.includes('dark')) theme = darkTheme;
            else if (label.includes('light')) theme = lightTheme;
            else return; // auto 或其他，不改變
            initChart(theme);
        });

        observer.observe(toggleBtn, { attributes: true, attributeFilter: ['aria-label'] });
    });
})();