import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { EChartsOption } from 'echarts';
import { ApiService, toApiError } from '../../../core/api.service';
import { Dashboard } from '../../../core/models';
import { AdminChartComponent } from '../../../shared/components/admin-chart.component';
import { IconComponent } from '../../../shared/icon.component';
import { DateLabelPipe, InrPipe } from '../../../shared/pipes';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, AdminChartComponent, IconComponent, InrPipe, DateLabelPipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
})
export class AdminDashboardComponent {
  private readonly api = inject(ApiService);
  protected readonly dashboard = signal<Dashboard | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly revenueOption = computed<EChartsOption>(() => {
    const points = this.dashboard()?.revenueSeries ?? [];
    return {
      color: ['#8e4a5b'],
      tooltip: { trigger: 'axis' },
      grid: { left: 18, right: 18, top: 18, bottom: 8, containLabel: true },
      xAxis: { type: 'category', boundaryGap: false, data: points.map((point) => point.label), axisLabel: { color: '#806e72', hideOverlap: true } },
      yAxis: { type: 'value', axisLabel: { color: '#806e72', formatter: (value: number) => `₹${Math.round(value / 1000)}k` }, splitLine: { lineStyle: { color: '#f0e8e3' } } },
      series: [{ name: 'Revenue', type: 'line', smooth: true, showSymbol: false, data: points.map((point) => point.value), lineStyle: { width: 3 }, areaStyle: { opacity: .12 } }],
    };
  });

  protected readonly orderStatusOption = computed<EChartsOption>(() => ({
    color: ['#8e4a5b', '#c9a26b', '#688f7d', '#8e9aa4', '#d6b2a8', '#6d5265'],
    tooltip: { trigger: 'item' },
    legend: { bottom: 0, left: 'center', textStyle: { color: '#705d61', fontSize: 11 } },
    series: [{ type: 'pie', radius: ['48%', '72%'], center: ['50%', '43%'], itemStyle: { borderColor: '#fffdfb', borderWidth: 3 }, label: { show: false }, data: (this.dashboard()?.orderStatuses ?? []).map((item) => ({ name: item.status, value: item.count })) }],
  }));

  protected readonly topProductsOption = computed<EChartsOption>(() => {
    const items = this.dashboard()?.topProducts ?? [];
    return {
      color: ['#c9a26b'],
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: 8, right: 18, top: 12, bottom: 8, containLabel: true },
      xAxis: { type: 'value', axisLabel: { color: '#806e72' }, splitLine: { lineStyle: { color: '#f0e8e3' } } },
      yAxis: { type: 'category', data: items.map((item) => item.name).reverse(), axisLabel: { color: '#705d61', width: 110, overflow: 'truncate' } },
      series: [{ type: 'bar', data: items.map((item) => item.revenue).reverse(), barMaxWidth: 22, itemStyle: { borderRadius: [0, 6, 6, 0] } }],
    };
  });

  constructor() { void this.load(); }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.dashboard.set(await this.api.get<Dashboard>('admin/dashboard'));
    } catch (error) {
      this.error.set(toApiError(error).message);
    } finally {
      this.loading.set(false);
    }
  }
}
