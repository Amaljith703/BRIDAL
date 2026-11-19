import { Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import type { EChartsOption } from 'echarts';
import { ApiService, toApiError } from '../../../core/api.service';
import { AnalyticsDashboard, ChartPoint } from '../../../core/models';
import { AdminChartComponent } from '../../../shared/components/admin-chart.component';
import { IconComponent } from '../../../shared/icon.component';

@Component({
  selector: 'app-admin-analytics',
  imports: [RouterLink, AdminChartComponent, IconComponent, DecimalPipe],
  templateUrl: './admin-analytics.component.html',
  styleUrl: './admin-analytics.component.scss',
})
export class AdminAnalyticsComponent {
  private readonly api = inject(ApiService);
  protected readonly days = signal(30);
  protected readonly analytics = signal<AnalyticsDashboard | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly ranges = [7, 30, 90, 365];

  protected readonly pageViewsOption = computed<EChartsOption>(() => {
    const points = this.analytics()?.pageViewSeries ?? [];
    return {
      color: ['#8e4a5b'], tooltip: { trigger: 'axis' }, grid: { left: 18, right: 14, top: 18, bottom: 8, containLabel: true },
      xAxis: { type: 'category', boundaryGap: false, data: points.map((point) => point.label), axisLabel: { color: '#806e72', hideOverlap: true } },
      yAxis: { type: 'value', minInterval: 1, axisLabel: { color: '#806e72' }, splitLine: { lineStyle: { color: '#f0e8e3' } } },
      series: [{ name: 'Views', type: 'line', smooth: true, showSymbol: false, data: points.map((point) => point.value), lineStyle: { width: 3 }, areaStyle: { opacity: .12 } }],
    };
  });

  protected readonly topPagesOption = computed<EChartsOption>(() => this.barOption(this.analytics()?.topPages ?? [], '#c9a26b'));
  protected readonly sourcesOption = computed<EChartsOption>(() => ({
    color: ['#8e4a5b', '#c9a26b', '#688f7d', '#8e9aa4', '#d6b2a8', '#6d5265'], tooltip: { trigger: 'item' },
    legend: { bottom: 0, left: 'center', textStyle: { color: '#705d61', fontSize: 11 } },
    series: [{ type: 'pie', radius: ['46%', '72%'], center: ['50%', '43%'], label: { show: false }, itemStyle: { borderColor: '#fffdfb', borderWidth: 3 }, data: (this.analytics()?.trafficSources ?? []).map((point) => ({ name: point.label, value: point.value })) }],
  }));
  protected readonly devicesOption = computed<EChartsOption>(() => this.barOption(this.analytics()?.deviceSplit ?? [], '#8e4a5b'));
  protected readonly eventsOption = computed<EChartsOption>(() => this.barOption(this.analytics()?.eventBreakdown ?? [], '#688f7d'));

  constructor() { void this.load(); }

  protected async selectRange(days: number): Promise<void> {
    this.days.set(days);
    await this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.analytics.set(await this.api.get<AnalyticsDashboard>('admin/analytics', { days: this.days() }));
    } catch (error) {
      this.error.set(toApiError(error).message);
    } finally {
      this.loading.set(false);
    }
  }

  private barOption(points: ChartPoint[], color: string): EChartsOption {
    const ordered = [...points].reverse();
    return {
      color: [color], tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: 12, right: 18, top: 12, bottom: 8, containLabel: true },
      xAxis: { type: 'value', minInterval: 1, axisLabel: { color: '#806e72' }, splitLine: { lineStyle: { color: '#f0e8e3' } } },
      yAxis: { type: 'category', data: ordered.map((point) => point.label), axisLabel: { color: '#705d61', width: 140, overflow: 'truncate' } },
      series: [{ type: 'bar', data: ordered.map((point) => point.value), barMaxWidth: 22, itemStyle: { borderRadius: [0, 6, 6, 0] } }],
    };
  }
}
