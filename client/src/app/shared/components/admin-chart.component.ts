import { afterNextRender, Component, effect, ElementRef, input, OnDestroy, viewChild } from '@angular/core';
import * as echarts from 'echarts/core';
import { BarChart, LineChart, PieChart } from 'echarts/charts';
import { AriaComponent, GridComponent, LegendComponent, TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import type { ECharts, EChartsOption } from 'echarts';

echarts.use([BarChart, LineChart, PieChart, AriaComponent, GridComponent, LegendComponent, TooltipComponent, CanvasRenderer]);

@Component({
  selector: 'app-admin-chart',
  template: `<div #chart class="chart" role="img" [attr.aria-label]="ariaLabel()"></div>`,
  styles: `:host{display:block;min-width:0}.chart{width:100%;height:270px;min-height:220px}`,
})
export class AdminChartComponent implements OnDestroy {
  readonly option = input.required<EChartsOption>();
  readonly ariaLabel = input('Analytics chart');

  private readonly chartElement = viewChild<ElementRef<HTMLDivElement>>('chart');
  private chart: ECharts | null = null;
  private resizeObserver: ResizeObserver | null = null;

  constructor() {
    effect(() => {
      const option = this.option();
      if (this.chart) this.chart.setOption(option, true);
    });
    afterNextRender(() => this.initialize());
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.chart?.dispose();
    this.chart = null;
  }

  private initialize(): void {
    const element = this.chartElement()?.nativeElement;
    if (!element) return;
    this.chart = echarts.init(element, undefined, { renderer: 'canvas' });
    this.chart.setOption(this.option(), true);
    this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
    this.resizeObserver.observe(element);
  }
}
