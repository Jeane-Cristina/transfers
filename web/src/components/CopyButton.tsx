import { useState } from 'react';
import { Icon } from './Icon';

export function CopyButton({ value, label }: { value: string; label: string }) {
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      className="copy-btn"
      onClick={async () => {
        try {
          await navigator.clipboard.writeText(value);
          setCopied(true);
          window.setTimeout(() => setCopied(false), 2000);
        } catch {
          setCopied(false);
        }
      }}
      aria-label={copied ? 'Copiado' : label}
    >
      <Icon name={copied ? 'check' : 'copy'} size={16} />
      <span>{copied ? 'Copiado' : 'Copiar'}</span>
    </button>
  );
}
