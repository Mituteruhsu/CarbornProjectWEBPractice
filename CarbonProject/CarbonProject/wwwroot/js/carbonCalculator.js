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
}

// === 載入排放係數（外部優先、本地備援） ===
async function loadEmissionData() {
    const API_URL = "https://data.moenv.gov.tw/api/v2/cfp_p_02/json";
    const LOCAL_URL = "/data/moenv_factors_full.json";

    let apiData = [];
    let localData = [];

    try {
        console.log("🌏 嘗試從外部 API 載入資料...");
        const res = await fetch(API_URL, { cache: "no-store", mode: "cors" });
        if (!res.ok) throw new Error(`API 回應錯誤: ${res.status}`);
        apiData = await res.json();
        console.log(`✅ 外部 API 載入成功，共 ${apiData.length} 筆資料`);
    } catch (err) {
        console.warn("⚠️ 外部 API 失敗，使用本地資料。", err);
    }

    try {
        const resLocal = await fetch(LOCAL_URL, { cache: "no-store" });
        if (!resLocal.ok) throw new Error(`本地 JSON 載入失敗: ${resLocal.status}`);
        localData = await resLocal.json();
        console.log(`📁 已載入本地 moenv_factors_full.json，共 ${localData.length} 筆`);
    } catch (err) {
        console.error("❌ 無法載入本地 moenv_factors_full.json，請確認路徑是否正確。", err);
    }

    const combined = [...apiData, ...localData];
    emissionFactors = formatEmissionData(combined);
    console.log(`✨ 載入完成，共 ${Object.keys(emissionFactors).length} 筆 emission factors`);
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

// === 一鍵加入類別推薦項目（模糊比對） ===
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
        } else {
            console.warn(`⚠️ 未找到關鍵字：「${keyword}」`);
        }
    });

    if (added === 0) {
        console.warn("⚠️ 本地資料集中沒有符合該類別的項目，請確認 moenv_factors_full.json。");
    }
    renderTable();
}

// === 使用者輸入使用量時自動更新 ===
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

// === 加入單筆紀錄（手動） ===
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

// === 渲染紀錄表 ===
function renderTable(showAlert = true) {
    const tbody = document.querySelector("#recordTable tbody");
    tbody.innerHTML = records
        .map(
            (r, i) => `
        <tr>
            <td>${r.name}</td>
            <td>
                <input type="number" class="usage-input" value="${r.usage}" min="0" 
                    oninput="updateUsage(${i}, this.value)" 
                    placeholder="${r.unit ? '單位：' + r.unit : '輸入使用量'}" />
            </td>
            <td>${r.factor}</td>
            <td>${r.emission}</td>
            <td><button class="btn-outline" onclick="removeRecord(${i})">刪除</button></td>
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

// === 移除單筆紀錄 ===
function removeRecord(index) {
    records.splice(index, 1);
    renderTable(false);
    updateChart();
}

// === 清空所有 ===
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
async function saveToDB(record = null) {
    let data;

    // ① 如果是一鍵加入（record有值），直接用它
    if (record) {
        data = {
            name: record.name || "未命名項目",
            usage: parseFloat(record.usage) || 0,
            unit: record.unit || "",
            factor: parseFloat(record.factor) || 0,
            emission: parseFloat(record.emission) || 0
        };
    } else {
        // ② 如果是手動輸入，才從畫面抓
        const name = document.getElementById("search").value.trim();
        const usage = parseFloat(document.getElementById("usage").value) || 0;
        const unit = document
            .getElementById("unitHint")
            ?.textContent?.replace("單位：", "")
            .replace(/\s+/g, "") || "";
        const factor = emissionFactors[name]?.factor || 0;
        const emission = usage * factor;

        data = { name, usage, unit, factor, emission };
    }

    console.log("🚀 傳送資料：", data);

    const res = await fetch("/api/Carbon/SaveRecord", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data)
    });

    if (res.ok) {
        console.log("✅ 資料已成功寫入資料庫！"); // 改成 console log，不彈窗
    } else {
        const msg = await res.text();
        alert("❌ 無法寫入資料庫：" + res.status + " → " + msg);
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

// === 圖表 ===
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
            plugins: { legend: { position: "right" } }
        },
    });
}

