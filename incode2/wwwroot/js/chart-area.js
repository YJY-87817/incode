// 從後端取得的會員數據
console.log('原始會員數據:', userDisplayData);

// 按年份分組數據
function groupDataByYear(userData) {
    const yearGroups = {};

    if (userData && userData.length > 0) {
        userData.forEach(item => {
            const year = item.createyear;
            if (!yearGroups[year]) {
                yearGroups[year] = new Array(12).fill(0);
            }
            const monthIndex = parseInt(item.createmonth) - 1;
            if (monthIndex >= 0 && monthIndex < 12) {
                yearGroups[year][monthIndex] += item.usercount;
            }
        });
    }

    return yearGroups;
}

// 計算所有年份的累計數據
function calculateAllYearsData(yearGroups) {
    const allYearsData = new Array(12).fill(0);
    Object.keys(yearGroups).forEach(year => {
        for (let i = 0; i < 12; i++) {
            allYearsData[i] += yearGroups[year][i];
        }
    });
    return allYearsData;
}

// 準備圖表數據
const monthNames = ["1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月"];
const yearGroups = groupDataByYear(userDisplayData);
const years = Object.keys(yearGroups).sort();

console.log('按年份分組的數據:', yearGroups);
console.log('可用年份:', years);

// 填充年份選擇器
const yearSelector = document.getElementById('yearSelector');
years.forEach(year => {
    const option = document.createElement('option');
    option.value = year;
    option.textContent = year + ' 年';
    yearSelector.appendChild(option);
});

// 初始化圖表
var ctx = document.getElementById("UserChart");
var myLineChart = new Chart(ctx, {
    type: 'line',
    data: {
        labels: monthNames,
        datasets: [{
            label: "會員數",
            tension: 0.3,
            backgroundColor: "rgba(78, 115, 223, 0.05)",
            borderColor: "rgba(78, 115, 223, 1)",
            pointRadius: 3,
            pointBackgroundColor: "rgba(78, 115, 223, 1)",
            pointBorderColor: "rgba(78, 115, 223, 1)",
            pointHoverRadius: 3,
            pointHoverBackgroundColor: "rgba(78, 115, 223, 1)",
            pointHoverBorderColor: "rgba(78, 115, 223, 1)",
            pointHitRadius: 10,
            pointBorderWidth: 2,
            data: calculateAllYearsData(yearGroups),
        }],
    },
    options: {
        maintainAspectRatio: false,
        layout: {
            padding: {
                left: 10,
                right: 25,
                top: 25,
                bottom: 0
            }
        },
        scales: {
            x: {
                grid: {
                    display: false,
                    drawBorder: false
                },
                ticks: {
                    maxTicksLimit: 12,
                    font: {
                        family: "'Nunito', -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
                    },
                    color: '#858796'
                }
            },
            y: {
                ticks: {
                    maxTicksLimit: 5,
                    padding: 10,
                    beginAtZero: true,
                    font: {
                        family: "'Nunito', -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
                    },
                    color: '#858796',
                    callback: function (value, index, values) {
                        return Math.floor(value) + ' 人';
                    }
                },
                grid: {
                    color: "rgb(234, 236, 244)",
                    drawBorder: false,
                    borderDash: [2]
                }
            }
        },
        plugins: {
            legend: {
                display: false
            },
            tooltip: {
                backgroundColor: "rgb(255,255,255)",
                bodyColor: "#858796",
                titleMarginBottom: 10,
                titleColor: '#6e707e',
                titleFont: {
                    size: 14,
                    family: "'Nunito', -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
                },
                bodyFont: {
                    family: "'Nunito', -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
                },
                borderColor: '#dddfeb',
                borderWidth: 1,
                padding: 15,
                displayColors: false,
                intersect: false,
                mode: 'index',
                caretPadding: 10,
                callbacks: {
                    label: function (context) {
                        let label = context.dataset.label || '';
                        if (label) {
                            label += ': ';
                        }
                        label += Math.floor(context.parsed.y) + ' 人';
                        return label;
                    }
                }
            }
        }
    }
});

// 年份選擇器事件處理
yearSelector.addEventListener('change', function () {
    const selectedYear = this.value;
    let newData;

    if (selectedYear === 'all') {
        newData = calculateAllYearsData(yearGroups);
        myLineChart.data.datasets[0].label = '會員數';
    } else {
        newData = yearGroups[selectedYear] || new Array(12).fill(0);
        myLineChart.data.datasets[0].label = selectedYear + ' 年會員數';
    }

    myLineChart.data.datasets[0].data = newData;
    myLineChart.update();

    console.log('切換到年份:', selectedYear, '數據:', newData);
});