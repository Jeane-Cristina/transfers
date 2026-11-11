import { decimalAmount } from '../lib/format';

interface AmountInputProps {
  id: string;
  cents: number;
  onChange: (cents: number) => void;
  invalid?: boolean;
  describedBy?: string;
}

export function AmountInput({ id, cents, onChange, invalid, describedBy }: AmountInputProps) {
  return (
    <div className={`amount-input${invalid ? ' is-invalid' : ''}`}>
      <span className="amount-input__prefix" aria-hidden="true">R$</span>
      <input
        id={id}
        inputMode="numeric"
        autoComplete="off"
        placeholder="0,00"
        value={cents > 0 ? decimalAmount(cents / 100) : ''}
        aria-invalid={invalid || undefined}
        aria-describedby={describedBy}
        onChange={(event) => {
          const digits = event.target.value.replace(/\D/g, '').replace(/^0+/, '').slice(0, 12);
          onChange(digits ? Number(digits) : 0);
        }}
      />
    </div>
  );
}
