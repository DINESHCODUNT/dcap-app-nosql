import React, { useMemo } from 'react';
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js';
import ChartDataLabels from 'chartjs-plugin-datalabels';
import { Doughnut } from 'react-chartjs-2';
import type { ProjectSidebarStatsAggregate } from '../../service/ProjectService';
import * as Icon from "react-bootstrap-icons";
import AppLoader from '../../Common/AppLoader';

ChartJS.register(ArcElement, Tooltip, Legend);

const CHART_ERROR_MESSAGE = "Failed to load chart data. Please try again.";

const contrastTextFor = (hex: string) => {
  const c = hex.replace('#', '');
  const r = parseInt(c.substring(0, 2), 16);
  const g = parseInt(c.substring(2, 4), 16);
  const b = parseInt(c.substring(4, 6), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.6 ? '#1a1a1a' : '#ffffff';
};

interface DashboardWidgetsProps {
  stats?: ProjectSidebarStatsAggregate | null;
  statusChartStats?: ProjectSidebarStatsAggregate | null;
  regionChartStats?: ProjectSidebarStatsAggregate | null;
  statsFailed?: boolean;
  onStatusClick?: (status: string | null) => void;
  onRegionClick?: (region: string | null) => void;
}

const DashboardWidgets: React.FC<DashboardWidgetsProps> = ({ stats, statusChartStats, regionChartStats, statsFailed = false, onStatusClick, onRegionClick }) => {

  const hasStatusData = !!(statusChartStats || stats);
  const hasRegionData = !!(regionChartStats || stats);

  const TOTAL_PROJECTS = (statusChartStats || stats)?.total ?? 0;
  const TOTAL_CERTIFICATION = (regionChartStats || stats)?.regions ? Object.values((regionChartStats || stats)!.regions).reduce((a, b) => a + b, 0) : 0;

  const STATUS_OPTIONS = useMemo(() => {
    const sourceStats = statusChartStats || stats;
    if (!sourceStats?.statuses) return [];

    const colorMap: Record<string, string> = {
      'Active': '#3176d2',
      'Contracted': '#d59a0f',
      'Closed': '#7e7e7e',
      'Open': '#169a61',
      'Ready for Closure': '#1bc2c2',
      'Cancelled': '#f75069'
    };
    const fallbackPalette = ['#9c27b0', '#e91e63', '#ff9800', '#009688', '#3f51b5', '#cddc39'];
    let fallbackIdx = 0;

    return Object.entries(sourceStats.statuses).map(([label, count]) => {
      let color = colorMap[label];
      if (!color) {
        color = fallbackPalette[fallbackIdx % fallbackPalette.length];
        fallbackIdx++;
      }
      return { label, count, color };
    });
  }, [stats, statusChartStats]);

  const REGION_OPTIONS = useMemo(() => {
    const sourceStats = regionChartStats || stats;
    if (!sourceStats?.regions) return [];
    const palette = ['#3176d2', '#169a61', '#1bc2c2', '#d59a0f', '#9c27b0', '#e91e63', '#ff9800'];

    return Object.entries(sourceStats.regions).map(([label, count], idx) => ({
      label,
      count,
      color: palette[idx % palette.length]
    }));
  }, [stats, regionChartStats]);

  const chartData = useMemo(() => ({
    labels: STATUS_OPTIONS.map((s) => s.label),
    datasets: [
      {
        data: STATUS_OPTIONS.map((s) => s.count),
        backgroundColor: STATUS_OPTIONS.map((s) => s.color),
        hoverBackgroundColor: STATUS_OPTIONS.map((s) => s.color),
        borderColor: '#fff',
        borderWidth: 2,
      },
    ],
  }), [STATUS_OPTIONS]);

  const regionChartData = useMemo(() => ({
    labels: REGION_OPTIONS.map((s) => s.label),
    datasets: [
      {
        data: REGION_OPTIONS.map((s) => s.count),
        backgroundColor: REGION_OPTIONS.map((s) => s.color),
        hoverBackgroundColor: REGION_OPTIONS.map((s) => s.color),
        borderColor: '#fff',
        borderWidth: 2,
      },
    ],
  }), [REGION_OPTIONS]);

  const statusOptions = useMemo(() => ({
    maintainAspectRatio: false,
    cutout: '65%',
    onClick: (_event: any, elements: any, chart: any) => {
      if (elements && elements.length > 0) {
        const index = elements[0].index;
        const label = chart.data.labels[index];
        onStatusClick?.(label.toLowerCase());
      }
    },
    plugins: {
      legend: {
        position: 'bottom' as const,
        labels: {
          usePointStyle: true,
          boxWidth: 8,
          boxHeight: 8,
          padding: 16,
          font: { size: 11 },
          generateLabels: (chart: any) => {
            const { labels, datasets } = chart.data;
            return labels.map((label: string, i: number) => ({
              text: `${label} - ${datasets[0].data[i]}`,
              fillStyle: datasets[0].backgroundColor[i],
              strokeStyle: datasets[0].backgroundColor[i],
              pointStyle: 'circle',
              index: i,
            }));
          },
        },
      },
      tooltip: {
        callbacks: {
          label: (ctx: any) => ` ${ctx.label}: ${ctx.parsed}`,
        },
      },
      datalabels: {
        color: (ctx: any) => contrastTextFor(ctx.dataset.backgroundColor[ctx.dataIndex]),
        font: { weight: 'bold' as const, size: 11 },
        formatter: (value: number) => value,
        display: (ctx: any) => {
          const data: number[] = ctx.dataset.data;
          const total = data.reduce((a, b) => a + b, 0);
          return total > 0 && data[ctx.dataIndex] / total > 0.05;
        },
      },
    },
  }), [onStatusClick]);

  const regionOptions = useMemo(() => ({
    maintainAspectRatio: false,
    cutout: '65%',
    onClick: (_event: any, elements: any, chart: any) => {
      if (elements && elements.length > 0) {
        const index = elements[0].index;
        const label = chart.data.labels[index];
        onRegionClick?.(label.toLowerCase());
      }
    },
    plugins: {
      legend: {
        position: 'bottom' as const,
        labels: {
          usePointStyle: true,
          boxWidth: 8,
          boxHeight: 8,
          padding: 16,
          font: { size: 11 },
          generateLabels: (chart: any) => {
            const { labels, datasets } = chart.data;
            return labels.map((label: string, i: number) => ({
              text: `${label} - ${datasets[0].data[i]}`,
              fillStyle: datasets[0].backgroundColor[i],
              strokeStyle: datasets[0].backgroundColor[i],
              pointStyle: 'circle',
              index: i,
            }));
          },
        },
      },
      tooltip: {
        callbacks: {
          label: (ctx: any) => ` ${ctx.label}: ${ctx.parsed}`,
        },
      },
      datalabels: {
        color: (ctx: any) => contrastTextFor(ctx.dataset.backgroundColor[ctx.dataIndex]),
        font: { weight: 'bold' as const, size: 11 },
        formatter: (value: number) => value,
        display: (ctx: any) => {
          const data: number[] = ctx.dataset.data;
          const total = data.reduce((a, b) => a + b, 0);
          return total > 0 && data[ctx.dataIndex] / total > 0.05;
        },
      },
    },
  }), [onRegionClick]);

  const centerTextPlugin = useMemo(() => ({
    id: 'centerText',
    afterDraw(chart: any) {
      const { ctx, chartArea } = chart;
      if (!chartArea) return;
      const { left, right, top, bottom } = chartArea;
      const centerX = (left + right) / 2;
      const centerY = (top + bottom) / 2;

      ctx.save();
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillStyle = '#0b0b0b';
      ctx.font = "600 22px system-ui, -apple-system, 'Segoe UI', sans-serif";
      ctx.fillText(String(TOTAL_PROJECTS), centerX, centerY - 8);
      ctx.fillStyle = '#666';
      ctx.font = "400 11px system-ui, -apple-system, 'Segoe UI', sans-serif";
      ctx.fillText('Total Projects', centerX, centerY + 12);
      ctx.restore();
    },
  }), [TOTAL_PROJECTS]);

  const regioncenterTextPlugin = useMemo(() => ({
    id: 'regioncenterText',
    afterDraw(chart: any) {
      const { ctx, chartArea } = chart;
      if (!chartArea) return;
      const { left, right, top, bottom } = chartArea;
      const centerX = (left + right) / 2;
      const centerY = (top + bottom) / 2;

      ctx.save();
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillStyle = '#0b0b0b';
      ctx.font = "600 22px system-ui, -apple-system, 'Segoe UI', sans-serif";
      ctx.fillText(String(TOTAL_CERTIFICATION), centerX, centerY - 8);
      ctx.fillStyle = '#666';
      ctx.font = "400 11px system-ui, -apple-system, 'Segoe UI', sans-serif";
      ctx.fillText('Total Certificates', centerX, centerY + 12);
      ctx.restore();
    },
  }), [TOTAL_CERTIFICATION]);

  return (
    <>
      <div className="row g-3 g-md-4">
        <div className="col-12 col-lg-6">
          <div className="card-area h-100 mb-0">
            <div className="section-title">
              <h5 className='_600'>Projects by Status</h5>
              <div className="section-actions">
                <Icon.ArrowClockwise className="cursor-pointer" size={18} onClick={() => { onStatusClick?.(null); onRegionClick?.(null); }} title="Clear filters" />
              </div>
            </div>
            <div className="dashboard-chart-area position-relative">
              {!hasStatusData && !statsFailed ? (
                <AppLoader inline />
              ) : !hasStatusData && statsFailed ? (
                <div className="text-center p-4 text-danger">{CHART_ERROR_MESSAGE}</div>
              ) : (
                <Doughnut id="statusChart" key={`status-${TOTAL_PROJECTS}`} data={chartData} options={statusOptions as any} plugins={[ChartDataLabels as any, centerTextPlugin as any]} />
              )}
            </div>
          </div>
        </div>

        <div className="col-12 col-lg-6">
          <div className="card-area h-100 mb-0">
            <div className="section-title">
              <h5 className='_600'>Certificates by Region</h5>
              <div className="section-actions">
                <Icon.ArrowClockwise className="cursor-pointer" size={18} onClick={() => { onStatusClick?.(null); onRegionClick?.(null); }} title="Clear filters" />
              </div>
            </div>
            <div className="dashboard-chart-area position-relative">
              {!hasRegionData && !statsFailed ? (
                <AppLoader inline />
              ) : !hasRegionData && statsFailed ? (
                <div className="text-center p-4 text-danger">{CHART_ERROR_MESSAGE}</div>
              ) : (
                <Doughnut id="regionChart" key={`region-${TOTAL_CERTIFICATION}`} data={regionChartData} options={regionOptions as any} plugins={[ChartDataLabels as any, regioncenterTextPlugin as any]} />
              )}
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default DashboardWidgets;
