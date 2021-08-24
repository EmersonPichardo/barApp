// Set new default font family and font color to mimic Bootstrap's default styling
Chart.defaults.global.defaultFontFamily = '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
Chart.defaults.global.defaultFontColor = '#858796';

// Pie Chart Example
var options = {
    type: 'doughnut',
    data: {
        labels: ["Direct", "Referral"],
        datasets: [{
            data: [55, 45],
            backgroundColor: ['#4e73df', '#1cc88a'],
            hoverBackgroundColor: ['#2e59d9', '#17a673'],
            hoverBorderColor: "rgba(234, 236, 244, 1)",
        }],
    },
    options: {
        legend: {
            position: 'bottom',
            labels: {
                padding: 30,
                boxWidth: 15
            }
        },
        cutoutPercentage: 65,
        aspectRatio: 2
    },
};

var ctx2 = document.getElementById("myPieChart2");
var myPieChart2 = new Chart(ctx2, options);
