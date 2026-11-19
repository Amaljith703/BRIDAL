import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { dateLabel, dateTimeLabel, durationLabel, fileSize, inr, timeAgo, videoDuration } from '../core/format';

@Pipe({ name: 'inr' })
export class InrPipe implements PipeTransform {
  transform(value: number | null | undefined, decimals = false): string {
    return inr(value, decimals);
  }
}

@Pipe({ name: 'duration' })
export class DurationPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return durationLabel(value);
  }
}

@Pipe({ name: 'videoDuration' })
export class VideoDurationPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return videoDuration(value);
  }
}

@Pipe({ name: 'dateLabel' })
export class DateLabelPipe implements PipeTransform {
  transform(value: string | Date | null | undefined, withTime = false): string {
    return withTime ? dateTimeLabel(value) : dateLabel(value);
  }
}

@Pipe({ name: 'timeAgo' })
export class TimeAgoPipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    return timeAgo(value);
  }
}

@Pipe({ name: 'fileSize' })
export class FileSizePipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return fileSize(value);
  }
}

@Pipe({ name: 'safeHtml' })
export class SafeHtmlPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);
  transform(value: string | null | undefined): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(value ?? '');
  }
}
