window.plotlyInterop = {
    plotChart: function (divId, data, layout) {
        var config = {
            responsive: true,
            displayModeBar: true,
            displaylogo: false,
            modeBarButtonsToRemove: ['zoomIn', 'zoomOut', 'sendDataToCloud', 'editInChartStudio', 'zoom2d', 'select2d', 'pan2d', 'lasso2d', 'autoScale2d', 'resetScale2d']
        };

        console.log("Plotting chart in div: " + divId);
        Plotly.react(divId, data, layout, config);
    }
};