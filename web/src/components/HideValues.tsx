import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { money } from '../lib/format';
import { Icon } from './Icon';

const STORAGE_KEY = 'transfers.hideValues';

interface HideValuesContextValue {
  hidden: boolean;
  toggle: () => void;
}

const HideValuesContext = createContext<HideValuesContextValue>({ hidden: false, toggle: () => {} });

function readInitial(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}

export function HideValuesProvider({ children }: { children: ReactNode }) {
  const [hidden, setHidden] = useState(readInitial);

  const toggle = useCallback(() => {
    setHidden((current) => {
      const next = !current;
      try {
        localStorage.setItem(STORAGE_KEY, next ? '1' : '0');
      } catch {
        return next;
      }
      return next;
    });
  }, []);

  const value = useMemo(() => ({ hidden, toggle }), [hidden, toggle]);
  return <HideValuesContext.Provider value={value}>{children}</HideValuesContext.Provider>;
}

export const useHideValues = () => useContext(HideValuesContext);

export function Money({ value, sign, className }: { value: number; sign?: '+' | '−'; className?: string }) {
  const { hidden } = useHideValues();
  if (hidden) {
    return (
      <span className={`money is-hidden${className ? ` ${className}` : ''}`}>
        <span aria-hidden="true">R$ ••••</span>
        <span className="visually-hidden">valor oculto</span>
      </span>
    );
  }
  return (
    <span className={`money num${className ? ` ${className}` : ''}`}>
      {sign ? `${sign} ` : ''}
      {money(value)}
    </span>
  );
}

export function HideValuesButton({ className }: { className?: string }) {
  const { hidden, toggle } = useHideValues();
  return (
    <button
      type="button"
      className={className}
      onClick={toggle}
      aria-pressed={hidden}
      aria-label={hidden ? 'Mostrar valores' : 'Esconder valores'}
      title={hidden ? 'Mostrar valores' : 'Esconder valores'}
    >
      <Icon name={hidden ? 'eyeOff' : 'eye'} size={22} />
    </button>
  );
}
