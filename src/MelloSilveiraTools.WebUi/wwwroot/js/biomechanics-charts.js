window.biomechanicsCharts = {
    instances: {},

    getOrCreateChart: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el || typeof echarts === 'undefined') {
            return null;
        }
        this.disposeChart(elementId);
        const chart = echarts.init(el, null, { renderer: 'canvas' });
        const resizeHandler = () => chart.resize();
        window.addEventListener('resize', resizeHandler);
        this.instances[elementId] = { chart, resizeHandler };
        return chart;
    },

    renderExperimentalAndSimulationChart: function (elementId, title, expPoints, simPoints) {
        const chart = this.getOrCreateChart(elementId);
        if (!chart) return;

        const segmentNames = {
            '0': 'Unknown',
            '1': 'Ramp',
            '2': 'Relaxation',
            '3': 'Descent',
            '4': 'Recovery'
        };

        const segmentColors = {
            'Ramp': '#38bdf8',
            'Relaxation': '#22c55e',
            'Descent': '#f59e0b',
            'Recovery': '#a855f7'
        };

        const grouped = {};
        for (const pt of expPoints) {
            const rawSeg = String(pt.segmentType || 'Experimental').trim();
            const seg = segmentNames[rawSeg] || rawSeg;
            if (!grouped[seg]) grouped[seg] = [];
            grouped[seg].push([pt.time, pt.stress]);
        }

        const series = Object.keys(grouped).map(seg => ({
            name: `Experimental (${seg})`,
            type: 'scatter',
            symbolSize: 4,
            large: true,
            itemStyle: { color: segmentColors[seg] || '#94a3b8' },
            data: grouped[seg]
        }));

        if (simPoints && simPoints.length > 0) {
            series.push({
                name: 'Model Fit Simulation',
                type: 'line',
                showSymbol: false,
                lineStyle: { width: 2.5, color: '#ef4444' },
                data: simPoints.map(pt => [pt.time, pt.stress])
            });
        }

        chart.setOption({
            backgroundColor: 'transparent',
            title: { text: title, textStyle: { color: '#f1f5f9', fontSize: 14 } },
            tooltip: { trigger: 'axis' },
            legend: { top: 28, textStyle: { color: '#cbd5e1' } },
            grid: { left: 60, right: 25, top: 75, bottom: 65 },
            xAxis: {
                type: 'value',
                name: 'Time (s)',
                nameTextStyle: { color: '#94a3b8' },
                axisLabel: { color: '#94a3b8' },
                splitLine: { lineStyle: { color: '#334155' } }
            },
            yAxis: {
                type: 'value',
                name: 'Stress (MPa)',
                nameTextStyle: { color: '#94a3b8' },
                axisLabel: { color: '#94a3b8' },
                splitLine: { lineStyle: { color: '#334155' } }
            },
            dataZoom: [
                { type: 'inside', xAxisIndex: 0 },
                { type: 'slider', xAxisIndex: 0, bottom: 10 }
            ],
            series: series
        });
    },

    renderSensitivityChart: function (elementId, title, points) {
        const chart = this.getOrCreateChart(elementId);
        if (!chart) return;

        chart.setOption({
            backgroundColor: 'transparent',
            title: { text: title, textStyle: { color: '#f1f5f9', fontSize: 14 } },
            tooltip: { trigger: 'axis' },
            legend: { top: 28, textStyle: { color: '#cbd5e1' } },
            grid: { left: 65, right: 65, top: 75, bottom: 55 },
            xAxis: {
                type: 'value',
                name: 'Variable Value',
                nameTextStyle: { color: '#94a3b8' },
                axisLabel: { color: '#94a3b8' },
                splitLine: { lineStyle: { color: '#334155' } }
            },
            yAxis: [
                {
                    type: 'value',
                    name: 'Stress (MPa)',
                    nameTextStyle: { color: '#94a3b8' },
                    axisLabel: { color: '#94a3b8' },
                    splitLine: { lineStyle: { color: '#334155' } }
                },
                {
                    type: 'value',
                    name: 'Delta (%)',
                    nameTextStyle: { color: '#94a3b8' },
                    axisLabel: { color: '#94a3b8' },
                    splitLine: { show: false }
                }
            ],
            series: [
                {
                    name: 'Initial Stress',
                    type: 'line',
                    yAxisIndex: 0,
                    itemStyle: { color: '#38bdf8' },
                    data: points.map(p => [p.variableValue, p.initialStress])
                },
                {
                    name: 'Final Stress',
                    type: 'line',
                    yAxisIndex: 0,
                    itemStyle: { color: '#22c55e' },
                    data: points.map(p => [p.variableValue, p.finalStress])
                },
                {
                    name: '% Delta Stress',
                    type: 'line',
                    yAxisIndex: 1,
                    itemStyle: { color: '#f59e0b' },
                    data: points.map(p => [p.variableValue, p.percentageDeltaStress])
                }
            ]
        });
    },

    disposeChart: function (elementId) {
        const entry = this.instances[elementId];
        if (entry) {
            window.removeEventListener('resize', entry.resizeHandler);
            entry.chart.dispose();
            delete this.instances[elementId];
        }
    }
};
