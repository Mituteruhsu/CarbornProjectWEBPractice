// 🌿 CustosCarbon - Windows Pack 版（使用本地 JSON 為主，外部 API 備援）

let emissionFactors = {};
let records = [];
let chart;

// === 初始化 ===
async function initCalculator() {
    console.log("🌿 CustosCarbon Calculator Loaded");
    await loadEmissionData();

    const searchInput = document.getElementById("search");
    if (searchInput) {
        searchInput.addEventListener("input", (e) => showSuggestions(e.target.value));
    }

    // 初始縮小圖表
    updateChartDisplayMode();
}

// === 載入排放係數 from DB ===
async function loadEmissionData() {
    const DB_URL = "/CarbonCalculator/GetEmissionFactors";

    try {
        console.log("📡 從後端載入 DB CarbonFactor...");

        const res = await fetch(DB_URL, { cache: "no-store" });
        if (!res.ok) throw new Error("後端回傳錯誤：" + res.status);

        const dbData = await res.json();
        console.log(`✅ 從 DB 成功載入 ${dbData.length} 筆 CarbonFactor`);

        emissionFactors = formatEmissionData(dbData);
        console.log("🌍 emissionFactors：", emissionFactors);
    }
    catch (err) {
        console.error("❌ 資料庫載入失敗", err);
        alert("後端發生錯誤，無法載入排放係數！");
    }
}

// === 格式化資料 & 去重 ===
function formatEmissionData(rawData) {
    const map = {};
    rawData.forEach((item) => {
        const name = item.Name || item.name || item["項目名稱"];
        const unit = item.Unit || item.unit || item["單位"] || "未知";
        const factor = parseFloat(item.CO2e || item.factor || item["排放係數"] || 0);
        if (name && !map[name]) {
            map[name] = { unit, factor };
        }
    });
    return map;
}

// === 搜尋提示 ===
function showSuggestions(keyword) {
    const box = document.getElementById("suggestions");
    if (!box) return;

    if (!keyword.trim()) {
        box.style.display = "none";
        return;
    }

    const suggestions = Object.keys(emissionFactors)
        .filter((k) => k.includes(keyword))
        .slice(0, 10);

    if (suggestions.length === 0) {
        box.style.display = "none";
        return;
    }

    box.innerHTML = suggestions
        .map((k) => {
            const { unit, factor } = emissionFactors[k];
            return `<div class="suggestion-item" onclick="selectSuggestion('${k}')">${k}（${factor} kgCO₂e／${unit}）</div>`;
        })
        .join("");
    box.style.display = "block";
}

// === 選擇提示項目 ===
function selectSuggestion(name) {
    const input = document.getElementById("search");
    const usageInput = document.getElementById("usage");
    const factor = emissionFactors[name];

    input.value = name;
    document.getElementById("suggestions").style.display = "none";

    if (factor) {
        usageInput.placeholder = `輸入使用量（單位：${factor.unit}）`;
        document.getElementById("unitHint").textContent = `單位：${factor.unit}`;
    } else {
        usageInput.placeholder = "輸入使用量";
        document.getElementById("unitHint").textContent = "";
    }
}

// === 一鍵加入類別推薦項目 ===
function quickAdd(category) {
    const mapping = {
        transport: ["汽車", "機車", "公車", "捷運"],
        food: ["牛肉", "雞肉", "豬肉", "蔬菜"],
        energy: ["電力", "天然氣", "柴油", "煤"],
        green: ["步行", "腳踏車"]
    };

    const examples = mapping[category] || [];
    let added = 0;

    examples.forEach((keyword) => {
        const matchedKeys = Object.keys(emissionFactors).filter((k) => k.includes(keyword));
        if (matchedKeys.length > 0) {
            matchedKeys.forEach((matchKey) => {
                const factorData = emissionFactors[matchKey];
                const record = {
                    name: matchKey,
                    usage: "",
                    factor: factorData.factor,
                    unit: factorData.unit,
                    emission: "",
                };
                records.push(record);
                saveToDB(record);
                added++;
            });
        }
    });

    renderTable();
    updateChart();
}

// === 使用者輸入使用量 ===
function updateUsage(index, value) {
    const usage = parseFloat(value);
    if (isNaN(usage) || usage < 0) {
        records[index].emission = "";
    } else {
        const factor = parseFloat(records[index].factor);
        records[index].emission = (usage * factor).toFixed(2);
        records[index].usage = usage;
    }
    renderTable(false);
    updateChart();
}

