import { Icon } from './Icon';

export type StepState = 'done' | 'current' | 'pending' | 'failed' | 'cancelled';

export interface TimelineStep {
  key: string;
  label: string;
  detail?: string;
  state: StepState;
}

const stateText: Record<StepState, string> = {
  done: 'concluído',
  current: 'em andamento',
  pending: 'pendente',
  failed: 'recusado',
  cancelled: 'cancelado',
};

export function Timeline({ steps }: { steps: TimelineStep[] }) {
  return (
    <ol className="timeline">
      {steps.map((step) => (
        <li key={step.key} className={`timeline__step timeline__step--${step.state}`}>
          <span className="timeline__marker" aria-hidden="true">
            {step.state === 'done' && <Icon name="check" size={14} />}
            {(step.state === 'failed' || step.state === 'cancelled') && <Icon name="x" size={14} />}
          </span>
          <div className="timeline__content">
            <p className="timeline__label">
              {step.label}
              <span className="visually-hidden">, {stateText[step.state]}</span>
            </p>
            {step.detail && <p className="timeline__detail">{step.detail}</p>}
          </div>
        </li>
      ))}
    </ol>
  );
}
