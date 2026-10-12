import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { PRIORITY_LABELS, Priority } from '../core/models';

@Component({
  selector: 'app-priority-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      height: 24px;
      padding: 0 9px;
      border-radius: 999px;
      font-size: 12px;
      font-weight: 600;
      white-space: nowrap;
    }

    .dot {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: currentcolor;
    }

    .low {
      background: var(--surface-muted);
      color: var(--text-muted);
    }

    .medium {
      background: var(--warning-soft);
      color: var(--warning);
    }

    .high {
      background: var(--danger-soft);
      color: var(--danger);
    }
  `,
  template: `
    <span [class]="cssClass()">
      <span class="dot"></span>
      {{ label() }}
    </span>
  `,
})
export class PriorityBadgeComponent {
  readonly priority = input.required<Priority>();

  protected readonly label = computed(() => PRIORITY_LABELS[this.priority()]);
  protected readonly cssClass = computed(() => `badge ${this.label().toLowerCase()}`);
}
