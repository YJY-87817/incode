// ¶ê»æ¹Ï½d¨Ò
var ctx = document.getElementById("myPieChart");
var myPieChart = new Chart(ctx, {
  type: 'doughnut',
  data: {
    labels: ["Direct", "Referral", "Social"],
    datasets: [{
      data: [55, 30, 15],
      backgroundColor: ['#4e73df', '#1cc88a', '#36b9cc'],
      hoverBackgroundColor: ['#2e59d9', '#17a673', '#2c9faf'],
      hoverBorderColor: "rgba(234, 236, 244, 1)",
    }],
  },
  options: {
    maintainAspectRatio: false,
    plugins: {
      tooltip: {
        backgroundColor: "rgb(255,255,255)",
        bodyColor: "#858796",
        titleColor: "#858796",
        borderColor: '#dddfeb',
        borderWidth: 1,
        padding: 15,
        displayColors: false,
        caretPadding: 10,
        bodyFont: {
          family: "'Nunito', -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
        },
        titleFont: {
          family: "'Nunito', -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
        }
      },
      legend: {
        display: false
      }
    },
    cutout: '80%'
  }
});