// === 加入一筆 ===
function addRecord() {
    const name = document.getElementById("search").value.trim();
    const usage = parseFloat(document.getElementById("usage").value);
    const unit = document.getElementById("unit").textContent?.replace("單位：", "") || "";

    if (!name) {
        alert("請輸入項目名稱或使用搜尋提示選擇！");
        return;
    }

    const factor = emissionFactors[name]?.factor || 0;
    const theUnit = emissionFactors[name]?.unit || unit || "";
    const emission = !isNaN(usage) && usage > 0 ? (usage * factor).toFixed(2) : "";

    const rec = { name, usage: isNaN(usage) ? "" : usage, unit: theUnit, factor, emission };
    records.push(rec);

    renderTable();
    updateChart();
    saveToDB(rec);

    document.getElementById("search").value = "";
    document.getElementById("usage").value = "";
    document.getElementById("unitHint").textContent = "";
}

// === 渲染表格 ===
function renderTable(showAlert = true) {
    const tbody = document.querySelector("#recordTable tbody");
    tbody.innerHTML = records
        .map(
            (r, i) => `
        <tr>
            <td>${r.name}</td>
            <td>
                <input type="number" style="max-width: 12rem;" value="${r.usage}" oninput="updateUsage(${i}, this.value)" 
                    placeholder="${r.unit ? '單位：' + r.unit : '輸入使用量'}" />
            </td>
            <td>${r.factor}</td>
            <td>${r.emission}</td>
            <td><button class="btn-outline text-nowrap py-0" onclick="removeRecord(${i})">刪除</button></td>
        </tr>
    `
        )
        .join("");

    const total = records.reduce(
        (sum, r) => sum + (parseFloat(r.emission) || 0),
        0
    );
    document.getElementById("totalEmission").textContent = total.toFixed(2);

    if (showAlert && records.length) {
        console.log(`✅ 已更新 ${records.length} 筆紀錄`);
    }
}

// === 刪除單筆 ===
function removeRecord(index) {
    records.splice(index, 1);
    renderTable(false);
    updateChart();
}

// === 清空全部 ===
async function clearAll() {
    try {
        const res = await fetch("/api/Carbon/ClearAll", { method: "DELETE" });
        if (!res.ok) throw new Error("HTTP " + res.status);
        records = [];
        renderTable(false);
        updateChart();
        console.log("🧹 [DB] 已清空所有 CarbonRecords");
    } catch (err) {
        console.error("❌ [DB] 清空失敗：", err);
    }
}

// === 寫入 DB ===
async function saveToDB(record) {
    try {
        await fetch("/api/CarbonCalculation/Save", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                name: record.name,
                inputValue: record.usage || 0,
                factor: record.factor || 0,
                resultValue: record.emission || 0
            })
        });
    } catch (err) {
        console.error("❌ 儲存到資料庫失敗:", err);
    }
}

// === PDF 匯出 ===
function downloadPDF() {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    doc.text("CustosCarbon 碳足跡紀錄", 14, 15);

    const tableData = records.map((r) => [
        r.name,
        `${r.usage || ""} ${r.unit || ""}`,
        r.factor,
        r.emission,
    ]);
    doc.autoTable({
        head: [["項目名稱", "使用量", "排放係數", "總排放量"]],
        body: tableData,
        startY: 25,
    });

    const total = records.reduce(
        (sum, r) => sum + (parseFloat(r.emission) || 0),
        0
    );
    doc.text(`🌍 總碳排量：${total.toFixed(2)} kgCO₂e`, 14, doc.lastAutoTable.finalY + 10);

    doc.save("CustosCarbon_碳足跡紀錄.pdf");
}

// =====================================================
// 🚀 圖表區域
// =====================================================

// ★ 新增：自動切換圖表高度（縮小 / 展開）
function updateChartDisplayMode() {
    const canvas = document.getElementById("emissionChart");
    if (!canvas) return;

    const hasData = records.length > 0;

    if (hasData) {
        canvas.classList.remove("chart-minimized");
        canvas.classList.add("chart-expanded");
    } else {
        canvas.classList.remove("chart-expanded");
        canvas.classList.add("chart-minimized");
    }
}

// === 更新 Chart.js ===
function updateChart() {
    const ctx = document.getElementById("emissionChart");
    if (!ctx) return;

    const labels = records.map((r) => r.name);
    const data = records.map((r) => parseFloat(r.emission) || 0);

    if (chart) chart.destroy();

    chart = new Chart(ctx, {
        type: "doughnut",
        data: {
            labels,
            datasets: [
                {
                    data,
                    backgroundColor: [
                        "#ffadc6", "#ffc2d1", "#ffe5ec", "#ff8fab", "#ffb3c6",
                        "#fad2e1", "#fbb1bd", "#ffcad4", "#ffe4e1", "#f9bec7"
                    ],
                    borderWidth: 1,
                    hoverOffset: 8,
                },
            ],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: "right",
                    labels: {
                        textAlign: "left",
                        padding: 30,
                    }
                }
            }
        },
    });

    // ★ 每次更新圖表時，同步更新高度
    updateChartDisplayMode();
}
