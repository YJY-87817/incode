// 設定新的預設字型家族與顏色，以模仿 Bootstrap 的預設樣式
Chart.defaults.global.defaultFontFamily = 'Nunito', '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
Chart.defaults.global.defaultFontColor = '#858796';

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
var ctx = document.getElementById("myAreaChart");
var myLineChart = new Chart(ctx, {
    type: 'line',
    data: {
        labels: monthNames,
        datasets: [{
            label: "會員數",
            lineTension: 0.3,
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
            xAxes: [{
                gridLines: {
                    display: false,
                    drawBorder: false
                },
                ticks: {
                    maxTicksLimit: 12
                }
            }],
            yAxes: [{
                ticks: {
                    maxTicksLimit: 5,
                    padding: 10,
                    beginAtZero: true,
                    // 顯示整數會員數
                    callback: function (value, index, values) {
                        return Math.floor(value) + ' 人';
                    }
                },
                gridLines: {
                    color: "rgb(234, 236, 244)",
                    zeroLineColor: "rgb(234, 236, 244)",
                    drawBorder: false,
                    borderDash: [2],
                    zeroLineBorderDash: [2]
                }
            }],
        },
        legend: {
            display: false
        },
        tooltips: {
            backgroundColor: "rgb(255,255,255)",
            bodyFontColor: "#858796",
            titleMarginBottom: 10,
            titleFontColor: '#6e707e',
            titleFontSize: 14,
            borderColor: '#dddfeb',
            borderWidth: 1,
            xPadding: 15,
            yPadding: 15,
            displayColors: false,
            intersect: false,
            mode: 'index',
            caretPadding: 10,
            callbacks: {
                label: function (tooltipItem, chart) {
                    var datasetLabel = chart.datasets[tooltipItem.datasetIndex].label || '';
                    return datasetLabel + ': ' + Math.floor(tooltipItem.yLabel) + ' 人';
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