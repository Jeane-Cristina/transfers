import { money } from '../lib/format';

interface BalanceGaugeProps {
  balance: number;
  overdraftLimit: number;
  size?: 'sm' | 'lg';
  showLegend?: boolean;
  showZero?: boolean;
  label?: string;
  onDark?: boolean;
}

export function BalanceGauge({ balance, overdraftLimit, size = 'lg', showLegend, showZero, label, onDark }: BalanceGaugeProps) {
  const positive = Math.max(balance, 0);
  const used = Math.min(Math.max(-balance, 0), overdraftLimit);
  const free = Math.max(overdraftLimit - used, 0);
  const total = positive + overdraftLimit;
  const pct = (value: number) => (total > 0 ? (value / total) * 100 : 0);
  const zeroAt = pct(overdraftLimit);

  const description =
    total === 0
      ? 'Sem saldo e sem cheque especial.'
      : `Saldo ${money(balance)}. Cheque especial em uso ${money(used)} de ${money(overdraftLimit)}.`;

  return (
    <div className={`gauge gauge--${size}${onDark ? ' gauge--on-dark' : ''}`}>
      {label && <span className="gauge__label">{label}</span>}
      <div className="gauge__track" role="img" aria-label={description}>
        <span className="gauge__seg gauge__seg--free" style={{ flexBasis: `${pct(free)}%` }} />
        <span className="gauge__seg gauge__seg--used" style={{ flexBasis: `${pct(used)}%` }} />
        <span className="gauge__seg gauge__seg--positive" style={{ flexBasis: `${pct(positive)}%` }} />
        {overdraftLimit > 0 && total > 0 && <span className="gauge__zero" style={{ left: `${zeroAt}%` }} />}
      </div>
      {showZero && overdraftLimit > 0 && total > 0 && (
        <div className="gauge__scale" aria-hidden="true">
          {zeroAt > 24 && <span>{money(-overdraftLimit)}</span>}
          <span
            className="gauge__scale-zero"
            style={{ left: `${zeroAt}%`, transform: `translateX(${zeroAt > 85 ? '-100%' : '-50%'})` }}
          >
            R$ 0
          </span>
          {positive > 0 && zeroAt < 76 && <span>{money(positive)}</span>}
        </div>
      )}
      {showLegend && (
        <dl className="gauge__legend">
          <div>
            <dt>
              <span className="swatch swatch--free" aria-hidden="true" />
              Especial livre
            </dt>
            <dd className="num">{money(free)}</dd>
          </div>
          <div>
            <dt>
              <span className="swatch swatch--used" aria-hidden="true" />
              Especial em uso
            </dt>
            <dd className={`num${used > 0 ? ' is-negative' : ''}`}>{money(used)}</dd>
          </div>
          <div>
            <dt>
              <span className="swatch swatch--positive" aria-hidden="true" />
              Saldo positivo
            </dt>
            <dd className="num">{money(positive)}</dd>
          </div>
        </dl>
      )}
    </div>
  );
}
