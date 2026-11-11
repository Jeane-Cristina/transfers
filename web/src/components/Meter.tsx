interface MeterProps {
  label: string;
  used: number;
  max: number;
  format: (value: number) => string;
  remainingLabel?: (remaining: number) => string;
}

export function Meter({ label, used, max, format, remainingLabel }: MeterProps) {
  const ratio = max > 0 ? Math.min(used / max, 1) : 0;
  const tone = ratio >= 1 ? 'full' : ratio >= 0.8 ? 'high' : 'ok';
  const remaining = Math.max(max - used, 0);
  return (
    <div className={`meter meter--${tone}`}>
      <div className="meter__head">
        <span className="meter__label">{label}</span>
        <span className="meter__value num">
          {format(used)} <span className="meter__of">de {format(max)}</span>
        </span>
      </div>
      <div
        className="meter__track"
        role="meter"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={max}
        aria-valuenow={used}
        aria-valuetext={`${format(used)} de ${format(max)}`}
      >
        <span className="meter__fill" style={{ width: `${ratio * 100}%` }} />
      </div>
      {remainingLabel && (
        <span className={`meter__caption${remaining === 0 ? ' is-exhausted' : ''}`}>
          {remaining === 0 ? 'Limite atingido nesta janela' : remainingLabel(remaining)}
        </span>
      )}
    </div>
  );
}
